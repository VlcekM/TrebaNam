using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Items.Endpoints;

public class SuggestItemsRequest
{
    /// <summary>
    /// Na ktory zoznam sa prave pridava. Navrhy sa nemenia podla zoznamu, ale to, co uz na nom
    /// stoji, sa z nich vyhadzuje - inde v domacnosti moze ta ista vec pokojne byt.
    /// </summary>
    public Guid ListID { get; set; }
}

/// <summary>
/// Co uz domacnost kupovala, od najcastejsieho, a pred tym vsetkym to, co si oznacila
/// hviezdickou. Zdroj je historia nakupov, lebo zoznam sa po nakupe vyprazdnuje - to, co je
/// na nom prave teraz, sa naopak z navrhov vyhadzuje, aby sa nedala pridat ta ista vec druhy raz.
/// </summary>
public class SuggestItemsEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<SuggestItemsRequest, List<ItemSuggestionDTO>>
{
    /// <summary>Naseptavac, nie archiv - dlhsi zoznam uz clovek neprecita.</summary>
    private const int MaxSuggestions = 60;

    public override void Configure()
    {
        Get("suggestions");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(SuggestItemsRequest req, CancellationToken ct)
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

        var favourites = await context.ItemFavourites
            .Where(f => f.HouseholdID == user.HouseholdID)
            .ToListAsync(ct);

        var onList = await context.Items
            .Where(i => i.HouseholdID == user.HouseholdID
                && (req.ListID == Guid.Empty || i.ListID == req.ListID))
            .Select(i => i.Name)
            .ToListAsync(ct);

        // Rovnaky kluc ako pri duplicitach, nech naseptavac neponuka to, co sa neda pridat.
        var listed = onList.Select(ItemName.Key).ToHashSet();
        var starred = favourites.Select(f => f.NameKey).ToHashSet();

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
                    Count = g.Count(),
                    IsFavourite = starred.Contains(g.Key)
                };
            })
            .ToDictionary(s => ItemName.Key(s.Name));

        // Hviezdicku moze mat aj vec, ktora este ziadny nakup nezazila - dava sa prave preto,
        // aby ju clovek nemusel zakazdym pisat.
        foreach (var favourite in favourites.Where(f => !listed.Contains(f.NameKey)))
        {
            if (suggestions.ContainsKey(favourite.NameKey))
                continue;

            suggestions[favourite.NameKey] = new ItemSuggestionDTO
            {
                Name = favourite.Name,
                Quantity = favourite.Quantity,
                Category = favourite.Category,
                Count = 0,
                IsFavourite = true
            };
        }

        var ordered = suggestions.Values
            .OrderByDescending(s => s.IsFavourite)
            .ThenByDescending(s => s.Count)
            .ThenBy(s => s.Name)
            .Take(MaxSuggestions)
            .ToList();

        await Send.OkAsync(ordered, ct);
    }
}
