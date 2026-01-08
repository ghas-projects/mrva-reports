using Microsoft.AspNetCore.Components;
using MRVA.Reports.Data.Models;
using MRVA.Reports.Data.Services;
using MRVA.Reports.WebAssembly.Properties;
using MudBlazor;

namespace MRVA.Reports.WebAssembly.Pages.Rules;

public partial class ItemPage
{
    
    [Parameter]
    public int? RowId { get; set; }
    
    [Inject]
    public required DataStore DataStore { get; set; }
    
    [Inject]
    public required NavigationManager NavigationManager { get; set; }

    private List<BreadcrumbItem> BreadcrumbItems { get; set; } = [];
    
    private Rule? Rule { get; set; }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        if (!RowId.HasValue)
        {
            NavigationManager.NavigateTo("/rule");
            return;
        }
        
        Rule = DataStore.SingleRule(RowId.Value);

        if (Rule == null)
        {
            NavigationManager.NavigateTo("/rule");
            return;
        }
        
        BreadcrumbItems =     [
            new(ScreenText.Home, href: "/"),
            new(ScreenText.Rules, href: "/rule"),
            new(Rule.RuleId, href: null, disabled: true)
        ];
    }
}