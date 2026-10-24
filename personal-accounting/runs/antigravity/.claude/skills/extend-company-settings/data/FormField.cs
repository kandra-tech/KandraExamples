// --- CompanySettingsDto (PersonalAccounting.Forms/DataProcessors/CompanySettings.cs) ---------------------------------

// Optional: a new section, above the class (the Printing section's Order moves up by one).
[FormSection("CompanySettings_Contacts", Order = 1)]

// The field, next to Name/Logo:
[SectionRef("CompanySettings_Contacts")]
[Caption("Company{{Detail}}")]
[Description("Company{{Detail}} constant: the company's {{description}}, printed in the letterhead.")]
public string? {{Detail}} { get; set; }

// --- A format rule, in CompanySettingsValidator (the class has no constructor until you need one) ----------------------
// Needs: using System.Text.RegularExpressions; using Microsoft.Extensions.Localization;
public class CompanySettingsValidator : AbstractValidator<CompanySettingsDto>
{
    // EXAMPLE ONLY: 8 or 10 digits is the Ukrainian tax code (EDRPOU / RNOKPP). Use your country's format, or drop the rule.
    // The message is a resx key (same convention as every validator here); it needs an entry in the resx.
    private static readonly Regex TaxCodeRegex = new(@"^\s*(\d{8}|\d{10})\s*$", RegexOptions.CultureInvariant);

    public CompanySettingsValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.TaxCode)
            .Matches(TaxCodeRegex).WithMessage(localizer["CompanySettings_TaxCodeFormat"])
            .When(x => !string.IsNullOrWhiteSpace(x.TaxCode));
    }
}
