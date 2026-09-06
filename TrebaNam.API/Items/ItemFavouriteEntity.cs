using System.ComponentModel.DataAnnotations;

namespace TrebaNam.API.Items;

/// <summary>
/// Vec, ktoru domacnost oznacila hviezdickou. Navrhy sa inak riadia tym, ako casto sa nieco
/// kupovalo, a to, co treba kazdy tyzden, sa tak da pod veci kupene raz vo velkom. Oblubene
/// preto stoja nad poradim - je to rozhodnutie cloveka, nie statistika.
///
/// Nie je to polozka zoznamu, len predloha: drzi si nazov, mnozstvo a kategoriu z posledneho
/// pouzitia, aby sa dala pridat jednym klepnutim.
/// </summary>
public class ItemFavouriteEntity
{
    public Guid ID { get; set; }

    public Guid HouseholdID { get; set; }

    /// <summary>Nazov tak, ako sa ma ponuknut.</summary>
    [MaxLength(ItemEntity.NameMaxLength)]
    public required string Name { get; set; }

    /// <summary>
    /// ItemName.Key(Name). Ten isty kluc ako pri duplicitach, takze hviezdicka sadne na vec
    /// aj vtedy, ked ju druhy clovek napise bez diakritiky.
    /// </summary>
    [MaxLength(ItemEntity.NameMaxLength)]
    public required string NameKey { get; set; }

    [MaxLength(ItemEntity.QuantityMaxLength)]
    public string? Quantity { get; set; }

    [MaxLength(ItemCategory.MaxLength)]
    public required string Category { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
