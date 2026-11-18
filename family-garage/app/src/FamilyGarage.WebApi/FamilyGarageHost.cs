using Kandra.Api.Lib;
using Kandra.Persistence;
using FamilyGarage.Application;
using FamilyGarage.Domain.Identity;
using FamilyGarage.Persistence;
using FamilyGarage.Persistence.Contexts;
using FamilyGarage.Persistence.Databases;

namespace FamilyGarage;

/// <summary>
/// The registrations and pipeline both hosts of this application share: the web host (<c>FamilyGarage.WebApi</c>) and the
/// desktop app (<c>FamilyGarage.Desktop</c>, which compiles this file in as a link). Kandra.Generators.Api emits the CRUD
/// controllers into each host's own assembly, so the controllers assembly is this file's assembly in both.
/// </summary>
internal static class FamilyGarageHost
{
    public static WebApplicationBuilder AddFamilyGarageHost(this WebApplicationBuilder builder)
    {
        builder.Services.AddFamilyGarageDatabase(builder.Configuration);
        builder.Services.AddKandra<FamilyGarageDbContext, DomainUser, DomainRole>(builder.Configuration, typeof(FamilyGarageHost).Assembly);
        // A configuration that sells its own license component declares it here; the engine then applies a license only
        // when it grants that component (no call = an engine license is enough):
        // (needs using Kandra.Application;)
        // builder.Services.AddKandraConfigurationIdentity("MyConfiguration", requiredComponent: "my.component");
        builder.Services.AddFamilyGaragePersistence();
        builder.Services.AddFamilyGarageApplication();
        return builder;
    }

    /// <summary>Migrates and seeds the database, then builds the request pipeline.</summary>
    public static async Task<WebApplication> UseFamilyGarageHostAsync(this WebApplication app)
    {
        await app.Services.MigrateAndSeedAsync<FamilyGarageDbContext>();

        app.UseKandra();
        return app;
    }
}
