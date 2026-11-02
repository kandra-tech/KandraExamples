using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.ReportingModels;
using HomePantry.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Reports;

[KandraReportBehavior(typeof(ReportShoppingNeedsDto))]
public class ShoppingNeedsReportBehavior(
    IReportsRepository repository,
    ICurrentTimeZone timeZone,
    IAuthorizationChecker authorizationChecker)
    : IReportBehavior<ReportShoppingNeedsDto, ShoppingNeedsReportResult>
{
    public string ObjectName => "ShoppingNeedsReport";

    public async Task<ShoppingNeedsReportResult> GetDataAsync(ReportShoppingNeedsDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetShoppingNeedsAsync(filters.RecipeId, filters.Servings, DateTime.UtcNow, filters.MealPlanWeek, filters.MealPlanId, timeZone.TimeZone, cancellationToken);
        var data = new ShoppingNeedsReportDataDto
        {
            Servings = filters.Servings,
            MealPlanWeek = filters.MealPlanWeek,
            Lines = ShoppingNeedsReportMapper.ToDtos(lines)
        };
        var result = new ShoppingNeedsReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, filters, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(ShoppingNeedsReportResult result, ReportShoppingNeedsDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);

        if (filters.RecipeId.HasValue && filters.RecipeId.Value != Guid.Empty)
        {
            var recipes = (await lookup.GetDictionaryAsync<Recipe>(new[] { filters.RecipeId.Value }, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
            result.Data.RecipeName = recipes.GetValueOrDefault(filters.RecipeId.Value);
        }
    }

    public IFileData Export(ShoppingNeedsReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"ShoppingNeedsReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class ShoppingNeedsReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<ShoppingNeedsReportLineDto> ToDtos(IEnumerable<ShoppingNeedsLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial ShoppingNeedsReportLineDto ToDto(ShoppingNeedsLine line);
}
