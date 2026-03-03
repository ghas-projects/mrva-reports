using Microsoft.AspNetCore.Components;
using MRVA.Reports.Data.Services;
using MRVA.Reports.WebAssembly.Properties;
using MudBlazor;

namespace MRVA.Reports.WebAssembly.Pages;

public partial class DashboardPage
{

    [Inject]
    public required DataStore DataStore { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    private List<BreadcrumbItem> BreadcrumbItems =>
    [
        new(ScreenText.Home, href: null, disabled: true),
    ];

    private int AlertCount { get; set; }
    private int RepositoryCount { get; set; }
    private int RuleCount { get; set; }
    private string AnalysisId { get; set; } = string.Empty;

    private double[] SeverityData { get; set; } = [];
    private string[] SeverityLabels { get; set; } = [];

    private record TopRepository(int Rank, string Name, int Count);
    private List<TopRepository> TopRepositories { get; set; } = [];

    private record TopFilePath(int Rank, string Path, string Repository,  int Count);
    private List<TopFilePath> TopFilePaths { get; set; } = [];

    private record RuleAlertCount(int Rank, string Rule, int Count);
    private List<RuleAlertCount> RuleAlertCounts { get; set; } = [];
    private bool RuleAlertCountsCapped { get; set; }

    private double[] RepoCoverageData { get; set; } = [];
    private string[] RepoCoverageLabels { get; set; } = [];

    private double[] RuleCoverageData { get; set; } = [];
    private string[] RuleCoverageLabels { get; set; } = [];

    private static readonly Dictionary<string, string> SeverityColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["error"] = "#F44336",
        ["warning"] = "#FF9800",
        ["note"] = "#42A5F5",
        ["none"] = "#78909C",
        ["recommendation"] = "#AB47BC",
    };

    private static readonly string[] FallbackColors =
    [
        "#66BB6A", // green
        "#FFA726", // orange
        "#26C6DA", // cyan
        "#EC407A", // pink
        "#8D6E63", // brown
        "#5C6BC0", // indigo
        "#D4E157", // lime
    ];

    private string GetSeverityColor(string label)
    {
        if (SeverityColors.TryGetValue(label, out var color))
            return color;

        var unmapped = SeverityLabels
            .Where(l => !SeverityColors.ContainsKey(l))
            .ToList();

        var index = unmapped.IndexOf(label);
        return FallbackColors[index % FallbackColors.Length];
    }

    private ChartOptions SeverityChartOptions => new()
    {
        ChartPalette = SeverityLabels
            .Select(GetSeverityColor)
            .ToArray(),
    };

    private static ChartOptions RepoCoverageChartOptions => new()
    {
        ChartPalette = ["#F44336", "#66BB6A"],
    };

