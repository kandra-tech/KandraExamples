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
using HomePantry.Forms.Dictionaries;
using HomePantry.Forms.Documents;

namespace HomePantry.Forms.Reports;

public sealed class ShoppingNeedsReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public Guid ProductId { get; set; }
    [Required]
    public string ProductCode { get; set; } = "";
    [Required]
    public string ProductName { get; set; } = "";

    [PrintColumn(WidthPercent = 25)]
    [Caption("Product")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string ProductText => CodeNameRef.Combine(ProductCode, ProductName);

    [PrintColumn(WidthPercent = 10)]
    [Caption("OnHandQuantity")]
    [Format(PrintFormats.Quantity)]
    [ReportViewColumn]
    public decimal OnHandQuantity { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("MinimumStock")]
    [Format(PrintFormats.Quantity)]
    [ReportViewColumn]
    public decimal MinimumStock { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("ShortfallQuantity")]
    [Format(PrintFormats.Quantity)]
    [ReportViewColumn]
    public decimal ShortfallQuantity { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("RecipeNeededQuantity")]
    [Format(PrintFormats.Quantity)]
    [ReportViewColumn]
    public decimal RecipeNeededQuantity { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("RecipeAdditionalShortfall")]
    [Format(PrintFormats.Quantity)]
    [ReportViewColumn]
    public decimal RecipeAdditionalShortfall { get; set; }

    [PrintColumn(WidthPercent = 10)]
    [Caption("TotalNeededQuantity")]
    [Format(PrintFormats.Quantity)]
    [ReportViewColumn]
    public decimal TotalNeededQuantity { get; set; }

    [PrintColumn(WidthPercent = 7)]
    [Caption("LastPrice")]
    [Format("N2")]
    [ReportViewColumn]
    public decimal LastPrice { get; set; }

    [PrintColumn(WidthPercent = 8)]
    [Caption("EstimatedCost")]
    [Format("N2")]
    [Totals(Aggregate.Sum)]
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal EstimatedCost { get; set; }
}

[KandraReportForm(Name = "ShoppingNeedsReport", ResultDtoType = typeof(ShoppingNeedsReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof(ShoppingNeedsReportDataDto))]
[ExecuteFormTitle("ShoppingNeedsReport")]
[Description("Computes shortfall to minimum stock plus what an optional recipe needs beyond on-hand quantity, with last price and estimated cost.")]
public sealed class ReportShoppingNeedsDto : IReportFiltersDto
{
    [Caption("Recipe")]
    [Dropdown(typeof(RecipeDto))]
    [Description("Optionally choose a recipe to scale ingredients for (optional).")]
    public Guid? RecipeId { get; set; }

    [Caption("Servings")]
    [Description("Servings to cook if a recipe is selected.")]
    public decimal Servings { get; set; } = 1m;

    [Caption("ShoppingNeeds_MealPlanWeek")]
    [DateEditor]
    [Description("Optionally choose a week date to include its meal plan ingredients in needs.")]
    public DateTime? MealPlanWeek { get; set; }

    [Caption("ShoppingNeeds_MealPlan")]
    [Search(typeof(MealPlanDto))]
    [Description("Optionally choose a meal plan document to include its planned ingredients in needs.")]
    public Guid? MealPlanId { get; set; }
}

[PrintForm]
[PrintTitle("ShoppingNeedsReport")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class ShoppingNeedsReportDataDto : ReportPrintForm
{
    [PrintRole(PrintRole.Parameter)]
    [Caption("Recipe")]
    public string? RecipeName { get; set; }

    [PrintRole(PrintRole.Parameter)]
    [Caption("Servings")]
    [Format("N1")]
    public decimal Servings { get; set; } = 1m;

    [PrintRole(PrintRole.Parameter)]
    [Caption("ShoppingNeeds_MealPlanWeek")]
    [Format("d")]
    public DateTime? MealPlanWeek { get; set; }

    [Required]
    [PrintTable(LineNumbers = true)]
    [ReportViewGrid]
    public required ICollection<ShoppingNeedsReportLineDto> Lines { get; set; }
}

public sealed class ShoppingNeedsReportResult : ReportResult<ReportShoppingNeedsDto, ShoppingNeedsReportDataDto>;

public class ReportShoppingNeedsDtoValidator : AbstractValidator<ReportShoppingNeedsDto>
{
}
