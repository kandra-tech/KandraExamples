using System.ComponentModel;
using System.Text.Json.Serialization;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Dictionaries;
using Kandra.Forms.Querying;
using Kandra.Validators.Dictionaries;
using HomePantry.Enums;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Dictionaries;

[TypeId(HomePantryTypeIds.Product)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "isFolder")]
[JsonDerivedType(typeof(ProductFolderDto), "1")]
[JsonDerivedType(typeof(ProductDto), "0")]
[KandraDictionaryForm(Name = "Products", ValidatorType = typeof(ProductNodeValidator), QueryDtoType = typeof(ProductQueryDto))]
[EditFormTitle("EditProduct")]
[AddFormTitle("CreateProduct")]
[ViewFormTitle("ViewProduct")]
[ListFormTitle("Products")]
[NavigationName("Nav_Products")]
[Description("A food or household item tracked in the pantry. A row is either a product group folder (e.g. Dairy, Vegetables, Cleaning) or a concrete product.")]
public abstract class ProductNodeDto : HierarchicalDictionaryItemDto
{
}

[Description("A concrete product tracked in the pantry with unit, barcode, minimum stock, default location, and shelf life.")]
public class ProductDto : ProductNodeDto
{
    [Caption("Product_Unit")]
    [ListFormColumn(Filterable = false)]
    [Description("The measurement unit of this product (e.g. pcs, g, kg, ml, l, pack).")]
    public ProductUnit Unit { get; set; }

    [Caption("Product_Barcode")]
    [ListFormColumn(Filterable = false)]
    [Description("The barcode of this product, used for barcode scanner lookup.")]
    public string? Barcode { get; set; }

    [Caption("Product_MinimumStock")]
    [ListFormColumn(Filterable = false)]
    [Description("The minimum stock threshold below which this product should be reordered.")]
    public decimal MinimumStock { get; set; }

    [Caption("Product_DefaultLocation")]
    [Search(typeof(LocationNodeDto))]
    [Description("The default storage location where this product is kept.")]
    public Guid? DefaultLocationId { get; set; }

    [Caption("Product_DefaultShelfLifeDays")]
    [ListFormColumn(Filterable = false)]
    [Description("The default shelf life in days from purchase; 0 if the product does not spoil.")]
    public int DefaultShelfLifeDays { get; set; }
}

[Description("A grouping folder in the product hierarchy (e.g. Dairy, Vegetables, Cleaning).")]
public class ProductFolderDto : ProductNodeDto
{
}

public class ProductQueryDto : HierarchicalDictionaryQueryDto
{
    public override IReadOnlyList<LookupSearchField> LookupSearchFields { get; } =
    [
        new("Barcode", "equals")
    ];
}

public class ProductValidator : DictionaryValidator<ProductDto>
{
    public ProductValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => x.MinimumStock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DefaultShelfLifeDays).GreaterThanOrEqualTo(0);
    }
}

public class ProductFolderValidator : DictionaryValidator<ProductFolderDto>
{
    public ProductFolderValidator(IStringLocalizer localizer) : base(localizer)
    {
    }
}

public class ProductNodeValidator : DictionaryValidator<ProductNodeDto>
{
    public ProductNodeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleFor(x => (ProductDto)x)
            .SetValidator(new ProductValidator(localizer))
            .When(x => x is ProductDto);

        RuleFor(x => (ProductFolderDto)x)
            .SetValidator(new ProductFolderValidator(localizer))
            .When(x => x is ProductFolderDto);
    }
}
