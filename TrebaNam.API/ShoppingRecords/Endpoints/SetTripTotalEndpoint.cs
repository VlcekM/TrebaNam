using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.ShoppingRecords.Endpoints;

public class SetTripTotalRequest
{
    public Guid ID { get; set; }

    /// <summary>Prazdna hodnota sumu z nakupu odoberie.</summary>
    public decimal? TotalCost { get; set; }
}

/// <summary>
/// Doplni alebo opravi sumu za uz ukonceny nakup. Jedina vec, ktora sa na zazname da menit -
/// pri pokladni sa nie vzdy stiha zapisovat a uctenka sa najde neskor.
/// </summary>
public class SetTripTotalEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<SetTripTotalRequest, ShoppingRecordDTO>
{
    public override void Configure()
    {
        Put("{id}/total");
        Group<ShoppingRecordGroup>();
    }

    public override async Task HandleAsync(SetTripTotalRequest req, CancellationToken ct)
    {
        if (!TripCost.IsValid(req.TotalCost))
            ThrowError(TripCost.OutOfRange);

        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Cudzia domacnost je pre nas to iste ako neexistujuci nakup.
        var record = await context.ShoppingRecords
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.ID == req.ID && r.HouseholdID == user.HouseholdID, ct);

        if (record is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        record.TotalCost = TripCost.Round(req.TotalCost);

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(record.HouseholdID, ChangeTopic.Trips, ct);

        await Send.OkAsync(record.ToDTO(), ct);
    }
}
