using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using MRVA.Reports.Data.Models;
using MRVA.Reports.Data.Services;
using MRVA.Reports.WebAssembly.Properties;
using MudBlazor;

namespace MRVA.Reports.WebAssembly.Pages.Rules;

public partial class ListPage
{
    [Inject]
    public required DataStore DataStore { get; set; }

    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    private List<BreadcrumbItem> BreadcrumbItems =>
    [
        new(ScreenText.Home, href: "/"),
        new(ScreenText.Rules, href: null, disabled: true),
    ];

    public record RuleRow(Rule Rule, int AlertCount)
    {
        public string PropertyTagsDisplay { get; } = string.Join(", ", Rule.PropertyTags);
    }

    [Parameter]
    [SupplyParameterFromQuery(Name = "search")]
    public string? InitialSearch { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "hasAlerts")]
    public string? HasAlertsFilter { get; set; }

    private IList<RuleRow>? RuleRows { get; set; }
    private RuleRow? SelectedRow { get; set; }

    private string? SearchString { get; set; }
    private int PageSize { get; set; } = 10;

    private Func<RuleRow, bool> RuleFilter => row =>
    {
        if (string.IsNullOrWhiteSpace(SearchString))
        {
            return true;
        }

        if (row.Rule.Id.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (row.Rule.RuleDescription.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (row.Rule.SeverityLevel.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (row.Rule.Kind.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (row.Rule.PropertyTags.Any(t => t.Contains(SearchString, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        return false;
    };

    protected override async Task OnInitializedAsync()
    {
        await DataStore.InitializeAsync();

        SearchString = InitialSearch;

        var alertCountsByRule = DataStore.AlertCountByRuleRowId;

        RuleRows = DataStore.RuleSet
            .OrderBy(r => r.Id)
            .Select(r => new RuleRow(
                r,
                alertCountsByRule.TryGetValue(r.RowId, out var count) ? count : 0))
            .Where(r => HasAlertsFilter switch
            {
                "true" => r.AlertCount > 0,
                "false" => r.AlertCount == 0,
                _ => true,
            })
            .ToImmutableList();

        PageSize = RuleRows.Count switch
        {
            > 100 => 100,
            > 50 => 50,
            > 10 => 25,
            _ => 10,
        };
    }

    private string RowStyleFunc(RuleRow row)
    {
        return row.Equals(SelectedRow) ? "background-color: var(--mud-palette-info-lighten)" : string.Empty;
    }

    private void RowClicked(DataGridRowClickEventArgs<RuleRow> args)
    {
        if (args.MouseEventArgs.Detail == 2)
        {
            SelectedRow = args.Item;
            NavigateToRule();
            return;
        }

        if (SelectedRow == args.Item)
        {
            SelectedRow = null;
            return;
        }

        SelectedRow = args.Item;
    }

    private void NavigateToRule()
    {
        if (SelectedRow == null)
        {
            return;
        }
        NavigationManager.NavigateTo($"rule/{SelectedRow.Rule.RowId}");
    }
}
