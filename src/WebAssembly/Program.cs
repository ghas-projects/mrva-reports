using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MRVA.Reports.Data.Extensions;
using MRVA.Reports.Data.Services;
using MRVA.Reports.WebAssembly;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddReportData();
builder.Services.AddMudServices();

var host = builder.Build();

// ── Phase 1: Instant dashboard ──────────────────────────────────────────
// Fetch the lightweight dashboard.json (< 2 KB) so the dashboard page can
// render immediately without waiting for the full SQLite database.
var http = host.Services.GetRequiredService<HttpClient>();
var dataStore = host.Services.GetRequiredService<DataStore>();

var dashboardJson = await http.GetStringAsync("data/dashboard.json");
dataStore.LoadDashboardFromJson(dashboardJson);

// ── Phase 2: Background database load ───────────────────────────────────
// Start downloading and decompressing the full SQLite database in the
// background. Drill-down pages (alerts, repos, rules) will await
// DataStore.WaitForDatabaseAsync() before querying.
_ = Task.Run(async () =>
{
    var js = host.Services.GetRequiredService<IJSRuntime>();
    var dbModule = await js.InvokeAsync<IJSObjectReference>("import", "./js/db-loader.js");
    var dbBytes = await dbModule.InvokeAsync<byte[]>("fetchAndDecompress", "data/mrva-analysis.db.gz");
    await dataStore.InitializeAsync(dbBytes);
});

await host.RunAsync();
