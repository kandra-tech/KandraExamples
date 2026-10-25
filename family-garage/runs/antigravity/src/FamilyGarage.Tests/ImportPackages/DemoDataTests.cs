using Kandra.Application.Abstractions.Services;
using Kandra.DataImport;
using Kandra.Forms.Querying;
using FamilyGarage.Forms.Dashboards;
using FamilyGarage.Forms.Dictionaries;
using FamilyGarage.Forms.Reports;
using FamilyGarage.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FamilyGarage.Tests.ImportPackages;

/// <summary>
/// The demo package applies on an empty database with every document posted, and on any day it is applied it shows
/// what the brief asks for: an overdue job, a policy that ends within the look-ahead and a finance payment due.
/// </summary>
public sealed class DemoDataTests : IDisposable
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private readonly FamilyGarageTestHost _host = new(timeZone: FamilyGarageTestHost.Kyiv);

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task DemoPackage_Applies_AndHasTheDueEventsTheBriefAsksFor()
    {
        var report = await _host.InScopeAsync(async sp =>
        {
            var registry = sp.GetRequiredService<IImportPackageRegistry>();
            using var stream = registry.Open("familygarage-demo")!;
            return await sp.GetRequiredService<IDataImportService>().ImportAsync(stream, new ImportOptions(), Ct);
        });
        var failures = report.Diagnostics.Where(d => d.Severity == ImportDiagnosticSeverity.Error).Select(d => $"{d.Path}: {d.Message}")
            .Concat(report.Items.Where(i => i.Outcome == ImportItemOutcome.Failed).Select(i => $"{i.Key}: {i.Message}"));
        Assert.False(report.HasErrors, string.Join("; ", failures));

        var upcoming = await _host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportUpcomingDto, UpcomingReportResult>>()
            .RunAsync(new ReportUpcomingDto { LookAheadDays = 30, LookAheadKm = 1000 }, Ct));
        var lines = upcoming.Data.Lines;

        Assert.Contains(lines, l => l.IsOverdue && l.DueKm.HasValue);
        Assert.Contains(lines, l => l.DueKm is null && l.Description == "Compulsory insurance" && l.RemainingDays is >= 0 and <= 30);
        Assert.Contains(lines, l => l.Description == "LS-2024-017");
        Assert.Contains(lines, l => l.Description == "LN-2025-042");

        var dashboard = await _host.InScopeAsync(sp => sp.GetRequiredService<IDashboardService<GarageDashboardDto, GarageDashboardResultDto>>()
            .RunAsync(new GarageDashboardDto(), Ct));
        Assert.Equal(2, dashboard.VehiclesTable.Rows.Count);
        Assert.InRange(dashboard.AvgFuelConsumption.Value!.Value, 6m, 8.5m);
        Assert.True(dashboard.TotalDebt.Value > 0);

        var rav4 = await _host.InScopeAsync(async sp => (await sp.GetRequiredService<IDictionaryCrudService<VehicleDto, VehicleQueryDto>>()
            .QueryAsync(new VehicleQueryDto(), Ct)).Results.Single(v => v.Code == "VEH-0001").Id);
        var fuel = await _host.InScopeAsync(sp => sp.GetRequiredService<IReportService<ReportFuelConsumptionDto, FuelConsumptionReportResult>>()
            .RunAsync(new ReportFuelConsumptionDto
            {
                VehicleId = rav4,
                Period = new DateRangeDto { From = DateTime.UtcNow.AddDays(-150), To = DateTime.UtcNow }
            }, Ct));
        Assert.Equal(8.5m, fuel.Data.PeriodAverageConsumption);
    }
}
