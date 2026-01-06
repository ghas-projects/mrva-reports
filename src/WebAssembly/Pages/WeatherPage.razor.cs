using Microsoft.AspNetCore.Components;
using MRVA.Reports.Data;
using MRVA.Reports.Data.Services;
using MudBlazor;

namespace MRVA.Reports.WebAssembly.Pages;

public partial class WeatherPage
{
    
    [Inject]
    public required WeatherStore WeatherStore { get; set; }
    
    private List<WeatherForecast>? Forecasts { get; set; }
    
    private string _searchString;
    private bool _sortNameByLength;
    private List<string> _events = new();

    // custom sort by name length
    private Func<WeatherForecast, object> _sortBy => x =>
    {
        if (_sortNameByLength)
            return x.Summary?.Length ?? 0;
        else
            return x.Summary ?? string.Empty;
    };
    
    // quick filter - filter globally across multiple columns with the same input
    private Func<WeatherForecast, bool> _quickFilter => weatherForecast =>
    {
        if (string.IsNullOrWhiteSpace(_searchString))
            return true;

        return weatherForecast
                   .Summary?
                   .Contains(_searchString, StringComparison.OrdinalIgnoreCase)
               ?? false;
    };

    protected override async Task OnInitializedAsync()
    {
        Forecasts = WeatherStore.GetForecastList();
    }
    
    // events
    void RowClicked(DataGridRowClickEventArgs<WeatherForecast> args)
    {
        _events.Insert(0, $"Event = RowClick, Index = {args.RowIndex}, Data = {System.Text.Json.JsonSerializer.Serialize(args.Item)}");
    }
    
    void RowRightClicked(DataGridRowClickEventArgs<WeatherForecast> args)
    {
        _events.Insert(0, $"Event = RowRightClick, Index = {args.RowIndex}, Data = {System.Text.Json.JsonSerializer.Serialize(args.Item)}");
    }

    void SelectedItemsChanged(HashSet<WeatherForecast> items)
    {
        _events.Insert(0, $"Event = SelectedItemsChanged, Data = {System.Text.Json.JsonSerializer.Serialize(items)}");
    }
    
}