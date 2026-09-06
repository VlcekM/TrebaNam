using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Items;

public static class ItemAccess
{
    /// <summary>
    /// Kody skupin domacnosti. Kategoria polozky sa kontroluje proti nim, lebo skupiny uz nie su
    /// pevne - domacnost si ich pomenuva a rusi sama.
    /// </summary>
    public static Task<List<string>> CategoryCodesAsync(
        Guid householdID, DataContext context, CancellationToken ct) =>
        context.HouseholdCategories
            .Where(c => c.HouseholdID == householdID)
            .Select(c => c.Code)
            .ToListAsync(ct);

    /// <summary>
    /// Nazov, ktory na tom istom zozname uz je. Diakritika ani velke pismena na tom nic nemenia,
    /// takze porovnavame cez ItemName.Key; iny zoznam domacnosti do toho nehovori, tam je ta ista
    /// vec celkom v poriadku (chlieb do mesta aj na chatu).
    /// </summary>
    public static async Task<string?> DuplicateOnListAsync(
        Guid listID, string name, Guid? exceptItemID, DataContext context, CancellationToken ct)
    {
        var key = ItemName.Key(name);

        var names = await context.Items
            .Where(i => i.ListID == listID && (exceptItemID == null || i.ID != exceptItemID))
            .Select(i => i.Name)
            .ToListAsync(ct);

        return names.FirstOrDefault(n => ItemName.Key(n) == key);
    }

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
