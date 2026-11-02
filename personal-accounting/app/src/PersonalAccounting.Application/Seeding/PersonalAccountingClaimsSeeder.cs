using System.Security.Claims;
using Kandra.Application.Abstractions;
using Kandra.Domain.Enums;
using Kandra.Persistence.Abstractions;

namespace PersonalAccounting.Application.Seeding;

public class PersonalAccountingClaimsSeeder : IClaimsSeeder
{
    public IEnumerable<Claim> MakeUserRoleAreasClaims()
    {
        return
        [
            .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Dictionary),
            .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Document),
            .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Report),
            .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Dashboard),
            .. ClaimsHelper.MakeAreaClaims(ApplicationArea.DataProcessor),
            .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Constant),
            .. ClaimsHelper.MakeAreaClaims(ApplicationArea.Register)
        ];
    }
}
