namespace HomeUtilities.Domain.Entities.ReportingModels;

public class CostsReportLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public Guid ServiceGroupId { get; set; }
    public string ServiceGroupName { get; set; } = string.Empty;
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}
