namespace TrebaNam.API.ShoppingLists;

/// <summary>Zoznam tak, ako ho vidi prepinac aj samotna obrazovka zoznamu.</summary>
public class ShoppingListDTO
{
    public Guid ID { get; set; }

    public required string Name { get; set; }

    public required string Color { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public static class ShoppingListMapping
{
    public static ShoppingListDTO ToDTO(this ShoppingListEntity list) => new()
    {
        ID = list.ID,
        Name = list.Name,
        Color = list.Color,
        Note = list.Note,
        CreatedAt = list.CreatedAt
    };
}
