using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.ShoppingRecords.Endpoints;

/// <summary>Ukoncene nakupy domacnosti, od najnovsieho.</summary>
public class ListShoppingRecordsEndpoint(IDbContextFactory<DataContext> factory)
    : EndpointWithoutRequest<List<ShoppingRecordDTO>>
{
    public override void Configure()
    {
        Get("");
        Group<ShoppingRecordGroup>();
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

        var records = await context.ShoppingRecords
            .Where(r => r.HouseholdID == user.HouseholdID)
            .Include(r => r.Items)
            .OrderByDescending(r => r.CompletedAt)
            .ToListAsync(ct);

        await Send.OkAsync(records.Select(r => r.ToDTO()).ToList(), ct);
    }
}
