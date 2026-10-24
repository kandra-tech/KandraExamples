using System.ComponentModel;       // [Description]
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using FluentValidation;
using Kandra.Attributes;
using Kandra.Attributes.Application;
using Kandra.Attributes.Editors;
using Kandra.Attributes.Layout;
using Kandra.Attributes.Naming;
using Kandra.Attributes.Printable;
using Kandra.DtoCommon.Printing;
using Kandra.Forms.Printable;
using Kandra.Forms.Reports;
// using Kandra.Forms.Querying; // OPTIONAL — only if a [DateRangeEditor] field is used (DateRangeDto)
using {{Config}}.Forms.Common;
using {{Config}}.Forms.Dictionaries;

namespace {{Config}}.Forms.Reports;

// ── One row of the results grid ──────────────────────────────────────────────────────────────────
public sealed class {{Name}}ReportLineDto
{
    [Required]
    public Guid Id { get; set; }

    // A plain wire Code/Name pair backing a composite display column below — no [ReportViewColumn]/
    // [PrintColumn] of its own, it isn't an independent column.
    [Required]
    public Guid {{DimensionField}}Id { get; set; }
    [Required]
    public string {{DimensionField}}Code { get; set; }
    [Required]
    public string {{DimensionField}}Name { get; set; }

    // Composite "Code - Name" column — CodeNameRef.Combine is the one shared convention for this, never a
    // hand-rolled $"{Code} - {Name}". [JsonIgnore] since only the two wire fields above round-trip.
    [Order(1)]
    [PrintColumn]
    [Caption("{{DimensionField}}")]
    [ReportViewColumn(Groupable = true)]
    [JsonIgnore]
    public string {{DimensionField}}Text => CodeNameRef.Combine({{DimensionField}}Code, {{DimensionField}}Name);

    // A numeric measure column, with a built-in group-footer aggregate — Kind default is Sum, set explicitly
    // for clarity. Delete [Aggregate] entirely if this column shouldn't sum/avg/count in a group footer.
    [Required]
    [Order(2)]
    [PrintColumn]
    [Caption("{{MeasureField}}")]
    [Format("N2")]
    [Totals(Aggregate.Sum)] // OPTIONAL — print-form totals row, see create-print-form; delete if not needed
    [ReportViewColumn]
    [Aggregate(Kind = AggregateKind.Sum)]
    public decimal {{MeasureField}} { get; set; }

    // OPTIONAL — delete this block if no column needs a non-additive aggregate (e.g. summing raw quantities
    // across different items mixes incompatible units of measure — sum only when grouped by the item column).
    // Replace [Aggregate(Kind = ...)] above with [Aggregate(CustomAggregateType = typeof({{Name}}QuantityAggregate))]
    // on the field that needs this, and delete the plain-Kind version.
}

// ── Filters Dto ───────────────────────────────────────────────────────────────────────────────────
[KandraReportForm(Name = "{{Name}}Report", ResultDtoType = typeof({{Name}}ReportResult))]
[SupportedExportFormats(ExportFormat.Csv | ExportFormat.Pdf | ExportFormat.Excel)]
[SupportedPrintForm(typeof({{Name}}ReportDataDto))]
[ExecuteFormTitle("{{Name}}Report")] // declared for convention/forward-compat — currently unconsumed, see reference.md §1/§6
// [Description] is mandatory on this class — it's the one Dto the MCP server's describe_entity_type/
// list_entity_types tools reflect for a Report ([KandraReportForm] lives here, not on the results-grid line
// Dto above or the print-only *ReportDataDto below, neither of which MCP ever sees); [Caption] is only a
// short UI label, not an explanation. See reference.md §2/§9.
[Description("TODO — one or two sentences: what this report shows and where the data comes from.")]
public sealed class Report{{Name}}Dto : IReportFiltersDto
{
    // A plain nullable filter, no editor attribute needed for a bool/simple value. Add [Description]
    // whenever a filter's meaning isn't obvious from its name/caption alone:
    [Caption("{{BoolFilterCaption}}")]
    [Description("What this filter does when true/false.")]
    public bool {{BoolFilterField}} { get; set; }

