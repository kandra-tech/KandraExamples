using System.ComponentModel;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Forms.DataProcessors;

namespace PersonalAccounting.Forms.DataProcessors;

/// <summary>
/// The company's details as one form over the Company* constants (name and logo to begin with). New fills the fields with the values
/// valid now, Execute writes only the ones that changed. The values stay constants, with their history, because every reader (the print
/// letterhead) asks for them as of a document's date. To add another detail (tax code, address, phone, bank requisites) see the
/// extend-company-settings skill.
/// </summary>
[KandraDataProcessorForm(Name = "CompanySettings", ValidatorType = typeof(CompanySettingsValidator), ResultDtoType = typeof(CompanySettingsResult))]
[ExecuteFormTitle("CompanySettings")]
[FormSection("CompanySettings_Identity", Order = 0)]
[FormSection("CompanySettings_Printing", Order = 1)]
[Description("Shows and changes the company's details: name and the company logo. New fills every " +
    "field with the value valid now; Execute writes only the values that differ from the ones valid on the effective date, as new " +
    "dated values of the matching constants, so documents dated earlier keep printing the details they had. This is the intended " +
    "editor of the CompanyLogo constant, whose value (a blob reference id) the constants admin page shows as raw JSON.")]
public class CompanySettingsDto : IDataProcessorInputDto
{
    /// <summary>The date the changed values apply from; defaulted to today by the behavior's OnNew hook.</summary>
    [Caption("CompanySettings_EffectiveFrom")]
    [DateEditor(DateEditorKind.Date)]
    [Description("The date the changed values apply from: today, or a later date to schedule a change. A date in the past also corrects " +
        "documents dated since then (constants are read as of the document's date). Defaulted to today by New.")]
    public DateTime EffectiveFrom { get; set; }

    [SectionRef("CompanySettings_Identity")]
    [Caption("CompanyName")]
    [Description("CompanyName constant: the company's name, printed in the letterhead. An empty name prints no letterhead.")]
    public string? Name { get; set; }

    /// <summary>The logo image (a blob reference id). Shows the current logo with a download link; an emptied field removes the logo from
    /// the effective date. A newly picked file is uploaded only after the form has validated, so the behavior checks it.</summary>
    [SectionRef("CompanySettings_Printing")]
    [Caption("CompanySettings_Logo")]
    [FilePicker(Extensions = "*.png;*.jpg;*.jpeg")]
    [Description("CompanyLogo constant: blob reference id of the logo printed in the letterhead. A PNG or JPEG image of at most 1 MB. " +
        "Earlier logos stay attached, so older documents keep printing theirs. Emptying the field removes the logo from the effective date " +
        "(deletes none).")]
    public Guid? Logo { get; set; }
}

[Description("Result of running CompanySettings: which values were written.")]
public class CompanySettingsResult : IDataProcessorResultDto
{
    [Order(1)]
    [Caption("CompanySettings_Outcome")]
    [Description("A sentence naming the values that were changed and the date they apply from, or saying that nothing changed.")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>Mandatory for every form-bearing entity, even with no rule of its own yet.</summary>
public class CompanySettingsValidator : AbstractValidator<CompanySettingsDto>;
