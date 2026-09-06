using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.ShoppingLists.Endpoints;

public class CreateShoppingListRequest : ShoppingListFields;

/// <summary>Zalozi dalsi zoznam domacnosti. Pribuda na koniec prepinaca, nie pred ostatne.</summary>
public class CreateShoppingListEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<CreateShoppingListRequest, ShoppingListDTO>
{
    /// <summary>Prepinac sa da prejst ocami; dlhsi rad zoznamov uz nikto nevyberie.</summary>
    private const int MaxLists = 20;

    public override void Configure()
    {
        Post("");
        Group<ShoppingListGroup>();
    }

    public override async Task HandleAsync(CreateShoppingListRequest req, CancellationToken ct)
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

        if (user.HouseholdID is null)
        {
            await Send.ResultAsync(TypedResults.Conflict("You are not in a household yet."));
            return;
        }

        var existing = await ShoppingListAccess.OfHouseholdAsync(user.HouseholdID.Value, context, ct);

        if (existing.Count >= MaxLists)
        {
            await Send.ResultAsync(TypedResults.Conflict("That is as many lists as one household can keep."));
            return;
        }

        var list = new ShoppingListEntity
        {
            HouseholdID = user.HouseholdID.Value,
            Name = values.Name,
            Color = values.Color,
            Note = values.Note,
            Position = existing.Count == 0 ? 0 : existing.Max(l => l.Position) + 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.ShoppingLists.Add(list);

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(list.HouseholdID, ChangeTopic.Lists, ct);

        await Send.OkAsync(list.ToDTO(), ct);
    }
}
