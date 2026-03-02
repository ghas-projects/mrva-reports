using Microsoft.AspNetCore.Components;
using MRVA.Reports.Data.Services;
using MRVA.Reports.WebAssembly.Properties;
using MudBlazor;

namespace MRVA.Reports.WebAssembly.Pages;

public partial class DashboardPage
{

    [Inject]
    public required DataStore DataStore { get; set; }

    private List<BreadcrumbItem> BreadcrumbItems =>
    [
        new(ScreenText.Home, href: null, disabled: true),
    ];

    private int AlertCount { get; set; }
    private int RepositoryCount { get; set; }
    private int RuleCount { get; set; }
    private int RunCount { get; set; }

    private double[] SeverityData { get; set; } = [];
    private string[] SeverityLabels { get; set; } = [];

    private record TopRepository(int Rank, string Name, int Count);
    private List<TopRepository> TopRepositories { get; set; } = [];

    private double[] RuleData { get; set; } = [];
    private string[] RuleLabels { get; set; } = [];

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

    protected override void OnInitialized()
    {
        base.OnInitialized();

        AlertCount = DataStore.AlertSet.Count;
        RepositoryCount = DataStore.RepositorySet.Count;
        RuleCount = DataStore.RuleSet.Count;
        RunCount = DataStore.RunSet.Count;

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

        RuleLabels = ruleGroups.Select(g => g.Key).ToArray();
        RuleData = ruleGroups.Select(g => (double)g.Count()).ToArray();

        TopRepositories = DataStore.AlertSet
            .GroupBy(a => a.RepositoryRowId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select((g, i) => new TopRepository(
                i + 1,
                repoNameMap.TryGetValue(g.Key, out var name) ? name : $"Repo {g.Key}",
                g.Count()))
            .ToList();
    }
}
