using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace TrebaNam.API.Auth;

/// <summary>Prihlaseny clovek podla Google sub v cookie - null, ked ho v DB este nemame.</summary>
public static class CurrentUser
{
    public static string? Sub(ClaimsPrincipal principal) =>
        principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

    public static async Task<UserEntity?> FindAsync(
        ClaimsPrincipal principal, DataContext context, CancellationToken ct)
    {
        var sub = Sub(principal);

        return sub is null
            ? null
            : await context.Users.SingleOrDefaultAsync(u => u.GoogleSub == sub, ct);
    }
}
