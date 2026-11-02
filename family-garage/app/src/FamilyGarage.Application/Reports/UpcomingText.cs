using Kandra.Localization;
using FamilyGarage.Domain.Entities.ReportingModels;
using Microsoft.Extensions.Localization;

namespace FamilyGarage.Application.Reports;

/// <summary>The words of a due event in the user's language: the repository returns data, this turns it into text.</summary>
public static class UpcomingText
{
    public static string Kind(UpcomingItem item, IStringLocalizer localizer) => item.Kind.GetCaption(localizer);

    /// <summary>The work item's name, the contract's number, or the policy's kind.</summary>
    public static string Description(UpcomingItem item, IStringLocalizer localizer)
        => item.PolicyKind is { } kind ? kind.GetCaption(localizer) : item.Subject ?? string.Empty;

    /// <summary>"Oil change, in 12 days" / "Oil change, in 640 km" / "Oil change, overdue".</summary>
    public static string NextDue(UpcomingItem item, IStringLocalizer localizer)
    {
        var description = Description(item, localizer);
        if (item.IsOverdue)
        {
            return localizer["Upcoming_NextDue_Overdue", description].Value;
        }

        return (item.RemainingDays, item.RemainingKm) switch
        {
            ({ } days, { } km) => localizer["Upcoming_NextDue_DaysKm", description, days, km].Value,
            ({ } days, null) => localizer["Upcoming_NextDue_Days", description, days].Value,
            (null, { } km) => localizer["Upcoming_NextDue_Km", description, km].Value,
            _ => description,
        };
    }
}
