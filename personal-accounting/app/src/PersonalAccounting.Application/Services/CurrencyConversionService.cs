using Kandra.Application.Abstractions.Services;
using Kandra.Domain.Exceptions;
using Kandra.Persistence.Abstractions;
using Kandra.Persistence.Abstractions.Registers;
using Microsoft.Extensions.Localization;
using PersonalAccounting.Application.Constants;
using PersonalAccounting.Domain.Entities.Dictionaries;
using PersonalAccounting.Domain.Entities.Registers;

namespace PersonalAccounting.Application.Services;

public interface ICurrencyConversionService
{
    ValueTask<decimal> ConvertToBaseCurrencyAsync(Guid currencyId, decimal amount, DateTime date, IStringLocalizer localizer, CancellationToken cancellationToken);
}

public class CurrencyConversionService(
    IConstantsService constants,
    ILookupRepository lookup,
    IInfoRegisterReader<ExchangeRatesRecord> exchangeRatesReader) : ICurrencyConversionService
{
    public async ValueTask<decimal> ConvertToBaseCurrencyAsync(Guid currencyId, decimal amount, DateTime date, IStringLocalizer localizer, CancellationToken cancellationToken)
    {
        if (amount == 0m)
            return 0m;

        var currencies = await lookup.GetDictionaryAsync<Currency>([currencyId], cancellationToken);
        var currencyRef = currencies.FirstOrDefault();
        var baseCurrencyConstant = await constants.GetAsync<BaseCurrencyConstant>(date, cancellationToken);
        var baseCurrencyCode = baseCurrencyConstant.Value;

        if (currencyRef != null && currencyRef.Code.Equals(baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var rates = await exchangeRatesReader.SliceOfLast(date, q => q.Where(r => r.Dimensions.CurrencyId == currencyId), cancellationToken);
        var rateRecord = rates.FirstOrDefault();

        if (rateRecord != null && rateRecord.Details.Rate > 0)
        {
            return decimal.Round(amount * rateRecord.Details.Rate, 2, MidpointRounding.AwayFromZero);
        }

        var code = currencyRef?.Code ?? currencyId.ToString();
        throw new InvalidStateException(localizer["Error_MissingExchangeRate", code, date.ToString("yyyy-MM-dd")]);
    }
}
