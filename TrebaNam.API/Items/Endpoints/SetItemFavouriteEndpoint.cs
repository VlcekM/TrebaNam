using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Items.Endpoints;

public class SetItemFavouriteRequest
{
    public string? Name { get; set; }

    public string? Quantity { get; set; }

    public string? Category { get; set; }

    /// <summary>Ci ma byt oblubena. Zapis stavu, nie prepnutie - dve klepnutia naraz tak
    /// skoncia rovnako, nech pridu v akomkolvek poradi.</summary>
    public bool IsFavourite { get; set; }
}

/// <summary>Prida alebo zoberie hviezdicku veci, ktoru domacnost kupuje stale dokola.</summary>
public class SetItemFavouriteEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<SetItemFavouriteRequest>
{
    public override void Configure()
    {
        Put("favourites");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(SetItemFavouriteRequest req, CancellationToken ct)
    {
        var name = req.Name?.Trim();

        if (string.IsNullOrEmpty(name))
            ThrowError("Write what to remember.");

        if (name!.Length > ItemEntity.NameMaxLength)
            ThrowError($"Keep the item under {ItemEntity.NameMaxLength} characters.");

        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (user.HouseholdID is null)
        {
            await Send.ResultAsync(TypedResults.Conflict("You are not in a household yet."));
            return;
        }

        var key = ItemName.Key(name);

        var favourite = await context.ItemFavourites
            .SingleOrDefaultAsync(f => f.HouseholdID == user.HouseholdID && f.NameKey == key, ct);

        if (req.IsFavourite)
        {
            var quantity = req.Quantity?.Trim();

            if (quantity?.Length > ItemEntity.QuantityMaxLength)
                ThrowError($"Keep the quantity under {ItemEntity.QuantityMaxLength} characters.");

            favourite ??= new ItemFavouriteEntity
            {
                HouseholdID = user.HouseholdID.Value,
                Name = ItemName.Display(name),
                NameKey = key,
                Category = ItemCategory.Other,
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Predloha ma niest posledne pouzitie, nie prve - inak by hviezdicka casom ponukala
            // mnozstvo, ktore uz nikto nekupuje.
            favourite.Name = ItemName.Display(name);
            favourite.Quantity = string.IsNullOrEmpty(quantity) ? null : quantity;
            favourite.Category = ItemCategory.Normalize(
                req.Category,
                await ItemAccess.CategoryCodesAsync(user.HouseholdID.Value, context, ct));

            if (context.Entry(favourite).State == EntityState.Detached)
                context.ItemFavourites.Add(favourite);
        }
        else if (favourite is not null)
        {
            context.ItemFavourites.Remove(favourite);
        }

        await context.SaveChangesAsync(ct);

        await Send.NoContentAsync(ct);
    }
}
