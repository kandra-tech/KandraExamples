using Kandra.Application.Abstractions.Services;
using Kandra.Constants;

namespace PersonalAccounting.Application.Seeding;

/// <summary>
/// The sample users of a demo installation (<c>IDemoSeeding</c>); a regular installation has none. Users are not part
/// of the import package format (they need passwords and are not stamped), so they come from this provider.
/// <para>
/// Free-tier limit: at most five users in total, <c>admin</c> included (the two service users, <c>mcp-agent</c> and
/// <c>jobs-scheduler</c>, do not count). Keep the list at four or fewer; <c>SeedAsync</c> checks the limit and refuses
/// to create users beyond it. Every user here gets the generic "user" role; add roles of your configuration when the
/// demo should show role-specific behaviour.
/// </para>
/// </summary>
internal sealed class PersonalAccountingDemoUsers : IDemoUserProvider
{
    private const int Count = 3;

    public IReadOnlyList<DemoUser> GetDemoUsers() =>
        Enumerable.Range(0, Count)
            .Select(i => new DemoUser(
                string.Format(AppConstants.UserName, i),
                string.Format(AppConstants.UserEmail, i),
                string.Format(AppConstants.UserPassword, i),
                [AppConstants.UserRoleName]))
            .ToList();
}
