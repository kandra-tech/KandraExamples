using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using FamilyGarage.Domain.Entities.Dictionaries;
using FamilyGarage.Enums;
using Microsoft.EntityFrameworkCore;

namespace FamilyGarage.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_Services")]
[KandraGeneratedEquality]
public sealed partial class Service : DocumentBase, IEntityWithRows<Service>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(FamilyGarageTypeIds.Service);

    public Service() : base(TypeId) { }

    public Guid VehicleId { get; set; }
    [JsonIgnore] public Vehicle? Vehicle { get; set; }

    public Guid ServiceStationId { get; set; }
    [JsonIgnore] public Provider? ServiceStation { get; set; }

    public ServiceKind Kind { get; set; } = ServiceKind.Maintenance;

    public int Odometer { get; set; }

    public string? Comment { get; set; }

    public bool Detailed { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }

    public Guid? AccidentId { get; set; }
    [JsonIgnore] public Accident? Accident { get; set; }

    [KandraTablePart(TableName = "Doc_ServiceLines")]
    public IList<ServiceLine> Lines { get; set; } = new List<ServiceLine>();
}

[KandraGeneratedEquality]
public partial class ServiceLine : IEntityRow
{
    public Guid Id { get; set; }

    public Guid ServiceId { get; set; }
    [JsonIgnore] public Service Service { get; set; } = null!;

    public Guid ItemId { get; set; }
    [JsonIgnore] public ItemNode? Item { get; set; }

    [Precision(14, 3)]
    public decimal Quantity { get; set; }

    [Precision(14, 2)]
    public decimal Price { get; set; }

    [Precision(14, 2)]
    public decimal Amount { get; set; }
}
