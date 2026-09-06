namespace TrebaNam.API.Items;

/// <summary>Polia, ktore o polozke urcuje clovek. Rovnake pri zalozeni aj pri uprave.</summary>
public class ItemFields
{
    public string? Name { get; set; }

    public string? Quantity { get; set; }

    public string? Category { get; set; }

    public string? Note { get; set; }
}

/// <summary>Orezane a skontrolovane hodnoty, s ktorymi sa da rovno pisat do entity.</summary>
public record ItemValues(string Name, string? Quantity, string Category, string? Note);

public static class ItemFieldsExtensions
{
    /// <summary>
    /// Kontroly su spolocne pre zalozenie aj upravu, aby jedno neprepustilo to,
    /// co druhe odmieta. Chybu vracia ako text, samotnu odpoved uz riesi endpoint.
    /// </summary>
    public static bool TryClean(this ItemFields fields, out ItemValues values, out string? error)
    {
        values = new ItemValues(string.Empty, null, ItemCategory.Other, null);

        var name = fields.Name?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            error = "Write what to buy.";
            return false;
        }

        if (name.Length > ItemEntity.NameMaxLength)
        {
            error = $"Keep the item under {ItemEntity.NameMaxLength} characters.";
            return false;
        }

        var quantity = fields.Quantity?.Trim();

        if (quantity?.Length > ItemEntity.QuantityMaxLength)
        {
            error = $"Keep the quantity under {ItemEntity.QuantityMaxLength} characters.";
            return false;
        }

        var note = fields.Note?.Trim();

        if (note?.Length > ItemEntity.NoteMaxLength)
        {
            error = $"Keep the note under {ItemEntity.NoteMaxLength} characters.";
            return false;
        }

        values = new ItemValues(
            ItemName.Display(name),
            string.IsNullOrEmpty(quantity) ? null : quantity,
            ItemCategory.Normalize(fields.Category),
            string.IsNullOrEmpty(note) ? null : note);

        error = null;
        return true;
    }
}
