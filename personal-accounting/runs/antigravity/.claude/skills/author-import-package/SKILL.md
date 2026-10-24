---
name: author-import-package
description: Write or extend an import package (kandra-import/1 JSON) for a Kandra configuration. Use for the configuration's demo data (the embedded demo package), for a one-off or repeatable data migration, or when a custom data processor should import from a user-supplied file. Covers the format, $id rules, references, date tokens, per-step policies, versioning, dry runs and the free-tier limits.
---

# Authoring an import package

One JSON format serves **demo data** (`origin: "demoSeed"`, embedded in the assembly, applied on a demo installation)
and **real data import** (`origin: "import"`, uploaded or embedded). The engine's import subsystem applies it through
the same CRUD pipeline as the UI (behaviors, validators, numbering, submit), stamps every row it writes, and is
idempotent: running a package again changes only what changed.

A new configuration already has `{Config}.Application/ImportPackages/{config}-demo.json` (a valid package with no
steps), registered with `services.AddImportPackages(...)`, a JSON Schema snapshot beside it and a test that keeps the
snapshot current. Start there.

## Format

```json
{
  "$schema": "./{config}.import.schema.json",
  "format": "kandra-import/1",
  "package": "{config}-demo",
  "version": 1,
  "origin": "demoSeed",
  "configuration": "{Config}",
  "title": "Demo data",
  "steps": [
    { "dictionary": "Products", "match": ["code"], "items": [
      { "$id": "prod-1", "code": "P-1", "name": "Bread", "categoryId": { "$code": "FOOD" } } ] },
    { "document": "Orders", "submit": true, "items": [
      { "$id": "ord-1", "date": "$today-7d", "productId": { "$ref": "prod-1" },
        "lines": [ { "productId": { "$ref": "prod-1" }, "quantity": 10 } ] } ] },
    { "constant": "OrganizationName", "$id": "org-name", "value": "Demo LLC" },
    { "createFrom": { "source": "ord-1", "target": "Shipments" }, "$id": "shp-1", "submit": true, "set": { "code": null } },
    { "dataProcessor": "ImportExchangeRates", "$id": "rates", "run": "once", "optional": true, "input": { "date": "$today-30d" } }
  ]
}
```

The `$schema` line gives editors completion and validation (the snapshot lists every entity and field of the
configuration). Entity names are the **catalog names** (plural, as in the `[Kandra*Form(Name = ...)]` attribute) and item
properties are the real Dto wire shape: camelCase, enums as camelCase strings, hierarchical dictionaries with the
`isFolder` discriminator. The schema (or `describe_entity_type` over MCP) shows exactly which fields exist and which are
required: a required non-nullable property that is missing is an error, even when the Dto has a default for it.

Steps run **strictly in order**, so write them in dependency order: dictionaries (folders before their leaves), then
settings other documents depend on, then documents, then documents created from other documents.

| Step | Meaning |
|---|---|
| `dictionary` / `document` | `items` are Dto JSON plus a required `$id`. `submit: true` inserts a document as submitted (it posts to registers and accounts). |
| `constant` | One value; `$id` required. Add `"effectiveFrom": "2000-01-01"` (an ISO date, UTC) for company details that documents dated earlier must print: constants are read as of the document's date, and without it the value applies only from the moment of the import. A fixed date is fine here (it is not a document date). |
| `createFrom` | Runs `New` on the target with the source document (Document Links prefill), applies `set`, inserts. Use it where the UI would use a "Create ..." button. |
| `dataProcessor` | Runs `Execute` with `input`. Rows it creates are not stamped; the step is recorded in the package state. `run`: `once` (default) or `always`. `optional: true` turns a failure (say, no network) into a warning. |

## `$id` rules: the import key

Every item and every constant / createFrom / dataProcessor step needs a `$id`, unique within the package. It becomes the
row's import key (`ImportStamp.Key`) **for good**:

- keep it stable: renaming an `$id` inserts a second row;
- never reuse one for a different object: it would silently point at the first object's row;
- use a readable scheme such as `<kind>-<code>` (`wh-main`, `ord-demo-01`).

## References: `$ref` and `$code`

