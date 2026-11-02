// TEMPLATE — Document-bound Info family: the most common shape in this repo (ExchangeRatesRegister/
// PricesRegister/CostingMethodSettingsRegister are all this family) — a document SETS a value rather than
// accumulating one. No Resources component at all; Details IS the stored value.
// Target: {{Config}}.Domain/Entities/Registers/{{Name}}.cs (append below Dimensions.cs and Details.cs in the
// same file — Details is effectively mandatory for this family, since there's nothing to store otherwise).
// Placeholder legend: data/README.md. Concept reference: ../reference.md §1/§4/§6/§8.
//
// [KandraRegisterEntity(TableName = "...")] AND [RegisterCaption] both go on the Record class. DI wiring
// (AddDocumentInfoRegister<...>()) and EF configuration are both fully generated — nothing to hand-write.

using Kandra.Attributes.Entities;
using Kandra.Attributes.Naming;
using Kandra.Domain.Registers;

namespace {{Config}}.Domain.Entities.Registers;

[KandraRegisterEntity(TableName = "Reg_{{Name}}")]
[RegisterCaption("{{RegisterCaptionKey}}")]
public sealed class {{Name}}Record : DocumentInfoRecord<{{Name}}Dimensions, {{Name}}Details>
{
}

[Description("{{RegisterDescription}}")] // what it measures/stores, the IsExpense direction meaning, resource units, sentinels
public sealed partial class {{Name}}Register : DocumentInfoRegister<{{Name}}Record>
{
}

// ---- Write this register only from a document Behavior's OnSubmitAsync/OnUnsubmitAsync/OnDeleteAsync, via
// ISubmitScope — see ../reference.md §5/§8 (RateImportBehavior is the live worked example) and
// ../SKILL.md step 6. Sketch, to paste into {{Config}}.Application/Documents/{{Doc}}.cs's Behavior class:
//
//   [KandraDocumentBehavior(typeof({{Doc}}Dto))]
//   [RegisterTransactions(typeof({{Name}}Record))]
//   public class {{Doc}}Behavior(...) : IDocumentBehavior<{{Doc}}>
//   {
//       public async ValueTask OnSubmitAsync({{Doc}} entity, bool isInsert, ISubmitScope registers, CancellationToken cancellationToken)
//       {
//           var records = entity.Lines.Select(line => new {{Name}}Record
//           {
//               LineId = line.Id,
//               Dimensions = new {{Name}}Dimensions { /* ... */ },
//               Details = new {{Name}}Details { /* ... */ },
//           }).ToList();
//
//           var writer = registers.GetWriter<IDocumentInfoRegisterWriter<{{Name}}Record>>();
//           await writer.PostAsync(entity.Id, entity.Date, records, cancellationToken);
//       }
//
//       public ValueTask OnUnsubmitAsync({{Doc}} entity, ISubmitScope registers, CancellationToken cancellationToken) =>
//           new(registers.GetWriter<IDocumentInfoRegisterWriter<{{Name}}Record>>().UnpostAsync(entity.Id, cancellationToken));
//
//       public ValueTask OnDeleteAsync(Guid id, ISubmitScope registers, CancellationToken cancellationToken) =>
//           new(registers.GetWriter<IDocumentInfoRegisterWriter<{{Name}}Record>>().DeleteAsync(id, cancellationToken));
//   }
