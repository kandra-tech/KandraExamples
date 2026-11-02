using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.ReportingModels;

public class ConsumptionReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public TariffZone Zone { get; set; }
    public decimal Month01 { get; set; }
    public decimal Month02 { get; set; }
    public decimal Month03 { get; set; }
    public decimal Month04 { get; set; }
    public decimal Month05 { get; set; }
    public decimal Month06 { get; set; }
    public decimal Month07 { get; set; }
    public decimal Month08 { get; set; }
    public decimal Month09 { get; set; }
    public decimal Month10 { get; set; }
    public decimal Month11 { get; set; }
    public decimal Month12 { get; set; }
    public decimal Total { get; set; }
    public decimal PreviousYearTotal { get; set; }
    public decimal ChangeYoY { get; set; }
}
