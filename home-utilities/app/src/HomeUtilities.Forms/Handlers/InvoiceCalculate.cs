using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Forms.Handlers;
using HomeUtilities.Enums;
using HomeUtilities.Forms.Documents;

namespace HomeUtilities.Forms.Handlers;

public sealed class InvoiceCalculateRequestDto
{
    public Guid ProviderId { get; set; }
    public Guid PropertyId { get; set; }
    public DateTime Month { get; set; }
}

public sealed class InvoiceCalculateLineDto
{
    public Guid ServiceId { get; set; }
    public Guid? MeterId { get; set; }
    public TariffZone Zone { get; set; } = TariffZone.Single;
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
}

public sealed class InvoiceCalculateResponseDto
{
    public List<InvoiceCalculateLineDto> Lines { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public string? PaymentDescription { get; set; }
}

[KandraHandler(Name = "InvoiceCalculate")]
[PermissionsFrom(typeof(InvoiceDto), ApplicationAction.View)]
public sealed class InvoiceCalculateHandler : IHandler<InvoiceCalculateRequestDto, InvoiceCalculateResponseDto>;
