using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Dictionaries;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Dictionaries;

[KandraDictionaryBehavior(typeof(RecipeDto))]
public sealed class RecipeBehavior(ISimpleNumberingSystem numbering) : IFlatDictionaryBehavior<Recipe>
{
    public async ValueTask OnNewAsync(Recipe entity, CancellationToken cancellationToken)
    {
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.Recipe, raw => $"RCP-{raw:D4}", cancellationToken: cancellationToken);
    }
}

[Mapper]
public sealed partial class RecipeMapper : IEntityMapper<Recipe, RecipeDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial RecipeDto ToDto(Recipe entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial Recipe ToEntity(RecipeDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial RecipeLineDto ToDto(RecipeLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial RecipeLine ToEntity(RecipeLineDto dto);
}

public class RecipeQueryDtoValidator : PagedQueryDtoValidator<RecipeQueryDto>;
