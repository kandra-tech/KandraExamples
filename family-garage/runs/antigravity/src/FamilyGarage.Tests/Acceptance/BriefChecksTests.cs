using Kandra.Application.Abstractions.Services;
using Kandra.Application.Services.DataProcessors;
using Kandra.Domain.Exceptions;
using Kandra.Forms.Documents;
using Kandra.Forms.Querying;
using Kandra.Forms.Reports;
using Kandra.Persistence.Abstractions.Registers;
using FamilyGarage.Domain.Entities.Registers;
using FamilyGarage.Enums;
using FamilyGarage.Forms.Dashboards;
using FamilyGarage.Forms.DataProcessors;
using FamilyGarage.Forms.Dictionaries;
using FamilyGarage.Forms.Documents;
using FamilyGarage.Forms.Reports;
using FamilyGarage.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FamilyGarage.Tests.Acceptance;

/// <summary>
/// The brief's acceptance checks 3, 4, 7, 9 and 11, and the rules the review found broken. Every test runs as a user in
/// Kyiv, east of UTC, with documents dated at local midnight the way the client sends them, relative to today.
/// </summary>
public sealed class BriefChecksTests : IDisposable
{
    private static readonly TimeZoneInfo Zone = FamilyGarageTestHost.Kyiv;
    private static readonly CancellationToken Ct = CancellationToken.None;
    private readonly FamilyGarageTestHost _host = new(timeZone: Zone);

    public void Dispose() => _host.Dispose();

