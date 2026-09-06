using System.ComponentModel.DataAnnotations;

namespace TrebaNam.API.Households;

/// <summary>
/// Domacnost, do ktorej patria pouzivatelia (UserEntity.HouseholdID). Nakupne zoznamy
/// budu vzdy visiet na domacnosti, nie na jednotlivcovi.
/// </summary>
public class HouseholdEntity
{
    public Guid ID { get; set; }

    [MaxLength(NameMaxLength)]
    public required string Name { get; set; }

    /// <summary>
    /// Kod v pozvankovom odkaze. Je to tajomstvo - kto ho ma, ten sa vie pridat,
    /// preto sa generuje nahodne a nie z nazvu domacnosti.
    /// </summary>
    [MaxLength(32)]
    public required string InviteCode { get; set; }

    public Guid CreatedByUserID { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public const int NameMaxLength = 80;
}
