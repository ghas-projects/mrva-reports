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

    // ── Pre-computed dashboard summaries (tiny .pb files) ──

    public IReadOnlyList<AlertsBySeverity> AlertsBySeverity { get; private set; } = [];

    public IReadOnlyList<AlertsByTag> AlertsByTag { get; private set; } = [];

    public IReadOnlyList<Top25CommonFilePaths> Top25CommonFilePaths { get; private set; } = [];

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

        AlertsBySeverity = await LoadListAsync("data/alerts-by-severity.pb", AlertsBySeverityList.Parser, list => list.Items);
        AlertsByTag = await LoadListAsync("data/alerts-by-tag.pb", AlertsByTagList.Parser, list => list.Items);
        Top25CommonFilePaths = await LoadListAsync("data/top25-common-file-paths.pb", Top25CommonFilePathsList.Parser, list => list.Items);

        IsLoaded = true;
    }

    /// <summary>
    /// Maximum number of bytes to download from a single alert .pb file.
    /// ~100 MB should cover ~100K alerts at ~1 KB each.
    /// </summary>
    private const int MaxDownloadBytes = 100 * 1024 * 1024;

    /// <summary>
    /// Loads alerts from a specific .pb file (e.g. a per-severity or per-repo split).
    /// Downloads at most <see cref="MaxDownloadBytes"/> and parses up to
    /// <see cref="MaxAlertRows"/> alert objects.
    /// </summary>
    /// <returns>The list of parsed alerts and whether the result was capped.</returns>
    public async Task<(IReadOnlyList<Alert> Alerts, bool WasCapped)> LoadAlertsFromFileAsync(string url)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
            return ([], false);

        // Read up to MaxDownloadBytes from the HTTP stream.
        await using var httpStream = await response.Content.ReadAsStreamAsync();
        using var ms = new MemoryStream(MaxDownloadBytes);
        var buffer = new byte[81920];
        var totalRead = 0;
        int bytesRead;
        while (totalRead < MaxDownloadBytes &&
               (bytesRead = await httpStream.ReadAsync(buffer)) > 0)
        {
            var toWrite = Math.Min(bytesRead, MaxDownloadBytes - totalRead);
            ms.Write(buffer, 0, toWrite);
            totalRead += toWrite;
        }

        // Determine if HTTP stream had more data beyond our cap.
        var downloadCapped = totalRead >= MaxDownloadBytes;
        ms.Position = 0;

        var alertsTag = WireFormat.MakeTag(
            AlertList.AlertsFieldNumber,
            WireFormat.WireType.LengthDelimited);

        var input = new CodedInputStream(ms);
        var alerts = new List<Alert>();
        var rowCapped = false;

        try
        {
            while (!input.IsAtEnd)
            {
                var tag = input.ReadTag();

                if (tag == alertsTag)
                {
                    var alert = new Alert();
                    input.ReadMessage(alert);

                    if (alerts.Count < MaxAlertRows)
                    {
                        alerts.Add(alert);
                    }
                    else
                    {
                        rowCapped = true;
                        break;
                    }
                }
                else
                {
                    input.SkipLastField();
                }
            }
        }
        catch (InvalidProtocolBufferException)
        {
            // Truncated download — last message was incomplete; ignore it.
        }

        return (alerts, downloadCapped || rowCapped);
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

    private async Task<IReadOnlyList<T>> LoadListAsync<TList, T>(
        string url,
        Google.Protobuf.MessageParser<TList> parser,
        Func<TList, IEnumerable<T>> selector)
        where TList : Google.Protobuf.IMessage<TList>
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var list = parser.ParseFrom(bytes);
        return selector(list).ToList();
    }
}
