using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Items.Endpoints;

public class UpdateItemRequest : ItemFields
{
    public Guid ID { get; set; }
}

/// <summary>Prepise polozku. Upravovat ju moze ktokolvek z domacnosti, zoznam je spolocny.</summary>
public class UpdateItemEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<UpdateItemRequest, ItemDTO>
{
    public override void Configure()
    {
        Put("{id}");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(UpdateItemRequest req, CancellationToken ct)
    {
        if (!req.TryClean(out var values, out var error))
            ThrowError(error!);

        await using var context = await factory.CreateDbContextAsync(ct);

        var item = await ItemAccess.FindForCurrentUserAsync(User, req.ID, context, ct);

        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Premenovat polozku na tu, ktora uz v zozname je, je ta ista duplicita ako pridat ju.
        var key = ItemName.Key(values.Name);

        var names = await context.Items
            .Where(i => i.HouseholdID == item.HouseholdID && i.ID != item.ID)
            .Select(i => i.Name)
            .ToListAsync(ct);

        var duplicate = names.FirstOrDefault(n => ItemName.Key(n) == key);

        if (duplicate is not null)
        {
            await Send.ResultAsync(TypedResults.Conflict(ItemName.AlreadyOnList(duplicate)));
            return;
        }

        item.Name = values.Name;
        item.Quantity = values.Quantity;
        item.Category = values.Category;
        item.Note = values.Note;

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(item.ToDTO(), ct);
    }
}
