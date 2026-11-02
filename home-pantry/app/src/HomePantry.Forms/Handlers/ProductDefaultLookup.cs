using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Forms.Handlers;
using HomePantry.Forms.Documents;

namespace HomePantry.Forms.Handlers;

public sealed class ProductDefaultLookupRequestDto
{
    public Guid ProductId { get; set; }
    public DateTime DocumentDate { get; set; }
}

public sealed class ProductDefaultLookupResponseDto
{
    public Guid? DefaultLocationId { get; set; }
    public DateTime BestBeforeDate { get; set; } = new(9999, 12, 31);
}

[KandraHandler(Name = "ProductDefaultLookup")]
public sealed class ProductDefaultLookupHandler : IHandler<ProductDefaultLookupRequestDto, ProductDefaultLookupResponseDto>;
