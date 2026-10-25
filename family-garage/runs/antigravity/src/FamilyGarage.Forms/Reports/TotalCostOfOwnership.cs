using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Forms.Reports;
using FamilyGarage.Forms.Dictionaries;

namespace FamilyGarage.Forms.Reports;

public sealed class TotalCostOfOwnershipReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Order(1)]
    [PrintColumn]
    [Caption("Vehicle")]
    [ReportViewColumn(Groupable = true)]
    public string Vehicle { get; set; } = string.Empty;

    [Order(2)]
    [PrintColumn]
    [Caption("PurchasePrice")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal PurchasePrice { get; set; }

    [Order(3)]
    [PrintColumn]
    [Caption("Fuel")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Fuel { get; set; }

    [Order(4)]
    [PrintColumn]
    [Caption("Maintenance")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Maintenance { get; set; }

    [Order(5)]
    [PrintColumn]
    [Caption("Repair")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Repair { get; set; }

    [Order(6)]
    [PrintColumn]
    [Caption("Insurance")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Insurance { get; set; }

    [Order(7)]
    [PrintColumn]
    [Caption("Tax")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Tax { get; set; }

    [Order(8)]
    [PrintColumn]
    [Caption("InterestAndFees")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal InterestAndFees { get; set; }

    [Order(9)]
    [PrintColumn]
    [Caption("Other")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Other { get; set; }

    [Order(10)]
    [PrintColumn]
    [Caption("SalePrice")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal SalePrice { get; set; }

    [Order(11)]
    [PrintColumn]
    [Caption("NetCost")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal NetCost { get; set; }

    [Order(12)]
    [PrintColumn]
    [Caption("KmDriven")]
    [Format("N0")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public int KmDriven { get; set; }

    [Order(13)]
    [PrintColumn]
    [Caption("CostPerKm")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal CostPerKm { get; set; }

    [Order(14)]
    [PrintColumn]
    [Caption("CostPerMonth")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal CostPerMonth { get; set; }

    [Order(15)]
    [PrintColumn]
    [Caption("DebtOwed")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal DebtOwed { get; set; }
}

[KandraReportForm(Name = "TotalCostOfOwnershipReport", ResultDtoType = typeof(TotalCostOfOwnershipReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(TotalCostOfOwnershipReportDataDto))]
[ExecuteFormTitle("TotalCostOfOwnershipReport")]
[Description("Computes total cost of ownership per vehicle over a chosen period: purchase price, costs by category, sale price, net cost, km driven, cost per km and per month, and debt owed.")]
public sealed class ReportTotalCostOfOwnershipDto : IReportFiltersDto
{
    [Caption("Vehicle")]
    [Dropdown(typeof(VehicleDto))]
    [Description("Optional vehicle filter; empty includes all vehicles.")]
    public Guid? VehicleId { get; set; }

    [DateRangeEditor]
    public DateRangeDto? Period { get; set; }
}

[PrintForm]
[PrintTitle("TotalCostOfOwnershipReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape)]
public sealed class TotalCostOfOwnershipReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<TotalCostOfOwnershipReportLineDto> Lines { get; set; }
}

public sealed class TotalCostOfOwnershipReportResult : ReportResult<ReportTotalCostOfOwnershipDto, TotalCostOfOwnershipReportDataDto>;

public class ReportTotalCostOfOwnershipDtoValidator : AbstractValidator<ReportTotalCostOfOwnershipDto>
{
    public ReportTotalCostOfOwnershipDtoValidator()
    {
        RuleFor(x => x.Period)
            .Must(r => r is null || r.From is null || r.To is null || r.From.Value <= r.To.Value)
            .WithName(nameof(ReportTotalCostOfOwnershipDto.Period));
    }
}
