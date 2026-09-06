using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Items;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Households.Endpoints;

public class DeleteCategoryRequest
{
    public Guid ID { get; set; }
}

/// <summary>
/// Zrusi skupinu. Polozky v nej sa nemazu - prejdu do "ostatne", lebo kupit ich treba dalej;
/// to iste plati o hviezdickach. Samotne "ostatne" zrusit neda, je to spodok, na ktory vsetko
/// ostatne padne.
/// </summary>
public class DeleteCategoryEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<DeleteCategoryRequest, HouseholdDTO>
{
    public override void Configure()
    {
        Delete("me/categories/{id}");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(DeleteCategoryRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var found = await HouseholdAccess.FindForCurrentUserAsync(User, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (_, household) = found.Value;

        var category = await context.HouseholdCategories
            .SingleOrDefaultAsync(c => c.ID == req.ID && c.HouseholdID == household.ID, ct);

        if (category is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (category.Code == ItemCategory.Other)
        {
            await Send.ResultAsync(TypedResults.Conflict("Every list keeps the last group."));
            return;
        }

        var items = await context.Items
            .Where(i => i.HouseholdID == household.ID && i.Category == category.Code)
            .ToListAsync(ct);

        foreach (var item in items)
            item.Category = ItemCategory.Other;

        var favourites = await context.ItemFavourites
            .Where(f => f.HouseholdID == household.ID && f.Category == category.Code)
            .ToListAsync(ct);

        foreach (var favourite in favourites)
            favourite.Category = ItemCategory.Other;

        context.HouseholdCategories.Remove(category);

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(household.ID, ChangeTopic.Household, ct);

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }
}
