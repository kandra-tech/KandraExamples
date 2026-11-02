using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.ReportingModels;

public class MeterReadingsHistoryReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public Guid MeterId { get; set; }
    public string MeterName { get; set; } = string.Empty;
    public TariffZone Zone { get; set; }
    public decimal Reading { get; set; }
    public string Unit { get; set; } = string.Empty;
}
