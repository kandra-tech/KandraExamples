using Kandra.Api.Lib;
using Kandra.Persistence;
using HomePantry.Application;
using HomePantry.Domain.Identity;
using HomePantry.Persistence;
using HomePantry.Persistence.Contexts;
using HomePantry.Persistence.Databases;

namespace HomePantry;

/// <summary>
/// The registrations and pipeline both hosts of this application share: the web host (<c>HomePantry.WebApi</c>) and the
/// desktop app (<c>HomePantry.Desktop</c>, which compiles this file in as a link). Kandra.Generators.Api emits the CRUD
/// controllers into each host's own assembly, so the controllers assembly is this file's assembly in both.
/// </summary>
internal static class HomePantryHost
{
    public static WebApplicationBuilder AddHomePantryHost(this WebApplicationBuilder builder)
    {
        builder.Services.AddHomePantryDatabase(builder.Configuration);
        builder.Services.AddKandra<HomePantryDbContext, DomainUser, DomainRole>(builder.Configuration, typeof(HomePantryHost).Assembly);
        // A configuration that sells its own license component declares it here; the engine then applies a license only
        // when it grants that component (no call = an engine license is enough):
        // (needs using Kandra.Application;)
        // builder.Services.AddKandraConfigurationIdentity("MyConfiguration", requiredComponent: "my.component");
        builder.Services.AddHomePantryPersistence();
        builder.Services.AddHomePantryApplication();
        return builder;
    }

    /// <summary>Migrates and seeds the database, then builds the request pipeline.</summary>
    public static async Task<WebApplication> UseHomePantryHostAsync(this WebApplication app)
    {
        await app.Services.MigrateAndSeedAsync<HomePantryDbContext>();

        app.UseKandra();
        return app;
    }
}
