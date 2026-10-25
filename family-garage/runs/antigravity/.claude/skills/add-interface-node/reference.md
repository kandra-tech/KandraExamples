# Interface navigation trees — reference

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

Self-contained explanation of the mechanism `SKILL.md` walks through step by step — no need to go read engine
source to use it. **Interface** (`Kandra.Application.Abstractions.Navigation`, engine-owned) is a named,
server-defined navigation tree — a 1C-style "command interface" — that the API serves and a generic engine
`NavMenu.razor` renders. There is no hand-edited nav `.razor` file to touch in this configuration anymore; the
whole sidebar for `FamilyGarage` is the one `FamilyGarageInterfaces.Default` tree defined in
`FamilyGarage.Application/Navigation/FamilyGarageInterfaces.cs`.

## The four node shapes

`InterfaceNodeDefinition` is a closed set of four shapes, built via static factories:

- **`Folder(caption, icon, children...)`** — a `MudNavGroup` client-side. A folder left with zero visible
  children (every child pruned by permission) is dropped from the served tree entirely.
- **`Entity(dtoType, icon?, caption?)`** — bound to a Dto that already carries its `[KandraDocumentForm]`/
  `[KandraDictionaryForm]`/`[KandraReportForm]`/`[KandraDataProcessorForm]` attribute (i.e. you've already run
  `create-document`/`create-dictionary`/`create-report`/`create-data-processor`). Its route, nav caption, icon,
  and View/Execute permission check are all auto-derived by reflecting over that attribute — see "What gets
  auto-derived" below.
- **`Special(kind, caption, icon?, visibility?)`** — an engine-recognized predefined page. Today the only kind
  is `SpecialInterfaceNodeKind.ChartOfAccounts`. This enum is engine-owned and closed on purpose — never add a
  `FamilyGarage`-specific value to it (that would leak a configuration concept into the engine); use `Link`
  instead for anything configuration-specific that isn't Dto-bound.
- **`Link(route, caption, icon?, visibility?)`** — a hardcoded, non-Dto-bound route. The escape hatch for
  anything the other three shapes don't cover: a dev/admin tool page, an external link, anything hand-routed.
  See the existing "Selector Demo" node in `FamilyGarageInterfaces.cs` for a real, working example (an admin-only
  dev page).

`icon` is a plain string matched against a field name on `MudBlazor.Icons.Material.Filled` (e.g.
`"Description"`, `"List"`, `"Print"`, `"CloudDownload"`, `"ShoppingCart"`) — resolved client-side. `caption`
takes an `InterfaceNodeText(value, isLiteral: false)`: a resource key by default (resolved via the client's
`IStringLocalizer` — add the key to the resx files under `FamilyGarage.Localization` the same way every other nav
caption already works), or `isLiteral: true` for a literal string with no localization lookup.

## What gets auto-derived for an `Entity` node

Reflecting over the bound Dto's `[Kandra*Form]` attribute reproduces the same route/permission convention every
generated CRUD page already follows:

| Dto kind | Route | Permission check |
|---|---|---|
| Document | `/document/{stem}/1` | Claim `Area=Document, ObjectName={Stem}, Action=View` |
| Flat dictionary | `/dictionary/{stem}/1` | Claim `Area=Dictionary, ObjectName={Stem}, Action=View` |
| Hierarchical dictionary (bind the **root** Dto, e.g. `ItemNodeDto`) | `/dictionary/{leafStem}` (no page number) | Claim `Area=Dictionary, ObjectName={LeafStem}, Action=View` |
| Report | `/reports/{name.ToLowerInvariant()}` | Claim `Area=Report, ObjectName={Name of [KandraReportForm]}, Action=View` |
| DataProcessor | `/dataprocessor/{stem}` | Claim `Area=DataProcessor, ObjectName={Stem}, Action=Execute` |

`{Stem}` is the Dto's simple type name with a trailing `Dto` stripped (`WaybillDto` → `Waybill`). For a
hierarchical dictionary the stem instead comes from the concrete **leaf** Dto (resolved from the root Dto's own
`[JsonDerivedType]` attributes, skipping the one ending `FolderDto`) — e.g. `ItemNodeDto`'s tree resolves to
`Item`, not `ItemNode`.

**A report's object name is the `Name` of its `[KandraReportForm]`** (e.g. `RestsReport`), not a stem of the Dto's type
name, and its behavior's `ObjectName` must equal it (see `create-report` reference §8).

## Gating with `InterfaceNodeVisibility` — not a named-policy string

`Special`/`Link` nodes take an optional `visibility:` parameter (an `Entity` node's is always auto-derived, see
above):

- `InterfaceNodeVisibility.AdminOnly()` — visible only to an admin.
- `InterfaceNodeVisibility.Claim(ApplicationArea.Document /* or Dictionary/Report/DataProcessor/Other */,
  "SomeObjectName", ApplicationAction.View /* or Insert/Update/Delete/Execute */)` — reuses the same
  claim-based check (`O|{ObjectName}|{Action}` / area-wide `A|{Area}|{Action}` claims, checked by
  `IAuthorizationChecker.Authorize`) that every generated entity page already enforces. Pass the same
  `ObjectName` an existing entity's own View claim uses if you want a node to share visibility with it.
- Omit `visibility` (or pass `null`) for "visible to everyone" — matches the `ChartOfAccounts` node's
  unguarded behavior.

**Do not use an ASP.NET Core named-policy string here** (e.g. something that looks like `"ViewWaybill"`).
Those names only exist client-side, registered purely to gate `AuthorizeView` markup in hand-written Blazor —
there is no server-side registration of named policies at all in this codebase. Passing one through
`InterfaceNodeVisibility` isn't even possible (the type doesn't accept a string); this note exists only so you
don't go looking for that shape by habit from older, hand-written pages.

## Registering an Interface and picking the default

```csharp
services.AddKandraInterface(FamilyGarageInterfaces.Default);
services.AddKandraInterface(FamilyGarageInterfaces.WarehouseOperator); // a second, optional Interface
```

Call order is significant: the **first** `AddKandraInterface` call is served as the default (used when a user
has no stored preference, or their stored preference is no longer visible to them). Once more than one
Interface is visible to a signed-in user, a switcher automatically appears next to the language selector — no
client-side wiring needed for that; it's driven entirely by how many Interfaces the API returns as visible.

`InterfaceDefinition` itself also takes a `Visibility` (same `InterfaceNodeVisibility?` shape as a node) —
gates the whole Interface, not just one node. `Default`'s is `null` (visible to every authenticated user); a
real per-role Interface almost always wants a `Claim(...)` gate here, seeded as a normal claim (see
`FamilyGarageClaimsSeeder`) for whichever role should see it.

## Troubleshooting

- **A node doesn't show up**: check the permission first — `GET /api/v1/interfaces/{name}` (any authenticated
  session) shows exactly the pruned tree the current caller can see; a missing node almost always means the
  visibility check failed, not a routing/typo bug.
- **A node's link 404s when clicked**: the auto-derived route (Entity nodes) is computed purely from the Dto's
  type name — double-check the Dto's actual generated page route matches (it always does if you haven't
  hand-renamed the Dto class since generating its page; if in doubt, check the Dto's own generated `[Route]`
  attribute under `FamilyGarage.Client.MudBlazor/Generated.Net/`).
- **Localized caption shows as the raw key**: the resx key referenced by `caption`/`[NavigationName]` doesn't
  exist yet in `FamilyGarage.Localization` for the active culture — add it, or pass `isLiteral: true` if it's
  meant to be a literal string.
