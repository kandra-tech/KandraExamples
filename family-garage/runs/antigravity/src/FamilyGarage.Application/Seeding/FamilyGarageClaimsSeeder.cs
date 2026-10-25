using System.Security.Claims;
using Kandra.Application.Abstractions;
using Kandra.Domain.Enums;
using Kandra.Persistence.Abstractions;

namespace FamilyGarage.Application.Seeding;

public class FamilyGarageClaimsSeeder : IClaimsSeeder
{
    public IEnumerable<Claim> MakeUserRoleAreasClaims()
    {
        var claims = new List<Claim>();
        claims.AddRange(ClaimsHelper.MakeAreaClaims(ApplicationArea.Dictionary));
        claims.AddRange(ClaimsHelper.MakeAreaClaims(ApplicationArea.Document));
        claims.AddRange(ClaimsHelper.MakeAreaClaims(ApplicationArea.Report));
        claims.AddRange(ClaimsHelper.MakeAreaClaims(ApplicationArea.Dashboard));
        claims.AddRange(ClaimsHelper.MakeAreaClaims(ApplicationArea.DataProcessor));
        claims.AddRange(ClaimsHelper.MakeAreaClaims(ApplicationArea.Register));
        return claims;
    }
}