- `{ "$ref": "<id>" }`: an object of **this package**, which must appear earlier (forward references are errors).
- `{ "$code": "<Code>" }`: an **existing row** looked up by its `Code` in the type the field points to (known from field
  metadata). Use it for reference data created by code-seeded data (currencies, built-in types) or by an earlier package.
  It is an error on a field that is not a reference.

## Date tokens

String values `$now`, `$today`, `$today-23d`, `$today+14d`. `$today` is the running user's local date; a DateTime field
gets local midnight converted to UTC, a `DateOnly` field the local date. Tokens resolve when the package is applied and
are hashed unresolved, so a token never counts as a change. Use tokens, not fixed dates, in demo data, so it stays
inside the list pages' default "recent" filter. An expiry or due date relative to a document date is a token computed from
today (`$today-5d` for a date 20 days after a document dated `$today-25d`).

## Per-step policies

Defaults are the safe choice; override only on purpose.

- `match`: Dto properties that identify an existing, not yet stamped row (they must be unique) to **adopt** instead of
  inserting, such as `["code"]` for a dictionary some seeder already populated. Default: always insert.
- `onChange` (the package item changed since the last apply): `update` | `skip` | `error`. Dictionaries and constants
  default to `update`; documents to `skip`, reported as drift (updating a submitted document means unsubmit, update, submit).
- `onUserModified` (a user changed the row after import): `skip` | `overwrite` | `error`, default `skip`.
- `onDeleted` (a user soft-deleted the row): `skip` | `restore` | `error`, default `skip`.

## Versioning

Bump the integer `version` whenever the content changes. A demo installation applies a newer embedded version at startup
and only new or changed objects are written (the user's edits and deletions survive under the default policies). The
package name is the stamp's `Source`, so never rename `package` either.

## Dry run, then run

- **Admin page** `/admin/import`: pick the embedded package or upload a file, **Dry run**, read the report (counts per
  outcome, items, diagnostics with JSON paths), then **Import**. Every row it wrote has an Import block on its View page
  (Change info tab) and the history shows the run.
- **API** (admin only): `POST api/v1/Import/run` with `{ "package": "<id>", "dryRun": true }` returns `202 { runId }`;
  poll `GET api/v1/Import/runs/{runId}`; run again without `dryRun`. One run at a time per installation (a second start
  is a 409). A multipart `file` upload works for a package that is not embedded.
- Fix every error diagnostic first. Then run it twice: the second run must report everything `Unchanged`.

## Free-tier limits

A demo or a first import must stay inside the free tier or the licence pre-check of the dry run fails: at most **5 users
in total including admin** (sample users come from the configuration's `IDemoUserProvider`, not from the package; the
service users do not count), fewer than **1 000 documents in the current month** (spread older documents over previous
months with date tokens), and no document over **200 lines**.

## Importing from a user's own file: an import data processor

When the source is a customer's file (CSV, Excel, a legacy export), do not ask them to write the package. Add a **data
processor** (see `create-data-processor`) whose input Dto has a file-picker field and whose behavior converts the file:

1. Input Dto: `[FilePicker(Extensions = "*.csv")] public Guid? FileId { get; set; }` plus `[Description]` on the class
   and the field. The picker uploads the file as a blob when the user presses Execute and puts the blob reference id
   in `FileId`. Keep `FileId` out of the validator's required rules and check it in the behavior instead: the form
   validates before the upload, while `FileId` is still empty.
2. Behavior: inject `IBlobService` and `IDataImportService`. `OpenReadAsync(fileId, ct)` gives the stream; parse it and
   build a package in memory (a `MemoryStream` of `kandra-import/1` JSON with `origin: "import"`, a stable `package` name
   per source such as `{config}-customers-csv`, a `$id` derived from the source's own key, e.g. `cust-<row key>`).
3. Call `ImportAsync(stream, new ImportOptions { DryRun = ... }, ct)`, return the counts and failed items in the result
   Dto. Deriving `$id` from the source key makes a second upload of the corrected file update rows instead of duplicating
   them; `match` adopts rows that already exist.

The built-in Data import page already handles a package file; a processor is for formats the engine does not read.
The Kandra WMS reference configuration's `ImportData` processor is a complete example (modes for a CSV of items or of counterparties, `$code` lookups
for the unit and the parent folder, `match: ["code"]`).
