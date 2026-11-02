using Kandra.Domain.Accounting;

namespace PersonalAccounting.Application.Accounting;

/// <summary>
/// Chart-of-accounts/posting subsystem - the concrete chart is the one application-specific piece;
/// everything else is registered by Kandra.Application/Kandra.Persistence's own AddKandraApplication/
/// AddKandraPersistence. This configuration has no chart of its own yet, so the chart starts empty -
/// replace this with real AccountLeaf/AccountFolder roots once you adopt the posting subsystem (see the
/// post-to-accounting skill).
/// </summary>
public sealed class PersonalAccountingChartOfAccountsBuilder : IChartOfAccountsBuilder
{
    public ChartOfAccounts BuildChart() => new();
}
