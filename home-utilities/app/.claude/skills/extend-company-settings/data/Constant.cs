using System.ComponentModel;
using Kandra.Application.Abstractions.Constants;
using Kandra.Attributes.Constants;
using HomeUtilities.Enums;

namespace HomeUtilities.Application.Constants;

// {{Detail}}: TaxCode, Address, Phone ... one constant per detail.
[KandraConstant]
[Description("The company's {{description}}, printed in the letterhead of print forms. Empty until set.")]
public sealed class Company{{Detail}}Constant() : Constant<string>(""), IConstant
{
    public static string Name => "Company{{Detail}}";

    public static Guid TypeId { get; } = new(HomeUtilitiesTypeIds.Company{{Detail}});
}
