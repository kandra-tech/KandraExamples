using HomeUtilities.Domain.Entities.ReportingModels;

namespace HomeUtilities.Domain.Commons;

public interface IReportsRepository
{
    Task<List<ProviderStatementLine>> GetProviderStatementAsync(Guid providerId, Guid propertyId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<List<ConsumptionReportLine>> GetConsumptionAsync(Guid propertyId, int year, CancellationToken cancellationToken = default);
    Task<List<CostsReportLine>> GetCostsAsync(DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
    Task<List<TariffHistoryReportLine>> GetTariffHistoryAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<List<MeterConsumptionReportLine>> GetMeterConsumptionAsync(Guid propertyId, int year, CancellationToken cancellationToken = default);
    Task<List<MeterReadingsHistoryReportLine>> GetMeterReadingsHistoryAsync(Guid propertyId, Guid? meterId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default);
}
