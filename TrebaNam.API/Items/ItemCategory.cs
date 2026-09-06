namespace TrebaNam.API.Items;

/// <summary>
/// Kategorie z handoffu. Su to pevne kody, nie volny text - podla nich sa zoznam zoskupuje
/// a v obchode sa chodi po oddeleniach, takze vlastne kategorie by tomu nepomohli.
/// Nazvy pre cloveka drzi klient, sem patri len kod.
/// </summary>
public static class ItemCategory
{
    public const string Other = "other";

    public const int MaxLength = 24;

    /// <summary>Poradie je zaroven poradim skupin na obrazovke.</summary>
    public static readonly string[] All =
        ["produce", "bakery", "dairy", "pantry", "household", Other];

    /// <summary>Nezname alebo prazdne kategorii beriem ako "ostatne", nie ako chybu.</summary>
    public static string Normalize(string? value)
    {
        var code = value?.Trim().ToLowerInvariant();

        return code is not null && All.Contains(code) ? code : Other;
    }
}