    // OPTIONAL — a single date filter:
    // [Caption("DateAsOf")] [DateEditor(DateEditorKind.Date)]
    // [Description("What this filter does when set.")]
    // public DateTime? DateAsOf { get; set; }

    // OPTIONAL — a From/To range filter (needs `using Kandra.Forms.Querying;` uncommented above):
    // [DateRangeEditor]
    // public DateRangeDto? DateRange { get; set; }

    // OPTIONAL — a required single-value picker that gates FiltersReady() (ReportUiEmitter auto-generates
    // "!= Guid.Empty" for every [Required] Guid field — see reference.md §5/§6):
    // [Required] [Caption("{{RequiredFilterCaption}}")] [Dropdown(typeof({{RequiredFilterSourceDto}}))]
    // [Description("What this filter does.")]
    // public Guid {{RequiredFilterField}}Id { get; set; }

    // A multi-select filter, moved into the collapsed "extended filters" panel:
    [Caption("{{MultiSelectFilterCaption}}")]
    [MultiSelect(typeof({{MultiSelectSourceDto}}))]
    [ExtendedFilter]
    [Description("Restricts results to these {{MultiSelectFilterCaption}} (Dictionary ids); null/empty includes everything.")]
    public IReadOnlyList<Guid>? {{MultiSelectFilterField}}Id { get; set; }
}

// ── Print/business data — everything but the technical envelope (extends ReportPrintForm, see create-print-form) ──
[PrintForm]
[PrintTitle("{{Name}}Report")]
[Page(Size = PageSize.A4, Orientation = PageOrientation.Landscape, MarginsMm = new[] { 15, 10, 15, 10 })]
public sealed class {{Name}}ReportDataDto : ReportPrintForm
{
    // GeneratedAt/GeneratedBy come from ReportPrintForm (the page footer) - enriched server-side in the Behavior's
    // GetDataAsync. A [PrintRole(PrintRole.Parameter)] property is one "Caption: value" part of the parameters line
    // under the title; an empty value is skipped. The Frame is Internal (no letterhead) unless the form says otherwise.
    [PrintRole(PrintRole.Parameter)]
    [Caption("{{MultiSelectFilterCaption}}")]
    public IReadOnlyCollection<CodeNameRef>? {{MultiSelectFilterField}}s { get; set; }

    [Required]
    [PrintTable]
    [ReportViewGrid]
    public required ICollection<{{Name}}ReportLineDto> Lines { get; set; }
}

// ── Technical envelope: Filters + Data + CacheTag, closed generic with no members of its own ──────
public sealed class {{Name}}ReportResult : ReportResult<Report{{Name}}Dto, {{Name}}ReportDataDto>;

// ── Validator — plain FluentValidation AbstractValidator<T>, mandatory even if empty ──────────────
public class Report{{Name}}DtoValidator : AbstractValidator<Report{{Name}}Dto>
{
    public Report{{Name}}DtoValidator()
    {
        RuleForEach(x => x.{{MultiSelectFilterField}}Id).NotEmpty().When(x => x.{{MultiSelectFilterField}}Id?.Any() == true);

        // OPTIONAL — if a [DateRangeEditor] field is used:
        // RuleFor(x => x.DateRange)
        //     .Must(r => r is null || r.From is null || r.To is null || r.From.Value < r.To.Value)
        //     .WithName(nameof(Report{{Name}}Dto.DateRange));

        // OPTIONAL — if a [Required] Guid filter is used:
        // RuleFor(x => x.{{RequiredFilterField}}Id).NotEmpty();
    }
}

// ── OPTIONAL — delete this whole class if no column needs a non-built-in aggregate. Lives alongside the
// line Dto it aggregates (referenced by [Aggregate(CustomAggregateType = typeof(...))] on one of its
// properties), never through the interface reference - the generator emits a direct static call. ──
public sealed class {{Name}}QuantityAggregate : ICustomAggregate
{
    public static string? Calculate<T>(IEnumerable<T> items, ReportGroupingState groupingState, string? format) =>
        groupingState.FrontGroup == nameof({{Name}}ReportLineDto.{{DimensionField}}Text) && items is IEnumerable<{{Name}}ReportLineDto> lines
            ? lines.Sum(x => x.{{MeasureField}}).ToString(format)
            : "";
}
