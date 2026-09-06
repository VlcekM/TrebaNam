using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Items;

public static class ItemAccess
{
    /// <summary>
    /// Polozka z domacnosti prihlaseneho cloveka. Cudziu vracia ako null, takze endpointy
    /// nad nou koncia rovnako ako nad neexistujucou - o cudzich zoznamoch netreba nic prezradit.
    /// </summary>
    public static async Task<ItemEntity?> FindForCurrentUserAsync(
        ClaimsPrincipal principal, Guid itemID, DataContext context, CancellationToken ct)
    {
        var user = await CurrentUser.FindAsync(principal, context, ct);

        if (user?.HouseholdID is null)
            return null;

        return await context.Items
            .SingleOrDefaultAsync(i => i.ID == itemID && i.HouseholdID == user.HouseholdID, ct);
    }
}
