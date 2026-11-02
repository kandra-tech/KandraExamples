using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Services")]
public abstract class ServiceNode : HierarchicalDictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.Service);
}

[KandraDictionaryFolder]
[KandraGeneratedEquality]
public sealed partial class ServiceFolder : ServiceNode
{
}

[KandraGeneratedEquality]
public sealed partial class Service : ServiceNode
{
    public string? Unit { get; set; }

    public BillingType Billing { get; set; }

    public Guid? ProviderId { get; set; }

    [JsonIgnore]
    public Provider? Provider { get; set; }
}
