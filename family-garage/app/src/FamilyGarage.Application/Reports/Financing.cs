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

[KandraReportBehavior(typeof(ReportFinancingDto))]
public class FinancingReportBehavior(
    IReportsRepository repository,
    ICurrentTimeZone timeZone)
    : IReportBehavior<ReportFinancingDto, FinancingReportResult>
{
    public string ObjectName => "FinancingReport";

    public async Task<FinancingReportResult> GetDataAsync(ReportFinancingDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetFinancingReportAsync(filters.AsOf, timeZone.TimeZone, cancellationToken);

        var dtos = lines.Select(l => new FinancingReportLineDto
        {
            Id = Guid.NewGuid(),
            ContractNumber = l.ContractNumber,
            Vehicle = l.VehicleName,
            Lender = l.LenderName,
            FinancedAmount = l.FinancedAmount,
            PrincipalPaid = l.PrincipalPaid,
            InterestAndFeesPaid = l.InterestAndFeesPaid,
            DebtOwed = l.DebtOwed,
            PaymentsMade = l.PaymentsMade,
            MonthsLeft = l.MonthsLeft
        }).ToList();

        var data = new FinancingReportDataDto { Lines = dtos };
        var result = new FinancingReportResult { Filters = filters, Data = data };

        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = null;

        return result;
    }

    public IFileData Export(FinancingReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"FinancingReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}
