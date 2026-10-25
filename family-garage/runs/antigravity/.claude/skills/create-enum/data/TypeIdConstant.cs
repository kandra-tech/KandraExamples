namespace {{Config}}.Enums;

public static partial class {{TypeIdsClass}}
{
    // Enum type-level id only — per-value [SubcontoId] attributes on individual members are unrelated
    // and stay on the members.
    public const string {{Name}} = "{{FreshGuid}}";
}
