using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TrebaNam.API.Auth;
using TrebaNam.API.Items;
using TrebaNam.API.Realtime;
using TrebaNam.API.ShoppingLists;
using TrebaNam.API.ShoppingRecords;

namespace TrebaNam.API.Receipts.Endpoints;

public class ImportReceiptRequest
{
    /// <summary>Identifikator blocku z adresy.</summary>
    public string ReceiptID { get; set; } = string.Empty;

    /// <summary>
    /// Identifikator, ktory nakupu dal klient - rovnako ako pri ukonceni nakupu. Ten isty zapis
    /// moze prist dvakrat a druhy raz sa podla neho vrati nakup, ktory uz je.
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>Ku ktoremu zoznamu nakup patri; nepovinne, blocek o zoznamoch nic nevie.</summary>
    public Guid? ListID { get; set; }

    public string? StoreName { get; set; }

    /// <summary>Kedy bol blocek vystaveny; bez neho sa nakup zapise na teraz.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    public decimal? TotalCost { get; set; }

    public List<ImportReceiptRow> Items { get; set; } = [];
}

public class ImportReceiptRow
{
    /// <summary>Nazov z blocku, z ktoreho riadok vznikol - podla neho si appka zapamata, co z neho clovek spravil.</summary>
    public string? ReceiptName { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Quantity { get; set; }

    public string? Category { get; set; }
}

/// <summary>
/// Zapise blocek ako ukonceny nakup - s tymi riadkami, ktore clovek v nahlade nechal, a s nazvami
/// a skupinami tak, ako ich tam upravil. Zo zoznamu nic neodobera: blocek je odpis toho, co sa
/// kupilo, rovnako ako nakup, a co z toho este na zozname visi, vie povedat len clovek.
/// </summary>
public class ImportReceiptEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<ImportReceiptRequest, ShoppingRecordDTO>
{
    /// <summary>Velky nakup ma pat desiatok riadkov; nad tymto to uz nie je blocek.</summary>
    private const int MaxRows = 300;

    public override void Configure()
    {
        Post("{receiptID}/import");
        Group<ReceiptGroup>();
    }

    public override async Task HandleAsync(ImportReceiptRequest req, CancellationToken ct)
    {
        var receiptID = req.ReceiptID.Trim().ToUpperInvariant();

        if (!ReceiptText.IsReceiptID(receiptID))
            ThrowError("This is not a receipt code.");

        if (!TripCost.IsValid(req.TotalCost))
            ThrowError(TripCost.OutOfRange);

        var rows = req.Items
            .Select(r => new { r.ReceiptName, Name = ItemName.Display(r.Name ?? string.Empty), Quantity = r.Quantity?.Trim(), r.Category })
            .Where(r => r.Name.Length > 0)
            .ToList();

        // Nakup bez jedineho riadku by bol prazdny zaznam v historii - rovnako ako pri ukonceni.
        if (rows.Count == 0)
            ThrowError("Nothing was picked from the receipt.");

        if (rows.Count > MaxRows
            || rows.Any(r => r.Name.Length > ItemEntity.NameMaxLength
                             || r.Quantity?.Length > ItemEntity.QuantityMaxLength))
            ThrowError("A row is too long.");

        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user?.HouseholdID is not { } householdID)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var existing = await context.ShoppingRecords
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.HouseholdID == householdID && r.ReceiptID == receiptID, ct);

        if (existing is not null)
        {
            // Ten isty zapis prisiel druhy raz - odpoved na prvy sa stratila po ceste.
            if (existing.ID == req.ID)
                await Send.OkAsync(existing.ToDTO(), ct);
            else
                // Ten isty blocek naskenoval niekto druhy; nakup, ktory uz je, ide spat, nech sa da ukazat.
                await Send.ResultAsync(TypedResults.Conflict(existing.ToDTO()));

            return;
        }

        ShoppingListEntity? list = null;

        if (req.ListID is { } listID)
        {
            list = await context.ShoppingLists
                .SingleOrDefaultAsync(l => l.ID == listID && l.HouseholdID == householdID, ct);

            if (list is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
        }

        var codes = await ItemAccess.CategoryCodesAsync(householdID, context, ct);
        var now = DateTimeOffset.UtcNow;
        var store = req.StoreName?.Trim();

        var record = new ShoppingRecordEntity
        {
            ID = req.ID,
            HouseholdID = householdID,
            CompletedByUserID = user.ID,
            // Blocek z buducnosti nie je; taky datum je chyba hodin v telefone, nie nakup.
            CompletedAt = req.CompletedAt is { } at && at <= now ? at.ToUniversalTime() : now,
            ListName = list?.Name,
            ListColor = list?.Color,
            TotalCost = TripCost.Round(req.TotalCost),
            ReceiptID = receiptID,
            StoreName = string.IsNullOrEmpty(store)
                ? null
                : store[..Math.Min(store.Length, ShoppingRecordEntity.StoreNameMaxLength)],
            Items = rows
                .Select(r => new ShoppingRecordItemEntity
                {
                    Name = r.Name,
                    Quantity = string.IsNullOrEmpty(r.Quantity) ? null : r.Quantity,
                    Category = ItemCategory.Normalize(r.Category, codes)
                })
                .ToList()
        };

        context.ShoppingRecords.Add(record);

        // Nazov a skupina, ktore clovek riadku dal, prisli nabuduce samy.
        await ReceiptAliases.RememberAsync(householdID,
            rows.Where(r => !string.IsNullOrWhiteSpace(r.ReceiptName))
                .Select(r => (r.ReceiptName!, r.Name, ItemCategory.Normalize(r.Category, codes))),
            context, ct);

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dvaja naskenovali ten isty blocek v tej istej chvili; vyhral ten druhy.
            await Send.ResultAsync(TypedResults.Conflict());
            return;
        }

        await notifier.ChangedAsync(householdID, ChangeTopic.Trips, ct);

        await Send.OkAsync(record.ToDTO(), ct);
    }
}
