using System.ComponentModel.DataAnnotations;

using TrebaNam.API.Items;

namespace TrebaNam.API.Households;

/// <summary>
/// Skupina, do ktorej patria polozky zoznamu. Patri domacnosti, lebo kazdy obchod ma oddelenia
/// inak pomenovane aj inak zoradene - "pecivo" niekomu staci a niekto chodi k svojmu maziarovi.
///
/// Kod je identita a nemeni sa; nazov je len popis. Bez nazvu je to jedna zo zakladnych
/// kategorii a text k nej dodava klient v jazyku cloveka, s nazvom je to kategoria domacnosti
/// a cita sa tak, ako si ju napisala - v oboch jazykoch rovnako.
/// </summary>
public class HouseholdCategoryEntity
{
    public Guid ID { get; set; }

    public Guid HouseholdID { get; set; }

    [MaxLength(ItemCategory.MaxLength)]
    public required string Code { get; set; }

    /// <summary>Null pri zakladnej kategorii - tu pomenuva preklad, nie domacnost.</summary>
    [MaxLength(NameMaxLength)]
    public string? Name { get; set; }

    /// <summary>Poradie oddeleni v obchode, do ktoreho chodite. Cisluje sa od nuly.</summary>
    public int Position { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public const int NameMaxLength = 40;

    /// <summary>Kategorie, s ktorymi zacina kazda nova domacnost.</summary>
    public static List<HouseholdCategoryEntity> Defaults(Guid householdID)
    {
        var now = DateTimeOffset.UtcNow;

        return [.. ItemCategory.Defaults.Select((code, index) => new HouseholdCategoryEntity
        {
            HouseholdID = householdID,
            Code = code,
            Name = null,
            Position = index,
            CreatedAt = now
        })];
    }
}
