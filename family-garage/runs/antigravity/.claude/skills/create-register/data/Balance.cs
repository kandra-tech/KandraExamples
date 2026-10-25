// TEMPLATE — Balance family: row entities + central register class.
// Target: {{Config}}.Domain/Entities/Registers/{{Name}}.cs (append below Dimensions.cs and, if used,
// Resources.cs/Details.cs in the same file).
// Placeholder legend: data/README.md. Concept reference: ../reference.md §1/§4/§6/§8.
//
// [KandraRegisterEntity(TableName = "...")] is mandatory on BOTH row types. [RegisterCaption] goes on the
// Movement only, never the Balance. Swap {{Name}}Details for NoDetails (Kandra.Domain.Registers) if this
// register carries no Details class. AddBalanceRegister<...>() DI wiring is fully generated — nothing to
// hand-write (../reference.md §6) — and so is the EF configuration (table/FK/precision), so there is
// nothing else in this file beyond what's below.

using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Domain.Registers;

namespace {{Config}}.Domain.Entities.Registers;

[KandraRegisterEntity(TableName = "Reg_{{Name}}_Balance")]
public sealed class {{Name}}Balance : Balance<{{Name}}Dimensions, {{Name}}Resources>
{
}

[KandraRegisterEntity(TableName = "Reg_{{Name}}")]
[RegisterCaption("{{RegisterCaptionKey}}")]
public sealed class {{Name}}Movement : Movement<{{Name}}Dimensions, {{Name}}Resources, {{Name}}Details>
{
}

[Description("{{RegisterDescription}}")] // what it measures/stores, the IsExpense direction meaning, resource units, sentinels
public sealed partial class {{Name}}Register : BalanceRegister<{{Name}}Movement, {{Name}}Balance>
{
}

// ---- Write this register only from a document Behavior's OnSubmitAsync/OnUnsubmitAsync/OnDeleteAsync, via
// ISubmitScope — see ../reference.md §5 for why (keyed DI, not just convention) and ../SKILL.md step 6.
// Sketch, to paste into {{Config}}.Application/Documents/{{Doc}}.cs's Behavior class:
//
//   [KandraDocumentBehavior(typeof({{Doc}}Dto))]
//   [RegisterTransactions(typeof({{Name}}Movement))]
//   public class {{Doc}}Behavior(...) : IDocumentBehavior<{{Doc}}>
//   {
//       public async ValueTask OnSubmitAsync({{Doc}} entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
//       {
//           var movements = entity.Lines.Select(line => new {{Name}}Movement
//           {
//               LineId = line.Id,
//               IsExpense = /* true for the expense leg, false for receipt */,
//               Dimensions = new {{Name}}Dimensions { /* ... */ },
//               Resources = new {{Name}}Resources { /* ... */ },
//               Details = new {{Name}}Details { /* ... */ }, // omit if NoDetails
//           }).ToList();
//
//           var writer = registers.GetWriter<IBalanceRegisterWriter<{{Name}}Movement, {{Name}}Balance>>();
//           await writer.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
//       }
//
//       public async ValueTask OnUnsubmitAsync({{Doc}} entity, ISubmitScope registers, CancellationToken cancellationToken) =>
//           await registers.GetWriter<IBalanceRegisterWriter<{{Name}}Movement, {{Name}}Balance>>().UnpostAsync(entity.Id, cancellationToken);
//
//       public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
//           new(registers.GetWriter<IBalanceRegisterWriter<{{Name}}Movement, {{Name}}Balance>>().DeleteAsync(id, cancellationToken));
//   }
