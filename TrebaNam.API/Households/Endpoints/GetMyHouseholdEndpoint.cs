using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Households.Endpoints;

/// <summary>Domacnost prihlaseneho cloveka. Ked ziadnu nema, vracia 204.</summary>
public class GetMyHouseholdEndpoint(IDbContextFactory<DataContext> factory)
    : EndpointWithoutRequest<HouseholdDTO>
{
    public override void Configure()
    {
        Get("me");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user?.HouseholdID is null)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var household = await context.Households
            .SingleOrDefaultAsync(h => h.ID == user.HouseholdID, ct);

        if (household is null)
        {
            await Send.NoContentAsync(ct);
            return;
        }

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }
}
