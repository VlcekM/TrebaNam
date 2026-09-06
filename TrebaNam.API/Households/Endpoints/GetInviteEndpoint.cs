using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Households.Endpoints;

public class InviteRequest
{
    public string Code { get; set; } = string.Empty;
}

/// <summary>
/// Nahlad pozvanky pre stranku /app/join/&lt;kod&gt;. Vracia len nazov a pocet clenov -
/// kto ma kod, nema este pravo vidiet zvysok domacnosti.
/// </summary>
public class GetInviteEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<InviteRequest, HouseholdInviteDTO>
{
    public override void Configure()
    {
        Get("invite/{code}");
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
        var memberCount = await context.Users.CountAsync(u => u.HouseholdID == household.ID, ct);

        await Send.OkAsync(new HouseholdInviteDTO
        {
            Name = household.Name,
            MemberCount = memberCount,
            AlreadyMember = user?.HouseholdID == household.ID,
            InAnotherHousehold = user?.HouseholdID is not null && user.HouseholdID != household.ID
        }, ct);
    }
}
