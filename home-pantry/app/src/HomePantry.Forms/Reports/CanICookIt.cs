using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Printable;
using Kandra.Forms.Reports;

namespace HomePantry.Forms.Reports;

public sealed class CanICookItReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid RecipeId { get; set; }
    [Required]
    public string RecipeCode { get; set; } = "";
    [Required]
    public string RecipeName { get; set; } = "";

    [PrintColumn(WidthPercent = 30)]
    [Caption("Recipe")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string RecipeText => CodeNameRef.Combine(RecipeCode, RecipeName);

    [PrintColumn(WidthPercent = 15)]
    [Caption("Servings")]
    [Format("N1")]
    [ReportViewColumn]
    public decimal Servings { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("CanCook")]
    [ReportViewColumn]
    public bool CanCook { get; set; }

    [PrintColumn(WidthPercent = 40)]
    [Caption("MissingIngredients")]
    [ReportViewColumn]
    public string MissingIngredients { get; set; } = "";
}

[KandraReportForm(Name = "CanICookItReport", ResultDtoType = typeof(CanICookItReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(CanICookItReportDataDto))]
[ExecuteFormTitle("CanICookItReport")]
[Description("Evaluates recipes against current stock on hand to indicate whether all ingredients are available for stated servings.")]
public sealed class ReportCanICookItDto : IReportFiltersDto
{
}

[PrintForm]
[PrintTitle("CanICookItReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class CanICookItReportDataDto : ReportPrintForm
{
    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<CanICookItReportLineDto> Lines { get; set; }
}

public sealed class CanICookItReportResult : ReportResult<ReportCanICookItDto, CanICookItReportDataDto>;

public class ReportCanICookItDtoValidator : AbstractValidator<ReportCanICookItDto>
{
}
