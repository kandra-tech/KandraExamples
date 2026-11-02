using Kandra.Application.Abstractions.Services;
using HomeUtilities.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dashboards;
using HomeUtilities.Forms.Dictionaries;
using HomeUtilities.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace HomeUtilities.Tests.Dashboards;

public sealed class DashboardDefaultsTests : IDisposable
{
    private readonly HomeUtilitiesTestHost _host = new();

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task HomeDashboard_NewAsync_And_RunAsync_WorkWithoutManualFilters()
    {
        var ct = CancellationToken.None;

        var provider = await _host.InScopeAsync(sp =>
            sp.GetRequiredService<IDictionaryCrudService<ProviderDto, ProviderQueryDto>>()
                .InsertAsync(new ProviderDto
                {
                    Code = "PRV-DASH",
                    Name = "Dashboard Provider",
                    Kind = ProviderKind.Electricity,
                }, ct));

        var meteredService = await _host.InScopeAsync(sp =>
            sp.GetRequiredService<IHierarchicalDictionaryCrudService<ServiceNodeDto, ServiceQueryDto>>()
                .InsertAsync(new ServiceDto
                {
                    Code = "SRV-01",
                    Name = "Custom Utility Service",
                    Billing = BillingType.Metered,
                    ProviderId = provider.Id,
                    Unit = "kWh",
                }, ct));

        var filters = await _host.InScopeAsync(sp =>
            sp.GetRequiredService<IDashboardService<HomeDashboardDto, HomeDashboardResultDto>>()
                .NewAsync(ct));

        // Must pick the metered service by Billing == BillingType.Metered and Code, not by English/Ukrainian name substring
        Assert.Equal(meteredService.Id, filters.ServiceId);

        var result = await _host.InScopeAsync(sp =>
            sp.GetRequiredService<IDashboardService<HomeDashboardDto, HomeDashboardResultDto>>()
                .RunAsync(filters, ct));

        Assert.NotNull(result);
        Assert.Equal(12, result.CostsByServiceGroup.Labels.Count);
        Assert.Equal(12, result.ServiceConsumption.Labels.Count);
    }
}
