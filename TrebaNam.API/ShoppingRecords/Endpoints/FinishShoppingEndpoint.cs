using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TrebaNam.API.Items;
using TrebaNam.API.Realtime;
using TrebaNam.API.Receipts;
using TrebaNam.API.Receipts.Endpoints;
using TrebaNam.API.ShoppingLists;

namespace TrebaNam.API.ShoppingRecords.Endpoints;

public class FinishShoppingRequest
{
    /// <summary>
    /// Identifikator, ktory nakupu dal klient. V obchode byva signal najhorsi, takze ukoncenie
    /// moze cakat v telefone a odist az potom - a moze odist aj dvakrat. Podla neho pozname, ze
    /// ten isty nakup uz zapisany je. Prazdna hodnota znamena bezne ukoncenie.
    /// </summary>
    public Guid ID { get; set; }

    /// <summary>Ktory zoznam sa prave donakupil. Nakupuje sa po zoznamoch, nie cez vsetky naraz.</summary>
    public Guid ListID { get; set; }

    /// <summary>Kolko cely nakup stal; nepovinne, sumu sa da doplnit aj neskor.</summary>
    public decimal? TotalCost { get; set; }

    /// <summary>
    /// Veci z inych zoznamov domacnosti, ktore sa vzali na tomto nakupe - ze ma obchod aj to
    /// na chatu, sa zisti az v nom. Idu po ID, nie cez vsetko odskrtnute na inych zoznamoch:
    /// tam mohol prave niekto iny nakupovat svoje a jeho kosik nie je nas.
    /// </summary>
    public List<Guid>? ExtraItemIDs { get; set; }

    /// <summary>Blocek z pokladne, ked sa nakup ukoncuje jeho naskenovanim; inak null.</summary>
    public FinishReceipt? Receipt { get; set; }
}

/// <summary>
/// Blocek k ukoncovanemu nakupu: z ktoreho obchodu, co z neho este do nakupu patri, hoci to na
/// zozname nebolo, a ktory riadok je ktora odskrtnuta vec - to si domacnost zapamata na dalsi raz.
/// </summary>
public class FinishReceipt
{
    public string ReceiptID { get; set; } = string.Empty;

    public string? StoreName { get; set; }

    /// <summary>Riadky blocku, ku ktorym na zozname nic nebolo, a clovek ich chce v nakupe.</summary>
    public List<ImportReceiptRow> Extra { get; set; } = [];

    public List<FinishPairing> Pairings { get; set; } = [];
}

public class FinishPairing
{
    public string ReceiptName { get; set; } = string.Empty;

    public Guid ItemID { get; set; }
}

