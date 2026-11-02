using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Files;
using Kandra.Application.Abstractions.Services;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.ReportingModels;
using HomePantry.Forms.Reports;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Reports;

[KandraReportBehavior(typeof(ReportCalendarDto))]
public class CalendarReportBehavior(
    IReportsRepository repository,
    IAuthorizationChecker authorizationChecker,
    IStringLocalizer localizer)
    : IReportBehavior<ReportCalendarDto, CalendarReportResult>
{
    public string ObjectName => "CalendarReport";

    public async Task<CalendarReportResult> GetDataAsync(ReportCalendarDto filters, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        var lines = await repository.GetCalendarAsync(filters.From, filters.To, cancellationToken);
        var dtos = CalendarReportMapper.ToDtos(lines);
        foreach (var dto in dtos)
        {
            dto.DayOfWeek = dto.Date.DayOfWeek switch
            {
                DayOfWeek.Monday => localizer["Enum_MealPlanDay_Monday"].Value,
                DayOfWeek.Tuesday => localizer["Enum_MealPlanDay_Tuesday"].Value,
                DayOfWeek.Wednesday => localizer["Enum_MealPlanDay_Wednesday"].Value,
                DayOfWeek.Thursday => localizer["Enum_MealPlanDay_Thursday"].Value,
                DayOfWeek.Friday => localizer["Enum_MealPlanDay_Friday"].Value,
                DayOfWeek.Saturday => localizer["Enum_MealPlanDay_Saturday"].Value,
                DayOfWeek.Sunday => localizer["Enum_MealPlanDay_Sunday"].Value,
                _ => dto.DayOfWeek
            };
        }

        var data = new CalendarReportDataDto
        {
            From = filters.From,
            To = filters.To,
            Lines = dtos
        };
        var result = new CalendarReportResult { Filters = filters, Data = data };
        await EnrichPrintHeaderAsync(result, lookup, cancellationToken);
        return result;
    }

    private async Task EnrichPrintHeaderAsync(CalendarReportResult result, ILookupRepository lookup, CancellationToken cancellationToken)
    {
        result.Data.GeneratedAt = DateTime.UtcNow;
        result.Data.GeneratedBy = await PrintHeaderUser.TryGetNameAsync(lookup, authorizationChecker, cancellationToken);
    }

    public IFileData Export(CalendarReportResult report, ExportFormat format, string? layoutTag, IPrintRendererResolver renderer, IStringLocalizer localizer, TimeZoneInfo? timeZone)
    {
        var output = renderer.Render(report.Data, localizer, timeZone, format, layoutTag ?? "default");
        return new FileData
        {
            FileName = $"CalendarReport_{DateTime.Now:yyyyMMdd_HHmmss}{output.FileExtension}",
            ContentType = output.ContentType,
            Content = output.Content,
        };
    }
}

[Mapper]
public static partial class CalendarReportMapper
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial List<CalendarReportLineDto> ToDtos(IEnumerable<CalendarReportLine> lines);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public static partial CalendarReportLineDto ToDto(CalendarReportLine line);
}
