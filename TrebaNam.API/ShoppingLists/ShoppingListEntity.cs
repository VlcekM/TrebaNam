using System.ComponentModel.DataAnnotations;

using TrebaNam.API.Items;

namespace TrebaNam.API.ShoppingLists;

/// <summary>
/// Jeden zoznam domacnosti. Nakupy nie su jedna kopa: tyzdenny nakup, veci do zahrady a to,
/// co treba na oslavu, sa kupuju inde a inokedy, takze maju byt oddelene. Zoznam visi na
/// domacnosti rovnako ako vsetko ostatne - je spolocny, nie jednoho cloveka.
/// </summary>
public class ShoppingListEntity
{
    public Guid ID { get; set; }

    public Guid HouseholdID { get; set; }

    [MaxLength(NameMaxLength)]
    public required string Name { get; set; }

    /// <summary>Kod farby zo ShoppingListColor - podla nej sa zoznam pozna na prvy pohlad.</summary>
    [MaxLength(ColorMaxLength)]
    public required string Color { get; set; }

    /// <summary>
    /// Na co ten zoznam je ("do chaty", "kupovat len v akcii"). Poznamka zoznamu, nie polozky -
    /// plati pre cely nakup a do ukonceneho nakupu neputuje.
    /// </summary>
    [MaxLength(ItemEntity.NoteMaxLength)]
    public string? Note { get; set; }

    /// <summary>Poradie zoznamov v prepinaci. Novy pribuda na koniec.</summary>
    public int Position { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public const int NameMaxLength = 60;

    public const int ColorMaxLength = 16;
}
