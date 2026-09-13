using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.ShoppingLists.Endpoints;

/// <summary>
/// Zrusi bezici nakup bez ukoncenia - kto otvoril rezim nakupu omylom alebo z obchodu odisiel
/// s prazdnym kosikom, nema nechat domacnosti na prehlade "nakup prave bezi" navzdy. Odskrtnute
/// polozky ostavaju odskrtnute; rusi sa len to, ze niekto nakupuje, nie co ma v kosiku.
/// </summary>
public class CancelShoppingEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<ShoppingListByIDRequest, ShoppingListDTO>
{
    public override void Configure()
    {
        Delete("{id}/shopping");
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

        if (list.ShoppingStartedAt is not null)
        {
            list.ShoppingStartedAt = null;
            list.ShoppingStartedByUserID = null;

            await context.SaveChangesAsync(ct);
            await notifier.ChangedAsync(list.HouseholdID, ChangeTopic.Lists, ct);
        }

        await Send.OkAsync(list.ToDTO(), ct);
    }
}
