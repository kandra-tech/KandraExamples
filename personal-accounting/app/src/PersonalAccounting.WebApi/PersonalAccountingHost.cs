using Kandra.Api.Lib;
using Kandra.Persistence;
using PersonalAccounting.Application;
using PersonalAccounting.Domain.Identity;
using PersonalAccounting.Persistence;
using PersonalAccounting.Persistence.Contexts;
using PersonalAccounting.Persistence.Databases;

namespace PersonalAccounting;

/// <summary>
/// The registrations and pipeline both hosts of this application share: the web host (<c>PersonalAccounting.WebApi</c>) and the
/// desktop app (<c>PersonalAccounting.Desktop</c>, which compiles this file in as a link). Kandra.Generators.Api emits the CRUD
/// controllers into each host's own assembly, so the controllers assembly is this file's assembly in both.
/// </summary>
internal static class PersonalAccountingHost
{
    public static WebApplicationBuilder AddPersonalAccountingHost(this WebApplicationBuilder builder)
    {
        builder.Services.AddPersonalAccountingDatabase(builder.Configuration);
        builder.Services.AddKandra<PersonalAccountingDbContext, DomainUser, DomainRole>(builder.Configuration, typeof(PersonalAccountingHost).Assembly);
        // A configuration that sells its own license component declares it here; the engine then applies a license only
        // when it grants that component (no call = an engine license is enough):
        // (needs using Kandra.Application;)
        // builder.Services.AddKandraConfigurationIdentity("MyConfiguration", requiredComponent: "my.component");
        builder.Services.AddPersonalAccountingPersistence();
        builder.Services.AddPersonalAccountingApplication();
        return builder;
    }

    /// <summary>Migrates and seeds the database, then builds the request pipeline.</summary>
    public static async Task<WebApplication> UsePersonalAccountingHostAsync(this WebApplication app)
    {
        await app.Services.MigrateAndSeedAsync<PersonalAccountingDbContext>();

        app.UseKandra();
        return app;
    }
}
