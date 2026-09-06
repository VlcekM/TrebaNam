using System.Security.Claims;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace TrebaNam.API.Auth.Endpoints;

/// <summary>Prihlaseny pouzivatel. Prve prihlasenie vytvori riadok v users.</summary>
public class GetMeEndpoint(IDbContextFactory<DataContext> factory, AdminEmails admins)
    : EndpointWithoutRequest<UserDTO>
{
    public override void Configure()
    {
        Get("me");
        Group<AuthGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var sub = CurrentUser.Sub(HttpContext.User)!;
        var now = DateTimeOffset.UtcNow;

        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await context.Users.SingleOrDefaultAsync(u => u.GoogleSub == sub, ct);

        if (user is null)
        {
            user = new UserEntity { GoogleSub = sub, CreatedAt = now };
            context.Users.Add(user);
        }

        // Claimy z Googlu su vzdy cerstve, DB si ich len drzi ako kopiu.
        var claims = HttpContext.User.Claims.ToList();
        user.EmailAddress = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        user.Name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
        user.GivenName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
        user.Surname = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
        user.PictureUrl = claims.FirstOrDefault(c => c.Type == "picture")?.Value;
        user.LastLoginAt = now;

        if (admins.Contains(user.EmailAddress))
            user.IsAdmin = true;

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(user.ToDTO(), ct);
    }
}
