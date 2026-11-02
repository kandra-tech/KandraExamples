using Kandra.Application.Abstractions;
using Kandra.Persistence.Abstractions;

namespace HomePantry.Application.Reports;

public static class PrintHeaderUser
{
    public static async Task<string> TryGetNameAsync(ILookupRepository lookup, IAuthorizationChecker authorizationChecker, CancellationToken cancellationToken)
    {
        var userId = authorizationChecker.UserId();
        if (userId == Guid.Empty) return "admin";

        try
        {
            var user = await lookup.GetUserAsync(userId, cancellationToken);
            return user?.Name ?? "admin";
        }
        catch
        {
            return "admin";
        }
    }
}
