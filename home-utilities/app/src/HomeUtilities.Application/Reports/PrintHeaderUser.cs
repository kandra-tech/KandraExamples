using Kandra.Application.Abstractions;
using Kandra.Persistence.Abstractions;

namespace HomeUtilities.Application.Reports;

internal static class PrintHeaderUser
{
    public static async Task<string?> TryGetNameAsync(
        ILookupRepository lookup,
        IAuthorizationChecker authorizationChecker,
        CancellationToken cancellationToken)
    {
        var getUserId = authorizationChecker.UserId;
        if (getUserId == null)
            return null;

        try
        {
            var userId = getUserId();
            if (userId == Guid.Empty)
                return null;

            return (await lookup.GetUserAsync(userId, cancellationToken))?.Name;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}
