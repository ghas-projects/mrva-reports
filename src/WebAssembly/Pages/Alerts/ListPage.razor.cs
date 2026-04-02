using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using MRVA.Reports.Data.Models;
using MRVA.Reports.Data.Services;
using MRVA.Reports.WebAssembly.Properties;
using MudBlazor;

namespace MRVA.Reports.WebAssembly.Pages.Alerts;

public partial class ListPage
{
    [Inject]
    public required DataStore DataStore { get; set; }

    private List<BreadcrumbItem> BreadcrumbItems =>
    [
        new(ScreenText.Home, href: "/"),
        new(ScreenText.Alerts, href: null, disabled: true),
    ];

    public record AlertRow(Alert Alert, string RuleName, string RuleKind, string RepositoryName, string Severity);

    // ── Filter parameters from query string ──

    [Parameter]
    [SupplyParameterFromQuery(Name = "severity")]
    public string? SeverityFilter { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "rule")]
    public string? RuleFilter { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "repo")]
    public string? RepoFilter { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "tag")]
    public string? TagFilter { get; set; }

    /// <summary>Legacy search parameter — maps to a text filter on the loaded results.</summary>
    [Parameter]
    [SupplyParameterFromQuery(Name = "search")]
    public string? InitialSearch { get; set; }

    // ── State ──

    private bool IsInitialised { get; set; }
    private bool IsLoading { get; set; }
    private bool HasFilter => !string.IsNullOrEmpty(SeverityFilter)
                           || !string.IsNullOrEmpty(RuleFilter)
                           || !string.IsNullOrEmpty(RepoFilter)
                           || !string.IsNullOrEmpty(TagFilter);

    private IList<AlertRow>? AlertRows { get; set; }
    private bool WasCapped { get; set; }
    private HashSet<int>? _ruleIdsWithTag;

    private int PageSize { get; set; } = 25;
    private string? SearchString { get; set; }

    // ── Filter dropdown options ──

    private IReadOnlyList<string> SeverityOptions { get; set; } = [];
    private IReadOnlyList<string> RuleOptions { get; set; } = [];
    private IReadOnlyList<string> RepoOptions { get; set; } = [];
    private IReadOnlyList<string> TagOptions { get; set; } = [];

    // ── Bindable filter values (for the dropdowns) ──

    private string? SelectedSeverity
    {
        get => SeverityFilter;
        set => SeverityFilter = value;
    }

    private string? SelectedRule
    {
        get => RuleFilter;
        set => RuleFilter = value;
    }

    private string? SelectedRepo
    {
        get => RepoFilter;
        set => RepoFilter = value;
    }

    private string? SelectedTag
    {
        get => TagFilter;
        set => TagFilter = value;
    }

    // ── Quick filter on loaded rows ──

    private Func<AlertRow, bool> AlertFilter => row =>
    {
        if (string.IsNullOrWhiteSpace(SearchString))
        {
            return true;
        }

        var s = SearchString;
        var c = StringComparison.OrdinalIgnoreCase;

        return row.RuleName.Contains(s, c)
            || row.RuleKind.Contains(s, c)
            || row.RepositoryName.Contains(s, c)
            || row.Severity.Contains(s, c)
            || row.Alert.FilePath.Contains(s, c)
            || row.Alert.Message.Contains(s, c)
            || row.Alert.CodeSnippetSource.Contains(s, c)
            || row.Alert.CodeSnippetSink.Contains(s, c)
            || row.Alert.CodeSnippet.Contains(s, c)
            || row.Alert.CodeSnippetContext.Contains(s, c)
            || row.Alert.ResultFingerprint.Contains(s, c);
    };

    protected override async Task OnInitializedAsync()
    {
        await DataStore.InitializeAsync();

        // Build filter dropdown options from the already-loaded lightweight data.
        SeverityOptions = DataStore.AlertsBySeverity
            .Select(s => s.Severity)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RuleOptions = DataStore.RuleSet
            .Select(r => r.Id)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

        RepoOptions = DataStore.RepositorySet
            .Select(r => r.RepositoryFullName)
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .ToList();

        TagOptions = DataStore.AlertsByTag
            .Select(t => t.Tag)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        SearchString = InitialSearch;
        IsInitialised = true;

        if (HasFilter)
        {
            await LoadAlertsAsync();
        }
    }

    private async Task LoadAlertsAsync()
    {
        if (!HasFilter) return;

        IsLoading = true;
        AlertRows = null;
        _ruleIdsWithTag = null;
        StateHasChanged();

        var url = BuildAlertFileUrl();
        var (alerts, capped) = await DataStore.LoadAlertsFromFileAsync(url);
        WasCapped = capped;

        var ruleNameMap = DataStore.RuleSet.ToDictionary(r => r.RowId, r => r.Id);
        var ruleKindMap = DataStore.RuleSet.ToDictionary(r => r.RowId, r => r.Kind);
        var ruleSeverityMap = DataStore.RuleSet.ToDictionary(r => r.RowId, r => r.SeverityLevel);
        var repoNameMap = DataStore.RepositorySet.ToDictionary(r => r.RowId, r => r.RepositoryFullName);

        AlertRows = alerts
            .OrderBy(a => a.RowId)
            .Select(a => new AlertRow(
                a,
                ruleNameMap.TryGetValue(a.RuleRowId, out var ruleName) ? ruleName : $"Rule {a.RuleRowId}",
                ruleKindMap.TryGetValue(a.RuleRowId, out var ruleKind) ? ruleKind : "unknown",
                repoNameMap.TryGetValue(a.RepositoryRowId, out var repoName) ? repoName : $"Repo {a.RepositoryRowId}",
                ruleSeverityMap.TryGetValue(a.RuleRowId, out var severity) ? severity : "unknown"))
            .Where(ApplyClientSideFilters)
            .ToImmutableList();

        PageSize = AlertRows.Count switch
        {
            > 100 => 100,
            > 50 => 50,
            > 10 => 25,
            _ => 10,
        };

        IsLoading = false;
        StateHasChanged();
    }

    /// <summary>
    /// Picks the .pb file from the most selective dimension.
    /// Priority: repo (smallest files) > rule > severity.
    /// The remaining filters are applied client-side via <see cref="ApplyClientSideFilters"/>.
    /// </summary>
    private string BuildAlertFileUrl()
    {
        if (!string.IsNullOrEmpty(RepoFilter))
            return $"data/alerts-by-repo/{ToFileName(RepoFilter)}.pb";

        if (!string.IsNullOrEmpty(RuleFilter))
            return $"data/alerts-by-rule/{ToFileName(RuleFilter)}.pb";

        if (!string.IsNullOrEmpty(TagFilter))
            return $"data/alerts-by-tag/{ToFileName(TagFilter)}.pb";

        if (!string.IsNullOrEmpty(SeverityFilter))
            return $"data/alerts-by-severity/{ToFileName(SeverityFilter)}.pb";

        throw new InvalidOperationException("No filter selected.");
    }

    /// <summary>
    /// Converts a logical name (e.g. "go/interface-returned" or
    /// "AliyunContainerService/gpushare-device-plugin") to the
    /// filename used in the split data folder (slashes become underscores).
    /// </summary>
    private static string ToFileName(string name) => name.Replace('/', '_');

    /// <summary>
    /// Applies the filters that were NOT used to select the .pb file.
    /// For example, if repo was used for file selection, severity and rule
    /// are checked here client-side.
    /// </summary>
    private bool ApplyClientSideFilters(AlertRow row)
    {
        // If the file was loaded by repo, severity and rule still need checking.
        if (!string.IsNullOrEmpty(SeverityFilter) &&
            !row.Severity.Equals(SeverityFilter, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(RuleFilter) &&
            !row.RuleName.Equals(RuleFilter, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(RepoFilter) &&
            !row.RepositoryName.Equals(RepoFilter, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!string.IsNullOrEmpty(TagFilter))
        {
            // Build a set of rule IDs that have the selected tag.
            _ruleIdsWithTag ??= DataStore.RuleSet
                .Where(r => r.PropertyTags.Any(t => t.Equals(TagFilter, StringComparison.OrdinalIgnoreCase)))
                .Select(r => r.RowId)
                .ToHashSet();

            if (!_ruleIdsWithTag.Contains(row.Alert.RuleRowId))
                return false;
        }

        return true;
    }
}
