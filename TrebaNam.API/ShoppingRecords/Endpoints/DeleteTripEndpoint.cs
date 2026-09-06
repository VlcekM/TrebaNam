using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.ShoppingRecords.Endpoints;

public class DeleteTripRequest
{
    public Guid ID { get; set; }
}

/// <summary>
/// Zmaze ukonceny nakup aj s jeho riadkami. Polozky sa uz do zoznamu nevracaju - zaznam je
/// odpis toho, co bolo, takze zmazat ho znamena povedat, ze taky nakup nebol.
/// </summary>
public class DeleteTripEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<DeleteTripRequest>
{
    public override void Configure()
    {
        Delete("{id}");
        Group<ShoppingRecordGroup>();
    }

    public override async Task HandleAsync(DeleteTripRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Cudzia domacnost je pre nas to iste ako neexistujuci nakup.
        var record = await context.ShoppingRecords
            .FirstOrDefaultAsync(r => r.ID == req.ID && r.HouseholdID == user.HouseholdID, ct);

        if (record is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Riadky visia na zazname kaskadou, takze staci zmazat jeho.
        var householdID = record.HouseholdID;

        context.ShoppingRecords.Remove(record);

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(householdID, ChangeTopic.Trips, ct);

        await Send.NoContentAsync(ct);
    }
}
