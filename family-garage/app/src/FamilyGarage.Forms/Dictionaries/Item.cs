using System.ComponentModel;
using System.Text.Json.Serialization;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Forms.Dictionaries;

[TypeId(FamilyGarageTypeIds.Item)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "isFolder")]
[JsonDerivedType(typeof(ItemFolderDto), "1")]
[JsonDerivedType(typeof(ItemDto), "0")]
[KandraDictionaryForm(Name = "Items", ValidatorType = typeof(ItemNodeValidator), QueryDtoType = typeof(ItemQueryDto))]
[EditFormTitle("EditItem")]
[AddFormTitle("CreateItem")]
[ViewFormTitle("ViewItem")]
[ListFormTitle("Items")]
[NavigationName("Nav_Items")]
[Description("An item representing a part, work (job/labor), or consumable in the garage hierarchy.")]
public abstract class ItemNodeDto : HierarchicalDictionaryItemDto
{
}

[Description("A grouping folder in the Items catalog (e.g., Engine, Brakes, Tyres, Works).")]
public class ItemFolderDto : ItemNodeDto
{
}

[Description("A leaf item (part, work, or consumable) with its pricing and service intervals.")]
public class ItemDto : ItemNodeDto
{
    [Caption("Kind")]
    [ListFormColumn(Filterable = false)]
    [Description("Classification of the item: part, work, or consumable.")]
    public ItemKind Kind { get; set; }

    [Caption("Unit")]
    [ListFormColumn(Filterable = false)]
    [Description("Measurement unit (e.g. pcs, litres, hours).")]
    public string Unit { get; set; } = "pcs";

    [Caption("DefaultPrice")]
    [ListFormColumn(Filterable = false)]
    [Description("Default unit price for the item.")]
    public decimal DefaultPrice { get; set; }

    [Caption("ServiceIntervalKm")]
    [ListFormColumn(Filterable = false)]
    [Description("For work items: optional recurring service interval in kilometres (0 if none).")]
    public int ServiceIntervalKm { get; set; }

    [Caption("ServiceIntervalMonths")]
    [ListFormColumn(Filterable = false)]
    [Description("For work items: optional recurring service interval in months (0 if none).")]
    public int ServiceIntervalMonths { get; set; }
}

public class ItemQueryDto : HierarchicalDictionaryQueryDto
{
}

public class ItemValidator : DictionaryValidator<ItemDto>
{
    public ItemValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.Unit).NotEmpty().WithMessage(localizer["FieldIsRequired", localizer["Unit"]]);
        RuleFor(x => x.DefaultPrice).GreaterThanOrEqualTo(0).WithMessage(localizer["MustBeNonNegative"]);
        RuleFor(x => x.ServiceIntervalKm).GreaterThanOrEqualTo(0).WithMessage(localizer["MustBeNonNegative"]);
        RuleFor(x => x.ServiceIntervalMonths).GreaterThanOrEqualTo(0).WithMessage(localizer["MustBeNonNegative"]);
    }
}

public class ItemFolderValidator : DictionaryValidator<ItemFolderDto>
{
    public ItemFolderValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

public class ItemNodeValidator : DictionaryValidator<ItemNodeDto>
{
    public ItemNodeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => (ItemDto)x)
            .SetValidator(new ItemValidator(localizer))
            .When(x => x is ItemDto);

        RuleFor(x => (ItemFolderDto)x)
            .SetValidator(new ItemFolderValidator(localizer))
            .When(x => x is ItemFolderDto);
    }
}
