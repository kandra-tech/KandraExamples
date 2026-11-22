using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Commons;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Domain.Entities.Registers;
using PersonalAccounting.Domain.Reporting;
using PersonalAccounting.Enums;
using PersonalAccounting.Persistence.Contexts;
using Kandra.Domain.Entities.Documents;
using Kandra.Persistence.Abstractions.Registers;

namespace PersonalAccounting.Persistence.Repositories;

internal class PersonalAccountingReportsRepository(
    PersonalAccountingDbContext db,
    IBalanceReader<AccountBalanceMovement, AccountBalancesBalance> accountReader,
    ITurnoverReader<CategoryTurnoverMovement> turnoverReader,
    IInfoRegisterReader<BudgetPlanRecord> budgetReader,
    IInfoRegisterReader<ExchangeRatesRecord> rateReader) : IReportsRepository
{
    // The registers are read through their readers (the table also keeps the rows of un-posted and deleted documents);
    // a reader takes a period, so "no bound" is the widest one.
    private static readonly DateTime Earliest = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Latest = new(9000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private sealed record TurnoverRow(Guid CategoryId, Guid AccountId, DateTime Period, decimal Amount, decimal AmountInBase);

    private sealed record AccountAmount(Guid AccountId, decimal Amount);

    private Task<List<TurnoverRow>> TurnoverRowsAsync(
        DateTime from,
        DateTime to,
        Func<IQueryable<CategoryTurnoverMovement>, IQueryable<CategoryTurnoverMovement>> adjust,
        CancellationToken ct)
    {
        return turnoverReader.Movements(from, to, q => adjust(q).Select(t => new TurnoverRow(
            t.Dimensions.CategoryId,
            t.Dimensions.AccountId,
            t.Period,
            t.Resources.AmountInAccountCurrency,
            t.Resources.AmountInBaseCurrency)), ct);
    }

    private Task<Dictionary<Guid, (CategoryKind Kind, string Name)>> CategoriesAsync(CancellationToken ct)
    {
        return db.Dict_Categories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => (c.Kind, c.Name), ct);
    }

    public async Task<IReadOnlyList<TransactionJournalRow>> GetTransactionJournalAsync(
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyList<Guid>? accountIds,
        IReadOnlyList<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        var movements = await accountReader.Movements(fromDate ?? Earliest, toDate ?? Latest,
            q =>
            {
                if (accountIds != null && accountIds.Count > 0)
                    q = q.Where(m => accountIds.Contains(m.Dimensions.AccountId));
                if (categoryIds != null && categoryIds.Count > 0)
                    q = q.Where(m => m.Details.CategoryId.HasValue && categoryIds.Contains(m.Details.CategoryId.Value));
                return q.OrderBy(m => m.Period).ThenBy(m => m.Id);
            }, cancellationToken);

        if (movements.Count == 0)
            return [];

        var accIds = movements.Select(m => m.Dimensions.AccountId).Distinct().ToList();
        var catIds = movements.Where(m => m.Details.CategoryId.HasValue).Select(m => m.Details.CategoryId!.Value).Distinct().ToList();
        var docIds = movements.Select(m => m.DocumentId).Distinct().ToList();

        var accounts = await db.Dict_Accounts.AsNoTracking().Where(a => accIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name, cancellationToken);
        var categories = await db.Dict_Categories.AsNoTracking().Where(c => catIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);
        var documents = await db.Set<DocumentBase>().AsNoTracking().Where(d => docIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => new { d.Code, d.DocumentTypeId }, cancellationToken);

        var result = new List<TransactionJournalRow>(movements.Count);
        foreach (var m in movements)
        {
            documents.TryGetValue(m.DocumentId, out var docInfo);
            accounts.TryGetValue(m.Dimensions.AccountId, out var accName);
            string? catName = null;
            if (m.Details.CategoryId.HasValue)
            {
                categories.TryGetValue(m.Details.CategoryId.Value, out catName);
            }

            result.Add(new TransactionJournalRow
            {
                Id = m.Id,
                Date = m.Period,
                DocumentId = m.DocumentId,
                DocumentTypeId = docInfo?.DocumentTypeId,
                DocumentCode = docInfo?.Code ?? "",
                DocumentType = "",
                AccountId = m.Dimensions.AccountId,
                AccountName = accName ?? m.Dimensions.AccountId.ToString(),
                CategoryId = m.Details.CategoryId,
                CategoryName = catName,
                Inflow = !m.IsExpense ? m.Resources.Amount : null,
                Outflow = m.IsExpense ? m.Resources.Amount : null,
                Comment = m.Details.Comment
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<BudgetVsActualRow>> GetBudgetVsActualAsync(
        LocalMonth month,
        CancellationToken cancellationToken)
    {
        var planPeriod = month.PlanPeriod;
        var start = month.FromUtc;
        var end = month.ToUtc;

        var plans = (await budgetReader.Records(planPeriod, planPeriod, q => q, cancellationToken))
            .GroupBy(b => b.Dimensions.CategoryId)
            .Select(g => new { CategoryId = g.Key, Planned = g.Sum(x => x.Details.PlannedAmount) })
            .ToList();

        // Turnovers in that month for expenses
        var kinds = await CategoriesAsync(cancellationToken);
        var turnovers = (await TurnoverRowsAsync(start, Latest, q => q.Where(t => t.Period < end), cancellationToken))
            .Where(t => kinds.TryGetValue(t.CategoryId, out var k) && k.Kind == CategoryKind.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new { CategoryId = g.Key, Actual = g.Sum(x => x.AmountInBase) })
            .ToList();

        var planDict = plans.ToDictionary(p => p.CategoryId, p => p.Planned);
        var turnoverDict = turnovers.ToDictionary(t => t.CategoryId, t => t.Actual);

        var allCatIds = planDict.Keys.Union(turnoverDict.Keys).Distinct().ToList();
        var categories = await db.Dict_Categories.AsNoTracking()
            .Where(c => allCatIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var rows = new List<BudgetVsActualRow>();
        foreach (var catId in allCatIds)
        {
            planDict.TryGetValue(catId, out var planned);
            turnoverDict.TryGetValue(catId, out var actual);
            categories.TryGetValue(catId, out var catName);

            var remaining = planned - actual;
            var percent = planned > 0 ? decimal.Round(actual / planned * 100m, 1, MidpointRounding.AwayFromZero) : 0m;
            var overspent = actual > planned;

            rows.Add(new BudgetVsActualRow
            {
                Id = catId,
                CategoryId = catId,
                CategoryName = catName ?? catId.ToString(),
                PlannedAmount = planned,
                ActualAmount = actual,
                RemainingAmount = remaining,
                PercentUsed = percent,
                IsOverspent = overspent
            });
        }

        return rows.OrderBy(r => r.CategoryName).ToList();
    }

    public async Task<IReadOnlyList<SpendingByCategoryRow>> GetSpendingByCategoryAsync(
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyList<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        var rows = await TurnoverRowsAsync(fromDate ?? Earliest, toDate ?? Latest,
            q => categoryIds != null && categoryIds.Count > 0
                ? q.Where(t => categoryIds.Contains(t.Dimensions.CategoryId))
                : q, cancellationToken);

        var categories = await CategoriesAsync(cancellationToken);
        var accounts = await (
            from a in db.Dict_Accounts.AsNoTracking()
            join cur in db.Dict_Currencies.AsNoTracking() on a.CurrencyId equals cur.Id
            select new { a.Id, a.Name, CurrencyCode = cur.Code }
        ).ToDictionaryAsync(a => a.Id, cancellationToken);

        var grouped = rows
            .Where(t => categories.TryGetValue(t.CategoryId, out var c) && c.Kind == CategoryKind.Expense && accounts.ContainsKey(t.AccountId))
            .GroupBy(t => new { t.CategoryId, t.AccountId })
            .Select(g =>
            {
                var a = accounts[g.Key.AccountId];
                return new
                {
                    g.Key.CategoryId,
                    CategoryName = categories[g.Key.CategoryId].Name,
                    AccountId = g.Key.AccountId,
                    AccountName = a.Name,
                    a.CurrencyCode,
                    Amount = g.Sum(x => x.Amount),
                    AmountInBaseCurrency = g.Sum(x => x.AmountInBase)
                };
            })
            .ToList();

        return grouped.Select(g => new SpendingByCategoryRow
        {
            Id = Guid.NewGuid(),
            CategoryId = g.CategoryId,
            CategoryName = g.CategoryName,
            AccountId = g.AccountId,
            AccountName = g.AccountName,
            CurrencyCode = g.CurrencyCode,
            Amount = g.Amount,
            AmountInBase = g.AmountInBaseCurrency
        }).OrderBy(r => r.CategoryName).ThenBy(r => r.AccountName).ToList();
    }

    public async Task<IReadOnlyList<AccountStatementRow>> GetAccountStatementAsync(
        Guid accountId,
        DateTime? fromDate,
        DateTime? toDate,
        string openingLabel,
        string closingLabel,
        CancellationToken cancellationToken)
    {
        var fromUtc = fromDate ?? DateTime.MinValue;
        var toUtc = toDate ?? DateTime.MaxValue;

        var openingMovements = await accountReader.Movements(Earliest, Latest,
            q => q.Where(m => m.Dimensions.AccountId == accountId && m.Period < fromUtc), cancellationToken);

        var openingBalance = openingMovements.Sum(m => !m.IsExpense ? m.Resources.Amount : -m.Resources.Amount);

        var periodMovements = await accountReader.Movements(fromDate ?? Earliest, toDate ?? Latest,
            q => q.Where(m => m.Dimensions.AccountId == accountId).OrderBy(m => m.Period).ThenBy(m => m.Id), cancellationToken);

        var docIds = periodMovements.Select(m => m.DocumentId).Distinct().ToList();
        var documents = await db.Set<DocumentBase>().AsNoTracking()
            .Where(d => docIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => new { d.Code, d.DocumentTypeId }, cancellationToken);

        var rows = new List<AccountStatementRow>();

        // Opening balance row
        rows.Add(new AccountStatementRow
        {
            Id = Guid.NewGuid(),
            Date = fromDate,
            Description = openingLabel,
            RunningBalance = openingBalance
        });

        var currentBalance = openingBalance;
        foreach (var m in periodMovements)
        {
            documents.TryGetValue(m.DocumentId, out var docInfo);
            var inflow = !m.IsExpense ? m.Resources.Amount : (decimal?)null;
            var outflow = m.IsExpense ? m.Resources.Amount : (decimal?)null;
            currentBalance += (!m.IsExpense ? m.Resources.Amount : -m.Resources.Amount);

            rows.Add(new AccountStatementRow
            {
                Id = m.Id,
                Date = m.Period,
                DocumentId = m.DocumentId,
                DocumentTypeId = docInfo?.DocumentTypeId,
                DocumentCode = docInfo?.Code,
                DocumentType = null,
                Description = m.Details.Comment ?? "",
                Inflow = inflow,
                Outflow = outflow,
                RunningBalance = currentBalance
            });
        }

        // Closing balance row
        rows.Add(new AccountStatementRow
        {
            Id = Guid.NewGuid(),
            Date = toDate,
            Description = closingLabel,
            RunningBalance = currentBalance
        });

        return rows;
    }

    public async Task<IReadOnlyList<ExchangeRateRow>> GetExchangeRatesAsync(
        DateTime dateAsOf,
        CancellationToken cancellationToken)
    {
        var currencies = await db.Dict_Currencies.AsNoTracking().ToListAsync(cancellationToken);
        var rates = (await rateReader.Records(Earliest, dateAsOf, q => q, cancellationToken))
            .OrderByDescending(r => r.Period)
            .ToList();

        var result = new List<ExchangeRateRow>();
        foreach (var cur in currencies)
        {
            var latestRate = rates.FirstOrDefault(r => r.Dimensions.CurrencyId == cur.Id);
            if (latestRate is null)
                continue;

            result.Add(new ExchangeRateRow
            {
                Id = cur.Id,
                CurrencyId = cur.Id,
                CurrencyCode = cur.Code,
                CurrencyName = cur.Name,
                Rate = latestRate.Details.Rate,
                ValidFrom = latestRate.Period
            });
        }

        return result.OrderBy(r => r.CurrencyCode).ToList();
    }

    public async Task<IReadOnlyList<AccountBalanceSummary>> GetAccountBalancesSummaryAsync(
        DateTime asOf,
        string baseCurrencyCode,
        CancellationToken cancellationToken)
    {
        var accounts = await (
            from a in db.Dict_Accounts.AsNoTracking()
            join c in db.Dict_Currencies.AsNoTracking() on a.CurrencyId equals c.Id
            select new { a.Id, a.Name, a.Type, a.CurrencyId, CurrencyCode = c.Code }
        ).ToListAsync(cancellationToken);

        var movements = (await accountReader.Movements(Earliest, asOf,
                q => q.Select(m => new AccountAmount(m.Dimensions.AccountId, !m.IsExpense ? m.Resources.Amount : -m.Resources.Amount)),
                cancellationToken))
            .GroupBy(m => m.AccountId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var rates = (await rateReader.Records(Earliest, asOf, q => q, cancellationToken))
            .OrderByDescending(r => r.Period)
            .ToList();

        var summaries = new List<AccountBalanceSummary>();
        foreach (var acc in accounts)
        {
            movements.TryGetValue(acc.Id, out var balance);

            decimal? rate = 1m;
            if (!acc.CurrencyCode.Equals(baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                var rateRec = rates.FirstOrDefault(r => r.Dimensions.CurrencyId == acc.CurrencyId);
                rate = rateRec?.Details.Rate;
            }

            var balanceInBase = rate is null ? 0m : decimal.Round(balance * rate.Value, 2, MidpointRounding.AwayFromZero);

            summaries.Add(new AccountBalanceSummary
            {
                AccountId = acc.Id,
                AccountName = acc.Name,
                AccountType = acc.Type,
                CurrencyId = acc.CurrencyId,
                CurrencyCode = acc.CurrencyCode,
                Balance = balance,
                BalanceInBase = balanceInBase,
                HasRate = rate is not null
            });
        }

        return summaries.OrderBy(s => s.AccountName).ToList();
    }

    public async Task<IReadOnlyList<MonthlyTurnoverSummary>> GetMonthlyTurnoversAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        var categories = await CategoriesAsync(cancellationToken);
        var turnovers = (await TurnoverRowsAsync(fromDate, toDate, q => q, cancellationToken))
            .Where(t => categories.ContainsKey(t.CategoryId))
            .Select(t => new { t.Period, categories[t.CategoryId].Kind, AmountInBaseCurrency = t.AmountInBase })
            .ToList();

        var grouped = turnovers
            .GroupBy(x => new { x.Period.Year, x.Period.Month })
            .Select(g => new MonthlyTurnoverSummary
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                IncomeInBase = g.Where(x => x.Kind == CategoryKind.Income).Sum(x => x.AmountInBaseCurrency),
                ExpenseInBase = g.Where(x => x.Kind == CategoryKind.Expense).Sum(x => x.AmountInBaseCurrency)
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        return grouped;
    }

    public async Task<IReadOnlyList<CategoryExpenseSummary>> GetCategoryExpensesAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        var categories = await CategoriesAsync(cancellationToken);
        var expenses = (await TurnoverRowsAsync(fromDate, toDate, q => q, cancellationToken))
            .Where(t => categories.TryGetValue(t.CategoryId, out var c) && c.Kind == CategoryKind.Expense)
            .GroupBy(t => t.CategoryId)
            .Select(g => new CategoryExpenseSummary
            {
                CategoryId = g.Key,
                CategoryName = categories[g.Key].Name,
                AmountInBase = g.Sum(x => x.AmountInBase)
            })
            .OrderByDescending(x => x.AmountInBase)
            .ToList();

        return expenses;
    }

    public async Task<(decimal planned, decimal actual)> GetBudgetSpentAsync(
        LocalMonth month,
        CancellationToken cancellationToken)
    {
        var planPeriod = month.PlanPeriod;
        var start = month.FromUtc;
        var end = month.ToUtc;

        var planned = (await budgetReader.Records(planPeriod, planPeriod, q => q, cancellationToken))
            .Sum(b => b.Details.PlannedAmount);

        var categories = await CategoriesAsync(cancellationToken);
        var actual = (await TurnoverRowsAsync(start, Latest, q => q.Where(t => t.Period < end), cancellationToken))
            .Where(t => categories.TryGetValue(t.CategoryId, out var c) && c.Kind == CategoryKind.Expense)
            .Sum(t => t.AmountInBase);

        return (planned, actual);
    }
}
