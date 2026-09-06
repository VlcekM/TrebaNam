namespace TrebaNam.API.ShoppingRecords;

/// <summary>Jeden ukonceny nakup aj s tym, co sa kupilo.</summary>
public class ShoppingRecordDTO
{
    public Guid ID { get; set; }

    public Guid CompletedByUserID { get; set; }

    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>Cena celeho nakupu v eurach; null, ked ju nikto nezadal.</summary>
    public decimal? TotalCost { get; set; }

    public List<ShoppingRecordItemDTO> Items { get; set; } = [];
}

public class ShoppingRecordItemDTO
{
    public Guid ID { get; set; }

    public required string Name { get; set; }

    public string? Quantity { get; set; }

    public required string Category { get; set; }
}

public static class ShoppingRecordMapping
{
    public static ShoppingRecordDTO ToDTO(this ShoppingRecordEntity record) => new()
    {
        ID = record.ID,
        CompletedByUserID = record.CompletedByUserID,
        CompletedAt = record.CompletedAt,
        TotalCost = record.TotalCost,
        Items = record.Items
            .Select(i => new ShoppingRecordItemDTO
            {
                ID = i.ID,
                Name = i.Name,
                Quantity = i.Quantity,
                Category = i.Category
            })
            .ToList()
    };
}
