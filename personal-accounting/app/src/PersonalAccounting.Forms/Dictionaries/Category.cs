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
using Microsoft.Extensions.Localization;
using PersonalAccounting.Enums;

namespace PersonalAccounting.Forms.Dictionaries;

[TypeId(PersonalAccountingTypeIds.Category)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "isFolder")]
[JsonDerivedType(typeof(CategoryFolderDto), "1")]
[JsonDerivedType(typeof(CategoryDto), "0")]
[KandraDictionaryForm(Name = "Categories", ValidatorType = typeof(CategoryNodeValidator), QueryDtoType = typeof(CategoryQueryDto))]
[EditFormTitle("EditCategory")]
[AddFormTitle("CreateCategory")]
[ViewFormTitle("ViewCategory")]
[ListFormTitle("Categories")]
[NavigationName("Nav_Categories")]
[Description("A hierarchical category for income or expense tracking.")]
public abstract class CategoryNodeDto : HierarchicalDictionaryItemDto
{
    [Caption("Category_Kind")]
    [ListFormColumn]
    [Description("Whether this category is for income or expense.")]
    public CategoryKind Kind { get; set; }
}

[Description("A single category for tracking personal money movements.")]
public class CategoryDto : CategoryNodeDto
{
}

[Description("A grouping folder for categories.")]
public class CategoryFolderDto : CategoryNodeDto
{
}

public class CategoryQueryDto : HierarchicalDictionaryQueryDto
{
}

public class CategoryValidator : DictionaryValidator<CategoryDto>
{
    public CategoryValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

public class CategoryFolderValidator : DictionaryValidator<CategoryFolderDto>
{
    public CategoryFolderValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

public class CategoryNodeValidator : DictionaryValidator<CategoryNodeDto>
{
    public CategoryNodeValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}
