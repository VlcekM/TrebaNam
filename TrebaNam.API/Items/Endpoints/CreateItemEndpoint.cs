using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Items.Endpoints;

public class CreateItemRequest : ItemFields;

/// <summary>Prida polozku do zoznamu domacnosti.</summary>
public class CreateItemEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<CreateItemRequest, ItemDTO>
{
    public override void Configure()
    {
        Post("");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(CreateItemRequest req, CancellationToken ct)
    {
        if (!req.TryClean(out var values, out var error))
            ThrowError(error!);

        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Zoznam patri domacnosti, takze bez nej nie je kam pridavat.
        if (user.HouseholdID is null)
        {
            await Send.ResultAsync(TypedResults.Conflict("You are not in a household yet."));
            return;
        }

        // Ta ista vec dvakrat je len zmatok pri regali; diakritika ani velke pismena na tom
        // nic nemenia, takze porovnavame cez ItemName.Key.
        var key = ItemName.Key(values.Name);

        var names = await context.Items
            .Where(i => i.HouseholdID == user.HouseholdID)
            .Select(i => i.Name)
            .ToListAsync(ct);

        var duplicate = names.FirstOrDefault(n => ItemName.Key(n) == key);

        if (duplicate is not null)
        {
            await Send.ResultAsync(TypedResults.Conflict(ItemName.AlreadyOnList(duplicate)));
            return;
        }

        var item = new ItemEntity
        {
            HouseholdID = user.HouseholdID.Value,
            Name = values.Name,
            Quantity = values.Quantity,
            Category = values.Category,
            Note = values.Note,
            AddedByUserID = user.ID,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Items.Add(item);

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(item.ToDTO(), ct);
    }
}
