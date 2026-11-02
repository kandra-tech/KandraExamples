using Kandra.Application.Abstractions.Services;
using Kandra.Application.Services.DataProcessors;
using Kandra.Domain.Exceptions;
using Kandra.Forms.Dashboards;
using Kandra.Forms.DataProcessors;
using Kandra.Forms.Querying;
using Kandra.Persistence.Abstractions.Registers;
using FamilyGarage.Domain.Entities.Dictionaries;
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

public sealed class AcceptanceTests : IDisposable
{
    private readonly FamilyGarageTestHost _host = new();
    private static readonly CancellationToken Ct = CancellationToken.None;

    public void Dispose() => _host.Dispose();

    private static DateTime Day(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    #region Service Accessors

    private static IDictionaryCrudService<VehicleDto, VehicleQueryDto> Vehicles(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<VehicleDto, VehicleQueryDto>>();

    private static IDictionaryCrudService<ProviderDto, ProviderQueryDto> Providers(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<ProviderDto, ProviderQueryDto>>();

    private static IDictionaryCrudService<FinanceContractDto, FinanceContractQueryDto> FinanceContracts(IServiceProvider sp) =>
        sp.GetRequiredService<IDictionaryCrudService<FinanceContractDto, FinanceContractQueryDto>>();

    private static IHierarchicalDictionaryCrudService<ItemNodeDto, ItemQueryDto> Items(IServiceProvider sp) =>
        sp.GetRequiredService<IHierarchicalDictionaryCrudService<ItemNodeDto, ItemQueryDto>>();

    private static IDocumentCrudService<VehiclePurchaseDto, VehiclePurchaseQueryDto> Purchases(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<VehiclePurchaseDto, VehiclePurchaseQueryDto>>();

    private static IDocumentCrudService<FinancePaymentDto, FinancePaymentQueryDto> Payments(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<FinancePaymentDto, FinancePaymentQueryDto>>();

    private static IDocumentCrudService<VehicleSaleDto, VehicleSaleQueryDto> Sales(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<VehicleSaleDto, VehicleSaleQueryDto>>();

    private static IDocumentCrudService<RefuelDto, RefuelQueryDto> Refuels(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<RefuelDto, RefuelQueryDto>>();

    private static IDocumentCrudService<ServiceDto, ServiceQueryDto> Services(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<ServiceDto, ServiceQueryDto>>();

    private static IDocumentCrudService<AccidentDto, AccidentQueryDto> Accidents(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<AccidentDto, AccidentQueryDto>>();

    private static IDocumentCrudService<PolicyDto, PolicyQueryDto> Policies(IServiceProvider sp) =>
        sp.GetRequiredService<IDocumentCrudService<PolicyDto, PolicyQueryDto>>();

    #endregion

    #region Helper Methods

    private async Task<Guid> CreateVehicleAsync(string make, string model, int year, string plate, string vin, FuelType fuelType)
    {
        var res = await _host.InScopeAsync(sp => Vehicles(sp).InsertAsync(new VehicleDto
        {
            Code = $"V-{plate}",
            Name = $"{make} {model}",
            Make = make,
            Model = model,
            Year = year,
            PlateNumber = plate,
            Vin = vin,
            FuelType = fuelType
        }, Ct));
        return res.Id;
    }

    private async Task<Guid> CreateProviderAsync(string name, ProviderKind kind, string phone = "+380501112233")
    {
        var res = await _host.InScopeAsync(sp => Providers(sp).InsertAsync(new ProviderDto
        {
            Code = $"P-{name[..Math.Min(3, name.Length)].ToUpperInvariant()}",
            Name = name,
            Kind = kind,
            Phone = phone
        }, Ct));
        return res.Id;
    }

    private async Task<Guid> CreateFinanceContractAsync(
        string number,
        Guid vehicleId,
        FinanceContractKind kind,
        Guid lenderId,
        DateTime signedOn,
        decimal rate,
        int termMonths,
        decimal monthlyPayment,
        int paymentDay)
    {
        var res = await _host.InScopeAsync(sp => FinanceContracts(sp).InsertAsync(new FinanceContractDto
        {
            Code = $"FC-{number}",
            Name = $"Contract {number}",
            Number = number,
            VehicleId = vehicleId,
            Kind = kind,
            LenderId = lenderId,
            SignedOn = signedOn,
            AnnualInterestRatePercent = rate,
            TermMonths = termMonths,
            MonthlyPayment = monthlyPayment,
            PaymentDayOfMonth = paymentDay
        }, Ct));
        return res.Id;
    }

    private async Task<Guid> CreateWorkItemAsync(string name, decimal defaultPrice, int intervalKm, int intervalMonths)
    {
        var res = await _host.InScopeAsync(sp => Items(sp).InsertAsync(new ItemDto
        {
            Code = $"ITM-{name[..Math.Min(3, name.Length)].ToUpperInvariant()}",
            Name = name,
            Kind = ItemKind.Work,
            Unit = "job",
            DefaultPrice = defaultPrice,
            ServiceIntervalKm = intervalKm,
            ServiceIntervalMonths = intervalMonths
        }, Ct));
        return res.Id;
    }

    private async Task<Guid> CreatePartItemAsync(string name, decimal defaultPrice)
    {
        var res = await _host.InScopeAsync(sp => Items(sp).InsertAsync(new ItemDto
        {
            Code = $"PART-{name[..Math.Min(3, name.Length)].ToUpperInvariant()}",
            Name = name,
            Kind = ItemKind.Part,
            Unit = "pcs",
            DefaultPrice = defaultPrice,
            ServiceIntervalKm = 0,
            ServiceIntervalMonths = 0
        }, Ct));
        return res.Id;
    }

    #endregion

    [Fact]
    public async Task Criterion1_FinancedVehiclePurchase_PostsOdometer_TurnoverCost_AndContractDebtBalance()
    {
        var vehicleId = await CreateVehicleAsync("Toyota", "RAV4", 2021, "AA1001BB", "JTMD1001", FuelType.Hybrid);
        var lenderId = await CreateProviderAsync("Oschadbank", ProviderKind.Bank);
        var contractId = await CreateFinanceContractAsync("LN-001", vehicleId, FinanceContractKind.BankLoan, lenderId, Day(2026, 1, 10), 12.5m, 36, 15000m, 15);

        var purchase = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new VehiclePurchaseDto
        {
            Code = "PUR-01",
            Date = Day(2026, 1, 15),
            VehicleId = vehicleId,
            SellerId = lenderId,
            Price = 900000m,
            Odometer = 35000,
            Payment = VehiclePurchasePaymentKind.BankLoan,
            DownPayment = 300000m,
            FinanceContractId = contractId
        }, Ct));

        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(purchase.Id, Ct));

        // 1. Odometer
        var odo = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IInfoRegisterReader<OdometerRecord>>();
            var slice = await reader.SliceOfLast(Day(2026, 1, 15), q => q.Where(r => r.Dimensions.VehicleId == vehicleId), Ct);
            return slice.FirstOrDefault()?.Details.Kilometres;
        });
        Assert.Equal(35000, odo);

        // 2. Vehicle costs turnover (full price 900 000, category Purchase)
        var cost = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>();
            var movs = await reader.Movements(Day(2026, 1, 1), Day(2026, 1, 31), q => q.Where(m => m.Dimensions.VehicleId == vehicleId && m.Dimensions.Category == CostCategory.Purchase), Ct);
            return movs.Sum(m => m.Resources.Amount);
        });
        Assert.Equal(900000m, cost);

        // 3. Finance debt balance (financed amount = price - down payment = 600 000)
        var debt = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IBalanceReader<FinanceDebtMovement, FinanceDebtBalance>>();
            var bals = await reader.Balances(q => q.Where(b => b.Dimensions.ContractId == contractId), Ct);
            return bals.Sum(b => b.Resources.Amount);
        });
        Assert.Equal(600000m, debt);
    }

