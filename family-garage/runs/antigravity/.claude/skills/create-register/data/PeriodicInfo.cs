// TEMPLATE — Independent/Periodic Info family: no live consumer in this repo yet (../reference.md §1/§8) —
// read that before picking this over document-bound Info. Rows are written directly (imports, admin UI,
// dictionary behaviors), NOT only through document posting — the one family whose writer is plain,
// constructor-injectable DI, not gated behind ISubmitScope.
// Target: {{Config}}.Domain/Entities/Registers/{{Name}}.cs (append below Dimensions.cs and Details.cs in the
// same file — Details is effectively mandatory for this family, since there's nothing to store otherwise).
// Placeholder legend: data/README.md. Concept reference: ../reference.md §1/§4/§5/§6.
//
// [KandraRegisterEntity(TableName = "...")] goes on the Record class. No [RegisterCaption] needed/useful here
// — the register-transactions viewer only reads Movement/DocumentInfoRecord rows (this family has no
// DocumentId to attribute rows to), so this row type never appears in that viewer regardless. DI wiring
// (AddPeriodicInfoRegister<...>()) and EF configuration are both fully generated — nothing to hand-write.

using Kandra.Attributes.Entities;
using Kandra.Domain.Registers;

namespace {{Config}}.Domain.Entities.Registers;

[KandraRegisterEntity(TableName = "Reg_{{Name}}")]
public sealed class {{Name}}Record : PeriodicInfoRecord<{{Name}}Dimensions, {{Name}}Details>
{
}

[Description("{{RegisterDescription}}")] // what it measures/stores, the IsExpense direction meaning, resource units, sentinels
public sealed partial class {{Name}}Register : PeriodicInfoRegister<{{Name}}Record>
{
}

// ---- Write from ANY service via plain constructor injection — NOT gated behind ISubmitScope, unlike every
// other family (../reference.md §5). Sketch:
//
//   public class {{SomeService}}(IInfoRegisterWriter<{{Name}}Record, {{Name}}Dimensions, {{Name}}Details> writer)
//   {
//       public Task SetAsync({{Name}}Dimensions dimensions, DateTime period, {{Name}}Details details, CancellationToken cancellationToken) =>
//           writer.SetAsync(dimensions, period, details, cancellationToken);
//
//       public Task DeleteAsync({{Name}}Dimensions dimensions, DateTime period, CancellationToken cancellationToken) =>
//           writer.DeleteAsync(dimensions, period, cancellationToken);
//   }
