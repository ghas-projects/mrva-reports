using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using MRVA.Reports.Data.Models;
using MRVA.Reports.Data.Services;
using MRVA.Reports.WebAssembly.Properties;
using MudBlazor;

namespace MRVA.Reports.WebAssembly.Pages.Repositories;

public partial class ListPage
{
    [Inject]
    public required DataStore DataStore { get; set; }

    private List<BreadcrumbItem> BreadcrumbItems =>
    [
        new(ScreenText.Home, href: "/"),
        new(ScreenText.Repositories, href: null, disabled: true),
    ];

    public record RepositoryRow(Repository Repository, int AlertCount);

    [Parameter]
    [SupplyParameterFromQuery(Name = "search")]
    public string? InitialSearch { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "hasAlerts")]
    public string? HasAlertsFilter { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "status")]
    public string? StatusFilter { get; set; }

    private IList<RepositoryRow>? RepositoryList { get; set; }
    private RepositoryRow? SelectedRepository { get; set; }

    private string? SearchString { get; set; }
    private int PageSize { get; set; } = 10;

    private Func<RepositoryRow, bool> RepositoryFilter => row =>
    {
        if (string.IsNullOrWhiteSpace(SearchString))
        {
            return true;
        }

        if (row.Repository.RepositoryFullName.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (row.Repository.RepositoryUrl.Contains(SearchString, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if( row.Repository.AnalysisStatus.Contains(SearchString, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return false;
    };

    protected override async Task OnInitializedAsync()
    {
        await DataStore.InitializeAsync();

        SearchString = InitialSearch;

        RepositoryList = DataStore.RepositorySet
            .OrderBy(r => r.RepositoryFullName)
            .Select(r => new RepositoryRow(r, r.TotalAlertsCount))
            .Where(r => HasAlertsFilter switch
            {
                "true" => r.AlertCount > 0,
                "false" => r.AlertCount == 0,
                _ => true,
            })
            .Where(r => string.IsNullOrEmpty(StatusFilter)
                || r.Repository.AnalysisStatus.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase))
            .ToImmutableList();

        PageSize = RepositoryList.Count switch
        {
            > 100 => 100,
            > 50 => 50,
            > 10 => 25,
            _ => 10,
        };
    }

    private string RowStyleFunc(RepositoryRow row)
    {
        return row.Equals(SelectedRepository) ? "background-color: var(--mud-palette-info-lighten)" : string.Empty;
    }

    private void RowClicked(DataGridRowClickEventArgs<RepositoryRow> args)
    {
        if (SelectedRepository == args.Item)
        {
            SelectedRepository = null;
            return;
        }

        SelectedRepository = args.Item;
    }
}