    [Fact]
    public async Task Criterion2_FinancePayment_PostsTurnoverCost_AndDecreasesDebtBalance()
    {
        var vehicleId = await CreateVehicleAsync("Toyota", "RAV4", 2021, "AA1002BB", "JTMD1002", FuelType.Hybrid);
        var lenderId = await CreateProviderAsync("PrivatBank", ProviderKind.Bank);
        var contractId = await CreateFinanceContractAsync("LN-002", vehicleId, FinanceContractKind.BankLoan, lenderId, Day(2026, 1, 10), 12m, 36, 18000m, 15);

        var purchase = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new VehiclePurchaseDto
        {
            Code = "PUR-02",
            Date = Day(2026, 1, 15),
            VehicleId = vehicleId,
            SellerId = lenderId,
            Price = 500000m,
            Odometer = 40000,
            Payment = VehiclePurchasePaymentKind.BankLoan,
            DownPayment = 100000m,
            FinanceContractId = contractId
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(purchase.Id, Ct));

        // Initial debt is 400 000. Post finance payment: principal 15 000, interest 3 000, fees 500. Total = 18 500.
        var payment = await _host.InScopeAsync(sp => Payments(sp).InsertAsync(new FinancePaymentDto
        {
            Code = "FP-01",
            Date = Day(2026, 2, 15),
            FinanceContractId = contractId,
            Principal = 15000m,
            Interest = 3000m,
            Fees = 500m,
            Comment = "Month 1 payment"
        }, Ct));
        await _host.InScopeAsync(sp => Payments(sp).SubmitAsync(payment.Id, Ct));

        // 1. Debt balance is reduced by principal only (400 000 - 15 000 = 385 000)
        var debt = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IBalanceReader<FinanceDebtMovement, FinanceDebtBalance>>();
            var bals = await reader.Balances(q => q.Where(b => b.Dimensions.ContractId == contractId), Ct);
            return bals.Sum(b => b.Resources.Amount);
        });
        Assert.Equal(385000m, debt);

        // 2. Vehicle costs turnover receives ONLY interest + fees = 3 500 (Category = InterestAndFees), NOT principal
        var interestCost = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>();
            var movs = await reader.Movements(Day(2026, 2, 1), Day(2026, 2, 28), q => q.Where(m => m.Dimensions.VehicleId == vehicleId && m.Dimensions.Category == CostCategory.InterestAndFees), Ct);
            return movs.Sum(m => m.Resources.Amount);
        });
        Assert.Equal(3500m, interestCost);
    }

    [Fact]
    public async Task Criterion3_VehicleSale_PostsOdometer_ReducesOwnershipCost_DebtRemains()
    {
        var vehicleId = await CreateVehicleAsync("Honda", "CR-V", 2019, "AA1003BB", "JTMD1003", FuelType.Petrol);
        var lenderId = await CreateProviderAsync("CreditAgricole", ProviderKind.Bank);
        var contractId = await CreateFinanceContractAsync("LN-003", vehicleId, FinanceContractKind.BankLoan, lenderId, Day(2026, 1, 10), 10m, 24, 10000m, 10);

        var purchase = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new VehiclePurchaseDto
        {
            Code = "PUR-03",
            Date = Day(2026, 1, 15),
            VehicleId = vehicleId,
            SellerId = lenderId,
            Price = 600000m,
            Odometer = 70000,
            Payment = VehiclePurchasePaymentKind.BankLoan,
            DownPayment = 200000m,
            FinanceContractId = contractId
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(purchase.Id, Ct));

        // Sell the vehicle: price 550 000, odometer 78 000
        var sale = await _host.InScopeAsync(sp => Sales(sp).InsertAsync(new VehicleSaleDto
        {
            Code = "SALE-01",
            Date = Day(2026, 6, 20),
            VehicleId = vehicleId,
            Buyer = "Petro Poroshenko",
            Price = 550000m,
            Odometer = 78000
        }, Ct));
        await _host.InScopeAsync(sp => Sales(sp).SubmitAsync(sale.Id, Ct));

        // 1. Odometer is updated to 78 000
        var odo = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IInfoRegisterReader<OdometerRecord>>();
            var slice = await reader.SliceOfLast(Day(2026, 6, 20), q => q.Where(r => r.Dimensions.VehicleId == vehicleId), Ct);
            return slice.FirstOrDefault()?.Details.Kilometres;
        });
        Assert.Equal(78000, odo);

        // 2. Vehicle sale movement is registered with IsSale=true, reducing net cost
        var saleReduction = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>();
            var movs = await reader.Movements(Day(2026, 6, 1), Day(2026, 6, 30), q => q.Where(m => m.Dimensions.VehicleId == vehicleId && m.Details.IsSale), Ct);
            return movs.Sum(m => m.Resources.Amount);
        });
        Assert.Equal(550000m, saleReduction);

        // 3. Contract debt balance remains (still 400 000)
        var debt = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IBalanceReader<FinanceDebtMovement, FinanceDebtBalance>>();
            var bals = await reader.Balances(q => q.Where(b => b.Dimensions.ContractId == contractId), Ct);
            return bals.Sum(b => b.Resources.Amount);
        });
        Assert.Equal(400000m, debt);
    }

    [Fact]
    public async Task Criterion4_Refuel_PostsOdometer_AndFuelCost()
    {
        var vehicleId = await CreateVehicleAsync("Volkswagen", "Golf", 2020, "AA1004BB", "WVW1004", FuelType.Diesel);
        var stationId = await CreateProviderAsync("OKKO", ProviderKind.FuelStation);

        var refuel = await _host.InScopeAsync(sp => Refuels(sp).InsertAsync(new RefuelDto
        {
            Code = "REF-01",
            Date = Day(2026, 3, 5),
            VehicleId = vehicleId,
            FuelStationId = stationId,
            Litres = 45m,
            Amount = 2475m,
            Odometer = 42500,
            FullTank = true
        }, Ct));
        await _host.InScopeAsync(sp => Refuels(sp).SubmitAsync(refuel.Id, Ct));

        // 1. Odometer
        var odo = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IInfoRegisterReader<OdometerRecord>>();
            var slice = await reader.SliceOfLast(Day(2026, 3, 5), q => q.Where(r => r.Dimensions.VehicleId == vehicleId), Ct);
            return slice.FirstOrDefault()?.Details.Kilometres;
        });
        Assert.Equal(42500, odo);

        // 2. Turnover cost (Category = Fuel, Litres = 45)
        var fuelCost = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>();
            var movs = await reader.Movements(Day(2026, 3, 1), Day(2026, 3, 31), q => q.Where(m => m.Dimensions.VehicleId == vehicleId && m.Dimensions.Category == CostCategory.Fuel), Ct);
            return movs.FirstOrDefault();
        });
        Assert.NotNull(fuelCost);
        Assert.Equal(2475m, fuelCost.Resources.Amount);
        Assert.Equal(45m, fuelCost.Details.Litres);
        Assert.True(fuelCost.Details.FullTank);
    }

    [Fact]
    public async Task Criterion5_SummaryService_PostsOdometer_AndMaintenanceCost()
    {
        var vehicleId = await CreateVehicleAsync("Skoda", "Octavia", 2022, "AA1005BB", "TMBA1005", FuelType.Petrol);
        var stationId = await CreateProviderAsync("Bosch Service", ProviderKind.ServiceStation);

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "SRV-01",
            Date = Day(2026, 4, 10),
            VehicleId = vehicleId,
            ServiceStationId = stationId,
            Kind = ServiceKind.Maintenance,
            Odometer = 30000,
            Detailed = false,
            Amount = 3800m,
            Comment = "Regular checkup"
        }, Ct));
        await _host.InScopeAsync(sp => Services(sp).SubmitAsync(service.Id, Ct));

        // 1. Odometer
        var odo = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IInfoRegisterReader<OdometerRecord>>();
            var slice = await reader.SliceOfLast(Day(2026, 4, 10), q => q.Where(r => r.Dimensions.VehicleId == vehicleId), Ct);
            return slice.FirstOrDefault()?.Details.Kilometres;
        });
        Assert.Equal(30000, odo);

        // 2. Cost turnover (Category = Maintenance)
        var maintCost = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>();
            var movs = await reader.Movements(Day(2026, 4, 1), Day(2026, 4, 30), q => q.Where(m => m.Dimensions.VehicleId == vehicleId && m.Dimensions.Category == CostCategory.Maintenance), Ct);
            return movs.Sum(m => m.Resources.Amount);
        });
        Assert.Equal(3800m, maintCost);

        // 3. Service lines turnover has NO rows for summary service
        var linesCount = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<ServiceLinesMovement>>();
            var movs = await reader.Movements(Day(2026, 4, 1), Day(2026, 4, 30), q => q.Where(m => m.Dimensions.VehicleId == vehicleId), Ct);
            return movs.Count;
        });
        Assert.Equal(0, linesCount);
    }

    [Fact]
    public async Task Criterion6_DetailedService_PostsServiceLines_AndUpdatesLastDone_ForIntervalItems()
    {
        var vehicleId = await CreateVehicleAsync("BMW", "X3", 2023, "AA1006BB", "WBAX1006", FuelType.Diesel);
        var stationId = await CreateProviderAsync("BMW Service Official", ProviderKind.ServiceStation);
        var workItemId = await CreateWorkItemAsync("Oil change", 1200m, intervalKm: 10000, intervalMonths: 12);
        var partItemId = await CreatePartItemAsync("Engine Oil 5W30", 400m);

        var service = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "SRV-02",
            Date = Day(2026, 4, 15),
            VehicleId = vehicleId,
            ServiceStationId = stationId,
            Kind = ServiceKind.Maintenance,
            Odometer = 20000,
            Detailed = true,
            Lines =
            [
                new ServiceLineDto { ItemId = workItemId, Quantity = 1, Price = 1200m, Amount = 1200m },
                new ServiceLineDto { ItemId = partItemId, Quantity = 5, Price = 400m, Amount = 2000m }
            ]
        }, Ct));
        await _host.InScopeAsync(sp => Services(sp).SubmitAsync(service.Id, Ct));

        // 1. Vehicle costs turnover sum is 3200
        var totalCost = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>();
            var movs = await reader.Movements(Day(2026, 4, 1), Day(2026, 4, 30), q => q.Where(m => m.Dimensions.VehicleId == vehicleId && m.Dimensions.Category == CostCategory.Maintenance), Ct);
            return movs.Sum(m => m.Resources.Amount);
        });
        Assert.Equal(3200m, totalCost);

        // 2. Service lines register contains both lines
        var serviceLines = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<ServiceLinesMovement>>();
            return await reader.Movements(Day(2026, 4, 1), Day(2026, 4, 30), q => q.Where(m => m.Dimensions.VehicleId == vehicleId), Ct);
        });
        Assert.Equal(2, serviceLines.Count);

        // 3. LastDone periodic info register has entry for work item (with interval), but NOT for part item
        var workLastDone = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IInfoRegisterReader<LastDoneRecord>>();
            var slice = await reader.SliceOfLast(Day(2026, 4, 15), q => q.Where(r => r.Dimensions.VehicleId == vehicleId && r.Dimensions.ItemId == workItemId), Ct);
            return slice.FirstOrDefault();
        });
        Assert.NotNull(workLastDone);
        Assert.Equal(20000, workLastDone.Details.Odometer);

        var partLastDone = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IInfoRegisterReader<LastDoneRecord>>();
            var slice = await reader.SliceOfLast(Day(2026, 4, 15), q => q.Where(r => r.Dimensions.VehicleId == vehicleId && r.Dimensions.ItemId == partItemId), Ct);
            return slice.FirstOrDefault();
        });
        Assert.Null(partLastDone);
    }

    [Fact]
    public async Task Criterion7_Accident_RecordsDamage_AndAllowsLinkedRepairService()
    {
        var vehicleId = await CreateVehicleAsync("Audi", "A6", 2021, "AA1007BB", "WAU1007", FuelType.Petrol);
        var stationId = await CreateProviderAsync("Audi Body Shop", ProviderKind.ServiceStation);

        var accident = await _host.InScopeAsync(sp => Accidents(sp).InsertAsync(new AccidentDto
        {
            Code = "ACC-01",
            Date = Day(2026, 5, 10),
            VehicleId = vehicleId,
            Odometer = 45000,
            Place = "Kyiv, Khreshchatyk",
            Description = "Front bumper damage",
            EstimatedDamage = 25000m
        }, Ct));
        await _host.InScopeAsync(sp => Accidents(sp).SubmitAsync(accident.Id, Ct));

        // Accident posts odometer reading
        var odo = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IInfoRegisterReader<OdometerRecord>>();
            var slice = await reader.SliceOfLast(Day(2026, 5, 10), q => q.Where(r => r.Dimensions.VehicleId == vehicleId), Ct);
            return slice.FirstOrDefault()?.Details.Kilometres;
        });
        Assert.Equal(45000, odo);

        // Create linked repair Service
        var repairService = await _host.InScopeAsync(sp => Services(sp).InsertAsync(new ServiceDto
        {
            Code = "SRV-REP-01",
            Date = Day(2026, 5, 15),
            VehicleId = vehicleId,
            ServiceStationId = stationId,
            Kind = ServiceKind.Repair,
            Odometer = 45050,
            Detailed = false,
            Amount = 24000m,
            AccidentId = accident.Id,
            Comment = "Bumper repair following accident"
        }, Ct));
        await _host.InScopeAsync(sp => Services(sp).SubmitAsync(repairService.Id, Ct));

        // Check repair cost turnover
        var repairCost = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>();
            var movs = await reader.Movements(Day(2026, 5, 1), Day(2026, 5, 31), q => q.Where(m => m.Dimensions.VehicleId == vehicleId && m.Dimensions.Category == CostCategory.Repair), Ct);
            return movs.Sum(m => m.Resources.Amount);
        });
        Assert.Equal(24000m, repairCost);
    }

    [Fact]
    public async Task Criterion8_Policy_PostsPolicyValidity_AndCost()
    {
        var vehicleId = await CreateVehicleAsync("Mazda", "CX-5", 2022, "AA1008BB", "JMZ1008", FuelType.Petrol);
        var insurerId = await CreateProviderAsync("ARX Insurance", ProviderKind.Insurer);

        var policy = await _host.InScopeAsync(sp => Policies(sp).InsertAsync(new PolicyDto
        {
            Code = "POL-01",
            Date = Day(2026, 1, 1),
            VehicleId = vehicleId,
            Kind = PolicyKind.CompulsoryInsurance,
            ProviderId = insurerId,
            ValidFrom = new DateOnly(2026, 1, 1),
            ValidTo = new DateOnly(2027, 1, 1),
            Amount = 2800m
        }, Ct));
        await _host.InScopeAsync(sp => Policies(sp).SubmitAsync(policy.Id, Ct));

        // 1. Policy validity periodic info register
        var validity = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<IInfoRegisterReader<PolicyValidityRecord>>();
            var slice = await reader.SliceOfLast(Day(2026, 1, 1), q => q.Where(r => r.Dimensions.VehicleId == vehicleId && r.Dimensions.PolicyKind == PolicyKind.CompulsoryInsurance), Ct);
            return slice.FirstOrDefault()?.Details.ValidTo;
        });
        Assert.Equal(Day(2027, 1, 1), validity);

        // 2. Cost turnover has 2800 for Category = Insurance
        var policyCost = await _host.InScopeAsync(async sp =>
        {
            var reader = sp.GetRequiredService<ITurnoverReader<VehicleCostsMovement>>();
            var movs = await reader.Movements(Day(2026, 1, 1), Day(2026, 1, 31), q => q.Where(m => m.Dimensions.VehicleId == vehicleId && m.Dimensions.Category == CostCategory.Insurance), Ct);
            return movs.Sum(m => m.Resources.Amount);
        });
        Assert.Equal(2800m, policyCost);
    }

    [Fact]
    public async Task Criterion9_PostingGuards_EnforceDomainRules()
    {
        var vehicleId = await CreateVehicleAsync("Volvo", "XC60", 2022, "AA1009BB", "YV11009", FuelType.Hybrid);
        var stationId = await CreateProviderAsync("WOG", ProviderKind.FuelStation);
        var bankId = await CreateProviderAsync("UkrSibbank", ProviderKind.Bank);
        var contractId = await CreateFinanceContractAsync("LN-009", vehicleId, FinanceContractKind.BankLoan, bankId, Day(2026, 1, 10), 11m, 36, 12000m, 10);

        // Guard 9a: Odometer reading lower than previous before document date throws InvalidStateException
        var refuel1 = await _host.InScopeAsync(sp => Refuels(sp).InsertAsync(new RefuelDto
        {
            Code = "REF-9A1",
            Date = Day(2026, 1, 10),
            VehicleId = vehicleId,
            FuelStationId = stationId,
            Litres = 40m,
            Amount = 2200m,
            Odometer = 50000,
            FullTank = true
        }, Ct));
        await _host.InScopeAsync(sp => Refuels(sp).SubmitAsync(refuel1.Id, Ct));

        var refuel2 = await _host.InScopeAsync(sp => Refuels(sp).InsertAsync(new RefuelDto
        {
            Code = "REF-9A2",
            Date = Day(2026, 1, 15),
            VehicleId = vehicleId,
            FuelStationId = stationId,
            Litres = 35m,
            Amount = 1900m,
            Odometer = 49000, // lower than 50000!
            FullTank = true
        }, Ct));
        await _host.InScopeAsync(async sp =>
        {
            await Assert.ThrowsAsync<InvalidStateException>(() => Refuels(sp).SubmitAsync(refuel2.Id, Ct));
        });

        // Guard 9b & 9c: Financed purchase with payments cannot be un-posted, and principal > debt is rejected
        var purchase = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new VehiclePurchaseDto
        {
            Code = "PUR-9BC",
            Date = Day(2026, 1, 20),
            VehicleId = vehicleId,
            SellerId = bankId,
            Price = 400000m,
            Odometer = 51000,
            Payment = VehiclePurchasePaymentKind.BankLoan,
            DownPayment = 100000m,
            FinanceContractId = contractId
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(purchase.Id, Ct)); // Financed amount = 300 000

        // Guard 9c: Principal payment larger than remaining debt (300 000) throws InvalidStateException
        var excessivePayment = await _host.InScopeAsync(sp => Payments(sp).InsertAsync(new FinancePaymentDto
        {
            Code = "FP-EXCESS",
            Date = Day(2026, 2, 10),
            FinanceContractId = contractId,
            Principal = 350000m, // > 300 000!
            Interest = 1000m,
            Fees = 100m
        }, Ct));
        await _host.InScopeAsync(async sp =>
        {
            await Assert.ThrowsAsync<InvalidStateException>(() => Payments(sp).SubmitAsync(excessivePayment.Id, Ct));
        });

        // Post valid payment
        var validPayment = await _host.InScopeAsync(sp => Payments(sp).InsertAsync(new FinancePaymentDto
        {
            Code = "FP-VALID",
            Date = Day(2026, 2, 10),
            FinanceContractId = contractId,
            Principal = 50000m,
            Interest = 2000m,
            Fees = 200m
        }, Ct));
        await _host.InScopeAsync(sp => Payments(sp).SubmitAsync(validPayment.Id, Ct));

        // Guard 9b: Un-posting purchase would reduce debt balance below zero (by 300 000 while remaining debt is 250 000) -> throws
        await _host.InScopeAsync(async sp =>
        {
            await Assert.ThrowsAnyAsync<Exception>(() => Purchases(sp).UnsubmitAsync(purchase.Id, Ct));
        });
    }

    [Fact]
    public async Task Criterion10_ReportsAndDashboard_ComputeCorrectMetrics()
    {
        var vehicleId = await CreateVehicleAsync("Hyundai", "Tucson", 2022, "AA1010BB", "KMH1010", FuelType.Petrol);
        var stationId = await CreateProviderAsync("Socar", ProviderKind.FuelStation);
        var workId = await CreateWorkItemAsync("Cabin filter", 500m, intervalKm: 15000, intervalMonths: 12);
        var srvStationId = await CreateProviderAsync("Official Hyundai", ProviderKind.ServiceStation);

        // 1. Initial purchase
        var purchase = await _host.InScopeAsync(sp => Purchases(sp).InsertAsync(new VehiclePurchaseDto
        {
            Code = "PUR-RPT",
            Date = Day(2026, 1, 1),
            VehicleId = vehicleId,
            SellerId = srvStationId,
            Price = 700000m,
            Odometer = 10000,
            Payment = VehiclePurchasePaymentKind.Cash
        }, Ct));
        await _host.InScopeAsync(sp => Purchases(sp).SubmitAsync(purchase.Id, Ct));

        // 2. Refuel 1: 50L, full tank, 10500 km
        var refuel1 = await _host.InScopeAsync(sp => Refuels(sp).InsertAsync(new RefuelDto
        {
            Code = "REF-RPT1",
            Date = Day(2026, 1, 10),
            VehicleId = vehicleId,
            FuelStationId = stationId,
            Litres = 50m,
            Amount = 2800m,
            Odometer = 10500,
            FullTank = true
        }, Ct));
        await _host.InScopeAsync(sp => Refuels(sp).SubmitAsync(refuel1.Id, Ct));

        // 3. Refuel 2: 45L, full tank, 11000 km -> 45L for 500 km = 9.0 L/100km
        var refuel2 = await _host.InScopeAsync(sp => Refuels(sp).InsertAsync(new RefuelDto
        {
            Code = "REF-RPT2",
            Date = Day(2026, 1, 20),
            VehicleId = vehicleId,
            FuelStationId = stationId,
            Litres = 45m,
            Amount = 2520m,
            Odometer = 11000,
            FullTank = true
        }, Ct));
        await _host.InScopeAsync(sp => Refuels(sp).SubmitAsync(refuel2.Id, Ct));

        // Test FuelConsumption report
        var fuelReportResult = await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportFuelConsumptionDto, FuelConsumptionReportResult>>();
            return await reportService.RunAsync(new ReportFuelConsumptionDto
            {
                VehicleId = vehicleId,
                Period = new DateRangeDto { From = Day(2026, 1, 1), To = Day(2026, 1, 31) }
            }, Ct);
        });
        Assert.NotNull(fuelReportResult);
        Assert.NotEmpty(fuelReportResult.Data.Lines);
        Assert.Equal(9.0m, fuelReportResult.Data.Lines.First().Consumption);

        // Test TCO Report
        var tcoResult = await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportTotalCostOfOwnershipDto, TotalCostOfOwnershipReportResult>>();
            return await reportService.RunAsync(new ReportTotalCostOfOwnershipDto
            {
                VehicleId = vehicleId,
                Period = new DateRangeDto { From = Day(2026, 1, 1), To = Day(2026, 1, 31) }
            }, Ct);
        });
        Assert.NotNull(tcoResult);
        var tcoLine = tcoResult.Data.Lines.FirstOrDefault(l => l.Vehicle == "Hyundai Tucson");
        Assert.NotNull(tcoLine);
        Assert.Equal(700000m, tcoLine.PurchasePrice);
        Assert.Equal(1000, tcoLine.KmDriven); // from 10000 to 11000

        // Test CostsPivot Report
        var pivotResult = await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportCostsPivotDto, CostsPivotReportResult>>();
            return await reportService.RunAsync(new ReportCostsPivotDto
            {
                Period = new DateRangeDto { From = Day(2026, 1, 1), To = Day(2026, 1, 31) }
            }, Ct);
        });
        Assert.NotNull(pivotResult);
        Assert.NotEmpty(pivotResult.Data.Lines);

        // Test Upcoming Report
        var upcomingResult = await _host.InScopeAsync(async sp =>
        {
            var reportService = sp.GetRequiredService<IReportService<ReportUpcomingDto, UpcomingReportResult>>();
            return await reportService.RunAsync(new ReportUpcomingDto
            {
                VehicleId = vehicleId,
                LookAheadDays = 30,
                LookAheadKm = 1000
            }, Ct);
        });
        Assert.NotNull(upcomingResult);

        // Test GarageDashboard
        var dashboardResult = await _host.InScopeAsync(async sp =>
        {
            var dashboardService = sp.GetRequiredService<IDashboardService<GarageDashboardDto, GarageDashboardResultDto>>();
            return await dashboardService.RunAsync(new GarageDashboardDto(), Ct);
        });
        Assert.NotNull(dashboardResult);
        Assert.NotEmpty(dashboardResult.VehiclesTable.Rows);
    }

    [Fact]
    public async Task Criterion11_UpcomingReminders_DataProcessor_RunsAndSendsNotifications()
    {
        var vehicleId = await CreateVehicleAsync("Kia", "Sportage", 2023, "AA1011BB", "KNAP1011", FuelType.Petrol);
        var insurerId = await CreateProviderAsync("TAS Insurance", ProviderKind.Insurer);

        // Expiring policy within 10 days
        var policy = await _host.InScopeAsync(sp => Policies(sp).InsertAsync(new PolicyDto
        {
            Code = "POL-REM",
            Date = Day(2025, 1, 1),
            VehicleId = vehicleId,
            Kind = PolicyKind.CompulsoryInsurance,
            ProviderId = insurerId,
            ValidFrom = new DateOnly(2025, 1, 1),
            ValidTo = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
            Amount = 1500m
        }, Ct));
        await _host.InScopeAsync(sp => Policies(sp).SubmitAsync(policy.Id, Ct));

        // Run UpcomingReminders data processor
        var processorResult = await _host.InScopeAsync(async sp =>
        {
            var processor = sp.GetRequiredService<IDataProcessorService<UpcomingRemindersDto, UpcomingRemindersResult>>();
            return await processor.ProcessAsync(new UpcomingRemindersDto
            {
                LookAheadDays = 30,
                LookAheadKm = 1000
            }, Ct);
        });

        Assert.NotNull(processorResult);
        Assert.True(processorResult.NotificationsSent >= 1);

        // Re-running data processor should not send duplicate notifications for the same event key
        var secondRunResult = await _host.InScopeAsync(async sp =>
        {
            var processor = sp.GetRequiredService<IDataProcessorService<UpcomingRemindersDto, UpcomingRemindersResult>>();
            return await processor.ProcessAsync(new UpcomingRemindersDto
            {
                LookAheadDays = 30,
                LookAheadKm = 1000
            }, Ct);
        });

        Assert.NotNull(secondRunResult);
        // Due to DeduplicationKey, previously sent notifications are skipped
        Assert.Equal(0, secondRunResult.NotificationsSent);
    }
}
