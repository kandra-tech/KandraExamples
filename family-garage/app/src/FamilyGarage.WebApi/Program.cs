using Kandra.Api.Lib;
using FamilyGarage;

// Kandra variant: a published app started from another folder still finds its appsettings and data folder.
var builder = KandraWebApplication.CreateBuilder(args);

// The registrations and pipeline live in FamilyGarageHost.cs, shared with the desktop host.
builder.AddFamilyGarageHost();

var app = builder.Build();

await app.UseFamilyGarageHostAsync();

app.Run();
