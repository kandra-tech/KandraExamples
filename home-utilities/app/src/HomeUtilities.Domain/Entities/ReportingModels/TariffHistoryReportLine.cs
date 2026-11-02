using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.ReportingModels;

public class TariffHistoryReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceId { get; set; }
    public DateTime Date { get; set; }
    public TariffZone Zone { get; set; }
    public decimal Price { get; set; }
    public decimal? ChangePercent { get; set; }
}
