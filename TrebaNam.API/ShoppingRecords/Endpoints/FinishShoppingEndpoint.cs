using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.ShoppingRecords.Endpoints;

public class FinishShoppingRequest
{
    /// <summary>Kolko cely nakup stal; nepovinne, sumu sa da doplnit aj neskor.</summary>
    public decimal? TotalCost { get; set; }
}

/// <summary>
/// Ukonci nakup: z odskrtnutych poloziek spravi zaznam a zo zoznamu ich odoberie.
/// Neodskrtnute ostavaju - to je to, co sa nekupilo a treba to nabuduce. Ciastocne kupene
/// idu do zaznamu odnesenym mnozstvom, ale zo zoznamu neodchadzaju.
/// </summary>
public class FinishShoppingEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<FinishShoppingRequest, ShoppingRecordDTO>
{
    public override void Configure()
    {
        Post("");
        Group<ShoppingRecordGroup>();
    }

    public override async Task HandleAsync(FinishShoppingRequest req, CancellationToken ct)
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

        if (user.HouseholdID is null)
        {
            await Send.ResultAsync(TypedResults.Conflict("You are not in a household yet."));
            return;
        }

        var bought = await context.Items
            .Where(i => i.HouseholdID == user.HouseholdID
                && (i.IsChecked || i.BoughtQuantity != null))
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        // Nakup bez jedinej odskrtnutej polozky by bol prazdny zaznam v historii.
        if (bought.Count == 0)
        {
            await Send.ResultAsync(TypedResults.Conflict("Nothing was checked off."));
            return;
        }

        var record = new ShoppingRecordEntity
        {
            HouseholdID = user.HouseholdID.Value,
            CompletedByUserID = user.ID,
            CompletedAt = DateTimeOffset.UtcNow,
            TotalCost = TripCost.Round(req.TotalCost),
            Items = bought
                .Select(i => new ShoppingRecordItemEntity
                {
                    Name = i.Name,
                    // Pri ciastocnom nakupe ide do historie to, co sa naozaj odnieslo.
                    Quantity = i.BoughtQuantity ?? i.Quantity,
                    Category = i.Category
                })
                .ToList()
        };

        context.ShoppingRecords.Add(record);

        // Cele polozky zo zoznamu odchadzaju, ciastocne v nom ostavaju - zvysok stale treba.
        // Poznamka o odnesenej casti sa maze, uz je zapisana v zazname.
        context.Items.RemoveRange(bought.Where(i => i.IsChecked));

        foreach (var partial in bought.Where(i => !i.IsChecked))
            partial.BoughtQuantity = null;

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(record.ToDTO(), ct);
    }
}
