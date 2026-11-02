using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Kandra.Attributes.Entities;
using Kandra.Domain.Commons;
using Kandra.Domain.Entities.Documents;
using HomePantry.Domain.Entities.Dictionaries;
using HomePantry.Enums;

namespace HomePantry.Domain.Entities.Documents;

[KandraDocumentEntity(TableName = "Doc_ChoresDone")]
[KandraGeneratedEquality]
public sealed partial class ChoreDone : DocumentBase, IEntityWithRows<ChoreDone>, IEntityWithTypeId
{
    public static Guid TypeId { get; } = new(HomePantryTypeIds.ChoreDone);

    public ChoreDone() : base(TypeId) { }

    public Guid ChoreId { get; set; }

    [JsonIgnore]
    public Chore Chore { get; set; } = null!;

    public Guid DoneById { get; set; }

    [JsonIgnore]
    public HouseholdMember DoneBy { get; set; } = null!;

    public string? Comment { get; set; }

    public static IEnumerable<Expression<Func<ChoreDone, object>>> RowNavigations => [];
    public void RemoveOrphans(ChoreDone fetched, IRowRemover remover) { }
}
