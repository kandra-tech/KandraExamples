---
name: localize-configuration
description: Add, check and translate the localization (resx) keys of a Kandra configuration - which keys each entity needs (form titles, nav entries, captions, Add/AddedSuccessfully messages, enum members, validator messages), how to add them without breaking encoding, and how to find missing ones. Use whenever you add or rename an entity, field, enum member, nav entry or validation message, or when asked for another UI language.
---

# Localizing a configuration

All UI text is a **resx key** looked up through the module chain (the platform's `Kandra.resx`, then this
configuration's `{Config}.Localization/{Config}.resx`, most recent module wins). A key that exists nowhere is **not an
error**: the page prints the raw key, often upper-cased ("ADDEXPENSE"). Build and startup stay green, so this is found
only by looking, or by the test below.

## 1. Which languages

- **The neutral file `{Config}.resx` (English) is the only required one.** A key missing from `{Config}.uk.resx` or
  `.ru.resx` falls back to the neutral text (verified), so those files can lag behind without breaking anything.
- A spec that says "English UI, labels easy to localize" or lists no language: write the neutral file only, leave the
  uk/ru files as they are. Do not translate "to be thorough": it triples the work and adds nothing the spec asked for.
- A spec that asks for another language, or a project whose uk/ru files hold every key of the neutral one (the shipped
  template and the reference configuration do): add the key to every file you can translate correctly, in the same change.
- Never delete the other language files and never edit `{Config}.Designer.cs` (generated from the resx on build).

## 2. Which keys an entity needs

The key-by-name rule: an attribute that takes a key (`[Caption("X")]`, `[NavigationName("X")]`, ...) looks `X` up unless
written `isLiteral: true` (non-translatable text, e.g. a brand). Plan the list while you write the Dto.

| Source | Keys |
|---|---|
| Dictionary / document `{Name}` (plural `{Plural}`) | `{Plural}` (list title), `Create{Name}`, `Edit{Name}`, `View{Name}` (the `[AddFormTitle]`/`[EditFormTitle]`/`[ViewFormTitle]`/`[ListFormTitle]` values), `Nav_{Plural}` (`[NavigationName]`), and three that **no attribute mentions**: `Add{Name}` (the list's add button), `{Name}AddedSuccessfully`, `{Name}UpdatedSuccessfully` (a hierarchical dictionary also has folder variants such as `{Name}FolderAddedSuccessfully`; the key test lists them) |
| Every field with `[Caption("K")]`, every line field | `K` (reuse an existing key when the wording is the same: `Amount`, `Date`, `Comment` ...) |
| `[TablePart(AddCaption = "K")]` | `K` (default `AddLine`, already provided by the platform) |
| `[FormTab("K")]` / `[FormSection("K")]` | `K` |
| Report `{Name}` | the `[KandraReportForm(Name = ...)]` value (page title and nav entry), every filter/line/header `[Caption]`, a print form's title key |
| Data processor | the `[KandraDataProcessorForm(Name = ...)]` value, `[ExecuteFormTitle]`, captions, result captions |
| Enum member | the `[EnumCaption("K")]` key of each member |
| Register | `[RegisterCaption("K")]` and the `[Caption]`s of dimensions/resources/details |
| Interface folder / node | `InterfaceNodeDefinition.Folder("Nav_X", ...)` caption keys (an `Entity` node takes the Dto's `[NavigationName]`) |
| Validators | every `localizer["K"]` in `WithMessage(...)` |
| Messages with values | the text holds `{0}`, `{1}`; call `localizer["K", a, b]` |
| Platform keys | `Save`, `Cancel`, `Code`, `Name`, `Date`, `Status`, `Button_Submit`, `NotAuthorized` ... already exist in the platform module; do not redefine them unless you mean to override the wording for this configuration |

## 2a. Text produced by code

- **Only a behavior or the UI produces text, and always through the localizer.** A report behavior or a document behavior
  injects `IStringLocalizer` and resolves a key (`localizer["StatementOpeningBalance"]`, with `{0}` arguments for
  values); an enum value uses its `[EnumCaption]`.
- **Never in a repository** (it returns kinds and ids, the behavior turns them into words) **and never into stored data**:
  a register detail or a document field holds what the user typed, not `"Opening balance"` written by code, because
  stored text cannot be translated afterwards. Text prefilled into a field the user then edits (a Service created from an
  Accident gets "Repair after the accident of 12.03") comes from the localizer too, in `OnNewAsync`.
- **The places it slips in:** an enum turned into a string with `.ToString()` (type the print-form or report-line property
  as the enum itself: the print formatter shows the member's `[EnumCaption]` in the reader's language); an exception message the user reads
  (`InvalidStateException(localizer[key, args].Value)`, `create-document` step 4); a notification (`NotificationText.ForKey`,
  never `ForLiteral` with composed text, `send-notifications`).
- `LocalizationKeysTests` checks keys, not literals: an English literal passes it silently. `CodeTextTests` (in a configuration
  that ships it) catches words in report repositories, literal notifications, register tables read directly and a missing
  value counted as 1; everywhere else the rule is yours to keep.

## 3. Adding keys

Append `<data>` entries to the resx files (open the file, add before `</root>`; the engine does not care about order):

```xml
  <data name="CreateExpense" xml:space="preserve">
    <value>New expense</value>
  </data>
```

- Use the editor tool on the file, **not a script piping text through the shell**. Windows consoles run OEM/ANSI code
  pages (866/1251/1252): Ukrainian `і ї є` and Russian text turn into `?` or mojibake when a Python/PowerShell one-liner
  prints or writes them without an explicit UTF-8. If you must script, open and write with `encoding="utf-8"`, never
  `print` the result to judge it, and re-read the file afterwards.
- Keep the XML indentation of the existing entries (two spaces, then four for `<value>`), UTF-8 without BOM.
- Escape `&`, `<`, `>` in values (`&amp;` ...).
- Do not invent translations you are not sure of; an English fallback is better than a wrong word.

## 4. Check that nothing is missing

A **platform** label (a button of the generated pages: Save, Save draft, Print, ...) that shows its raw key is missing from
the engine's own resources, not from yours. Define the key in your resx (the configuration's module wins over the
platform's, so this overrides it) and report it; the engine has a test that should catch such gaps.

```bash
dotnet build                                                    # the generated UI is the input of the second test
dotnet test src/{Config}.Tests --filter LocalizationKeysTests   # lists every missing key and where it is used
```

`LocalizationKeysTests` (in `src/{Config}.Tests/Localization/`) does two things through the production module chain:
it scans every `Loc["..."]` the **generated client UI** asks for (captions, titles, the derived `Add...`/`...AddedSuccessfully`
keys) and every key written in the configuration's **own source** (`[Caption]`, titles, `[FormTab]`, `[EnumCaption]`,
`localizer["..."]`, nav folders), and fails with the list of keys that no resx defines. Run it after every entity. A key
built at runtime (string interpolation) is not covered; look at those screens yourself.

**Many keys missing at once** (a new document and its report bring twenty or more): in a configuration made from the
template, `UPDATE_RESX=1 dotnet test src/{Config}.Tests --filter LocalizationKeysTests` appends each missing key to the
English resx as a formatted entry, with text guessed from the key (`Validation_AmountMustBePositive` becomes "Amount must
be positive", `ExpenseDto_CategoryId` becomes "Category"). The run fails once and lists what it wrote: read the guesses,
fix the wrong ones in the file, add the other languages by hand, and run again. Do not hand-write one long line of
`<data>` elements instead: the file stays one entry per element so a diff shows what changed.

Typical finds: `Add{Name}` / `{Name}AddedSuccessfully` / `{Name}UpdatedSuccessfully` forgotten; a validator message used
in two languages but defined once; a renamed field whose old key is still in the Dto.

## 5. Switching language in the browser (to look at it)

The language menu in the top bar, or in the browser console: `localStorage.setItem('blazorCulture', 'uk'); location.reload();`
(also `en`, `ru`).
