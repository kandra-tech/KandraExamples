using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Entities;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.Documents;
using Kandra.Forms.Querying;
using Kandra.Validators.Documents;
using HomePantry.Enums;
using HomePantry.Forms.Dictionaries;
using Microsoft.Extensions.Localization;

namespace HomePantry.Forms.Documents;

[TypeId(HomePantryTypeIds.Consume)]
[KandraDocumentForm(Name = "Consumes", ValidatorType = typeof(ConsumeValidator), QueryDtoType = typeof(ConsumeQueryDto))]
[EditFormTitle("EditConsume")]
[AddFormTitle("CreateConsume")]
[ViewFormTitle("ViewConsume")]
[ListFormTitle("Consumes")]
[NavigationName("Nav_Consumes")]
[Description("Records the consumption, spoilage, or usage of pantry stock.")]
public class ConsumeDto : DocumentDto
{
    [Caption("Consume_Reason")]
    [ListFormColumn]
    [Description("Reason for consuming or discarding items (eaten, spoiled, used up, other).")]
    public ConsumeReason Reason { get; set; }

    [TablePart(AddCaption = "AddLine")]
    [Caption("Consume_Lines")]
    [Description("Items consumed from specified storage locations.")]
    public ICollection<ConsumeLineDto> Lines { get; set; } = [];
}

[Description("A consumed item line specifying the product, storage location, and quantity consumed.")]
public class ConsumeLineDto
{
    [Required]
    public Guid Id { get; set; }

    [Caption("Product")]
    [Search(typeof(ProductNodeDto))]
    [Required]
    [GridColumn(Order = 1, WidthPercent = 40)]
    public Guid ProductId { get; set; }

    [Caption("Location")]
    [Search(typeof(LocationNodeDto))]
    [Required]
    [GridColumn(Order = 2, WidthPercent = 35)]
    public Guid LocationId { get; set; }

    [Caption("Quantity")]
    [GridColumn(Order = 3, WidthPercent = 25)]
    public decimal Quantity { get; set; }
}

public class ConsumeQueryDto : DocumentQueryDto
{
}

public class ConsumeValidator : DocumentValidator<ConsumeDto>
{
    public ConsumeValidator(IStringLocalizer localizer) : base(localizer)
    {
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId).NotEmpty();
            line.RuleFor(l => l.LocationId).NotEmpty();
            line.RuleFor(l => l.Quantity).GreaterThan(0);
        });
    }
}
