using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.ShoppingLists.Endpoints;

public class ShoppingListByIDRequest
{
    public Guid ID { get; set; }
}

/// <summary>
/// Zmaze zoznam aj s tym, co na nom ostalo - polozky visia na nom a inde nemaju co robit.
/// Posledny zoznam zmazat neda: domacnost bez zoznamu by nemala kam pridavat a zakladat ho
/// hned po zmazani znova je len praca navyse.
/// </summary>
public class DeleteShoppingListEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<ShoppingListByIDRequest>
{
    public override void Configure()
    {
        Delete("{id}");
        Group<ShoppingListGroup>();
    }

    public override async Task HandleAsync(ShoppingListByIDRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var found = await ShoppingListAccess.FindForCurrentUserAsync(User, req.ID, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (_, list) = found.Value;

        var others = await context.ShoppingLists
            .CountAsync(l => l.HouseholdID == list.HouseholdID && l.ID != list.ID, ct);

        if (others == 0)
        {
            await Send.ResultAsync(TypedResults.Conflict("A household keeps at least one list."));
            return;
        }

        var householdID = list.HouseholdID;

        context.ShoppingLists.Remove(list);

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(householdID, ChangeTopic.Lists, ct);

        await Send.NoContentAsync(ct);
    }
}
