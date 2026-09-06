using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;
using TrebaNam.API.ShoppingLists;

namespace TrebaNam.API.Items.Endpoints;

public class UpdateItemRequest : ItemFields
{
    public Guid ID { get; set; }
}

/// <summary>
/// Prepise polozku. Upravovat ju moze ktokolvek z domacnosti, zoznam je spolocny. Presunut sa
/// da aj na iny zoznam - to, ze sa chlieb kupi az na chate, sa zisti az potom, co ho niekto
/// napisal do tyzdenneho nakupu.
/// </summary>
public class UpdateItemEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<UpdateItemRequest, ItemDTO>
{
    public override void Configure()
    {
        Put("{id}");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(UpdateItemRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var item = await ItemAccess.FindForCurrentUserAsync(User, req.ID, context, ct);

        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var codes = await ItemAccess.CategoryCodesAsync(item.HouseholdID, context, ct);

        if (!req.TryClean(codes, out var values, out var error))
            ThrowError(error!);

        // Prazdne ID znamena "necha, kde je" - starsi klient o zoznamoch nemusi vediet nic.
        var listID = item.ListID;

        if (req.ListID != Guid.Empty && req.ListID != item.ListID)
        {
            var moved = await context.ShoppingLists
                .SingleOrDefaultAsync(l => l.ID == req.ListID && l.HouseholdID == item.HouseholdID, ct);

            if (moved is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            listID = moved.ID;
        }

        // Premenovat polozku na tu, ktora na zozname uz je, je ta ista duplicita ako pridat ju;
        // po presune sa pyta na cielovom zozname, lebo tam ma stat.
        var duplicate = await ItemAccess.DuplicateOnListAsync(listID, values.Name, item.ID, context, ct);

        if (duplicate is not null)
        {
            await Send.ResultAsync(TypedResults.Conflict(ItemName.AlreadyOnList(duplicate)));
            return;
        }

        item.ListID = listID;
        item.Name = values.Name;
        item.Quantity = values.Quantity;
        item.Category = values.Category;
        item.Note = values.Note;

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(item.HouseholdID, ChangeTopic.Items, ct);

        await Send.OkAsync(item.ToDTO(), ct);
    }
}
