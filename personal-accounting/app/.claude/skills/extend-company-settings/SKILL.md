---
name: extend-company-settings
description: Add a company detail (tax code, address, phone, website, bank requisites, a second logo) to the Company settings form and the letterhead that print forms show. A fresh configuration ships only CompanyName and CompanyLogo; use this when a print form, a document header or a spec needs more company data than that.
---

# Extending the company settings

A new configuration ships the smallest useful letterhead: the **Company settings** data processor edits two dated
constants, `CompanyName` and `CompanyLogo`, and `{Config}PrintFrameProvider` turns them into the letterhead
(`PrintLetterhead(name, requisitesLine, contactsLine, logo)`) of every `[PrintForm]` with `Frame = Letterhead`. Nothing
else about the company is stored, because most configurations never print it. When one needs more, add it the way the
shipped two work. The pieces, with their copy-paste templates in [`data/`](data/):

| Piece | File | Template |
|---|---|---|
| The value (a dated constant) | `{Config}.Application/Constants/Company{Detail}Constant.cs` | `data/Constant.cs` |
| Its type id (a new GUID, never reused) | `{Config}.Enums/{Config}TypeIds/{Config}TypeIds.Company{Detail}.cs` | `data/TypeIdConstant.cs` |
| The form field | `{Config}.Forms/DataProcessors/CompanySettings.cs` (`CompanySettingsDto`) | `data/FormField.cs` |
| Read on open, write on execute | `{Config}.Application/DataProcessors/CompanySettings.cs` (`OnNewAsync`, `OnProcessAsync`) | `data/Behavior.cs` |
| Show on the letterhead | `{Config}.Application/Printing/{Config}PrintFrameProvider.cs` | `data/FrameProvider.cs` |
| Captions and messages | `{Config}.Localization/{Config}.resx` (and uk/ru when translated) | `data/Localization.resx.xml` |
| Import schema snapshot | `UPDATE_IMPORT_SCHEMA=1 dotnet test` | - |

For a plain text detail (tax code, address, phone) that is all: it appears in the form, is saved as a new dated value
of the constant (so documents dated earlier keep printing the old text), and is read by the frame provider as of the
document's date.

## Steps

1. **Constant and type id**: copy the two templates (`create-constant` explains the constant mechanics and the dated
   history; one constant per detail, never one constant holding several). Name it `Company{Detail}`; the type id is
   `Guid.NewGuid()` pasted as a string.
2. **Form field** in `CompanySettingsDto`: a `string?` with `[SectionRef("CompanySettings_Contacts")]` (or the identity
   section for requisites), `[Caption("Company{Detail}")]` and a `[Description]`. A detail that needs a new section
   adds a `[FormSection("CompanySettings_Contacts", Order = 1)]` on the Dto (the Printing section moves to order 2) and
   its caption key.
3. **Behavior**: read it in `OnNewAsync` with `ReadAsync<Company{Detail}Constant>(now, ...)` and write it in `OnProcessAsync`
   with `WriteAsync<Company{Detail}Constant>("Company{Detail}", input.{Detail}, at, now, changed, ...)`. The helper methods
   are already in the class (they write only when the value changed and name it in the result message).
4. **Letterhead**: the two lines under the company name are `requisites` (identity: tax code, VAT number) and `contacts`
   (address, phone, website). Read the new constant in `GetLetterheadAsync` and add it to the right line with the
   `Join(...)` helper from `data/FrameProvider.cs` (copy it into the provider with the read). The tax-code line uses the platform's own key
   `Print_Party_TaxCode` (`Tax code: {0}`); any other label needs a key of yours.
5. **Validation** (only when the value has a format): a rule in `CompanySettingsValidator` in the Forms project
   (`data/FormField.cs` has the tax-code example). The validator is shared by client and server.
6. **Localization**: the `Company{Detail}` caption, the new section caption if any, the validator message
   (`localize-configuration`). Then `dotnet test --filter LocalizationKeysTests`.
7. **Test**: extend `LetterheadTests.WithTheCompanyConstantsSet...`: set the constant with `IConstantsManageService.SetAsync`
   and assert the letterhead line contains it (`data/Test.cs`). A document dated before the constant's `from` date still
   prints without it.
8. `UPDATE_IMPORT_SCHEMA=1 dotnet test` (the new constant and field are part of the import schema), then build.

## Things to know

- **Do not edit the constant on the constants admin page for the logo**: the logo is a blob reference that the form keeps
  attached; the form is its only editor. Text constants can be edited either way.
- The constants are history-aware on purpose: a reprint of a document shows the company details valid on its date. Do not
  replace them with plain fields.
- A **second logo** (a stamp, a signature) is not a text constant: copy the `CompanyLogo` pieces (blob sync, image check,
  `EntityTag`) rather than adding a text field, and read the bytes in the frame provider the same way.
- A print form that needs the details **in its own body** (a seller block on an invoice) takes them from the constants
  through its behavior's `OnPrintAsync` (see `create-print-form`), not from the letterhead.