/// <summary>
/// Ukonci nakup jedneho zoznamu: z odskrtnutych poloziek spravi zaznam a zo zoznamu ich odoberie.
/// K nim sa pridaju veci z inych zoznamov, ktore klient vymenoval - tie sa vzali popri tom.
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

        // Ten isty nakup uz zapisany je - poziadavka len prisla druhy raz. Zoznam medzitym mohol
        // ostat prazdny, takze bez tejto kontroly by druhy pokus skoncil na "nic nie je odskrtnute".
        if (req.ID != Guid.Empty)
        {
            var already = await context.ShoppingRecords
                .Include(r => r.Items)
                .SingleOrDefaultAsync(r => r.ID == req.ID && r.HouseholdID == list.HouseholdID, ct);

            if (already is not null)
            {
                await Send.OkAsync(already.ToDTO(), ct);
                return;
            }
        }

        string? receiptID = null;
        var extraRows = new List<ShoppingRecordItemEntity>();
        var codes = await ItemAccess.CategoryCodesAsync(list.HouseholdID, context, ct);

        if (req.Receipt is { } receipt)
        {
            receiptID = receipt.ReceiptID.Trim().ToUpperInvariant();

            if (!ReceiptText.IsReceiptID(receiptID))
                ThrowError("This is not a receipt code.");

            // Ten isty blocek uz je nakupom - naskenoval ho niekto druhy alebo sa nacital v historii.
            var scanned = await context.ShoppingRecords
                .Include(r => r.Items)
                .FirstOrDefaultAsync(r => r.HouseholdID == list.HouseholdID && r.ReceiptID == receiptID, ct);

            if (scanned is not null)
            {
                await Send.ResultAsync(TypedResults.Conflict(scanned.ToDTO()));
                return;
            }

            extraRows = receipt.Extra
                .Select(r => new ShoppingRecordItemEntity
                {
                    Name = ItemName.Display(r.Name ?? string.Empty),
                    Quantity = string.IsNullOrWhiteSpace(r.Quantity) ? null : r.Quantity.Trim(),
                    Category = ItemCategory.Normalize(r.Category, codes)
                })
                .Where(r => r.Name.Length > 0)
                .ToList();

            if (extraRows.Count > 300
                || extraRows.Any(r => r.Name.Length > ItemEntity.NameMaxLength
                                      || r.Quantity?.Length > ItemEntity.QuantityMaxLength))
                ThrowError("A row is too long.");
        }

        var extra = (req.ExtraItemIDs ?? []).Distinct().ToList();

        var bought = await context.Items
            .Where(i => i.HouseholdID == list.HouseholdID
                && (i.ListID == list.ID || extra.Contains(i.ID))
                && (i.IsChecked || i.BoughtQuantity != null))
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        // Nakup bez jedinej odskrtnutej polozky by bol prazdny zaznam v historii.
        if (bought.Count == 0 && extraRows.Count == 0)
        {
            await Send.ResultAsync(TypedResults.Conflict("Nothing was checked off."));
            return;
        }

        var record = new ShoppingRecordEntity
        {
            // Prazdny identifikator si doplni EF Core sam, tak ako pri kazdej inej entite.
            ID = req.ID,
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
                .Concat(extraRows)
                .ToList(),
            ReceiptID = receiptID,
            StoreName = TrimStore(req.Receipt?.StoreName)
        };

        context.ShoppingRecords.Add(record);

        if (req.Receipt is { } paired)
        {
            // Riadok blocku je ta odskrtnuta vec - nabuduce ju appka sparuje sama. Riadky navyse
            // si pamataju to, ako ich clovek nechal.
            var byID = bought.ToDictionary(i => i.ID);

            await ReceiptAliases.RememberAsync(list.HouseholdID,
                paired.Pairings
                    .Where(p => byID.ContainsKey(p.ItemID))
                    .Select(p => (p.ReceiptName, byID[p.ItemID].Name, byID[p.ItemID].Category))
                    .Concat(paired.Extra
                        .Where(r => !string.IsNullOrWhiteSpace(r.ReceiptName))
                        .Select(r => (r.ReceiptName!, ItemName.Display(r.Name ?? string.Empty),
                            ItemCategory.Normalize(r.Category, codes)))),
                context, ct);
        }

        // Cele polozky zo zoznamu odchadzaju, ciastocne v nom ostavaju - zvysok stale treba.
        // Poznamka o odnesenej casti sa maze, uz je zapisana v zazname.
        context.Items.RemoveRange(bought.Where(i => i.IsChecked));

        foreach (var partial in bought.Where(i => !i.IsChecked))
            partial.BoughtQuantity = null;

        // Nakup skoncil, takze na prehlade uz nema co bezat.
        list.ShoppingStartedAt = null;
        list.ShoppingStartedByUserID = null;

        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException e) when (receiptID is not null
                                           && e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Ten isty blocek naskenoval v tej istej chvili niekto druhy.
            await Send.ResultAsync(TypedResults.Conflict());
            return;
        }

        // Zoznam sa tym vyprazdnil a v historii pribudol nakup - druha strana ma co menit na oboch.
        await notifier.ChangedAsync(record.HouseholdID, ChangeTopic.Trips, ct);

        await Send.OkAsync(record.ToDTO(), ct);
    }

    private static string? TrimStore(string? store)
    {
        var name = store?.Trim();

        return string.IsNullOrEmpty(name)
            ? null
            : name[..Math.Min(name.Length, ShoppingRecordEntity.StoreNameMaxLength)];
    }
}
