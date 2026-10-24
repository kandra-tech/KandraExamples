---
name: add-interface-node
description: Add an entity/folder/link to the navigation menu, or define a new Interface (a named navigation tree for a business role). Use when the user asks to add something to the sidebar/nav menu, regroup existing nav links, hide a menu item behind a permission, or create a role-specific menu.
---

# Adding to an Interface navigation tree

> **Note:** This recipe's worked example(s) use concrete entity names (specific dictionaries, documents, reports, etc.) carried over from a real, previously-built Kandra configuration, kept as-is because they're verified working code — this bare configuration has no entities of its own yet. Treat those names as illustrative only and substitute your own domain's names throughout; only the underlying pattern/mechanism matters.

**Interface** is a server-defined navigation tree (a 1C-style "command interface") that the API serves and a
generic renderer displays — there is no hand-edited nav `.razor` file to touch in this configuration anymore.
The whole sidebar for `PersonalAccounting` is one `InterfaceDefinition` tree,
`PersonalAccounting.Application/Navigation/PersonalAccountingInterfaces.cs`'s `Default` — that file is the concrete,
currently-working example for everything below. See [`reference.md`](reference.md) for the full mechanism (the
four node shapes, exactly what gets auto-derived for an Entity node, the permission model, and
troubleshooting) — this file is the step-by-step "where do I add my line" recipe.

## 1. The most common case: add an entity to the existing `Default` Interface

Open `PersonalAccounting.Application/Navigation/PersonalAccountingInterfaces.cs` and add one line inside the relevant
`InterfaceNodeDefinition.Folder(...)` call:

```csharp
InterfaceNodeDefinition.Entity(typeof(YourNewThingDto)),
```

That's it for a Document/Dictionary/Report/DataProcessor Dto that already carries its `[Kandra*Form]` attribute
(i.e. you've already run `create-document`/`create-dictionary`/`create-report`/`create-data-processor`) — route,
nav caption, icon, and View/Execute permission check are all auto-derived from that attribute (see
[`reference.md`](reference.md) for exactly how, and the one exception: Reports never get an auto-derived
permission check).

For a hierarchical dictionary, bind to the **root** Dto (e.g. `ItemNodeDto`, `CounterpartyNodeDto`), not the
leaf — same as the `[KandraDictionaryForm]` attribute's own placement.

Overrides, if the defaults aren't right:

```csharp
InterfaceNodeDefinition.Entity(typeof(YourNewThingDto), icon: "ShoppingCart", caption: new InterfaceNodeText("Nav_YourThing"))
```

`icon` is a plain string matched against a field on `MudBlazor.Icons.Material.Filled` (e.g. `"Description"`,
`"List"`, `"Print"`, `"CloudDownload"`, `"ShoppingCart"` — any real field name on that class). `caption` takes
an `InterfaceNodeText(value, isLiteral: false)`: a resource key by default (add it to the resx files under
`PersonalAccounting.Localization` the same way every other nav caption is), or `isLiteral: true` for a literal string.

## 2. Add a new folder / regroup

```csharp
InterfaceNodeDefinition.Folder("Nav_YourGroup", null,
    InterfaceNodeDefinition.Entity(typeof(ThingOneDto)),
    InterfaceNodeDefinition.Entity(typeof(ThingTwoDto))),
```

A `Folder`'s second argument is its icon (usually `null` — the existing top-level groups don't have one, only
their leaf links do). A folder left with zero visible children (every child pruned by permission) is itself
omitted from the served tree — nothing extra to write for that.

## 3. A page that isn't bound to any Dto (dev tool, external link, ...)

Use `InterfaceNodeDefinition.Link(route, caption, icon?, visibility?)` — see the existing "Selector Demo" entry
in `PersonalAccountingInterfaces.cs` for a real example (an admin-only dev page). This is also the escape hatch for
anything the attribute-driven `Entity`/engine-recognized `Special` shapes don't cover. Don't add a new
`SpecialInterfaceNodeKind` member for a configuration-specific page — `Link` is the mechanism for that instead
(see [`reference.md`](reference.md) for why).

## 4. Gating a node by permission

- An `Entity` node's gate is auto-derived — nothing to write.
- A `Special`/`Link` node takes an optional `visibility:` parameter:
  `InterfaceNodeVisibility.AdminOnly()`, or `InterfaceNodeVisibility.Claim(ApplicationArea.Document /* or
  Dictionary/Report/DataProcessor/Other */, "SomeObjectName", ApplicationAction.View /* or Insert/Update/
  Delete/Execute */)` to reuse an existing claim.
- Omit `visibility` (or pass `null`) for "visible to everyone" — matches the `ChartOfAccounts` node's unguarded
  behavior.

See [`reference.md`](reference.md) for what `InterfaceNodeVisibility` actually checks server-side and why a
policy-name-string shape doesn't apply here.

## 5. Defining a second Interface (a different menu for a business role)

```csharp
public static InterfaceDefinition WarehouseOperator { get; } = new(
    Name: "WarehouseOperator",
    Title: new InterfaceNodeText("Nav_WarehouseOperatorInterface"),
    Visibility: InterfaceNodeVisibility.Claim(ApplicationArea.Other, "Interface:WarehouseOperator", ApplicationAction.View),
    Nodes: [ /* a subset of nodes, same shapes as above */ ]);
```

Register it alongside `Default` in `PersonalAccounting.Application/ConfigureServices.cs`:

```csharp
services.AddKandraInterface(PersonalAccountingInterfaces.Default);
services.AddKandraInterface(PersonalAccountingInterfaces.WarehouseOperator);
```

**Registration order is significant** — the first `AddKandraInterface` call is served as the default. Once more
than one Interface is visible to a user, a switcher automatically appears next to the language selector — no
client-side wiring needed. See [`reference.md`](reference.md) for how the default/switcher selection works.

## 6. Verify

`dotnet build PersonalAccounting.slnx`, then run the app (`dotnet run --project src/PersonalAccounting.WebApi
--launch-profile PersonalAccounting.WebApi`) and confirm in the browser: the new node/folder appears in the sidebar in
the right place with the right icon/caption, clicking it navigates to the right page, and (if you added a
`visibility` gate) it disappears for a user/role that shouldn't see it. `GET /api/v1/interfaces/Default` (or
whichever Interface name) also lets you inspect the resolved tree directly — see
[`reference.md`](reference.md)'s Troubleshooting section if something doesn't show up as expected.
