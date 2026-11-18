using Kandra.Api.Lib;
using PersonalAccounting;

// Kandra variant: a published app started from another folder still finds its appsettings and data folder.
var builder = KandraWebApplication.CreateBuilder(args);

// The registrations and pipeline live in PersonalAccountingHost.cs, shared with the desktop host.
builder.AddPersonalAccountingHost();

var app = builder.Build();

await app.UsePersonalAccountingHostAsync();

app.Run();
