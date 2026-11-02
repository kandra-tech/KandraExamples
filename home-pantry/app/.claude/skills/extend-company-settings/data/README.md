# data/ - extend-company-settings templates

Snippets, not whole files: each says which file it belongs to. `{{Detail}}` is the PascalCase detail name (TaxCode,
Address, Phone), `{{description}}` its plain-English meaning, `{{new-guid}}` a fresh GUID. They are the pieces the
template removed from its first version (tax code, address, phone), so they are known to build.
