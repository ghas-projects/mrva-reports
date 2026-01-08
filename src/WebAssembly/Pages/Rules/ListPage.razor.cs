using Microsoft.AspNetCore.Components;
using MRVA.Reports.Data.Models;
using MRVA.Reports.Data.Services;
using MRVA.Reports.WebAssembly.Properties;
using MudBlazor;

namespace MRVA.Reports.WebAssembly.Pages.Rules;

public partial class ListPage : ComponentBase
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
    
    private List<Rule>? RuleList { get; set; }
    private Rule? SelectedRule { get; set; }
    
    private string? SearchString { get; set; }

    private Func<Rule, bool> RuleFilter => rule =>
    {
        if (string.IsNullOrWhiteSpace(SearchString))
        {
            return true;
        }

        return rule
            .RuleId
            .Contains(
                SearchString,
                StringComparison.OrdinalIgnoreCase
            );
    };

    protected override async Task OnInitializedAsync()
    {
        RuleList = DataStore.GetRuleList();
    }
    
    string RowStyleFunc(Rule rule)
    {
        return rule.Equals(SelectedRule) ? "background-color: var(--mud-palette-info-lighten)" : string.Empty;
    }
    
    void RowClicked(DataGridRowClickEventArgs<Rule> args)
    {
        if (args.MouseEventArgs.Detail == 2)
        {
            // handle double click
            SelectedRule = args.Item;
            NavigateToRule();
            return;
        }

        if (SelectedRule == args.Item)
        {
            SelectedRule = null;
            return;
        }
        
        SelectedRule = args.Item;
    }

    void NavigateToRule()
    {
        if (SelectedRule == null)
        {
            return;
        }
        NavigationManager.NavigateTo($"rule/{SelectedRule.RowId}");
    }
    
}