using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Persistence.Abstractions.Registers;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Registers;
using HomePantry.Domain.Numbering;
using HomePantry.Enums;
using HomePantry.Forms.Documents;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Documents;

[KandraDocumentBehavior(typeof(MealPlanDto))]
[RegisterTransactions(typeof(PlannedMealsRecord))]
public sealed class MealPlanBehavior(ISimpleNumberingSystem numbering) : IDocumentBehavior<MealPlan>
{
    private static DateTime ToMonday(DateTime date)
    {
        var d = date.Date;
        var diff = (7 + (int)d.DayOfWeek - (int)DayOfWeek.Monday) % 7;
        return d.AddDays(-diff);
    }

    public async ValueTask OnNewAsync(MealPlan entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.WeekStartDate = ToMonday(date ?? DateTime.UtcNow);
        entity.Date = date ?? entity.WeekStartDate;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.MealPlan, raw => $"MPL-{raw:D4}", cancellationToken: cancellationToken);
    }

    public ValueTask OnSaveAsync(MealPlan entity, bool isInsert, CancellationToken cancellationToken)
    {
        entity.WeekStartDate = ToMonday(entity.WeekStartDate == default ? entity.Date : entity.WeekStartDate);
        return ValueTask.CompletedTask;
    }

    public async ValueTask OnSubmitAsync(MealPlan entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<PlannedMealsRecord>>();

        var monday = ToMonday(entity.WeekStartDate == default ? entity.Date : entity.WeekStartDate);
        entity.WeekStartDate = monday;
        var records = entity.Lines.Select(line =>
        {
            var dayOffset = (int)line.Day - 1;
            var mealDate = monday.AddDays(dayOffset);
            return new PlannedMealsRecord
            {
                LineId = line.Id,
                Dimensions = new PlannedMealsDimensions
                {
                    Date = mealDate,
                    RecipeId = line.RecipeId
                },
                Details = new PlannedMealsDetails
                {
                    Servings = line.Servings
                }
            };
        }).ToList();

        await writer.PostAsync(entity.Id, monday, records, cancellationToken);
    }

    public async ValueTask OnUnsubmitAsync(MealPlan entity, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<PlannedMealsRecord>>();
        await writer.UnpostAsync(entity.Id, cancellationToken);
    }

    public async ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken)
    {
        var writer = registers.GetWriter<IDocumentInfoRegisterWriter<PlannedMealsRecord>>();
        await writer.DeleteAsync(id, cancellationToken);
    }
}

[Mapper]
public partial class MealPlanMapper : IEntityMapper<MealPlan, MealPlanDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial MealPlanDto ToDto(MealPlan entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial MealPlan ToEntity(MealPlanDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial MealPlanLineDto ToDto(MealPlanLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial MealPlanLine ToEntity(MealPlanLineDto line);
}

public class MealPlanQueryDtoValidator : PagedQueryDtoValidator<MealPlanQueryDto>;
