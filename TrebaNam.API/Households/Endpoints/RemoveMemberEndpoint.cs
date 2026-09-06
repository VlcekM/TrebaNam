using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Households.Endpoints;

public class RemoveMemberRequest
{
    public Guid ID { get; set; }
}

/// <summary>
/// Vyhodi cloveka z domacnosti. Moze to ktokolvek z nej: domacnost nema sefa, vsetci v nej
/// vidia to iste a kazdy z nej vie sam odist. Seba vyhodit neda - na to je odchod, ktory
/// vie aj upratat prazdnu domacnost.
/// </summary>
public class RemoveMemberEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<RemoveMemberRequest, HouseholdDTO>
{
    public override void Configure()
    {
        Delete("members/{id}");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(RemoveMemberRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var found = await HouseholdAccess.FindForCurrentUserAsync(User, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (user, household) = found.Value;

        if (req.ID == user.ID)
        {
            await Send.ResultAsync(TypedResults.Conflict("Leave the household instead."));
            return;
        }

        var member = await context.Users
            .SingleOrDefaultAsync(u => u.ID == req.ID && u.HouseholdID == household.ID, ct);

        if (member is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        member.HouseholdID = null;

        await context.SaveChangesAsync(ct);

        // Chodi aj vyhodenemu - jeho spojenie je v skupine az do odpojenia, takze sa mu
        // obrazovka prekresli sama namiesto toho, aby ukazovala cudzi zoznam.
        await notifier.ChangedAsync(household.ID, ChangeTopic.Household, ct);

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }
}
