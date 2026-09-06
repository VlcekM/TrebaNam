namespace TrebaNam.API.Auth;

public class UserDTO
{
    public Guid ID { get; set; }

    public bool IsAdmin { get; set; }

    public string? Name { get; set; }

    public string? EmailAddress { get; set; }

    public string? GivenName { get; set; }

    public string? Surname { get; set; }

    public string? PictureUrl { get; set; }
}

public static class UserMapping
{
    public static UserDTO ToDTO(this UserEntity user) => new()
    {
        ID = user.ID,
        IsAdmin = user.IsAdmin,
        Name = user.Name,
        EmailAddress = user.EmailAddress,
        GivenName = user.GivenName,
        Surname = user.Surname,
        PictureUrl = user.PictureUrl
    };
}
