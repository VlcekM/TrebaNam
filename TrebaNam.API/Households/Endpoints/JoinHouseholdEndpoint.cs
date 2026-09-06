using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Households.Endpoints;

/// <summary>Prijme pozvanku - prida prihlaseneho cloveka do domacnosti podla kodu.</summary>
public class JoinHouseholdEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<InviteRequest, HouseholdDTO>
{
    public override void Configure()
    {
        Post("invite/{code}/join");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(InviteRequest req, CancellationToken ct)
    {
        var code = InviteCode.Normalize(req.Code);

        await using var context = await factory.CreateDbContextAsync(ct);

        var household = await context.Households.SingleOrDefaultAsync(h => h.InviteCode == code, ct);

        if (household is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Prestup z domacnosti do domacnosti tu vedome neriesime - odchod je vlastna akcia
        // a robit ho ticho na pozadi pozvanky by clovek zistil az podla zmiznutych zoznamov.
        if (user.HouseholdID is not null && user.HouseholdID != household.ID)
        {
            await Send.ResultAsync(TypedResults.Conflict("You are already in another household."));
            return;
        }

        user.HouseholdID = household.ID;

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(household.ID, ChangeTopic.Household, ct);

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }
}
