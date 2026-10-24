using Kandra.ClientLib.MudBlazor;
using PersonalAccounting.Client.MudBlazor;
using PersonalAccounting.ClientLib.Common;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.ConfigureClientServices(baseAddress);
builder.Services.ConfigurePersonalAccountingClientServices(baseAddress);

var host = builder.Build();

await host.SetDefaultCulture();

await host.RunAsync();
