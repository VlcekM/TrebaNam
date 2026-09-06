using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Households;

public static class HouseholdAccess
{
    /// <summary>
    /// Domacnost prihlaseneho cloveka aj s nim samotnym. Kto v ziadnej nie je, nema co menit,
    /// takze dostane null a endpointy nad tym koncia rovnako ako nad neexistujucou.
    /// </summary>
    public static async Task<(UserEntity User, HouseholdEntity Household)?> FindForCurrentUserAsync(
        ClaimsPrincipal principal, DataContext context, CancellationToken ct)
    {
        var user = await CurrentUser.FindAsync(principal, context, ct);

        if (user?.HouseholdID is null)
            return null;

        var household = await context.Households
            .SingleOrDefaultAsync(h => h.ID == user.HouseholdID, ct);

        return household is null ? null : (user, household);
    }

    /// <summary>Clenovia v poradi, v akom pribudli - podla neho maju avatary svoje farby.</summary>
    public static Task<List<UserEntity>> MembersAsync(
        Guid householdID, DataContext context, CancellationToken ct) =>
        context.Users
            .Where(u => u.HouseholdID == householdID)
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(ct);

    /// <summary>Skupiny v poradi oddeleni v obchode, do ktoreho domacnost chodi.</summary>
    public static Task<List<HouseholdCategoryEntity>> CategoriesAsync(
        Guid householdID, DataContext context, CancellationToken ct) =>
        context.HouseholdCategories
            .Where(c => c.HouseholdID == householdID)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Cela domacnost pre obrazovku. Kazdy zapis nad nou vracia to iste, takze clenov aj
    /// skupiny dopyta jedno miesto a nie kazdy endpoint po svojom.
    /// </summary>
    public static async Task<HouseholdDTO> DetailAsync(
        this HouseholdEntity household, DataContext context, CancellationToken ct) =>
        household.ToDTO(
            await MembersAsync(household.ID, context, ct),
            await CategoriesAsync(household.ID, context, ct));
}
