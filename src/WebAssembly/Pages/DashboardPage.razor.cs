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
    private string ToolName { get; set; } = string.Empty;
    private string ToolVersion { get; set; } = string.Empty;
    private string ControllerRepo { get; set; } = string.Empty;
    private string AnalysisDate { get; set; } = string.Empty;
    private string AnalysisState { get; set; } = string.Empty;
    private string QueryLanguage { get; set; } = string.Empty;
    private string AnalysisStarted { get; set; } = string.Empty;
    private string AnalysisCompleted { get; set; } = string.Empty;
    private string AnalysisStatus { get; set; } = string.Empty;
    private string FailureReason { get; set; } = string.Empty;
    private int ScannedReposCount { get; set; }
    private int SkippedReposCount { get; set; }
    private int NotFoundReposCount { get; set; }
    private int NoCodeqlDbReposCount { get; set; }
    private int OverLimitReposCount { get; set; }
    private long ActionsWorkflowRunId { get; set; }
    private int TotalReposCount { get; set; }

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

        var stats = DataStore.GetDashboardStats();

        AlertCount = stats.AlertCount;
        RepositoryCount = stats.RepositoryCount;
        RuleCount = stats.RuleCount;

        var analysis = stats.Analysis;
        AnalysisId = analysis?.AnalysisId ?? string.Empty;
        ToolName = analysis?.ToolName ?? string.Empty;
        ToolVersion = analysis?.ToolVersion ?? string.Empty;
        ControllerRepo = analysis?.ControllerRepo ?? string.Empty;
        AnalysisDate = analysis?.Date ?? string.Empty;
        AnalysisState = analysis?.State ?? string.Empty;
        QueryLanguage = analysis?.QueryLanguage ?? string.Empty;
        AnalysisStarted = analysis?.CreatedAt ?? string.Empty;
        AnalysisCompleted = analysis?.CompletedAt ?? string.Empty;
        AnalysisStatus = analysis?.Status ?? string.Empty;
        FailureReason = analysis?.FailureReason ?? string.Empty;
        ScannedReposCount = analysis?.ScannedReposCount ?? 0;
        SkippedReposCount = analysis?.SkippedReposCount ?? 0;
        NotFoundReposCount = analysis?.NotFoundReposCount ?? 0;
        NoCodeqlDbReposCount = analysis?.NoCodeqlDbReposCount ?? 0;
        OverLimitReposCount = analysis?.OverLimitReposCount ?? 0;
        ActionsWorkflowRunId = analysis?.ActionsWorkflowRunId ?? 0L;
        TotalReposCount = analysis?.TotalReposCount ?? 0;

        SeverityLabels = stats.SeverityCounts.Select(g => g.Label).ToArray();
        SeverityData = stats.SeverityCounts.Select(g => (double)g.Count).ToArray();

        RuleAlertCountsCapped = stats.RulesWithAlerts > 10;

        RuleAlertCounts = stats.TopRules
            .Select((r, i) => new RuleAlertCount(i + 1, r.RuleName, r.Count))
            .ToList();

        var reposWithoutAlerts = RepositoryCount - stats.ReposWithAlerts;
        RepoCoverageLabels = [ScreenText.WithAlerts, ScreenText.WithoutAlerts];
        RepoCoverageData = [stats.ReposWithAlerts, reposWithoutAlerts];

        var rulesWithoutAlerts = RuleCount - stats.RulesWithAlerts;
        RuleCoverageLabels = [ScreenText.WithAlerts, ScreenText.WithoutAlerts];
        RuleCoverageData = [stats.RulesWithAlerts, rulesWithoutAlerts];

        TopRepositories = stats.TopRepositories
            .Select((r, i) => new TopRepository(i + 1, r.RepositoryName, r.Count))
            .ToList();

        TopFilePaths = stats.TopFilePaths
            .Select((f, i) => new TopFilePath(i + 1, f.FilePath, f.RepositoryName, f.Count))
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
                NavigationManager.NavigateTo($"alert?search={Uri.EscapeDataString(SeverityLabels[value])}");
            }
        }
    }

    private void RuleAlertCountClicked(TableRowClickEventArgs<RuleAlertCount> args)
    {
        if (args.Item is null) return;
        NavigationManager.NavigateTo($"alert?search={Uri.EscapeDataString(args.Item.Rule)}");
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
                NavigationManager.NavigateTo($"repo?hasAlerts={hasAlerts}");
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
                NavigationManager.NavigateTo($"rule?hasAlerts={hasAlerts}");
            }
        }
    }

    private void TopRepositoryClicked(TableRowClickEventArgs<TopRepository> args)
    {
        if (args.Item is null) return;
        NavigationManager.NavigateTo($"alert?search={Uri.EscapeDataString(args.Item.Name)}");
    }

    private void TopFilePathClicked(TableRowClickEventArgs<TopFilePath> args)
    {
        if (args.Item is null) return;
        NavigationManager.NavigateTo($"alert?search={Uri.EscapeDataString(args.Item.Path)}");
    }

    private void NavigateToAlerts() => NavigationManager.NavigateTo("alert");
    private void NavigateToRepositories() => NavigationManager.NavigateTo("repo");
    private void NavigateToRules() => NavigationManager.NavigateTo("rule");

    private void NavigateToReposByStatus(string? status = null)
    {
        var url = status is null ? "repo" : $"repo?status={Uri.EscapeDataString(status)}";
        NavigationManager.NavigateTo(url);
    }
}
