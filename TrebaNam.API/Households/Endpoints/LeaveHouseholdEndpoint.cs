using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Households.Endpoints;

/// <summary>
/// Odchod z domacnosti. Zoznam ani nakupy si clovek neberie so sebou - patria domacnosti,
/// nie jemu. Ked odchadza posledny, nema uz kto tie data citat, tak s nim padne aj domacnost;
/// kym tam ostava niekto dalsi, ostava vsetko na svojom mieste.
/// </summary>
public class LeaveHouseholdEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("me/leave");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var found = await HouseholdAccess.FindForCurrentUserAsync(User, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (user, household) = found.Value;

        user.HouseholdID = null;

        var alone = !await context.Users.AnyAsync(
            u => u.HouseholdID == household.ID && u.ID != user.ID, ct);

        // Zoznam, nakupy aj oblubene visia na domacnosti cez cascade, takze odchadzaju s nou.
        if (alone)
            context.Households.Remove(household);

        await context.SaveChangesAsync(ct);

        if (!alone)
            await notifier.ChangedAsync(household.ID, ChangeTopic.Household, ct);

        await Send.NoContentAsync(ct);
    }
}
