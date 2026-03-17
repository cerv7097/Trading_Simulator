using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using BlazorTradingApp;
using BlazorTradingApp.Services;
using ApexCharts;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");


builder.Services.AddApexCharts();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<TwelveDataService>(sp =>
    new TwelveDataService(
        builder.Configuration,
        sp.GetRequiredService<HttpClient>()));
builder.Services.AddScoped<TradingSimulator>();
builder.Services.AddScoped<PriceForecaster>();

await builder.Build().RunAsync();

