using TrebaNam.API.Auth;

namespace TrebaNam.API.Households;

/// <summary>Domacnost aj s clenmi - odpoved pre obrazovku "nasa domacnost".</summary>
public class HouseholdDTO
{
    public Guid ID { get; set; }

    public required string Name { get; set; }

    /// <summary>Vidia ho len clenovia, preto sa vracia rovno v detaile domacnosti.</summary>
    public required string InviteCode { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Skupiny zoznamu v poradi oddeleni v obchode. Vzdy vsetky, aj tie zakladne.</summary>
    public List<HouseholdCategoryDTO> Categories { get; set; } = [];

    public List<HouseholdMemberDTO> Members { get; set; } = [];
}

/// <summary>Skupina zoznamu. Bez nazvu je zakladna a pomenuva ju preklad na klientovi.</summary>
public class HouseholdCategoryDTO
{
    public Guid ID { get; set; }

    public required string Code { get; set; }

    public string? Name { get; set; }
}

/// <summary>Clen domacnosti. Zamerne uzsi nez UserDTO - suseda po appke neriesime.</summary>
public class HouseholdMemberDTO
{
    public Guid ID { get; set; }

    public string? Name { get; set; }

    public string? GivenName { get; set; }

    public string? PictureUrl { get; set; }

    public DateTimeOffset JoinedAt { get; set; }
}

/// <summary>Nahlad pozvanky - to jedine, co ukazeme pred samotnym pridanim sa.</summary>
public class HouseholdInviteDTO
{
    public required string Name { get; set; }

    public int MemberCount { get; set; }

    /// <summary>Uz je v tejto domacnosti - pozvanku netreba prijimat.</summary>
    public bool AlreadyMember { get; set; }

    /// <summary>Je v inej domacnosti - pridanie by ho z nej vyhodilo, tak ho nepustime.</summary>
    public bool InAnotherHousehold { get; set; }
}

public static class HouseholdMapping
{
    public static HouseholdMemberDTO ToMemberDTO(this UserEntity user) => new()
    {
        ID = user.ID,
        Name = user.Name,
        GivenName = user.GivenName,
        PictureUrl = user.PictureUrl,
        JoinedAt = user.CreatedAt
    };

    public static HouseholdCategoryDTO ToDTO(this HouseholdCategoryEntity category) => new()
    {
        ID = category.ID,
        Code = category.Code,
        Name = category.Name
    };

    public static HouseholdDTO ToDTO(
        this HouseholdEntity household,
        IEnumerable<UserEntity> members,
        IEnumerable<HouseholdCategoryEntity> categories) => new()
    {
        ID = household.ID,
        Name = household.Name,
        InviteCode = household.InviteCode,
        CreatedAt = household.CreatedAt,
        Categories = categories.Select(c => c.ToDTO()).ToList(),
        Members = members.Select(m => m.ToMemberDTO()).ToList()
    };
}
