using System.Security.Claims;
using Kandra.Application.Abstractions;
using Kandra.Domain.Enums;
using Kandra.Persistence.Abstractions;

namespace HomePantry.Application.Seeding;

/// <summary>
/// Seeds the default User role area claims across Dictionaries, Documents, Reports, Dashboards, DataProcessors, and Registers.
/// </summary>
public class HomePantryClaimsSeeder : IClaimsSeeder
{
    public IEnumerable<Claim> MakeUserRoleAreasClaims() =>
    [
        .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Dictionary),
        .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Document),
        .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Report),
        .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Dashboard),
        .. ClaimsHelper.MakeAreaClaims(ApplicationArea.DataProcessor),
        .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Register)
    ];
}
