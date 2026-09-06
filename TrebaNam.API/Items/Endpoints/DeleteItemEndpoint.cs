using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Items.Endpoints;

public class ItemByIDRequest
{
    public Guid ID { get; set; }
}

/// <summary>Zmaze polozku zo zoznamu.</summary>
public class DeleteItemEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<ItemByIDRequest>
{
    public override void Configure()
    {
        Delete("{id}");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(ItemByIDRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var item = await ItemAccess.FindForCurrentUserAsync(User, req.ID, context, ct);

        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var householdID = item.HouseholdID;

        context.Items.Remove(item);

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(householdID, ChangeTopic.Items, ct);

        await Send.NoContentAsync(ct);
    }
}
