using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace TrebaNam.API.Items.Endpoints;

public class ItemByIDRequest
{
    public Guid ID { get; set; }
}

/// <summary>Zmaze polozku zo zoznamu.</summary>
public class DeleteItemEndpoint(IDbContextFactory<DataContext> factory)
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

        context.Items.Remove(item);

        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
