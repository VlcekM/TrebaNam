using System.ComponentModel.DataAnnotations;

namespace TrebaNam.API.Items;

/// <summary>
/// Polozka nakupneho zoznamu. Visi na domacnosti, nie na cloveku - kto ju pridal,
/// je len informacia navyse, zoznam je spolocny.
/// </summary>
public class ItemEntity
{
    public Guid ID { get; set; }

    public Guid HouseholdID { get; set; }

    [MaxLength(NameMaxLength)]
    public required string Name { get; set; }

    /// <summary>Volny text ("2 kg", "3 balenia") - merne jednotky za cloveka nevymyslame.</summary>
    [MaxLength(QuantityMaxLength)]
    public string? Quantity { get; set; }

    [MaxLength(ItemCategory.MaxLength)]
    public required string Category { get; set; }

    /// <summary>
    /// Odskrtnutie je stav polozky, nie stav jednej obrazovky - v obchode ju odskrtne jeden
    /// a druhy to ma vidiet tiez.
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// Kolko z polozky sa naozaj odnieslo, ked to nebolo cele mnozstvo. Volny text rovnako ako
    /// Quantity - zvysok sa z neho neda odpocitat, takze polozka ostava v zozname a do historie
    /// ide len toto mnozstvo. Null znamena, ze sa z nej nekupilo nic.
    /// </summary>
    [MaxLength(QuantityMaxLength)]
    public string? BoughtQuantity { get; set; }

    /// <summary>
    /// Poznamka pre toho, kto pojde nakupovat ("ta v modrom obale", "iba ak je v akcii").
    /// Nie je to nazov ani mnozstvo, takze do historie nakupu neide - plati na teraz.
    /// </summary>
    [MaxLength(NoteMaxLength)]
    public string? Note { get; set; }

    public Guid AddedByUserID { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public const int NameMaxLength = 120;

    public const int QuantityMaxLength = 24;

    public const int NoteMaxLength = 280;
}
