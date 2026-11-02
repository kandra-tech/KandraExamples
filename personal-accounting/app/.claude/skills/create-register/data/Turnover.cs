// TEMPLATE — Turnover family: no live consumer in this repo yet (../reference.md §1/§8) — read that before
// picking this over Balance/document-bound Info just because it looks like a lighter-weight fit.
// Target: {{Config}}.Domain/Entities/Registers/{{Name}}.cs (append below Dimensions.cs and, if used,
// Resources.cs/Details.cs in the same file).
// Placeholder legend: data/README.md. Concept reference: ../reference.md §1/§4/§6.
//
// Unlike every other family, DI wiring for Turnover is NOT generated — Kandra.Generators.Di.Persistence only
// emits AddBalanceRegister<>() by walking discovered Balance entities and matching their Movement; a Movement
// with no paired Balance is silently skipped. You must add the AddTurnoverRegister<>() line yourself — see the
// bottom of this file and ../SKILL.md step 5. EF configuration IS still fully generated, same as every family.

using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Domain.Registers;

namespace {{Config}}.Domain.Entities.Registers;

[KandraRegisterEntity(TableName = "Reg_{{Name}}")]
[RegisterCaption("{{RegisterCaptionKey}}")]
public sealed class {{Name}}Movement : Movement<{{Name}}Dimensions, {{Name}}Resources, {{Name}}Details>
{
}

[Description("{{RegisterDescription}}")] // what it measures/stores, the IsExpense direction meaning, resource units, sentinels
public sealed partial class {{Name}}Register : TurnoverRegister<{{Name}}Movement>
{
}

// ---- REQUIRED, hand-written (this family only) — add one line to
// {{Config}}.Persistence/ConfigureServices.cs's AddPersonalAccountingPersistence():
//
//   services.AddTurnoverRegister<{{Name}}Movement, {{Name}}Dimensions, {{Name}}Resources, {{Name}}Details>();
//
// (swap {{Name}}Details for NoDetails if this register carries no Details class)

// ---- Write this register only from a document Behavior's OnSubmitAsync/OnUnsubmitAsync/OnDeleteAsync, via
// ISubmitScope — see ../reference.md §5 and ../SKILL.md step 6. Sketch, to paste into
// {{Config}}.Application/Documents/{{Doc}}.cs's Behavior class:
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
//           var writer = registers.GetWriter<ITurnoverRegisterWriter<{{Name}}Movement>>();
//           await writer.PostAsync(entity.Id, entity.Date, movements, cancellationToken);
//       }
//
//       public async ValueTask OnUnsubmitAsync({{Doc}} entity, ISubmitScope registers, CancellationToken cancellationToken) =>
//           await registers.GetWriter<ITurnoverRegisterWriter<{{Name}}Movement>>().UnpostAsync(entity.Id, cancellationToken);
//
//       public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
//           new(registers.GetWriter<ITurnoverRegisterWriter<{{Name}}Movement>>().DeleteAsync(id, cancellationToken));
//   }
