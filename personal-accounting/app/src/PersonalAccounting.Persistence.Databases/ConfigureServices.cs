using Kandra.Persistence.Abstractions.Tenancy;
using PersonalAccounting.Persistence.Contexts;
using PersonalAccounting.Persistence.Databases.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PersonalAccounting.Persistence.Databases;

public static class ConfigureServices
{
    /// <summary>
    /// Registers each provider's context subclass as itself and routes <see cref="PersonalAccountingDbContext"/> to the subclass of the current tenant's
    /// provider, so one host can serve tenants on different providers. The connection string comes from the engine's
    /// <see cref="ITenantDatabaseResolver"/> (a SQLite file already placed under the data root, resolved once per process
    /// in Single mode); the options are built per scope, so a scope sees its own tenant's database.
    /// </summary>
    public static void AddPersonalAccountingDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PersonalAccountingDbContextSqlite>((sp, options) =>
            options.UseSqlite(sp.GetRequiredService<ITenantDatabaseResolver>().Current().ConnectionString,
                x => x.MigrationsAssembly(Provider.Sqlite.Assembly)));

        services.AddScoped<PersonalAccountingDbContext>(sp => sp.GetRequiredService<ITenantDatabaseResolver>().Current().Provider switch
        {
            DatabaseProvider.Sqlite => sp.GetRequiredService<PersonalAccountingDbContextSqlite>(),
            var other => throw new NotSupportedException($"Database provider {other} is not part of this build."),
        });
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<PersonalAccountingDbContext>());
    }
}
