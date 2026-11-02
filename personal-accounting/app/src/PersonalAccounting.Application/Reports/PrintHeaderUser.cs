using Kandra.Application.Abstractions;
using Kandra.Persistence.Abstractions;

namespace PersonalAccounting.Application.Reports;

public static class PrintHeaderUser
{
    public static async Task<string?> TryGetNameAsync(ILookupRepository lookup, IAuthorizationChecker authorizationChecker, CancellationToken cancellationToken)
    {
        try
        {
            var userId = authorizationChecker.UserId();
            if (userId != Guid.Empty)
            {
                var user = await lookup.GetUserAsync(userId, cancellationToken);
                return user?.Name;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }
}
