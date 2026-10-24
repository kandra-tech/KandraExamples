// --- CompanySettingsBehavior (PersonalAccounting.Application/DataProcessors/CompanySettings.cs) --------------------------

// In OnNewAsync, after the existing reads:
input.{{Detail}} = await ReadAsync<Company{{Detail}}Constant>(now, cancellationToken);

// In OnProcessAsync, after the existing WriteAsync<CompanyNameConstant>(...) call:
await WriteAsync<Company{{Detail}}Constant>("Company{{Detail}}", input.{{Detail}}, at, now, changed, cancellationToken);
// The first argument is the caption key shown in the "Saved ..." message, so it needs a resx entry (it is also the form caption).
