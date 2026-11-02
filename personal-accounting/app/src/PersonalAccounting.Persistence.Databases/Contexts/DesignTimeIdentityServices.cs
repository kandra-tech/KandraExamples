using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PersonalAccounting.Persistence.Databases.Contexts;

/// <summary>
/// <see cref="IdentityDbContext{}"/>'s OnModelCreating (base class of <see cref="PersonalAccountingDbContext"/> via
/// AppIdentityDbContext/PlatformDbContext) reads Stores.MaxLengthForKeys off the DbContext's
/// ApplicationServiceProvider, not its internal EF service provider - populated automatically by
/// AddDbContext(...) at runtime (see Kandra.Api.Lib.KandraServiceCollectionExtensions.AddKandra's
/// AddIdentityCore(o => o.Stores.MaxLengthForKeys = 128)), but never set by a bare
/// IDesignTimeDbContextFactory. Without it, `dotnet ef`/kandra-migrate builds a model with the Identity
/// default key length (450) while the running app builds one with 128, so migrations recorded via the
/// factory silently drift from the real runtime model - invisible until Migrate() throws
/// PendingModelChangesWarning on providers whose column type string encodes length (SqlServer/PostgreSql;
/// Sqlite's TEXT affinity hides the drift). Every design-time factory must wire this in via
/// UseApplicationServiceProvider so the recorded migrations match the app's actual model.
/// </summary>
internal static class DesignTimeIdentityServices
{
    public static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddOptions<IdentityOptions>().Configure(o => o.Stores.MaxLengthForKeys = 128);
        return services.BuildServiceProvider();
    }
}
