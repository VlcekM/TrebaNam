using System.Globalization;

namespace TrebaNam.API.Items;

/// <summary>
/// Kategoria polozky je kod, nie volny text: podla neho sa zoznam zoskupuje a v obchode sa
/// chodi po oddeleniach. Nazov pre cloveka je inde - zakladne kategorie preklada klient,
/// vlastnu si domacnost pomenovala sama (HouseholdCategoryEntity.Name).
/// </summary>
public static class ItemCategory
{
    /// <summary>Kategoria, do ktorej padne vsetko, co inam nepatri. Zrusit sa neda.</summary>
    public const string Other = "other";

    public const int MaxLength = 24;

    /// <summary>S tymto domacnost zacina; poradie je zaroven poradim skupin na zozname.</summary>
    public static readonly string[] Defaults =
        ["produce", "bakery", "dairy", "pantry", "household", Other];

    /// <summary>Nezname alebo prazdne kategorii beriem ako "ostatne", nie ako chybu.</summary>
    public static string Normalize(string? value, IReadOnlyCollection<string> allowed)
    {
        var code = value?.Trim().ToLowerInvariant();

        return code is not null && allowed.Contains(code) ? code : Other;
    }

    /// <summary>
    /// Kod vlastnej kategorie sa odvodi z nazvu, ale dalej zije sam za seba - premenovanie meni
    /// nazov a kod necha, takze polozky ani ukoncene nakupy nemusia nikam prechadzat. Je to ten
    /// isty rozdiel ako medzi nazvom polozky a jej klucom: kod porovnavame, nazov citame.
    /// </summary>
    public static string Slug(string name)
    {
        var key = ItemName.Key(name);
        var builder = new System.Text.StringBuilder(key.Length);

        foreach (var character in key)
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
            else if (builder.Length > 0 && builder[^1] != '-')
                builder.Append('-');
        }

        var slug = builder.ToString().Trim('-');

        if (slug.Length > SlugMaxLength)
            slug = slug[..SlugMaxLength].Trim('-');

        // Nazov moze byt cely z emoji alebo z interpunkcie a kod aj tak nejaky byt musi.
        return slug.Length == 0 ? "category" : slug;
    }

    /// <summary>Kod, ktory v domacnosti este nie je. Dve rovnako nazvane kategorie su jej vec.</summary>
    public static string Unique(string slug, IReadOnlyCollection<string> taken)
    {
        if (!taken.Contains(slug))
            return slug;

        for (var attempt = 2; attempt < 1000; attempt++)
        {
            var suffix = attempt.ToString(CultureInfo.InvariantCulture);
            var trimmed = slug.Length + suffix.Length + 1 > MaxLength
                ? slug[..(MaxLength - suffix.Length - 1)].Trim('-')
                : slug;

            var candidate = $"{trimmed}-{suffix}";

            if (!taken.Contains(candidate))
                return candidate;
        }

        throw new InvalidOperationException("Could not find a free category code.");
    }

    /// <summary>Kratsi nez stlpec, aby sa do neho zmestila aj pripadna koncovka pri zhode.</summary>
    private const int SlugMaxLength = 20;
}
