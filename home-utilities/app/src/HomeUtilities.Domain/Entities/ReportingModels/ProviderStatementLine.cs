using HomeUtilities.Enums;

namespace HomeUtilities.Domain.Entities.ReportingModels;

public class ProviderStatementLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Date { get; set; }
    public StatementRowKind Kind { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public decimal Invoiced { get; set; }
    public decimal Paid { get; set; }
    public decimal Balance { get; set; }
}
