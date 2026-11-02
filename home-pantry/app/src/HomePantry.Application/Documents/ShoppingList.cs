using Kandra.Application.Abstractions;
using Kandra.Application.Abstractions.Behaviors;
using Kandra.Application.Abstractions.Mapping;
using Kandra.Application.Abstractions.Services;
using Kandra.Application.Validation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Printable;
using Kandra.Domain.Registers;
using Kandra.Persistence.Abstractions;
using Kandra.Printing;
using HomePantry.Domain.Commons;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Domain.Entities.Documents;
using HomePantry.Domain.Numbering;
using HomePantry.Forms.Documents;
using Microsoft.Extensions.Localization;
using Riok.Mapperly.Abstractions;

namespace HomePantry.Application.Documents;

[KandraDocumentBehavior(typeof(ShoppingListDto))]
public sealed class ShoppingListBehavior(
    ISimpleNumberingSystem numbering,
    IReportsRepository reportsRepo,
    ICurrentTimeZone timeZone) : IDocumentBehavior<ShoppingList>
{
    public async ValueTask OnNewAsync(ShoppingList entity, DateTime? date, Guid? sourceDocumentId, CancellationToken cancellationToken)
    {
        entity.Date = date ?? DateTime.UtcNow;
        entity.Code = await numbering.GetNextAsync(NumberingBuckets.ShoppingList, raw => $"SHP-{raw:D4}", cancellationToken: cancellationToken);

        var needs = await reportsRepo.GetShoppingNeedsAsync(null, null, entity.Date, timeZone.TimeZone, cancellationToken);
        foreach (var need in needs)
        {
            entity.Lines.Add(new ShoppingListLine
            {
                Id = Guid.NewGuid(),
                ProductId = need.ProductId,
                Quantity = need.TotalNeededQuantity,
                Bought = false
            });
        }
    }

    public ValueTask OnSubmitAsync(ShoppingList entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
    {
        // Shopping list does not post to registers.
        return ValueTask.CompletedTask;
    }

    public async ValueTask<PrintRenderOutput?> OnPrintAsync(
        ShoppingList entity,
        ExportFormat format,
        string? layoutTag,
        ILookupRepository lookup,
        IPrintRendererResolver renderer,
        IStringLocalizer localizer,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken)
    {
        string? storeName = null;
        if (entity.StoreId.HasValue)
        {
            var stores = (await lookup.GetDictionaryAsync<Store>(new[] { entity.StoreId.Value }, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);
            storeName = stores.GetValueOrDefault(entity.StoreId.Value);
        }

        var productIds = entity.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = (await lookup.GetDictionaryAsync<ProductNode>(productIds, cancellationToken)).ToDictionary(x => x.Id, x => x.Name);

        var form = new ShoppingListPrintForm
        {
            Number = entity.Code ?? "",
            Date = entity.Date,
            IsSubmitted = entity.IsActive,
            StoreName = storeName,
            TotalLines = entity.Lines.Count,
            Lines = entity.Lines.Select(l => new ShoppingListPrintLine
            {
                ProductName = products.GetValueOrDefault(l.ProductId) ?? "",
                Quantity = l.Quantity,
                Bought = l.Bought ? (localizer["Yes"].Value ?? "Yes") : (localizer["No"].Value ?? "No")
            }).ToList()
        };

        return await renderer.RenderAsync(form, localizer, timeZone ?? TimeZoneInfo.Utc, format, layoutTag ?? "default", cancellationToken);
    }
}

[Mapper]
public partial class ShoppingListMapper : IEntityMapper<ShoppingList, ShoppingListDto>
{
    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    public partial ShoppingListDto ToDto(ShoppingList entity);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    public partial ShoppingList ToEntity(ShoppingListDto dto);

    [MapperRequiredMapping(RequiredMappingStrategy.Target)]
    private partial ShoppingListLineDto ToDto(ShoppingListLine line);

    [MapperRequiredMapping(RequiredMappingStrategy.Source)]
    private partial ShoppingListLine ToEntity(ShoppingListLineDto line);
}

public class ShoppingListQueryDtoValidator : PagedQueryDtoValidator<ShoppingListQueryDto>;
