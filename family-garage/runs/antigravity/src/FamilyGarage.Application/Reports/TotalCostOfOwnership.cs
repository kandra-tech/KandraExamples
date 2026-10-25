using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using FamilyGarage.Domain.Commons;
using FamilyGarage.Forms.Reports;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Application.Reports;

[KandraReportBehavior(typeof(ReportTotalCostOfOwnershipDto))]
public class TotalCostOfOwnershipReportBehavior(
    IReportsRepository repository,
    ICurrentTimeZone timeZone)
    : IReportBehavior<ReportTotalCostOfOwnershipDto, TotalCostOfOwnershipReportResult>
{
    public string ObjectName => "TotalCostOfOwnershipReport";

    public async Task<TotalCostOfOwnershipReportResult> GetDataAsync(ReportTotalCostOfOwnershipDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetTotalCostOfOwnershipAsync(
            filters.Period?.From,
            filters.Period?.To,
            filters.VehicleId,
            timeZone.TimeZone,
            cancellationToken);

        var dtos = lines.Select(l => new TotalCostOfOwnershipReportLineDto
        {
            Id = Guid.NewGuid(),
            Vehicle = l.VehicleName,
            PurchasePrice = l.PurchasePrice,
            Fuel = l.Fuel,
            Maintenance = l.Maintenance,
            Repair = l.Repair,
            Insurance = l.Insurance,
            Tax = l.Tax,
            InterestAndFees = l.InterestAndFees,
            Other = l.Other,
            SalePrice = l.SalePrice,
            NetCost = l.NetCost,
            KmDriven = l.KmDriven,
            CostPerKm = l.CostPerKm,
            CostPerMonth = l.CostPerMonth,
            DebtOwed = l.DebtOwed
        }).ToList();

        var data = new TotalCostOfOwnershipReportDataDto { Lines = dtos };
        var result = new TotalCostOfOwnershipReportResult { Filters = filters, Data = data };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = null;

        return result;
    }

    public IFileData Export(TotalCostOfOwnershipReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"TotalCostOfOwnershipReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}
