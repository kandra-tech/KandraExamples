using Kandra.Application.Abstractions.Navigation;
using Kandra.Persistence.Abstractions;
using HomeUtilities.Application.Navigation;
using HomeUtilities.Application.Seeding;
using HomeUtilities.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace HomeUtilities.Tests.Security;

public sealed class PermissionCatalogTests : IDisposable
{
    private readonly HomeUtilitiesTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public void DefaultInterface_RegistersHomeDashboard_AndAllNavigationNodes()
    {
        var def = HomeUtilitiesInterfaces.Default;
        Assert.Equal("Default", def.Name);
        Assert.NotNull(def.HomeDashboard);
        Assert.NotEmpty(def.Nodes);
    }

    [Fact]
    public void ClaimsSeeder_GrantsAllStandardUserAreaClaims()
    {
        using var scope = _host.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<IClaimsSeeder>();
        Assert.IsType<HomeUtilitiesClaimsSeeder>(seeder);
        Assert.NotEmpty(seeder.MakeUserRoleAreasClaims());
    }
}
