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
using HomePantry.Enums;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Dictionaries;

[TypeId(HomePantryTypeIds.Location)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "isFolder")]
[JsonDerivedType(typeof(LocationFolderDto), "1")]
[JsonDerivedType(typeof(LocationDto), "0")]
[KandraDictionaryForm(Name = "Locations", ValidatorType = typeof(LocationNodeValidator), QueryDtoType = typeof(LocationQueryDto))]
[EditFormTitle("EditLocation")]
[AddFormTitle("CreateLocation")]
[ViewFormTitle("ViewLocation")]
[ListFormTitle("Locations")]
[NavigationName("Nav_Locations")]
[Description("A household location where stock is stored (e.g. Kitchen, Fridge, Freezer, Shelf, Bathroom, Storage room). A row is either a group folder or a storage location.")]
public abstract class LocationNodeDto : HierarchicalDictionaryItemDto
{
}

[Description("A concrete storage location in the household where stock batches are kept.")]
public class LocationDto : LocationNodeDto
{
    [Caption("Location_IsFreezer")]
    [ListFormColumn(Filterable = false)]
    [Description("Whether this storage location is a freezer.")]
    public bool IsFreezer { get; set; }
}

[Description("A grouping folder in the location hierarchy (e.g. Kitchen, Bathroom, Storage room).")]
public class LocationFolderDto : LocationNodeDto
{
}

public class LocationQueryDto : HierarchicalDictionaryQueryDto
{
}

public class LocationValidator : DictionaryValidator<LocationDto>
{
    public LocationValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

public class LocationFolderValidator : DictionaryValidator<LocationFolderDto>
{
    public LocationFolderValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

public class LocationNodeValidator : DictionaryValidator<LocationNodeDto>
{
    public LocationNodeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => (LocationDto)x)
            .SetValidator(new LocationValidator(localizer))
            .When(x => x is LocationDto);

        RuleFor(x => (LocationFolderDto)x)
            .SetValidator(new LocationFolderValidator(localizer))
            .When(x => x is LocationFolderDto);
    }
}
