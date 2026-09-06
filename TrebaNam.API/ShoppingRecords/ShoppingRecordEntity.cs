using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Items;

namespace TrebaNam.API.ShoppingRecords;

/// <summary>
/// Ukonceny nakup - co sa v obchode naozaj odskrtlo. Vznika pri ukonceni rezimu nakupu
/// a od tej chvile sa uz nemeni; je to zaznam o tom, co bolo, nie dalsi zoznam.
/// </summary>
public class ShoppingRecordEntity
{
    public Guid ID { get; set; }

    public Guid HouseholdID { get; set; }

    public Guid CompletedByUserID { get; set; }

    public DateTimeOffset CompletedAt { get; set; }

    /// <summary>
    /// Kolko cely nakup stal, v eurach. Jedna suma za nakup, nie cena za polozku - to je to,
    /// co je na uctenke pri pokladni. Null znamena, ze ju nikto nezadal.
    /// </summary>
    [Precision(CostPrecision, CostScale)]
    public decimal? TotalCost { get; set; }

    public List<ShoppingRecordItemEntity> Items { get; set; } = [];

    public const int CostPrecision = 10;

    public const int CostScale = 2;

    /// <summary>Nad tuto sumu uz to nie je nakup, ale preklep.</summary>
    public const decimal MaxTotalCost = 100_000m;
}

/// <summary>
/// Riadok nakupu. Je to odpis polozky, nie odkaz na nu - polozka zo zoznamu po nakupe zmizne
/// a historia sa nema menit spolu s tym, ako niekto neskor prepise nazov.
/// </summary>
public class ShoppingRecordItemEntity
{
    public Guid ID { get; set; }

    public Guid ShoppingRecordID { get; set; }

    [MaxLength(ItemEntity.NameMaxLength)]
    public required string Name { get; set; }

    [MaxLength(ItemEntity.QuantityMaxLength)]
    public string? Quantity { get; set; }

    [MaxLength(ItemCategory.MaxLength)]
    public required string Category { get; set; }
}
