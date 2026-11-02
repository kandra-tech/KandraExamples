using Kandra.ClientLib.MudBlazor;
using HomePantry.Client.MudBlazor;
using HomePantry.ClientLib.Common;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.ConfigureClientServices(baseAddress);
builder.Services.ConfigureHomePantryClientServices(baseAddress);

var host = builder.Build();

await host.SetDefaultCulture();

await host.RunAsync();
