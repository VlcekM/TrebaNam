using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.ShoppingLists;

public static class ShoppingListAccess
{
    /// <summary>
    /// Zoznam z domacnosti prihlaseneho cloveka. Cudzi vracia ako null, takze endpointy nad nim
    /// koncia rovnako ako nad neexistujucim - o cudzich zoznamoch netreba nic prezradit.
    /// </summary>
    public static async Task<(UserEntity User, ShoppingListEntity List)?> FindForCurrentUserAsync(
        ClaimsPrincipal principal, Guid listID, DataContext context, CancellationToken ct)
    {
        var user = await CurrentUser.FindAsync(principal, context, ct);

        if (user?.HouseholdID is null)
            return null;

        var list = await context.ShoppingLists
            .SingleOrDefaultAsync(l => l.ID == listID && l.HouseholdID == user.HouseholdID, ct);

        return list is null ? null : (user, list);
    }

    /// <summary>Zoznamy domacnosti v poradi, v akom si ich zoradila.</summary>
    public static Task<List<ShoppingListEntity>> OfHouseholdAsync(
        Guid householdID, DataContext context, CancellationToken ct) =>
        context.ShoppingLists
            .Where(l => l.HouseholdID == householdID)
            .OrderBy(l => l.Position)
            .ThenBy(l => l.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Zoznam, s ktorym domacnost zacina. Nazov posiela klient, lebo je to text pre cloveka
    /// a API o jazyku, v ktorom appku prave cita, nevie nic.
    /// </summary>
    public static ShoppingListEntity First(Guid householdID, string? name)
    {
        var wanted = name?.Trim();

        if (string.IsNullOrEmpty(wanted))
            wanted = "Shopping list";

        return new ShoppingListEntity
        {
            HouseholdID = householdID,
            Name = wanted[..Math.Min(wanted.Length, ShoppingListEntity.NameMaxLength)],
            Color = ShoppingListColor.Default,
            Position = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
