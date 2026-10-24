using Microsoft.EntityFrameworkCore;
using PersonalAccounting.Domain.Commons;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Domain.Entities.Registers;
using PersonalAccounting.Domain.Reporting;
using PersonalAccounting.Enums;
using PersonalAccounting.Persistence.Contexts;
using Kandra.Domain.Entities.Documents;

namespace PersonalAccounting.Persistence.Repositories;

internal class PersonalAccountingReportsRepository(PersonalAccountingDbContext db) : IReportsRepository
{
    public async Task<IReadOnlyList<TransactionJournalRow>> GetTransactionJournalAsync(
        DateTime? fromDate,
        DateTime? toDate,
        IReadOnlyList<Guid>? accountIds,
        IReadOnlyList<Guid>? categoryIds,
        CancellationToken cancellationToken)
    {
        var movementsQuery = db.Reg_AccountBalances.AsNoTracking().Where(r => r.Active);

        if (fromDate.HasValue)
            movementsQuery = movementsQuery.Where(m => m.Period >= fromDate.Value);
        if (toDate.HasValue)
            movementsQuery = movementsQuery.Where(m => m.Period <= toDate.Value);
        if (accountIds != null && accountIds.Count > 0)
            movementsQuery = movementsQuery.Where(m => accountIds.Contains(m.Dimensions.AccountId));
        if (categoryIds != null && categoryIds.Count > 0)
            movementsQuery = movementsQuery.Where(m => m.Details.CategoryId.HasValue && categoryIds.Contains(m.Details.CategoryId.Value));

        var movements = await movementsQuery
            .OrderBy(m => m.Period)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);

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
        DateTime month,
        CancellationToken cancellationToken)
    {
        var start = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var plans = await db.Reg_BudgetPlan.AsNoTracking().Where(r => r.Active)
            .Where(b => b.Period == start)
            .GroupBy(b => b.Dimensions.CategoryId)
            .Select(g => new { CategoryId = g.Key, Planned = g.Sum(x => x.Details.PlannedAmount) })
            .ToListAsync(cancellationToken);

        // Turnovers in that month for expenses
        var turnovers = await (
            from t in db.Reg_CategoryTurnover.AsNoTracking().Where(r => r.Active)
            join c in db.Dict_Categories.AsNoTracking() on t.Dimensions.CategoryId equals c.Id
            where t.Period >= start && t.Period < end && c.Kind == CategoryKind.Expense
            group t by t.Dimensions.CategoryId into g
            select new { CategoryId = g.Key, Actual = g.Sum(x => x.Resources.AmountInBaseCurrency) }
        ).ToListAsync(cancellationToken);

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
        var query = from t in db.Reg_CategoryTurnover.AsNoTracking().Where(r => r.Active)
                    join c in db.Dict_Categories.AsNoTracking() on t.Dimensions.CategoryId equals c.Id
                    join a in db.Dict_Accounts.AsNoTracking() on t.Dimensions.AccountId equals a.Id
                    join cur in db.Dict_Currencies.AsNoTracking() on a.CurrencyId equals cur.Id
                    where c.Kind == CategoryKind.Expense
                    select new { t, c, a, cur };

        if (fromDate.HasValue)
            query = query.Where(x => x.t.Period >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(x => x.t.Period <= toDate.Value);
        if (categoryIds != null && categoryIds.Count > 0)
            query = query.Where(x => categoryIds.Contains(x.t.Dimensions.CategoryId));

        var grouped = await query
            .GroupBy(x => new
            {
                CategoryId = x.c.Id,
                CategoryName = x.c.Name,
                AccountId = x.a.Id,
                AccountName = x.a.Name,
                CurrencyCode = x.cur.Code
            })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.CategoryName,
                g.Key.AccountId,
                g.Key.AccountName,
                g.Key.CurrencyCode,
                Amount = g.Sum(x => x.t.Resources.AmountInAccountCurrency),
                AmountInBaseCurrency = g.Sum(x => x.t.Resources.AmountInBaseCurrency)
            })
            .ToListAsync(cancellationToken);

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

        var openingMovements = await db.Reg_AccountBalances.AsNoTracking().Where(r => r.Active)
            .Where(m => m.Dimensions.AccountId == accountId && m.Period < fromUtc)
            .ToListAsync(cancellationToken);

        var openingBalance = openingMovements.Sum(m => !m.IsExpense ? m.Resources.Amount : -m.Resources.Amount);

        var periodMovements = await db.Reg_AccountBalances.AsNoTracking().Where(r => r.Active)
            .Where(m => m.Dimensions.AccountId == accountId && m.Period >= fromUtc && m.Period <= toUtc)
            .OrderBy(m => m.Period)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);

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
        var rates = await db.Reg_ExchangeRates.AsNoTracking().Where(r => r.Active)
            .Where(r => r.Period <= dateAsOf)
            .OrderByDescending(r => r.Period)
            .ToListAsync(cancellationToken);

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

        var movements = await db.Reg_AccountBalances.AsNoTracking().Where(r => r.Active)
            .Where(m => m.Period <= asOf)
            .GroupBy(m => m.Dimensions.AccountId)
            .Select(g => new
            {
                AccountId = g.Key,
                Balance = g.Sum(m => !m.IsExpense ? m.Resources.Amount : -m.Resources.Amount)
            })
            .ToDictionaryAsync(x => x.AccountId, x => x.Balance, cancellationToken);

        var rates = await db.Reg_ExchangeRates.AsNoTracking().Where(r => r.Active)
            .Where(r => r.Period <= asOf)
            .OrderByDescending(r => r.Period)
            .ToListAsync(cancellationToken);

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
        var turnovers = await (
            from t in db.Reg_CategoryTurnover.AsNoTracking().Where(r => r.Active)
            join c in db.Dict_Categories.AsNoTracking() on t.Dimensions.CategoryId equals c.Id
            where t.Period >= fromDate && t.Period <= toDate
            select new { t.Period, c.Kind, t.Resources.AmountInBaseCurrency }
        ).ToListAsync(cancellationToken);

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
        var expenses = await (
            from t in db.Reg_CategoryTurnover.AsNoTracking().Where(r => r.Active)
            join c in db.Dict_Categories.AsNoTracking() on t.Dimensions.CategoryId equals c.Id
            where t.Period >= fromDate && t.Period <= toDate && c.Kind == CategoryKind.Expense
            group t by new { c.Id, c.Name } into g
            select new CategoryExpenseSummary
            {
                CategoryId = g.Key.Id,
                CategoryName = g.Key.Name,
                AmountInBase = g.Sum(x => x.Resources.AmountInBaseCurrency)
            }
        ).OrderByDescending(x => x.AmountInBase).ToListAsync(cancellationToken);

        return expenses;
    }

    public async Task<(decimal planned, decimal actual)> GetBudgetSpentAsync(
        DateTime month,
        CancellationToken cancellationToken)
    {
        var start = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        var planned = await db.Reg_BudgetPlan.AsNoTracking().Where(r => r.Active)
            .Where(b => b.Period == start)
            .SumAsync(b => (decimal?)b.Details.PlannedAmount, cancellationToken) ?? 0m;

        var actual = await (
            from t in db.Reg_CategoryTurnover.AsNoTracking().Where(r => r.Active)
            join c in db.Dict_Categories.AsNoTracking() on t.Dimensions.CategoryId equals c.Id
            where t.Period >= start && t.Period < end && c.Kind == CategoryKind.Expense
            select (decimal?)t.Resources.AmountInBaseCurrency
        ).SumAsync(cancellationToken) ?? 0m;

        return (planned, actual);
    }
}
