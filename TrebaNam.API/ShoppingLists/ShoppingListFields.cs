using TrebaNam.API.Items;

namespace TrebaNam.API.ShoppingLists;

/// <summary>Polia, ktore o zozname urcuje clovek. Rovnake pri zalozeni aj pri uprave.</summary>
public class ShoppingListFields
{
    public string? Name { get; set; }

    public string? Color { get; set; }

    public string? Note { get; set; }

    /// <summary>Kody skupin na tomto zozname; null aj prazdne pole znamena vsetky.</summary>
    public List<string>? Categories { get; set; }
}

/// <summary>Orezane a skontrolovane hodnoty, s ktorymi sa da rovno pisat do entity.</summary>
public record ShoppingListValues(string Name, string Color, string? Note, List<string> Categories);

public static class ShoppingListFieldsExtensions
{
    /// <summary>
    /// Vyber skupin oproti tomu, co domacnost ma: cudzie kody padaju a vyber, ktory pokryva
    /// vsetky skupiny, sa uklada ako prazdny - to je "vsetky" aj pre skupiny, ktore este len
    /// pribudnu. Poradie je poradie domacnosti, nie poradie klikania.
    /// </summary>
    public static List<string> Restrict(IEnumerable<string> wanted, IEnumerable<string> household)
    {
        var codes = household.ToList();
        var chosen = wanted.ToHashSet();
        var kept = codes.Where(chosen.Contains).ToList();

        return kept.Count == codes.Count ? [] : kept;
    }

    /// <summary>
    /// Kontroly su spolocne pre zalozenie aj upravu, aby jedno neprepustilo to,
    /// co druhe odmieta. Chybu vracia ako text, samotnu odpoved uz riesi endpoint.
    /// </summary>
    public static bool TryClean(this ShoppingListFields fields, out ShoppingListValues values, out string? error)
    {
        values = new ShoppingListValues(string.Empty, ShoppingListColor.Default, null, []);

        var name = fields.Name?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            error = "Give the list a name.";
            return false;
        }

        if (name.Length > ShoppingListEntity.NameMaxLength)
        {
            error = $"Keep the name under {ShoppingListEntity.NameMaxLength} characters.";
            return false;
        }

        var note = fields.Note?.Trim();

        if (note?.Length > ItemEntity.NoteMaxLength)
        {
            error = $"Keep the note under {ItemEntity.NoteMaxLength} characters.";
            return false;
        }

        // Kody len ocistene; ci ich domacnost naozaj ma, vie az endpoint s jej skupinami v ruke.
        var categories = (fields.Categories ?? [])
            .Select(c => c?.Trim().ToLowerInvariant())
            .Where(c => !string.IsNullOrEmpty(c) && c.Length <= ItemCategory.MaxLength)
            .Select(c => c!)
            .Distinct()
            .ToList();

        values = new ShoppingListValues(
            name,
            ShoppingListColor.Normalize(fields.Color),
            string.IsNullOrEmpty(note) ? null : note,
            categories);

        error = null;
        return true;
    }
}
