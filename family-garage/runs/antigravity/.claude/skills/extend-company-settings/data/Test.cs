// --- LetterheadTests.WithTheCompanyConstantsSet_TheLetterheadCarriesThem_AsOfTheDocumentDate -----------------------------
await constants.SetAsync<string>(CompanyAddressConstant.Name, "1 Main St", from, CancellationToken.None);
await constants.SetAsync<string>(CompanyPhoneConstant.Name, "+380441234567", from, CancellationToken.None);
// ...after the letterhead is read:
Assert.Contains("1 Main St", letterhead.ContactsLine);
Assert.Contains("+380441234567", letterhead.ContactsLine);
