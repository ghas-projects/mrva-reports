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

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await DataStore.WaitForDatabaseAsync();
        await Task.Yield(); // Yield to render the loading indicator

        if (!RowId.HasValue)
        {
            NavigationManager.NavigateTo("rule");
            return;
        }
        
        Rule = DataStore.GetRuleByRowId(RowId.Value);

        if (Rule == null)
        {
            NavigationManager.NavigateTo("rule");
            return;
        }
        
        BreadcrumbItems =     [
            new(ScreenText.Home, href: "./"),
            new(ScreenText.Rules, href: "rule"),
            new(Rule.Id, href: null, disabled: true)
        ];
    }
}
