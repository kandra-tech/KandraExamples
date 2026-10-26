# AGENTS.md — FamilyGarage Configuration

This is a configuration built on the [Kandra](https://github.com/kandra-tech) ERP engine. It starts nearly empty — no
Dictionaries, Documents, Reports or Registers yet, and one Data Processor (Company settings: the company name and logo,
the letterhead of print forms) — but is fully buildable and runnable as scaffolded. This file is the day-to-day guide for working in it; the recipes in
[`.claude/skills/`](.claude/skills/) are step-by-step instructions for the most common changes — prefer them
over improvising the shape from scratch.

## Building a configuration from a specification — start here

If the task is a whole application ("build me an app that tracks X") rather than one entity, **read
[`plan-configuration`](.claude/skills/plan-configuration/SKILL.md) first** (and its `reference.md` of modeling patterns),
then follow its stages. The rules that matter most:

1. **Model with the engine's kinds, not around them.** Dictionaries hold *things*; documents record *events*; registers
   hold the *numbers that result from events or values that hold from a date*; reports read registers. A rate or price is
   a register written by a "set values" document, not a dictionary row; an opening balance is a document posting to a
   balance register, not a field on the account. `plan-configuration/reference.md` section 1 lists the red flags.
2. **Read a kind's whole `SKILL.md` before writing its first file**, and start from its `data/` skeletons. The long
   `reference.md` beside it is for looking things up.
3. **Never hand-write what is generated** (controllers, Razor pages, EF configuration, DI wiring) and never edit
   `Generated.Net/`, `*.Designer.cs` or migration snapshots.
4. **A green build proves little.** Check each stage the way its gate says: posting rules with tests
   ([`test-configuration`](.claude/skills/test-configuration/SKILL.md)), missing resx keys with
   `dotnet test --filter LocalizationKeysTests`, forms by opening them in the browser.
5. **Build what the spec lists.** English resx is enough unless the spec asks for languages
   ([`localize-configuration`](.claude/skills/localize-configuration/SKILL.md)).

## Run, sign in, test

```bash
dotnet build
dotnet test                                                      # applies the migrations on SQLite in memory
dotnet run --project src/FamilyGarage.WebApi --launch-profile FamilyGarage.UI   # https://localhost:7226
```

The app redirects `http://localhost:5226` to `https://localhost:7226`; call it over https (accept the development
certificate), since many HTTP clients drop the `Authorization` header on a redirect. The first start opens the first-run
wizard (it sets the admin password); to skip it set `Kandra__AdminPassword`, or start a demo installation with
`Kandra__Setup__Mode=Demo` **and** `ACCEPT_EULA=Y` (setup waits for the licence acceptance), which applies the demo import package and creates the sample users: `admin` / `Admin!1`,
`user0` / `User!0`, `user1` / `User!1` ... (see `Seeding/FamilyGarageDemoUsers.cs`). The SQLite file lives under
the app folder (`Kandra:DataPath`); delete it, or set `Kandra__Database__ResetToken`, to start clean.

## What "configuration" means here

This configuration is built entirely on the Kandra engine's public surface (the `Kandra.*` NuGet packages) — it
must never reach into engine internals that aren't meant to be extension points. If you find yourself wanting
to bypass a `Kandra.*` abstraction (e.g. resolving a register writer or the posting service outside
`ISubmitScope`, hand-writing a controller instead of letting the generator emit one), that's a signal you're
about to reintroduce something the engine deliberately closed off, not that the abstraction is optional here.

`Kandra.*` is consumed purely via `PackageReference` (see `Directory.Packages.props`/`Directory.Build.props`'s
`$(KandraCoreVersion)` — a manually-maintained pin, bump it by hand after validating a newer engine release).
There is no engine source anywhere in this repo to fall back on reading directly, unless you happen to have the
engine's own repository checked out separately for reference.

## Configuration name (`KandraConfigurationName`)

`Directory.Build.props` sets `<KandraConfigurationName>FamilyGarage</KandraConfigurationName>`, read by
the DI-emitting generators (`Kandra.Generators.Di.Application`/`.Di.Persistence`/`.ClientLib`) to name their
generated extension classes/methods after this configuration — `AddGeneratedFamilyGarageApplicationEntities()`,
`AddGeneratedFamilyGaragePersistenceEntities()`, `AddGeneratedFamilyGarageClientServices()`, etc.
If you ever rename this configuration's projects, update this property to match, or the generators fall back
to deriving a name from `RootNamespace` instead — a safety net, not a substitute for setting it correctly.

## Project map

- **`FamilyGarage.Domain`** — concrete `*Base` entities go under `Entities/Dictionaries`,
  `Entities/Documents`, `Entities/Registers`, `Entities/ReportingModels` (create these folders as you add your
  first entity of each kind); `Identity/` already has `DomainUser`/`DomainRole`; a `Numbering/` folder holds
  bucket name constants for `ISimpleNumberingSystem`/`IGaplessNumberingSystem` once you add documents that need
  one. `Commons/IReportsRepository` is an empty placeholder interface for hand-written, bespoke report
  queries — declared here, not in Persistence, specifically so a report's Behavior in
  `FamilyGarage.Application` can inject it (Application references Domain but never Persistence
  directly); its implementation lives in `FamilyGarage.Persistence/Repositories/`.
- **`FamilyGarage.Forms`** — `*Dto` + `*Validator` pairs, one file per entity, organized under
  `Dictionaries/`/`Documents/`/`Reports/`/`DataProcessors/`/`Dashboards/` as you add them. **Never reference
  `.Application`/`.Domain`/`.Persistence`** — the Blazor WASM client only ever sees this project plus
  `FamilyGarage.Enums`/`FamilyGarage.Localization`.
- **`FamilyGarage.Application`** — `*Behavior` + `*Mapper` pairs, same folder split as Forms. A
  `*Mapper` is a hand-written Mapperly `[Mapper]` partial stub implementing `IEntityMapper<TEntity, TDto>` (a static
  class for reports); Mapperly generates the bodies, an unmapped member is a build error (RMG012/RMG020 in
  `.editorconfig`), and a dictionary/document without one fails with `KANDRAGEN014`. Never put mapping attributes
  on `*Base` entities or `*Dto`s. `ConfigureServices.cs` calls the generated `AddGeneratedFamilyGarageApplicationEntities()`,
  which will cover dictionaries/documents/reports/data processors alike once you have any (one
  `AddDictionary`/`AddDocument`/`AddReport`/`AddDataProcessor` call per discovered `Kandra*Behavior`-attributed
  class — nothing to wire by hand). Three stub implementations exist here purely because the engine
  constructor-injects them directly and has no zero-registration fallback — see each class's own doc comment
  for why: `Accounting/FamilyGarageChartOfAccountsBuilder` (empty chart — replace once you adopt the
  posting subsystem, see `post-to-accounting`), `Seeding/FamilyGarageClaimsSeeder` (no area claims yet
  — extend as you add entities), `Navigation/FamilyGarageInterfaces` (empty nav tree — extend via
  `add-interface-node`).
- **`FamilyGarage.Persistence`** — `EntityConfigurations/` will be generated once you have entities
  (`Generated.Net/`) — don't hand-write one. `Repositories/FamilyGarageReportsRepository` is the
  (currently empty) implementation of `Domain/Commons/IReportsRepository` — add a method to both per report
  that needs a bespoke query, and inject the interface into that report's behavior; see the `create-report`
  skill. `Documents/FamilyGarageDocumentTypeResolver` is the same kind of required-stub as the Application
  ones above — always returns `null` until you add your first Document type, at which point extend it to
  resolve real `DocumentTypeId` values.
- **`FamilyGarage.Persistence.Databases`** — `Contexts/` (per-provider `FamilyGarageDbContext*`),
  `Migrations/{Sqlite,SqlServer,PostgreSql}/`. `Providers` (this project's own csproj property, no separate
  config needed) is computed from which `Contexts/*.cs` files actually exist on disk — i.e. whichever
  providers you included at scaffold time — so it always reflects reality even after you add/remove a
  provider by hand later. A `migrations.manifest.json` at this repo's root declares all three
  providers/contexts/output dirs for [`kandra-migrate`](https://github.com/kandra-tech/KandraEfMigrationTool),
  a CLI that wraps `dotnet ef` uniformly across every provider currently enabled (resolved from `Providers`,
  not duplicated in the manifest). `.config/dotnet-tools.json` pins `dotnet-ef` and `kandra-migrate` as local tools,
  restored from the public Kandra feed by `dotnet tool restore`: `dotnet kandra-migrate add <Name>`, `list`, `status`
  (see its README for the rest). The manual per-provider commands below do the same by hand; either way, regenerate
  every enabled provider whenever the model changes and keep them in lockstep:
  ```bash
  cd src/FamilyGarage.Persistence.Databases
  dotnet ef migrations add <Name> --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextSqlite     --output-dir Migrations/Sqlite
  dotnet ef migrations add <Name> --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextSqlServer  --output-dir Migrations/SqlServer
  dotnet ef migrations add <Name> --project . --startup-project ../FamilyGarage.WebApi/FamilyGarage.WebApi.csproj --context FamilyGarageDbContextPostgreSql --output-dir Migrations/PostgreSql
  ```
  (Only run the command for a provider you actually included at scaffold time — the others won't exist.)
  **Squashing or regenerating the migrations** (`dotnet kandra-migrate add Initial` after deleting the old files and snapshots): each provider's `Initial`
  creates the 15 subconto composite indexes on the chart-of-accounts tables (`grep -c 'IX_Acc_.*_Entity_'` gives 15): the engine declares them in
  the model, so the regenerated `Initial` has them.
  The base `FamilyGarageDbContext.OnModelCreating` (in `FamilyGarage.Persistence`) contains a
  self-healing guard that unmaps the engine's abstract `DocumentBase` entity until you have at least one
  concrete Document type — don't remove it, it stops applying on its own once you add one.
- **`FamilyGarage.WebApi`** — the runnable host. `Program.cs` is intentionally thin
  (`AddFamilyGarageDatabase` + `AddKandra<...>` + `AddFamilyGaragePersistence` +
  `AddFamilyGarageApplication`). Controllers land under its `Generated.Net/` — 100% generated, never
  hand-write one.
  **Where local data lives:** everything the running app keeps on disk (the SQLite file, `blobs/`, the generated
  JWT signing key in `keys/`, `backups/` taken before migrations) sits under one folder, `Kandra:DataPath`
  (env `Kandra__DataPath`; default: the app folder, never the working directory). A relative SQLite `Data Source`
  is resolved against it, and `appsettings.json` deliberately carries no `JwtOptions:Secret` (each install
  generates its own). Set `JwtOptions__Secret` only when several nodes must share a key. If migrations are ever
  squashed, an old SQLite database is refused with an explanation; `Kandra__Database__ResetToken=<new value>` (or
  `Kandra__Database__IncompatibleHistory=Reset`) recreates it after saving a copy to `backups/`. Keep `Program.cs`
  on `KandraWebApplication.CreateBuilder(args)` so a published app started from another folder still finds its
  `appsettings.json`.
- **`FamilyGarage.Client.MudBlazor`** — the runnable Blazor WebAssembly app. Entity pages are 100%
  generated (`Kandra.Generators.Ui.Blazor`) once you have entities — there is no hand-written page anywhere in
  this project, and there shouldn't be; if a generated page doesn't look right, the fix is in the Dto's
  attributes, never in a hand-edited `.razor` file.
  Branding lives in its `wwwroot/`: `images/logo-gradient-{white,black}.png` (the engine's `KandraLogo` picks
  the white contour on dark surfaces, the black one on light — keep both file names), `favicon.ico`, the
  `icons/` set (16/32 favicons, 192/512 PWA icons, maskable 512, 180 Apple touch) and `manifest.webmanifest`,
  all referenced from `index.html`. To rebrand, replace the files in place.
- **`FamilyGarage.ClientLib.Common`** — hand-written Refit `I*ApiClient` interfaces (Refit's own
  generator emits the implementation into this project's `Generated.Net/`) plus client-side services. Holds
  `ICompanySettingsApiClient` today; `AddFamilyGarageClientServices` (called from
  `ClientFamilyGarageServicesExtensions`) exists because there is at least one form-bearing entity. That
  extension also registers every `*Validator` of the Forms project in the client container
  (`AddValidatorsFromAssembly`): each generated form injects its validator, so one missing here crashes the form in the
  browser while the backend builds and runs fine. Keep that line when you edit the file.
- **`FamilyGarage.Tests`** — a smoke test proving the checked-in migrations apply, the demo-package test, the
  letterhead test and `Localization/LocalizationKeysTests` (lists every resx key the generated UI or your source uses that no
  resx defines). `Support/FamilyGarageTestHost` builds the configuration's real services on SQLite in memory with the
  migrations applied: **use it for every new test** (posting rules, reports) instead of writing DI plumbing, one
  `InScopeAsync` call per test step (editing a document in the scope that read it fails on the change tracker); see
  [`test-configuration`](.claude/skills/test-configuration/SKILL.md). `Services/ServiceResolutionTests` creates every generated CRUD,
  report and data-processor service, so a dependency missing only at runtime (a `{Name}QueryDtoValidator`) fails a test, not a page.
  `Localization/CodeTextTests` fails on words in a report repository (`"Unknown"`, `"Work"`, `"... in 3 days"`), on a
  notification written with `ForLiteral`, on a register table read directly (`db.Reg_X`, `db.Set<XMovement>()`: use the register's
  reader) and on a value that falls back to 1 (`rate ?? 1m`); `DemoPackageTests` applies the demo package (every document must
  post) and fails on fixed dates in it (use `$today-30d`). The test host runs as a test user (`TestUserId`), so a notification
  to the caller can be tested.
  `UPDATE_RESX=1 dotnet test --filter LocalizationKeysTests` appends the missing keys to the English resx with guessed text.

## Adding a new entity — use the skills

- A whole application or module from a specification (mapping its concepts onto entity kinds, the build order, the checks per stage) → [`plan-configuration`](.claude/skills/plan-configuration/SKILL.md)
- Tests for posting rules, register effects and reports, on the shared test host → [`test-configuration`](.claude/skills/test-configuration/SKILL.md)
- Resx keys: which an entity needs, how to add them safely, how to find the missing ones, another UI language → [`localize-configuration`](.claude/skills/localize-configuration/SKILL.md)
- A build error, failing test, startup/runtime error or wrong screen: its usual cause and fix → [`troubleshoot-configuration`](.claude/skills/troubleshoot-configuration/SKILL.md)
- Finding an exact engine signature, generic arguments, DI registration, route or platform resx key (instead of guessing or reading DLLs) → [`look-up-engine-api`](.claude/skills/look-up-engine-api/SKILL.md)
- More company details (tax code, address, phone, bank requisites) on the letterhead → [`extend-company-settings`](.claude/skills/extend-company-settings/SKILL.md)
- New reference/master data (customers, products, warehouses, ...) → [`create-dictionary`](.claude/skills/create-dictionary/SKILL.md)
- New transactional record that posts and moves registers → [`create-document`](.claude/skills/create-document/SKILL.md)
- New read-only parameterized query/export → [`create-report`](.claude/skills/create-report/SKILL.md)
- A document's Print button or a report's PDF/CSV export (parties, table, totals/VAT, signatures, drafts, a report's parameters line and grouping) → [`create-print-form`](.claude/skills/create-print-form/SKILL.md)
- New page of charts, KPI tiles and small tables computed from existing data, optionally auto-refreshing or as the Interface's home page → [`create-dashboard`](.claude/skills/create-dashboard/SKILL.md)
- New transient batch operation (import, period close, recalculation, ...) → [`create-data-processor`](.claude/skills/create-data-processor/SKILL.md)
- New single scalar configuration value (VAT rate, home currency, ...) → [`create-constant`](.claude/skills/create-constant/SKILL.md)
- New closed set of named values (item category, costing method, payment kind, status), plain or as an accounting subconto dimension → [`create-enum`](.claude/skills/create-enum/SKILL.md)
- New analytical/accumulation storage (balances, ledgers, rate tables, ...) → [`create-register`](.claude/skills/create-register/SKILL.md)
- Turning an existing flat dictionary into a folder/leaf tree (grouping/categories added later, data already exists) → [`convert-dictionary-to-hierarchical`](.claude/skills/convert-dictionary-to-hierarchical/SKILL.md)
- New mid-edit server round-trip (price lookup, discount recalc, payment-state check, ...) — the server endpoint, independent of any document → [`create-handler`](.claude/skills/create-handler/SKILL.md)
- UI logic of a form: client-side recompute (row total, document sum, derived field), dependent fields, show/hide, hooks around open/save, calling a handler from the form → [`create-ui-behavior`](.claude/skills/create-ui-behavior/SKILL.md)
- Post a document to the chart-of-accounts ledger (debit/credit account pairs + subconto values) → [`post-to-accounting`](.claude/skills/post-to-accounting/SKILL.md)
- Send an in-app notification (bell/inbox) from a data processor or document behavior, incl. unattended scheduled runs → [`send-notifications`](.claude/skills/send-notifications/SKILL.md)
- Add account/subconto picker fields to a Dto (manual-posting documents, report filters) → [`add-account-subconto-field`](.claude/skills/add-account-subconto-field/SKILL.md)
- Adding a nav entry, regrouping one, gating it by permission, or defining a second Interface for a different business role → [`add-interface-node`](.claude/skills/add-interface-node/SKILL.md)
- Writing or extending an import package: the configuration's demo data, a data migration, or an importer for a user's own file → [`author-import-package`](.claude/skills/author-import-package/SKILL.md)

## Demo data (`ImportPackages/`)

`FamilyGarage.Application/ImportPackages/` holds the configuration's **import packages**, embedded in the assembly
(`<EmbeddedResource Include="ImportPackages\*.json" Exclude="ImportPackages\*.schema.json" />`) and registered by
`services.AddImportPackages(...)` in `ConfigureServices`. It starts with `{name}-demo.json`, a valid `origin: "demoSeed"`
package with no steps: the demo company of this configuration goes there, applied on a demo installation
(`IDemoSeeding`, which also creates the sample users from `Seeding/FamilyGarageDemoUsers.cs`; keep those within
the free tier's five users including admin) and from the admin page `/admin/import`. `familygarage.import.schema.json` is a generated JSON Schema snapshot of
the configuration's entities and fields for editor completion (`$schema` in the package); `Tests/ImportPackages/DemoPackageTests`
fails when it is stale, and `UPDATE_IMPORT_SCHEMA=1 dotnet test` rewrites it after you add an entity or field.
See [`author-import-package`](.claude/skills/author-import-package/SKILL.md).

These skills are written config-agnostically, but their worked examples still show concrete entity names
carried over from a real, previously-built configuration (kept as-is because they're verified working code —
this configuration has none of its own yet). Treat those names as illustrative and substitute your own
domain's names throughout.

## Docker (`Dockerfile`, `compose.*.yml`)

`Dockerfile` publishes `FamilyGarage.WebApi` (which hosts the WASM client) into one image for every scaffolded database
provider; the runtime `Provider` env var picks one. `Kandra__DataPath=/data` is a volume holding the SQLite file, blobs and the
signing key. `compose.{sqlite,sqlserver,postgres}.yml` exist only for the providers included at scaffold time; the app service is
named `app`, the DB service `db`, and `.env.example` documents the variables (`MSSQL_SA_PASSWORD`, `POSTGRES_PASSWORD`, `APP_*`).
`compose.demo.yml` is the demo overlay (`Kandra__Setup__Mode=Demo`). When you add a project the WebApi needs at build time, nothing
changes (the Dockerfile copies the whole folder); when you rename the WebApi project, update the `dotnet publish` path and the
`ENTRYPOINT` dll. The image and the database names in the compose files are `kandra-<lowercase configuration name>`
(e.g. `kandra-acme`); `appsettings.json` keeps its own development database names, and compose overrides the connection strings.

## Navigation (Interfaces)

The sidebar nav menu is server-driven, not a hand-edited `.razor` file.
`FamilyGarage.Application/Navigation/FamilyGarageInterfaces.cs` is this configuration's one
registered `Default` Interface, which holds only the **Company settings** data processor (a Data Processors folder)
— every configuration needs at least one registered Interface. Add nodes to it (or define a second Interface
for a different business role) via [`add-interface-node`](.claude/skills/add-interface-node/SKILL.md).

## Chart-of-accounts / posting subsystem

Lives partly in the engine and partly here as configuration (chart definition, subconto wiring) — it's a
second, deliberately independent accounting engine, not built on the Balance/Movement register engine
`create-register`/`create-document` cover — don't try to unify the two.
`FamilyGarage.Application/Accounting/FamilyGarageChartOfAccountsBuilder` currently returns an
empty chart; see [`post-to-accounting`](.claude/skills/post-to-accounting/SKILL.md) for wiring a document's
posting and adding real accounts, and
[`add-account-subconto-field`](.claude/skills/add-account-subconto-field/SKILL.md) for the account/subconto
picker UI (only needed when the account is a user runtime choice — most documents post to fixed,
compile-time-known accounts and skip the UI entirely).
