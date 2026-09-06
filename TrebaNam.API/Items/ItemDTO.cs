namespace TrebaNam.API.Items;

/// <summary>Polozka tak, ako ju vidi zoznam.</summary>
public class ItemDTO
{
    public Guid ID { get; set; }

    /// <summary>Na ktorom zozname domacnosti stoji.</summary>
    public Guid ListID { get; set; }

    public required string Name { get; set; }

    public string? Quantity { get; set; }

    public required string Category { get; set; }

    public bool IsChecked { get; set; }

    /// <summary>Odnesena cast mnozstva, ked sa kupila len cast. Null, ak nie.</summary>
    public string? BoughtQuantity { get; set; }

    /// <summary>Poznamka pre toho, kto pojde nakupovat. Null, ked ziadna nie je.</summary>
    public string? Note { get; set; }

    /// <summary>Kto ju pridal - obrazovka si k tomu doplni clena z domacnosti.</summary>
    public Guid AddedByUserID { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public static class ItemMapping
{
    public static ItemDTO ToDTO(this ItemEntity item) => new()
    {
        ID = item.ID,
        ListID = item.ListID,
        Name = item.Name,
        Quantity = item.Quantity,
        Category = item.Category,
        IsChecked = item.IsChecked,
        BoughtQuantity = item.BoughtQuantity,
        Note = item.Note,
        AddedByUserID = item.AddedByUserID,
        CreatedAt = item.CreatedAt
    };
}
