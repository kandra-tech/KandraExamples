using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.ReportingModels;

public class MeterConsumptionReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeterId { get; set; }
    public string MeterName { get; set; } = string.Empty;
    public TariffZone Zone { get; set; }
    public string Unit { get; set; } = string.Empty;
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
}
