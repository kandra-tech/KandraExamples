using Kandra.Api.Lib;
using HomeUtilities;

// Kandra variant: a published app started from another folder still finds its appsettings and data folder.
var builder = KandraWebApplication.CreateBuilder(args);

// The registrations and pipeline live in HomeUtilitiesHost.cs, shared with the desktop host.
builder.AddHomeUtilitiesHost();

var app = builder.Build();

await app.UseHomeUtilitiesHostAsync();

app.Run();
