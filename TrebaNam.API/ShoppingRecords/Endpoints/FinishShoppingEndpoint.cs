using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;
using TrebaNam.API.ShoppingLists;

namespace TrebaNam.API.ShoppingRecords.Endpoints;

public class FinishShoppingRequest
{
    /// <summary>Ktory zoznam sa prave donakupil. Nakupuje sa po zoznamoch, nie cez vsetky naraz.</summary>
    public Guid ListID { get; set; }

    /// <summary>Kolko cely nakup stal; nepovinne, sumu sa da doplnit aj neskor.</summary>
    public decimal? TotalCost { get; set; }
}

/// <summary>
/// Ukonci nakup jedneho zoznamu: z odskrtnutych poloziek spravi zaznam a zo zoznamu ich odoberie.
/// Neodskrtnute ostavaju - to je to, co sa nekupilo a treba to nabuduce. Ciastocne kupene
/// idu do zaznamu odnesenym mnozstvom, ale zo zoznamu neodchadzaju.
/// </summary>
public class FinishShoppingEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
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

        var found = await ShoppingListAccess.FindForCurrentUserAsync(User, req.ListID, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (user, list) = found.Value;

        var bought = await context.Items
            .Where(i => i.ListID == list.ID && (i.IsChecked || i.BoughtQuantity != null))
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
            HouseholdID = list.HouseholdID,
            CompletedByUserID = user.ID,
            CompletedAt = DateTimeOffset.UtcNow,
            // Odpis, nie odkaz: zoznam sa da premenovat aj zrusit a nakup ostava tym, comu bol.
            ListName = list.Name,
            ListColor = list.Color,
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

        // Zoznam sa tym vyprazdnil a v historii pribudol nakup - druha strana ma co menit na oboch.
        await notifier.ChangedAsync(record.HouseholdID, ChangeTopic.Trips, ct);

        await Send.OkAsync(record.ToDTO(), ct);
    }
}
