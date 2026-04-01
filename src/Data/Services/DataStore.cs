using Google.Protobuf;
using MRVA.Reports.Data.Models;

namespace MRVA.Reports.Data.Services;

public class DataStore
{
    private readonly HttpClient _httpClient;
    private Task? _initTask;

    /// <summary>
    /// Maximum number of individual <see cref="Alert"/> objects kept in memory
    /// for the detail / list page. Alerts beyond this limit are still counted
    /// in the aggregations but not retained.
    /// </summary>
    private const int MaxAlertRows = 100_000;

    public bool IsLoaded { get; private set; }

    public IReadOnlySet<Rule> RuleSet { get; private set; } = new HashSet<Rule>();

    public IReadOnlySet<Repository> RepositorySet { get; private set; } = new HashSet<Repository>();

    public IReadOnlySet<Analysis> AnalysisSet { get; private set; } = new HashSet<Analysis>();

    // ── Alert aggregations (computed via streaming – no full materialisation) ──

    /// <summary>Total number of alerts in the data set.</summary>
    public int AlertCount { get; private set; }

    /// <summary>Number of alerts grouped by <see cref="Alert.RuleRowId"/>.</summary>
    public IReadOnlyDictionary<int, int> AlertCountByRuleRowId { get; private set; } =
        new Dictionary<int, int>();

    /// <summary>Number of alerts grouped by <see cref="Alert.RepositoryRowId"/>.</summary>
    public IReadOnlyDictionary<int, int> AlertCountByRepositoryRowId { get; private set; } =
        new Dictionary<int, int>();

    /// <summary>
    /// Top file-path / repository combinations by alert count, pre-sorted descending.
    /// Each tuple contains (FilePath, RepositoryRowId, Count).
    /// </summary>
    public IReadOnlyList<(string FilePath, int RepositoryRowId, int Count)> TopFilePathAggregates { get; private set; } =
        [];

    /// <summary>
    /// A capped list of individual alerts (up to <see cref="MaxAlertRows"/>)
    /// for use on the alerts detail page.
    /// </summary>
    public IReadOnlyList<Alert> Alerts { get; private set; } = [];

    /// <summary>Whether the alert list was capped at <see cref="MaxAlertRows"/>.</summary>
    public bool AlertsCapped { get; private set; }

    public DataStore(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Loads all data asynchronously via HTTP streaming.
    /// Safe to call multiple times; only the first call triggers loading.
    /// </summary>
    public Task InitializeAsync()
    {
        _initTask ??= LoadAllAsync();
        return _initTask;
    }

    private async Task LoadAllAsync()
    {
        RuleSet = await LoadSetAsync("data/rule.pb", RuleList.Parser, list => list.Rules);
        RepositorySet = await LoadSetAsync("data/repository.pb", RepositoryList.Parser, list => list.Repositories);
        AnalysisSet = await LoadSetAsync("data/analysis.pb", AnalysisList.Parser, list => list.Analyses);
        await LoadAlertAggregatesAsync();
        IsLoaded = true;
    }

    /// <summary>
    /// Stream-parses alert.pb one message at a time so that the full set of
    /// <see cref="Alert"/> objects never needs to reside in memory simultaneously.
    /// Aggregations are built incrementally and only up to <see cref="MaxAlertRows"/>
    /// individual alerts are retained for the detail page.
    /// </summary>
    private async Task LoadAlertAggregatesAsync()
    {
        using var response = await _httpClient.GetAsync("data/alert.pb", HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
            return;

        // Copy the HTTP stream into a MemoryStream so protobuf can perform
        // synchronous reads (WASM forbids sync reads on HTTP streams).
        // Pre-allocate based on Content-Length to avoid repeated buffer doubling.
        await using var httpStream = await response.Content.ReadAsStreamAsync();
        var contentLength = response.Content.Headers.ContentLength;
        using var ms = new MemoryStream(contentLength.HasValue
            ? (int)Math.Min(contentLength.Value, int.MaxValue)
            : 4096);
        await httpStream.CopyToAsync(ms);
        ms.Position = 0;

        // Aggregation accumulators
        var alertCountByRule = new Dictionary<int, int>();
        var alertCountByRepo = new Dictionary<int, int>();
        var filePathCounts = new Dictionary<(string, int), int>();
        var alerts = new List<Alert>();
        var totalCount = 0;

        // The wire-format tag for field 1 (length-delimited) in AlertList.
        var alertsTag = WireFormat.MakeTag(
            AlertList.AlertsFieldNumber,
            WireFormat.WireType.LengthDelimited);

        var input = new CodedInputStream(ms);

        while (!input.IsAtEnd)
        {
            var tag = input.ReadTag();

            if (tag == alertsTag)
            {
                var alert = new Alert();
                input.ReadMessage(alert);
                totalCount++;

                // ── by rule ──
                if (!alertCountByRule.TryGetValue(alert.RuleRowId, out var rc))
                    rc = 0;
                alertCountByRule[alert.RuleRowId] = rc + 1;

                // ── by repository ──
                if (!alertCountByRepo.TryGetValue(alert.RepositoryRowId, out var repc))
                    repc = 0;
                alertCountByRepo[alert.RepositoryRowId] = repc + 1;

                // ── by (filePath, repository) ──
                if (!string.IsNullOrEmpty(alert.FilePath))
                {
                    var key = (alert.FilePath, alert.RepositoryRowId);
                    if (!filePathCounts.TryGetValue(key, out var fpc))
                        fpc = 0;
                    filePathCounts[key] = fpc + 1;
                }

                // Keep individual alerts up to the cap for the list page.
                if (alerts.Count < MaxAlertRows)
                    alerts.Add(alert);
            }
            else
            {
                input.SkipLastField();
            }
        }

        AlertCount = totalCount;
        AlertCountByRuleRowId = alertCountByRule;
        AlertCountByRepositoryRowId = alertCountByRepo;
        Alerts = alerts;
        AlertsCapped = totalCount > MaxAlertRows;

        TopFilePathAggregates = filePathCounts
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .Select(kv => (kv.Key.Item1, kv.Key.Item2, kv.Value))
            .ToList();
    }

    private async Task<IReadOnlySet<T>> LoadSetAsync<TList, T>(
        string url,
        Google.Protobuf.MessageParser<TList> parser,
        Func<TList, IEnumerable<T>> selector)
        where TList : Google.Protobuf.IMessage<TList>
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            return new HashSet<T>();
        }

        // Read into a byte array first because Blazor WebAssembly does not support
        // synchronous reads on HTTP streams, which protobuf's ParseFrom(Stream) requires.
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var list = parser.ParseFrom(bytes);
        return selector(list).ToHashSet();
    }
}
