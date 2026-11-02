# HomePantry

Single-household pantry and stock management application built on the Kandra platform. How it was built and reviewed: [BUILD-NOTES.md](../BUILD-NOTES.md).

## Build

```bash
dotnet restore
dotnet build HomePantry.slnx
```

## Run

To run the application with the Web UI on `http://localhost:5228` (and `https://localhost:7228`):

```bash
dotnet run --project src/HomePantry.WebApi --launch-profile HomePantry.UI
```

To run with automatic demo data seeding and sample user initialization:

```bash
$env:Kandra__Setup__Mode="Demo"
$env:ACCEPT_EULA="Y"
dotnet run --project src/HomePantry.WebApi --launch-profile HomePantry.UI
```

## Sign In

- **URL:** `http://localhost:5228` or `https://localhost:7228`
- **Username:** `admin`
- **Password:** `Admin!1` (for demo mode) or the password configured during the first-run setup wizard.

## Run Tests

Run the full automated test suite (including acceptance checks 1–7):

```bash
dotnet test HomePantry.slnx
```