    private static ChartOptions RuleCoverageChartOptions => new()
    {
        ChartPalette = ["#F44336", "#66BB6A"],
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();

        AlertCount = DataStore.AlertSet.Count;
        RepositoryCount = DataStore.RepositorySet.Count;
        RuleCount = DataStore.RuleSet.Count;

        AnalysisId = DataStore.RunSet
            .Select(r => r.AnalysisRowId)
            .FirstOrDefault(id => !string.IsNullOrEmpty(id)) ?? string.Empty;

        var ruleSeverityMap = DataStore.RuleSet.ToDictionary(r => r.RowId, r => r.SeverityLevel);
        var ruleNameMap = DataStore.RuleSet.ToDictionary(r => r.RowId, r => r.Id);

        var severityGroups = DataStore.AlertSet
            .GroupBy(a => ruleSeverityMap.TryGetValue(a.RuleRowId, out var s) && !string.IsNullOrEmpty(s) ? s : "unknown")
            .OrderByDescending(g => g.Count())
            .ToList();

        SeverityLabels = severityGroups.Select(g => g.Key).ToArray();
        SeverityData = severityGroups.Select(g => (double)g.Count()).ToArray();

        var repoNameMap = DataStore.RepositorySet.ToDictionary(r => r.RowId, r => r.RepositoryName);

        var ruleGroups = DataStore.AlertSet
            .GroupBy(a => ruleNameMap.TryGetValue(a.RuleRowId, out var id) ? id : "unknown")
            .OrderByDescending(g => g.Count())
            .ToList();

        RuleAlertCountsCapped = ruleGroups.Count > 10;

        RuleAlertCounts = ruleGroups
            .Take(10)
            .Select((g, i) => new RuleAlertCount(i + 1, g.Key, g.Count()))
            .ToList();

        var reposWithAlerts = DataStore.AlertSet.Select(a => a.RepositoryRowId).Distinct().Count();
        var reposWithoutAlerts = RepositoryCount - reposWithAlerts;
        RepoCoverageLabels = [ScreenText.WithAlerts, ScreenText.WithoutAlerts];
        RepoCoverageData = [reposWithAlerts, reposWithoutAlerts];

        var rulesWithAlerts = DataStore.AlertSet.Select(a => a.RuleRowId).Distinct().Count();
        var rulesWithoutAlerts = RuleCount - rulesWithAlerts;
        RuleCoverageLabels = [ScreenText.WithAlerts, ScreenText.WithoutAlerts];
        RuleCoverageData = [rulesWithAlerts, rulesWithoutAlerts];

        TopRepositories = DataStore.AlertSet
            .GroupBy(a => a.RepositoryRowId)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select((g, i) => new TopRepository(
                i + 1,
                repoNameMap.TryGetValue(g.Key, out var name) ? name : $"Repo {g.Key}",
                g.Count()))
            .ToList();

        TopFilePaths = DataStore.AlertSet
            .Where(a => !string.IsNullOrEmpty(a.FilePath))
            .GroupBy(a => (a.FilePath, a.RepositoryRowId))
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select((g, i) => new TopFilePath(
                i + 1,
                g.Key.FilePath,
                repoNameMap.TryGetValue(g.Key.RepositoryRowId, out var rName) ? rName : $"Repo {g.Key.RepositoryRowId}",
                g.Count()))
            .ToList();
    }

    private int _selectedSeverityIndex = -1;
    private int SelectedSeverityIndex
    {
        get => _selectedSeverityIndex;
        set
        {
            _selectedSeverityIndex = value;
            if (value >= 0 && value < SeverityLabels.Length)
            {
                NavigationManager.NavigateTo($"/alert?search={Uri.EscapeDataString(SeverityLabels[value])}");
            }
        }
    }

    private void RuleAlertCountClicked(TableRowClickEventArgs<RuleAlertCount> args)
    {
        if (args.Item is null) return;
        NavigationManager.NavigateTo($"/alert?search={Uri.EscapeDataString(args.Item.Rule)}");
    }

    private int _selectedRepoCoverageIndex = -1;
    private int SelectedRepoCoverageIndex
    {
        get => _selectedRepoCoverageIndex;
        set
        {
            _selectedRepoCoverageIndex = value;
            if (value >= 0 && value < RepoCoverageLabels.Length)
            {
                var hasAlerts = value == 0 ? "true" : "false";
                NavigationManager.NavigateTo($"/repo?hasAlerts={hasAlerts}");
            }
        }
    }

    private int _selectedRuleCoverageIndex = -1;
    private int SelectedRuleCoverageIndex
    {
        get => _selectedRuleCoverageIndex;
        set
        {
            _selectedRuleCoverageIndex = value;
            if (value >= 0 && value < RuleCoverageLabels.Length)
            {
                var hasAlerts = value == 0 ? "true" : "false";
                NavigationManager.NavigateTo($"/rule?hasAlerts={hasAlerts}");
            }
        }
    }

    private void TopRepositoryClicked(TableRowClickEventArgs<TopRepository> args)
    {
        if (args.Item is null) return;
        NavigationManager.NavigateTo($"/alert?search={Uri.EscapeDataString(args.Item.Name)}");
    }

    private void TopFilePathClicked(TableRowClickEventArgs<TopFilePath> args)
    {
        if (args.Item is null) return;
        NavigationManager.NavigateTo($"/alert?search={Uri.EscapeDataString(args.Item.Path)}");
    }

    private void NavigateToAlerts() => NavigationManager.NavigateTo("/alert");
    private void NavigateToRepositories() => NavigationManager.NavigateTo("/repo");
    private void NavigateToRules() => NavigationManager.NavigateTo("/rule");
}
