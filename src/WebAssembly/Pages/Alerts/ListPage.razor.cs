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

    [Parameter]
    [SupplyParameterFromQuery(Name = "search")]
    public string? InitialSearch { get; set; }

    private IList<AlertRow>? AlertRows { get; set; }

    private int PageSize { get; set; } = 10;

    private string? SearchString { get; set; }

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

        SearchString = InitialSearch;

        var ruleNameMap = DataStore.RuleSet.ToDictionary(r => r.RowId, r => r.Id);
        var ruleKindMap = DataStore.RuleSet.ToDictionary(r => r.RowId, r => r.Kind);
        var ruleSeverityMap = DataStore.RuleSet.ToDictionary(r => r.RowId, r => r.SeverityLevel);
        var repoNameMap = DataStore.RepositorySet.ToDictionary(r => r.RowId, r => r.RepositoryFullName);

        AlertRows = DataStore.Alerts
            .OrderBy(a => a.RowId)
            .Select(a => new AlertRow(
                a,
                ruleNameMap.TryGetValue(a.RuleRowId, out var ruleName) ? ruleName : $"Rule {a.RuleRowId}",
                ruleKindMap.TryGetValue(a.RuleRowId, out var ruleKind) ? ruleKind : "unknown",
                repoNameMap.TryGetValue(a.RepositoryRowId, out var repoName) ? repoName : $"Repo {a.RepositoryRowId}",
                ruleSeverityMap.TryGetValue(a.RuleRowId, out var severity) ? severity : "unknown"))
            .ToImmutableList();

        PageSize = AlertRows.Count switch
        {
            > 100 => 100,
            > 50 => 50,
            > 10 => 25,
            _ => 10,
        };
    }
}
