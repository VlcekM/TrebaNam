using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;
using TrebaNam.API.ShoppingLists;

namespace TrebaNam.API.Items.Endpoints;

public class CreateItemRequest : ItemFields;

/// <summary>Prida polozku na jeden zo zoznamov domacnosti.</summary>
public class CreateItemEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<CreateItemRequest, ItemDTO>
{
    public override void Configure()
    {
        Post("");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(CreateItemRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        // Zoznam patri domacnosti, takze cudzi je pre nas to iste ako neexistujuci - a bez
        // zoznamu nie je kam pridavat.
        var found = await ShoppingListAccess.FindForCurrentUserAsync(User, req.ListID, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (user, list) = found.Value;

        var codes = await ItemAccess.CategoryCodesAsync(list.HouseholdID, context, ct);

        if (!req.TryClean(codes, out var values, out var error))
            ThrowError(error!);

        var duplicate = await ItemAccess.DuplicateOnListAsync(list.ID, values.Name, null, context, ct);

        if (duplicate is not null)
        {
            await Send.ResultAsync(TypedResults.Conflict(ItemName.AlreadyOnList(duplicate)));
            return;
        }

        var item = new ItemEntity
        {
            HouseholdID = list.HouseholdID,
            ListID = list.ID,
            Name = values.Name,
            Quantity = values.Quantity,
            Category = values.Category,
            Note = values.Note,
            AddedByUserID = user.ID,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Items.Add(item);

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(item.HouseholdID, ChangeTopic.Items, ct);

        await Send.OkAsync(item.ToDTO(), ct);
    }
}
