using TrebaNam.API.Items;

namespace TrebaNam.API.ShoppingLists;

/// <summary>Polia, ktore o zozname urcuje clovek. Rovnake pri zalozeni aj pri uprave.</summary>
public class ShoppingListFields
{
    public string? Name { get; set; }

    public string? Color { get; set; }

    public string? Note { get; set; }
}

/// <summary>Orezane a skontrolovane hodnoty, s ktorymi sa da rovno pisat do entity.</summary>
public record ShoppingListValues(string Name, string Color, string? Note);

public static class ShoppingListFieldsExtensions
{
    /// <summary>
    /// Kontroly su spolocne pre zalozenie aj upravu, aby jedno neprepustilo to,
    /// co druhe odmieta. Chybu vracia ako text, samotnu odpoved uz riesi endpoint.
    /// </summary>
    public static bool TryClean(this ShoppingListFields fields, out ShoppingListValues values, out string? error)
    {
        values = new ShoppingListValues(string.Empty, ShoppingListColor.Default, null);

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

        values = new ShoppingListValues(
            name,
            ShoppingListColor.Normalize(fields.Color),
            string.IsNullOrEmpty(note) ? null : note);

        error = null;
        return true;
    }
}
