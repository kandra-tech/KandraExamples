namespace FamilyGarage.Domain.Reporting;

public sealed record RefuelReading(DateTime Date, int Odometer, decimal Litres, bool FullTank);

public sealed record FuelInterval(RefuelReading Refuel, int Distance, decimal Litres, decimal Consumption);

/// <summary>
/// Litres per 100 km between consecutive full-tank refuels: at each full tank after the first, the litres of every refuel
/// since the previous full tank (partial ones in between included) over the kilometres driven since it.
/// </summary>
public static class FuelConsumptionCalculator
{
    public static IReadOnlyList<FuelInterval> Intervals(IEnumerable<RefuelReading> refuels)
    {
        var result = new List<FuelInterval>();
        RefuelReading? lastFull = null;
        decimal litresSinceFull = 0m;

        foreach (var refuel in refuels.OrderBy(r => r.Date).ThenBy(r => r.Odometer))
        {
            if (lastFull is null)
            {
                result.Add(new FuelInterval(refuel, 0, 0m, 0m));
                if (refuel.FullTank)
                {
                    lastFull = refuel;
                }
                continue;
            }

            litresSinceFull += refuel.Litres;
            if (!refuel.FullTank)
            {
                result.Add(new FuelInterval(refuel, 0, 0m, 0m));
                continue;
            }

            var distance = refuel.Odometer - lastFull.Odometer;
            result.Add(distance > 0
                ? new FuelInterval(refuel, distance, litresSinceFull, Math.Round(litresSinceFull / distance * 100m, 2))
                : new FuelInterval(refuel, 0, 0m, 0m));
            lastFull = refuel;
            litresSinceFull = 0m;
        }

        return result;
    }

    public static decimal Average(IEnumerable<FuelInterval> intervals)
    {
        var measured = intervals.Where(i => i.Distance > 0).ToList();
        var distance = measured.Sum(i => i.Distance);
        return distance > 0 ? Math.Round(measured.Sum(i => i.Litres) / distance * 100m, 2) : 0m;
    }
}
