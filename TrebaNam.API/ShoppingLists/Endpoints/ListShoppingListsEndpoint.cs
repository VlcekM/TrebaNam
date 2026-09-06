using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.ShoppingLists.Endpoints;

/// <summary>Zoznamy domacnosti prihlaseneho cloveka. Bez domacnosti vracia prazdne pole.</summary>
public class ListShoppingListsEndpoint(IDbContextFactory<DataContext> factory)
    : EndpointWithoutRequest<List<ShoppingListDTO>>
{
    public override void Configure()
    {
        Get("");
        Group<ShoppingListGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user?.HouseholdID is null)
        {
            await Send.OkAsync([], ct);
            return;
        }

        var lists = await ShoppingListAccess.OfHouseholdAsync(user.HouseholdID.Value, context, ct);

        await Send.OkAsync(lists.Select(l => l.ToDTO()).ToList(), ct);
    }
}
