namespace HomeUtilities.Persistence.Databases;

public record Provider(string Name, string Assembly)
{
    private const string AssemblyName = "HomeUtilities.Persistence.Databases";

    public static readonly Provider Sqlite = new(nameof(Sqlite), AssemblyName);
}
