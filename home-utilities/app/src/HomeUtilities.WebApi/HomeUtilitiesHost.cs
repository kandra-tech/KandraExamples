using Kandra.Api.Lib;
using Kandra.Persistence;
using HomeUtilities.Application;
using HomeUtilities.Domain.Identity;
using HomeUtilities.Persistence;
using HomeUtilities.Persistence.Contexts;
using HomeUtilities.Persistence.Databases;

namespace HomeUtilities;

/// <summary>
/// The registrations and pipeline both hosts of this application share: the web host (<c>HomeUtilities.WebApi</c>) and the
/// desktop app (<c>HomeUtilities.Desktop</c>, which compiles this file in as a link). Kandra.Generators.Api emits the CRUD
/// controllers into each host's own assembly, so the controllers assembly is this file's assembly in both.
/// </summary>
internal static class HomeUtilitiesHost
{
    public static WebApplicationBuilder AddHomeUtilitiesHost(this WebApplicationBuilder builder)
    {
        builder.Services.AddHomeUtilitiesDatabase(builder.Configuration);
        builder.Services.AddKandra<HomeUtilitiesDbContext, DomainUser, DomainRole>(builder.Configuration, typeof(HomeUtilitiesHost).Assembly);
        // A configuration that sells its own license component declares it here; the engine then applies a license only
        // when it grants that component (no call = an engine license is enough):
        // (needs using Kandra.Application;)
        // builder.Services.AddKandraConfigurationIdentity("MyConfiguration", requiredComponent: "my.component");
        builder.Services.AddHomeUtilitiesPersistence();
        builder.Services.AddHomeUtilitiesApplication();
        return builder;
    }

    /// <summary>Migrates and seeds the database, then builds the request pipeline.</summary>
    public static async Task<WebApplication> UseHomeUtilitiesHostAsync(this WebApplication app)
    {
        await app.Services.MigrateAndSeedAsync<HomeUtilitiesDbContext>();

        app.UseKandra();
        return app;
    }
}
