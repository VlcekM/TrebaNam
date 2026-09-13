using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.ShoppingLists.Endpoints;

public class SetShoppingListOrderRequest
{
    /// <summary>Zoznamy v novom poradi. Co v nom chyba, ostava za nimi tak, ako bolo.</summary>
    public List<Guid>? Order { get; set; }
}

/// <summary>
/// Prestavi poradie zoznamov v prepinaci. Ten, do ktoreho sa chodi kazdy tyzden, ma stat prvy
/// a ten na chatu az za nim - a ktory je ktory, vie len domacnost, tak si to zoradi sama.
/// </summary>
public class SetShoppingListOrderEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<SetShoppingListOrderRequest, List<ShoppingListDTO>>
{
    public override void Configure()
    {
        Put("order");
        Group<ShoppingListGroup>();
    }

    public override async Task HandleAsync(SetShoppingListOrderRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user?.HouseholdID is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var lists = await ShoppingListAccess.OfHouseholdAsync(user.HouseholdID.Value, context, ct);
        var wanted = (req.Order ?? []).Distinct().ToList();

        // Rovnako ako pri skupinach: cudzie a nezname ID padaju a zoznamy, ktore v poziadavke
        // nie su, sa poradia za ne. Ziadny nemoze vypadnut, ani ked ho medzitym niekto pridal.
        var ordered = wanted
            .Select(id => lists.SingleOrDefault(l => l.ID == id))
            .Where(l => l is not null)
            .Select(l => l!)
            .ToList();

        ordered.AddRange(lists.Where(l => !ordered.Contains(l)));

        for (var index = 0; index < ordered.Count; index++)
            ordered[index].Position = index;

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(user.HouseholdID.Value, ChangeTopic.Lists, ct);

        await Send.OkAsync(ordered.Select(l => l.ToDTO()).ToList(), ct);
    }
}
