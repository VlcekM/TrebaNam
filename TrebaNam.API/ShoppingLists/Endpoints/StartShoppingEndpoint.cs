using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.ShoppingLists.Endpoints;

/// <summary>
/// Oznaci, ze sa z tohto zoznamu prave nakupuje. Vola sa pri otvoreni rezimu nakupu, takze
/// prichadza aj opakovane a aj od druheho cloveka, ktory sa pripojil: bezici nakup sa nemeni,
/// zacal ho ten prvy. Nic sa tym nekupuje - zaznam vznikne az pri ukonceni.
/// </summary>
public class StartShoppingEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<ShoppingListByIDRequest, ShoppingListDTO>
{
    public override void Configure()
    {
        Post("{id}/shopping");
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

        var (user, list) = found.Value;

        if (list.ShoppingStartedAt is null)
        {
            list.ShoppingStartedAt = DateTimeOffset.UtcNow;
            list.ShoppingStartedByUserID = user.ID;

            await context.SaveChangesAsync(ct);
            await notifier.ChangedAsync(list.HouseholdID, ChangeTopic.Lists, ct);
        }

        await Send.OkAsync(list.ToDTO(), ct);
    }
}
