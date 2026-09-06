using System.ComponentModel.DataAnnotations;

namespace TrebaNam.API.Auth;

public class UserEntity
{
    public Guid ID { get; set; }

    /// <summary>Google subject z cookie - jediny stabilny identifikator cloveka.</summary>
    [MaxLength(128)]
    public required string GoogleSub { get; set; }

    [MaxLength(255)]
    public string? EmailAddress { get; set; }

    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? GivenName { get; set; }

    [MaxLength(255)]
    public string? Surname { get; set; }

    [MaxLength(2048)]
    public string? PictureUrl { get; set; }

    /// <summary>Pristup na /app/admin. Nastavuje sa podla Admin:Emails v konfiguracii.</summary>
    public bool IsAdmin { get; set; }

    /// <summary>Domacnost, do ktorej patri. Null = este si ziadnu nezalozil ani neprijal pozvanku.</summary>
    public Guid? HouseholdID { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastLoginAt { get; set; }
}
