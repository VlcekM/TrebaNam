using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.ShoppingLists.Endpoints;

public class UpdateShoppingListRequest : ShoppingListFields
{
    public Guid ID { get; set; }
}

/// <summary>Prepise nazov, farbu a poznamku zoznamu. Na polozkach v nom sa tym nemeni nic.</summary>
public class UpdateShoppingListEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<UpdateShoppingListRequest, ShoppingListDTO>
{
    public override void Configure()
    {
        Put("{id}");
        Group<ShoppingListGroup>();
    }

    public override async Task HandleAsync(UpdateShoppingListRequest req, CancellationToken ct)
    {
        if (!req.TryClean(out var values, out var error))
            ThrowError(error!);

        await using var context = await factory.CreateDbContextAsync(ct);

        var found = await ShoppingListAccess.FindForCurrentUserAsync(User, req.ID, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (_, list) = found.Value;

        list.Name = values.Name;
        list.Color = values.Color;
        list.Note = values.Note;

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(list.HouseholdID, ChangeTopic.Lists, ct);

        await Send.OkAsync(list.ToDTO(), ct);
    }
}