    private static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zone));

    private static DateTime At(DateOnly day) => FamilyGarageTestHost.LocalMidnight(Zone, day.Year, day.Month, day.Day);

    private static IDocumentCrudService<TDto, TQuery> Docs<TDto, TQuery>(IServiceProvider sp)
        where TDto : DocumentDto where TQuery : DocumentQueryDto
        => sp.GetRequiredService<IDocumentCrudService<TDto, TQuery>>();

    // ---------------------------------------------------------------- set-up helpers

    private async Task<Guid> VehicleAsync(string code)
        => (await _host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<VehicleDto, VehicleQueryDto>>().InsertAsync(new VehicleDto
        {
            Code = code, Name = $"Car {code}", Make = "Skoda", Model = "Octavia", Year = 2022, PlateNumber = code, Vin = code, FuelType = FuelType.Petrol
        }, Ct))).Id;

    private async Task<Guid> ProviderAsync(string code, ProviderKind kind)
        => (await _host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<ProviderDto, ProviderQueryDto>>().InsertAsync(new ProviderDto
        {
            Code = code, Name = $"Provider {code}", Kind = kind
        }, Ct))).Id;

    private async Task<Guid> ContractAsync(string number, Guid vehicleId, Guid lenderId, FinanceContractKind kind = FinanceContractKind.BankLoan)
        => (await _host.InScopeAsync(sp => sp.GetRequiredService<IDictionaryCrudService<FinanceContractDto, FinanceContractQueryDto>>().InsertAsync(new FinanceContractDto
        {
            Code = number, Name = number, Number = number, VehicleId = vehicleId, Kind = kind, LenderId = lenderId,
            SignedOn = At(Today.AddMonths(-6)), AnnualInterestRatePercent = 12m, TermMonths = 24, MonthlyPayment = 5000m, PaymentDayOfMonth = 10
        }, Ct))).Id;

    private async Task<Guid> ItemAsync(string code, ItemKind kind, int intervalKm = 0, int intervalMonths = 0)
        => (await _host.InScopeAsync(sp => sp.GetRequiredService<IHierarchicalDictionaryCrudService<ItemNodeDto, ItemQueryDto>>().InsertAsync(new ItemDto
        {
            Code = code, Name = $"Item {code}", Kind = kind, Unit = "pcs", DefaultPrice = 100m,
            ServiceIntervalKm = intervalKm, ServiceIntervalMonths = intervalMonths
        }, Ct))).Id;

    private async Task<Guid> PostAsync<TDto, TQuery>(TDto dto)
        where TDto : DocumentDto where TQuery : DocumentQueryDto
    {
        var id = (await _host.InScopeAsync(sp => Docs<TDto, TQuery>(sp).InsertAsync(dto, Ct))).Id;
        await _host.InScopeAsync(sp => Docs<TDto, TQuery>(sp).SubmitAsync(id, Ct));
        return id;
    }

    private Task<Guid> PurchaseAsync(Guid vehicleId, Guid sellerId, DateOnly day, decimal price, int odometer,
        VehiclePurchasePaymentKind payment = VehiclePurchasePaymentKind.Cash, decimal downPayment = 0m, Guid? contractId = null)
        => PostAsync<VehiclePurchaseDto, VehiclePurchaseQueryDto>(new VehiclePurchaseDto
        {
            Code = $"PUR-{Guid.NewGuid():N}"[..12], Date = At(day), VehicleId = vehicleId, SellerId = sellerId, Price = price,
            Odometer = odometer, Payment = payment, DownPayment = downPayment, FinanceContractId = contractId
        });

    private Task<Guid> PaymentAsync(Guid contractId, DateOnly day, decimal principal, decimal interest = 0m, decimal fees = 0m)
        => PostAsync<FinancePaymentDto, FinancePaymentQueryDto>(new FinancePaymentDto
        {
            Code = $"FP-{Guid.NewGuid():N}"[..12], Date = At(day), FinanceContractId = contractId, Principal = principal, Interest = interest, Fees = fees
        });

    private Task<Guid> RefuelAsync(Guid vehicleId, Guid stationId, DateOnly day, int odometer, decimal litres, bool fullTank = true, decimal amount = 1000m)
        => PostAsync<RefuelDto, RefuelQueryDto>(new RefuelDto
        {
            Code = $"REF-{Guid.NewGuid():N}"[..12], Date = At(day), VehicleId = vehicleId, FuelStationId = stationId,
            Litres = litres, Amount = amount, Odometer = odometer, FullTank = fullTank
        });

    private async Task EditAsync<TDto, TQuery>(Guid id, Action<TDto> change)
        where TDto : DocumentDto where TQuery : DocumentQueryDto
    {
        var dto = await _host.InScopeAsync(sp => Docs<TDto, TQuery>(sp).GetByIdAAsync(id, Ct));
        change(dto);
        await _host.InScopeAsync(sp => Docs<TDto, TQuery>(sp).UpdateAsync(dto, Ct));
    }

    private async Task<decimal> DebtAsync(Guid contractId) => await _host.InScopeAsync(async sp =>
        (await sp.GetRequiredService<IBalanceReader<FinanceDebtMovement, FinanceDebtBalance>>()
            .Balances(q => q.Where(b => b.Dimensions.ContractId == contractId), Ct)).Sum(b => b.Resources.Amount));

    private async Task<decimal> CostsAsync(Guid vehicleId, CostCategory? category = null) => await _host.InScopeAsync(async sp =>
        (await sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>().Movements(DateTime.MinValue, DateTime.MaxValue,
            q => q.Where(m => m.Dimensions.VehicleId == vehicleId && (category == null || m.Dimensions.Category == category)), Ct))
        .Sum(m => m.IsExpense ? -m.Resources.Amount : m.Resources.Amount));

    private async Task<TResult> RunReportAsync<TFilters, TResult>(TFilters filters)
        where TFilters : class, IReportFiltersDto, new() where TResult : class, IReportResult<TFilters>
        => await _host.InScopeAsync(sp => sp.GetRequiredService<IReportService<TFilters, TResult>>().RunAsync(filters, Ct));

    private async Task<UpcomingRemindersResult> RunRemindersAsync() => await _host.InScopeAsync(sp =>
        sp.GetRequiredService<IDataProcessorService<UpcomingRemindersDto, UpcomingRemindersResult>>()
            .ProcessAsync(new UpcomingRemindersDto { LookAheadDays = 30, LookAheadKm = 1000 }, Ct));

    // ---------------------------------------------------------------- brief checks

    [Fact]
    public async Task Check3_ChangingUnpostingAndDeletingAService_UpdatesCostsHistoryAndLastDone()
    {
        var vehicle = await VehicleAsync("C3");
        var station = await ProviderAsync("C3S", ProviderKind.ServiceStation);
        var oilChange = await ItemAsync("C3W", ItemKind.Work, intervalKm: 10000, intervalMonths: 12);
        var filter = await ItemAsync("C3P", ItemKind.Part);
        var day = Today.AddDays(-20);

        var serviceId = await PostAsync<ServiceDto, ServiceQueryDto>(new ServiceDto
        {
            Code = "SRV-C3", Date = At(day), VehicleId = vehicle, ServiceStationId = station, Kind = ServiceKind.Maintenance,
            Odometer = 20000, Detailed = true,
            Lines = [new ServiceLineDto { ItemId = oilChange, Quantity = 1, Price = 1000m }, new ServiceLineDto { ItemId = filter, Quantity = 2, Price = 300m }]
        });

        async Task<int> HistoryRowsAsync() => await _host.InScopeAsync(async sp =>
            (await sp.GetRequiredService<ITurnoverReader<ServiceLinesMovement>>().Movements(DateTime.MinValue, DateTime.MaxValue,
                q => q.Where(m => m.Dimensions.VehicleId == vehicle), Ct)).Count);
        async Task<int?> LastDoneKmAsync() => await _host.InScopeAsync(async sp =>
            (await sp.GetRequiredService<IInfoRegisterReader<LastDoneRecord>>().SliceOfLast(DateTime.UtcNow,
                q => q.Where(r => r.Dimensions.VehicleId == vehicle && r.Dimensions.ItemId == oilChange), Ct)).FirstOrDefault()?.Details.Odometer);

        Assert.Equal(1600m, await CostsAsync(vehicle, CostCategory.Maintenance));
        Assert.Equal(2, await HistoryRowsAsync());
        Assert.Equal(20000, await LastDoneKmAsync());

        // Change the posted service: the oil change is taken off and the odometer corrected.
        await EditAsync<ServiceDto, ServiceQueryDto>(serviceId, d =>
        {
            d.Odometer = 20100;
            d.Lines = d.Lines.Where(l => l.ItemId == filter).ToList();
        });
        Assert.Equal(600m, await CostsAsync(vehicle, CostCategory.Maintenance));
        Assert.Equal(1, await HistoryRowsAsync());
        Assert.Null(await LastDoneKmAsync());

        await _host.InScopeAsync(sp => Docs<ServiceDto, ServiceQueryDto>(sp).UnsubmitAsync(serviceId, Ct));
        Assert.Equal(0m, await CostsAsync(vehicle));
        Assert.Equal(0, await HistoryRowsAsync());

        await _host.InScopeAsync(sp => Docs<ServiceDto, ServiceQueryDto>(sp).DeleteAsync(serviceId, Ct));
        Assert.Equal(0m, await CostsAsync(vehicle));
    }

    [Fact]
    public async Task Check4_Upcoming_ReportsAWorkItemWhenItsKmOrItsMonthIntervalRunsOut()
    {
        var vehicle = await VehicleAsync("C4");
        var dealer = await ProviderAsync("C4D", ProviderKind.Dealer);
        var station = await ProviderAsync("C4F", ProviderKind.FuelStation);
        var byMonths = await ItemAsync("C4M", ItemKind.Work, intervalKm: 50000, intervalMonths: 12);
        var byKm = await ItemAsync("C4K", ItemKind.Work, intervalKm: 5000, intervalMonths: 24);
        var notYet = await ItemAsync("C4N", ItemKind.Work, intervalKm: 60000, intervalMonths: 36);

        // Bought 11 months and 20 days ago at 10 000 km, now at 14 500 km: the 12-month job is due in about ten days (its
        // kilometres are far off), the 5 000 km job has 500 km left (its months are far off), the third has neither.
        await PurchaseAsync(vehicle, dealer, Today.AddMonths(-11).AddDays(-20), 500000m, 10000);
        await RefuelAsync(vehicle, station, Today.AddDays(-1), 14500, 40m);

        var report = await RunReportAsync<ReportUpcomingDto, UpcomingReportResult>(new ReportUpcomingDto { VehicleId = vehicle, LookAheadDays = 30, LookAheadKm = 1000 });
        var names = report.Data.Lines.Select(l => l.Description).ToList();

        Assert.Contains("Item C4M", names);
        Assert.Contains("Item C4K", names);
        Assert.DoesNotContain("Item C4N", names);
        Assert.Equal(500, report.Data.Lines.Single(l => l.Description == "Item C4K").RemainingKm);
        Assert.InRange(report.Data.Lines.Single(l => l.Description == "Item C4M").RemainingDays!.Value, 1, 30);
        _ = notYet;
    }

    [Fact]
    public async Task Check7_TotalCostOfOwnership_IsPriceMinusSalePlusCosts_InterestIncluded_PrincipalNot()
    {
        var vehicle = await VehicleAsync("C7");
        var dealer = await ProviderAsync("C7D", ProviderKind.Dealer);
        var bank = await ProviderAsync("C7B", ProviderKind.Bank);
        var station = await ProviderAsync("C7F", ProviderKind.FuelStation);
        var contract = await ContractAsync("LN-C7", vehicle, bank);

        await PurchaseAsync(vehicle, dealer, Today.AddMonths(-3), 400000m, 10000, VehiclePurchasePaymentKind.BankLoan, 100000m, contract);
        await PaymentAsync(contract, Today.AddMonths(-2), principal: 10000m, interest: 3000m, fees: 100m);
        await RefuelAsync(vehicle, station, Today.AddMonths(-1), 11000, 40m, amount: 2000m);
        await PostAsync<VehicleSaleDto, VehicleSaleQueryDto>(new VehicleSaleDto
        {
            Code = "SAL-C7", Date = At(Today.AddDays(-1)), VehicleId = vehicle, Buyer = "Neighbour", Price = 350000m, Odometer = 12000
        });

        var report = await RunReportAsync<ReportTotalCostOfOwnershipDto, TotalCostOfOwnershipReportResult>(new ReportTotalCostOfOwnershipDto
        {
            VehicleId = vehicle,
            Period = new DateRangeDto { From = At(Today.AddYears(-1)), To = At(Today) }
        });
        var line = Assert.Single(report.Data.Lines);

        Assert.Equal(400000m, line.PurchasePrice);
        Assert.Equal(3100m, line.InterestAndFees);
        Assert.Equal(350000m, line.SalePrice);
        Assert.Equal(400000m - 350000m + 3100m + 2000m, line.NetCost);
        Assert.Equal(2000, line.KmDriven);
        // The debt is shown beside the cost, not added to it: 300 000 financed, 10 000 of principal paid.
        Assert.Equal(290000m, line.DebtOwed);
    }

    [Fact]
    public async Task Check9_OnAnEmptyDatabase_EveryDocumentTypePosts_AndANewOneGetsANumberAndToday()
    {
        var vehicle = await VehicleAsync("C9");
        var provider = await ProviderAsync("C9P", ProviderKind.Other);
        var contract = await ContractAsync("LN-C9", vehicle, provider);

        async Task NewHasNumberAndToday<TDto, TQuery>() where TDto : DocumentDto where TQuery : DocumentQueryDto
        {
            var dto = await _host.InScopeAsync(sp => Docs<TDto, TQuery>(sp).NewAsync(null, null, Ct));
            Assert.False(string.IsNullOrWhiteSpace(dto.Code));
            Assert.Equal(Today, DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(dto.Date, DateTimeKind.Utc), Zone)));
        }

        await NewHasNumberAndToday<VehiclePurchaseDto, VehiclePurchaseQueryDto>();
        await NewHasNumberAndToday<FinancePaymentDto, FinancePaymentQueryDto>();
        await NewHasNumberAndToday<VehicleSaleDto, VehicleSaleQueryDto>();
        await NewHasNumberAndToday<RefuelDto, RefuelQueryDto>();
        await NewHasNumberAndToday<ServiceDto, ServiceQueryDto>();
        await NewHasNumberAndToday<AccidentDto, AccidentQueryDto>();
        await NewHasNumberAndToday<PolicyDto, PolicyQueryDto>();

        var start = Today.AddDays(-30);
        await PurchaseAsync(vehicle, provider, start, 300000m, 1000, VehiclePurchasePaymentKind.BankLoan, 50000m, contract);
        await PaymentAsync(contract, start.AddDays(5), 5000m, 2000m);
        await RefuelAsync(vehicle, provider, start.AddDays(6), 1500, 40m);
        await PostAsync<ServiceDto, ServiceQueryDto>(new ServiceDto
        {
            Code = "SRV-C9", Date = At(start.AddDays(7)), VehicleId = vehicle, ServiceStationId = provider, Odometer = 1600, Amount = 2500m
        });
        await PostAsync<AccidentDto, AccidentQueryDto>(new AccidentDto
        {
            Code = "ACC-C9", Date = At(start.AddDays(8)), VehicleId = vehicle, Odometer = 1700, Place = "Kyiv", Description = "Scratch", EstimatedDamage = 3000m
        });
        await PostAsync<PolicyDto, PolicyQueryDto>(new PolicyDto
        {
            Code = "POL-C9", Date = At(start.AddDays(9)), VehicleId = vehicle, ProviderId = provider, Kind = PolicyKind.CompulsoryInsurance,
            ValidFrom = start.AddDays(9), ValidTo = start.AddDays(9).AddYears(1), Amount = 1800m
        });
        await PostAsync<VehicleSaleDto, VehicleSaleQueryDto>(new VehicleSaleDto
        {
            Code = "SAL-C9", Date = At(start.AddDays(10)), VehicleId = vehicle, Buyer = "Buyer", Price = 280000m, Odometer = 1800
        });

        Assert.Equal(300000m + 2000m + 1000m + 2500m + 1800m - 280000m, await CostsAsync(vehicle));
        Assert.Equal(245000m, await DebtAsync(contract));
    }

    [Fact]
    public async Task Check11_DashboardFiguresEqualTheReports_ForAUserEastOfUtc()
    {
        var vehicle = await VehicleAsync("C11");
        var dealer = await ProviderAsync("C11D", ProviderKind.Dealer);
        var bank = await ProviderAsync("C11B", ProviderKind.Bank);
        var insurer = await ProviderAsync("C11I", ProviderKind.Insurer);
        var contract = await ContractAsync("LN-C11", vehicle, bank);
        var monthStart = new DateOnly(Today.Year, Today.Month, 1);

        // Bought on the 1st of this month at local midnight: still the previous month in UTC.
        await PurchaseAsync(vehicle, dealer, monthStart, 200000m, 5000, VehiclePurchasePaymentKind.BankLoan, 50000m, contract);
        await PaymentAsync(contract, monthStart, 2000m, interest: 1500m);
        // A policy that ended yesterday: one overdue event.
        await PostAsync<PolicyDto, PolicyQueryDto>(new PolicyDto
        {
            Code = "POL-C11", Date = At(monthStart), VehicleId = vehicle, ProviderId = insurer, Kind = PolicyKind.CompulsoryInsurance,
            ValidFrom = Today.AddYears(-1), ValidTo = Today.AddDays(-1), Amount = 1000m
        });

        var dashboard = await _host.InScopeAsync(sp =>
            sp.GetRequiredService<IDashboardService<GarageDashboardDto, GarageDashboardResultDto>>().RunAsync(new GarageDashboardDto(), Ct));

        var pivot = await RunReportAsync<ReportCostsPivotDto, CostsPivotReportResult>(new ReportCostsPivotDto
        {
            Period = new DateRangeDto { From = At(monthStart.AddMonths(-1)), To = At(Today) }
        });
        var thisMonth = monthStart.ToString("yyyy-MM");
        Assert.Equal(pivot.Data.Lines.Where(l => l.Month == thisMonth).Sum(l => l.Amount), dashboard.ThisMonthCosts.Value);
        Assert.Equal(200000m + 1500m + 1000m, dashboard.ThisMonthCosts.Value);
        Assert.DoesNotContain(pivot.Data.Lines, l => l.Month == monthStart.AddMonths(-1).ToString("yyyy-MM"));

        var financing = await RunReportAsync<ReportFinancingDto, FinancingReportResult>(new ReportFinancingDto { AsOf = At(Today) });
        Assert.Equal(financing.Data.Lines.Sum(l => l.DebtOwed), dashboard.TotalDebt.Value);
        Assert.Equal(148000m, dashboard.TotalDebt.Value);

        var upcoming = await RunReportAsync<ReportUpcomingDto, UpcomingReportResult>(new ReportUpcomingDto { LookAheadDays = 30, LookAheadKm = 1000 });
        Assert.Equal(upcoming.Data.Lines.Count(l => l.IsOverdue), (int)dashboard.OverdueEvents.Value!.Value);
        Assert.True(dashboard.OverdueEvents.Value >= 1);
    }

    // ---------------------------------------------------------------- rules the review found broken

    [Fact]
    public async Task ADetailedServiceWithTwoWorkItems_Posts_AndMovesLastDoneForBoth()
    {
        var vehicle = await VehicleAsync("R0");
        var station = await ProviderAsync("R0S", ProviderKind.ServiceStation);
        var oilChange = await ItemAsync("R0W1", ItemKind.Work, intervalKm: 10000, intervalMonths: 12);
        var filters = await ItemAsync("R0W2", ItemKind.Work, intervalKm: 15000, intervalMonths: 12);

        await PostAsync<ServiceDto, ServiceQueryDto>(new ServiceDto
        {
            Code = "SRV-R0", Date = At(Today.AddDays(-5)), VehicleId = vehicle, ServiceStationId = station, Odometer = 30000, Detailed = true,
            Lines = [new ServiceLineDto { ItemId = oilChange, Quantity = 1, Price = 600m }, new ServiceLineDto { ItemId = filters, Quantity = 1, Price = 400m }]
        });

        var lastDone = await _host.InScopeAsync(async sp => await sp.GetRequiredService<IInfoRegisterReader<LastDoneRecord>>()
            .SliceOfLast(DateTime.UtcNow, q => q.Where(r => r.Dimensions.VehicleId == vehicle), Ct));
        Assert.Equal(2, lastDone.Count);
        Assert.Equal(1000m, await CostsAsync(vehicle));
    }

    [Fact]
    public async Task APostedFinalPayment_CanBeSavedAgain()
    {
        var vehicle = await VehicleAsync("R1");
        var bank = await ProviderAsync("R1B", ProviderKind.LeasingCompany);
        var contract = await ContractAsync("LS-R1", vehicle, bank, FinanceContractKind.Leasing);
        await PurchaseAsync(vehicle, bank, Today.AddMonths(-2), 120000m, 100, VehiclePurchasePaymentKind.Leasing, 20000m, contract);
        var buyout = await PaymentAsync(contract, Today.AddDays(-3), principal: 100000m);

        await EditAsync<FinancePaymentDto, FinancePaymentQueryDto>(buyout, d =>
        {
            d.Comment = "Buyout";
        });

        Assert.Equal(0m, await DebtAsync(contract));
    }

    [Fact]
    public async Task AFinancedPurchaseChangedToCash_TakesBackItsDebt()
    {
        var vehicle = await VehicleAsync("R2");
        var bank = await ProviderAsync("R2B", ProviderKind.Bank);
        var contract = await ContractAsync("LN-R2", vehicle, bank);
        var purchase = await PurchaseAsync(vehicle, bank, Today.AddMonths(-1), 100000m, 100, VehiclePurchasePaymentKind.BankLoan, 30000m, contract);
        Assert.Equal(70000m, await DebtAsync(contract));

        await EditAsync<VehiclePurchaseDto, VehiclePurchaseQueryDto>(purchase, d =>
        {
            d.Payment = VehiclePurchasePaymentKind.Cash;
        });

        Assert.Equal(0m, await DebtAsync(contract));
    }

    [Fact]
    public async Task UnpostingAFinancedPurchaseWithPayments_IsRefusedWithAReadableMessage()
    {
        var vehicle = await VehicleAsync("R3");
        var bank = await ProviderAsync("R3B", ProviderKind.Bank);
        var contract = await ContractAsync("LN-R3", vehicle, bank);
        var purchase = await PurchaseAsync(vehicle, bank, Today.AddMonths(-2), 100000m, 100, VehiclePurchasePaymentKind.BankLoan, 0m, contract);
        await PaymentAsync(contract, Today.AddMonths(-1), 5000m);

        var error = await Assert.ThrowsAsync<InvalidStateException>(() =>
            _host.InScopeAsync(sp => Docs<VehiclePurchaseDto, VehiclePurchaseQueryDto>(sp).UnsubmitAsync(purchase, Ct)));
        Assert.DoesNotContain("Dimensions", error.Message);
        Assert.Equal(95000m, await DebtAsync(contract));
    }

    [Fact]
    public async Task APurchaseOnAnotherVehiclesContract_OrOfAnotherKind_IsRefused()
    {
        var vehicle = await VehicleAsync("R4");
        var other = await VehicleAsync("R4X");
        var bank = await ProviderAsync("R4B", ProviderKind.Bank);
        var othersContract = await ContractAsync("LN-R4X", other, bank);
        var leasing = await ContractAsync("LS-R4", vehicle, bank, FinanceContractKind.Leasing);

        await Assert.ThrowsAsync<InvalidStateException>(() =>
            PurchaseAsync(vehicle, bank, Today.AddDays(-5), 100000m, 100, VehiclePurchasePaymentKind.BankLoan, 0m, othersContract));
        await Assert.ThrowsAsync<InvalidStateException>(() =>
            PurchaseAsync(vehicle, bank, Today.AddDays(-5), 100000m, 100, VehiclePurchasePaymentKind.BankLoan, 0m, leasing));
    }

    [Fact]
    public async Task Reminders_AreSentOncePerOccurrence_AndAgainWhenTheJobIsDueTheNextTime()
    {
        var vehicle = await VehicleAsync("R5");
        var dealer = await ProviderAsync("R5D", ProviderKind.Dealer);
        var station = await ProviderAsync("R5S", ProviderKind.ServiceStation);
        var oilChange = await ItemAsync("R5W", ItemKind.Work, intervalKm: 10000);

        await PurchaseAsync(vehicle, dealer, Today.AddDays(-300), 300000m, 0);
        await RefuelAsync(vehicle, station, Today.AddDays(-200), 9500, 40m);
        Assert.Equal(1, (await RunRemindersAsync()).NotificationsSent);
        Assert.Equal(0, (await RunRemindersAsync()).NotificationsSent);

        await PostAsync<ServiceDto, ServiceQueryDto>(new ServiceDto
        {
            Code = "SRV-R5", Date = At(Today.AddDays(-100)), VehicleId = vehicle, ServiceStationId = station, Odometer = 9600, Detailed = true,
            Lines = [new ServiceLineDto { ItemId = oilChange, Quantity = 1, Price = 1000m }]
        });
        Assert.Equal(0, (await RunRemindersAsync()).NotificationsSent);

        await RefuelAsync(vehicle, station, Today.AddDays(-2), 19200, 40m);
        Assert.Equal(1, (await RunRemindersAsync()).NotificationsSent);
    }

    [Fact]
    public async Task FuelConsumption_CountsThePartialRefuelsBetweenTwoFullTanks()
    {
        var vehicle = await VehicleAsync("R6");
        var station = await ProviderAsync("R6F", ProviderKind.FuelStation);
        await RefuelAsync(vehicle, station, Today.AddDays(-20), 10000, 40m);
        await RefuelAsync(vehicle, station, Today.AddDays(-15), 10300, 20m, fullTank: false);
        await RefuelAsync(vehicle, station, Today.AddDays(-10), 10600, 25m);

        var report = await RunReportAsync<ReportFuelConsumptionDto, FuelConsumptionReportResult>(new ReportFuelConsumptionDto
        {
            VehicleId = vehicle,
            Period = new DateRangeDto { From = At(Today.AddDays(-30)), To = At(Today) }
        });

        // 20 + 25 litres over 600 km.
        Assert.Equal(7.5m, report.Data.PeriodAverageConsumption);
        Assert.Equal(7.5m, report.Data.Lines.First().Consumption);
    }
}
