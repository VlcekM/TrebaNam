using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Items.Endpoints;

/// <summary>Zoznam domacnosti prihlaseneho cloveka. Bez domacnosti vracia prazdny zoznam.</summary>
public class ListItemsEndpoint(IDbContextFactory<DataContext> factory)
    : EndpointWithoutRequest<List<ItemDTO>>
{
    public override void Configure()
    {
        Get("");
        Group<ItemGroup>();
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

        // Najnovsie hore - prave pridana polozka ma byt vidiet hned, bez skrolovania.
        var items = await context.Items
            .Where(i => i.HouseholdID == user.HouseholdID)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        await Send.OkAsync(items.Select(i => i.ToDTO()).ToList(), ct);
    }
}
