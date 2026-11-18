using Kandra.Api.Lib;
using HomePantry;

// Kandra variant: a published app started from another folder still finds its appsettings and data folder.
var builder = KandraWebApplication.CreateBuilder(args);

// The registrations and pipeline live in HomePantryHost.cs, shared with the desktop host.
builder.AddHomePantryHost();

var app = builder.Build();

await app.UseHomePantryHostAsync();

app.Run();
