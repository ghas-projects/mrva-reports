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
        new(ScreenText.Home, href: "./"),
        new(ScreenText.Alerts, href: null, disabled: true),
    ];

    public record AlertRow(int AlertRowId, string RuleName, string RuleKind, string RepositoryName, string Severity, string FilePath);

    [Parameter]
    [SupplyParameterFromQuery(Name = "search")]
    public string? InitialSearch { get; set; }

    private string? SearchString { get; set; }

    private MudDataGrid<AlertRow>? _dataGrid;

    private bool _isLoading = true;
    private bool _detailVisible;
    private AlertDetail? _selectedDetail;

    private readonly DialogOptions _dialogOptions = new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseOnEscapeKey = true,
    };

    protected override void OnInitialized()
    {
        base.OnInitialized();
        SearchString = InitialSearch;
    }

    private async Task<GridData<AlertRow>> LoadServerData(GridState<AlertRow> state)
    {
        _isLoading = true;
        StateHasChanged();
        await DataStore.WaitForDatabaseAsync();
        await Task.Yield();

        try
        {
            var (headers, totalItems) = DataStore.GetAlertHeadersPaged(state.Page, state.PageSize, SearchString);

            var rows = headers
                .Select(h => new AlertRow(h.AlertRowId, h.RuleName, h.RuleKind, h.RepositoryName, h.Severity, h.FilePath))
                .ToList();

            return new GridData<AlertRow>
            {
                TotalItems = totalItems,
                Items = rows,
            };
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private void OnSearchChanged(string? value)
    {
        SearchString = value;
        _dataGrid?.ReloadServerData();
    }

    private void OnRowClick(DataGridRowClickEventArgs<AlertRow> args)
    {
        _selectedDetail = DataStore.GetAlertDetailByRowId(args.Item.AlertRowId);
        _detailVisible = true;
        StateHasChanged();
    }

    private static Color GetSeverityColor(string severity) => severity.ToLowerInvariant() switch
    {
        "error" => Color.Error,
        "warning" => Color.Warning,
        "note" or "recommendation" => Color.Info,
        _ => Color.Default,
    };
}
