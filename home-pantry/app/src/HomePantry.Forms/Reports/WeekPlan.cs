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
using HomePantry.Enums;

namespace HomePantry.Forms.Reports;

public sealed class WeekPlanReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("Date")]
    [Format("d")]
    [ReportViewColumn(Groupable = true)]
    public DateTime Date { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("MealPlan_Day")]
    [ReportViewColumn(Groupable = true)]
    public MealPlanDay Day { get; set; }

    [Required]
    public Guid RecipeId { get; set; }
    [Required]
    public string RecipeCode { get; set; } = "";
    [Required]
    public string RecipeName { get; set; } = "";

    [PrintColumn(WidthPercent = 40)]
    [Caption("Recipe")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string RecipeText => CodeNameRef.Combine(RecipeCode, RecipeName);

    [PrintColumn(WidthPercent = 15)]
    [Caption("Servings")]
    [Format("N1")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal Servings { get; set; }

    [PrintColumn(WidthPercent = 15)]
    [Caption("CanCook")]
    [ReportViewColumn(Groupable = true)]
    public bool CanCook { get; set; }
}

[KandraReportForm(Name = "WeekPlanReport", ResultDtoType = typeof(WeekPlanReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(WeekPlanReportDataDto))]
[ExecuteFormTitle("WeekPlanReport")]
[Description("Displays the weekly meal plan with days as rows, recipes, planned servings, and whether each meal can be cooked from current stock.")]
public sealed class ReportWeekPlanDto : IReportFiltersDto
{
    [Caption("Week")]
    [DateEditor]
    [Description("Choose any day within the week to display its meal plan.")]
    public DateTime Week { get; set; } = DateTime.UtcNow.Date;
}

[PrintForm]
[PrintTitle("WeekPlanReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class WeekPlanReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("Week")]
    [Format("d")]
    public DateTime Week { get; set; }

    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<WeekPlanReportLineDto> Lines { get; set; }
}

public sealed class WeekPlanReportResult : ReportResult<ReportWeekPlanDto, WeekPlanReportDataDto>;

public class ReportWeekPlanDtoValidator : AbstractValidator<ReportWeekPlanDto>
{
}
