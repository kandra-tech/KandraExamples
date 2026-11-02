using HomeUtilities.Enums;

namespace HomeUtilities.Tests.Enums;

/// <summary>
/// Pins the underlying integer values of every persisted enum in <c>HomeUtilities.Enums</c> so reordering or inserting
/// a member in the middle never silently corrupts existing database rows.
/// </summary>
public sealed class PersistedEnumValuesTests
{
    [Fact]
    public void BillingType_ValuesArePinned()
    {
        Assert.Equal(0, (int)BillingType.Metered);
        Assert.Equal(1, (int)BillingType.FixedPerMonth);
        Assert.Equal(2, (int)BillingType.PerSquareMetre);
        Assert.Equal(3, (int)BillingType.PerPerson);
    }

    [Fact]
    public void MeterZones_ValuesArePinned()
    {
        Assert.Equal(0, (int)MeterZones.Single);
        Assert.Equal(1, (int)MeterZones.DayNight);
    }

    [Fact]
    public void TariffZone_ValuesArePinned()
    {
        Assert.Equal(0, (int)TariffZone.Single);
        Assert.Equal(1, (int)TariffZone.Day);
        Assert.Equal(2, (int)TariffZone.Night);
    }

    [Fact]
    public void StatementRowKind_ValuesArePinned()
    {
        Assert.Equal(0, (int)StatementRowKind.OpeningBalance);
        Assert.Equal(1, (int)StatementRowKind.Invoice);
        Assert.Equal(2, (int)StatementRowKind.Payment);
        Assert.Equal(3, (int)StatementRowKind.ClosingBalance);
    }
}
