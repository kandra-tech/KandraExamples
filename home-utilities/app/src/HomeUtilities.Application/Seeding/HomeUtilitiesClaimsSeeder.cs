using System.Security.Claims;
using Kandra.Application.Abstractions;
using Kandra.Domain.Enums;
using Kandra.Persistence.Abstractions;

namespace HomeUtilities.Application.Seeding;

public class HomeUtilitiesClaimsSeeder : IClaimsSeeder
{
    public IEnumerable<Claim> MakeUserRoleAreasClaims()
    {
        return ClaimsHelper.MakeAreaClaims(ApplicationArea.Dictionary)
            .Concat(ClaimsHelper.MakeAreaClaims(ApplicationArea.Document))
            .Concat(ClaimsHelper.MakeAreaClaims(ApplicationArea.Report))
            .Concat(ClaimsHelper.MakeAreaClaims(ApplicationArea.Dashboard))
            .Concat(ClaimsHelper.MakeAreaClaims(ApplicationArea.DataProcessor));
    }
}
