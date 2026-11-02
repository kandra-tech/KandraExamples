using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Printable;
using Kandra.Forms.Querying;
using Kandra.Forms.Reports;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Dictionaries;

namespace HomeUtilities.Forms.Reports;

public sealed class ProviderStatementReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    [Order(1)]
    [PrintColumn]
    [Caption("ProviderStatement_Date")]
    [DateEditor(DateEditorKind.Date)]
    [ReportViewColumn]
    public DateTime Date { get; set; }

    [Order(2)]
    [PrintColumn]
    [Caption("ProviderStatement_Document")]
    [ReportViewColumn]
    public string DocumentCode { get; set; } = string.Empty;

    [Required]
    [Order(3)]
    [PrintColumn]
    [Caption("ProviderStatement_Description")]
    [ReportViewColumn]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Order(4)]
    [PrintColumn]
    [Caption("ProviderStatement_Invoiced")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Invoiced { get; set; }

    [Required]
    [Order(5)]
    [PrintColumn]
    [Caption("ProviderStatement_Paid")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Paid { get; set; }

    [Required]
    [Order(6)]
    [PrintColumn]
    [Caption("ProviderStatement_Balance")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal Balance { get; set; }
}

[KandraReportForm(Name = "ProviderStatementReport", ResultDtoType = typeof(ProviderStatementReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(ProviderStatementReportDataDto))]
[ExecuteFormTitle("ProviderStatementReport")]
[Description("A statement of settlements with a utility provider for a property: opening balance, invoices, payments, and closing balance.")]
public sealed class ReportProviderStatementDto : IReportFiltersDto
{
    [Required]
    [Caption("ProviderStatement_Provider")]
    [Search(typeof(ProviderDto))]
    [Description("The utility provider.")]
    public Guid ProviderId { get; set; }

    [Required]
    [Caption("ProviderStatement_Property")]
    [Search(typeof(PropertyDto))]
    [Description("The household property.")]
    public Guid PropertyId { get; set; }

    [Caption("DateRange")]
    [DateRangeEditor]
    [Description("The statement period.")]
    public DateRangeDto? DateRange { get; set; }
}

[PrintForm]
[PrintTitle("ProviderStatementReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class ProviderStatementReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("ProviderStatement_Provider")]
    public string? ProviderName { get; set; }

    [PrintRole(PrintRole.Parameter)]
    [Caption("ProviderStatement_Property")]
    public string? PropertyName { get; set; }

    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<ProviderStatementReportLineDto> Lines { get; set; }
}

public sealed class ProviderStatementReportResult : ReportResult<ReportProviderStatementDto, ProviderStatementReportDataDto>;

public class ReportProviderStatementDtoValidator : AbstractValidator<ReportProviderStatementDto>
{
    public ReportProviderStatementDtoValidator()
    {
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.PropertyId).NotEmpty();
    }
}
