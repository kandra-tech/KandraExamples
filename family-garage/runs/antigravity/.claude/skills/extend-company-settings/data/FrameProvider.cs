// --- FamilyGaragePrintFrameProvider.GetLetterheadAsync (FamilyGarage.Application/Printing) ---------------
// The provider needs IStringLocalizer again for labelled values: add `IStringLocalizer localizer` to the primary
// constructor (using Microsoft.Extensions.Localization) and the two helpers below.

var taxCode = (await constants.GetAsync<CompanyTaxCodeConstant>(asOf, cancellationToken)).Value;
var address = (await constants.GetAsync<CompanyAddressConstant>(asOf, cancellationToken)).Value;
var phone = (await constants.GetAsync<CompanyPhoneConstant>(asOf, cancellationToken)).Value;

var requisites = Format("Print_Party_TaxCode", taxCode);       // platform key: "Tax code: {0}"
var contacts = Join(address, phone);
return new PrintLetterhead(name.Trim(), requisites, contacts, await ReadLogoAsync(logoId, cancellationToken));

private string? Format(string key, string? value) =>
    string.IsNullOrWhiteSpace(value) ? null : localizer[key, value.Trim()].Value;

/// <summary>The non-empty parts joined with a middle dot; null when none.</summary>
private static string? Join(params string?[] parts)
{
    var present = parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()).ToList();
    return present.Count == 0 ? null : string.Join(" · ", present);
}
