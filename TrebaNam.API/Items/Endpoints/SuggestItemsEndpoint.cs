using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Items.Endpoints;

/// <summary>
/// Co uz domacnost kupovala, od najcastejsieho. Zdroj je historia nakupov, lebo zoznam sa
/// po nakupe vyprazdnuje - to, co je na nom prave teraz, sa naopak z navrhov vyhadzuje,
/// aby sa nedala pridat ta ista vec druhy raz.
/// </summary>
public class SuggestItemsEndpoint(IDbContextFactory<DataContext> factory)
    : EndpointWithoutRequest<List<ItemSuggestionDTO>>
{
    /// <summary>Naseptavac, nie archiv - dlhsi zoznam uz clovek neprecita.</summary>
    private const int MaxSuggestions = 60;

    public override void Configure()
    {
        Get("suggestions");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user?.HouseholdID is null)
        {
            await Send.OkAsync([], ct);
            return;
        }

        var bought = await context.ShoppingRecords
            .Where(r => r.HouseholdID == user.HouseholdID)
            .SelectMany(r => r.Items.Select(i => new
            {
                i.Name,
                i.Quantity,
                i.Category,
                r.CompletedAt
            }))
            .ToListAsync(ct);

        var onList = await context.Items
            .Where(i => i.HouseholdID == user.HouseholdID)
            .Select(i => i.Name)
            .ToListAsync(ct);

        // Rovnaky kluc ako pri duplicitach, nech naseptavac neponuka to, co sa neda pridat.
        var listed = onList.Select(ItemName.Key).ToHashSet();

        // Zoskupujeme na kluc, teda bez diakritiky a velkych pismen, ale zobrazujeme posledny
        // zapis - clovek vidi vec tak, ako ju napisal naposledy, nie ako ju napisal prvykrat.
        var suggestions = bought
            .GroupBy(i => ItemName.Key(i.Name))
            .Where(g => !listed.Contains(g.Key))
            .Select(g =>
            {
                var latest = g.MaxBy(i => i.CompletedAt)!;

                return new ItemSuggestionDTO
                {
                    Name = latest.Name,
                    Quantity = latest.Quantity,
                    Category = latest.Category,
                    Count = g.Count()
                };
            })
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Name)
            .Take(MaxSuggestions)
            .ToList();

        await Send.OkAsync(suggestions, ct);
    }
}
