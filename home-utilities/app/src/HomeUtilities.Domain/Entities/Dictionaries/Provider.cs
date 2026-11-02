using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Dictionaries;
using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.Dictionaries;

[KandraDictionaryEntity(TableName = "Dict_Providers")]
[KandraGeneratedEquality]
public sealed partial class Provider : DictionaryBase, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.Provider);

    public ProviderKind Kind { get; set; }

    public string? PersonalAccountNumber { get; set; }

    public string? Phone { get; set; }

    public string? Iban { get; set; }

    public string? Edrpou { get; set; }

    public string? BankName { get; set; }

    public string? Address { get; set; }

    public string? PaymentDescriptionTemplate { get; set; }
}
