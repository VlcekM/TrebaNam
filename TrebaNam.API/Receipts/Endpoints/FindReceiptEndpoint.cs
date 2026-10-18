using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Items;

namespace TrebaNam.API.Receipts.Endpoints;

public class FindReceiptRequest
{
    public string ID { get; set; } = string.Empty;
}

/// <summary>
/// Nahlad blocku z eKasy podla identifikatora z QR kodu. Nic neuklada - je to podklad pre
/// obrazovku, na ktorej clovek povie, co z blocku do nakupu patri.
/// </summary>
public class FindReceiptEndpoint(IDbContextFactory<DataContext> factory, EKasaClient ekasa)
    : Endpoint<FindReceiptRequest, ReceiptPreviewDTO>
{
    public override void Configure()
    {
        Get("{id}");
        Group<ReceiptGroup>();
    }

    public override async Task HandleAsync(FindReceiptRequest req, CancellationToken ct)
    {
        var receiptID = req.ID.Trim().ToUpperInvariant();

        // Na eKasu ide len to, co ako blocek naozaj vyzera - nie sme otvorena brana na cokolvek.
        if (!ReceiptText.IsReceiptID(receiptID))
            ThrowError("This is not a receipt code.");

        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user?.HouseholdID is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        EKasaReceipt? receipt;

        try
        {
            receipt = await ekasa.FindAsync(receiptID, ct);
        }
        catch (Exception e) when (e is EKasaUnavailableException or HttpRequestException or TaskCanceledException
                                      && !ct.IsCancellationRequested)
        {
            await Send.ResultAsync(TypedResults.StatusCode(StatusCodes.Status502BadGateway));
            return;
        }

        if (receipt is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var imported = await context.ShoppingRecords
            .Where(r => r.HouseholdID == user.HouseholdID && r.ReceiptID == receiptID)
            .Select(r => new { r.ID, r.CompletedAt })
            .FirstOrDefaultAsync(ct);

        // Skupinu hadame podla toho, kam domacnost tu istu vec dala naposledy. Nazvy z blocku sa
        // opakuju presne, takze druhy nakup z toho isteho obchodu uz pride roztriedeny.
        var past = await context.ShoppingRecords
            .Where(r => r.HouseholdID == user.HouseholdID)
            .OrderByDescending(r => r.CompletedAt)
            .SelectMany(r => r.Items.Select(i => new { i.Name, i.Category }))
            .ToListAsync(ct);

        var codes = await ItemAccess.CategoryCodesAsync(user.HouseholdID.Value, context, ct);
        var aliases = await ReceiptAliases.OfHouseholdAsync(user.HouseholdID.Value, context, ct);
        var lastCategory = new Dictionary<string, string>();

        foreach (var row in past)
            lastCategory.TryAdd(ItemName.Key(row.Name), row.Category);

        var items = ReceiptText.SoldLines(receipt.Items)
            .Select(i => new { Name = ReceiptText.ItemName(i.Name), i.Quantity, i.Price })
            .GroupBy(i => ItemName.Key(i.Name))
            .Select(g =>
            {
                // Co domacnost s tymto riadkom spravila minule, ma prednost pred hadanim skupiny.
                var alias = aliases.GetValueOrDefault(g.Key);

                return new ReceiptItemDTO
                {
                    ReceiptName = g.First().Name,
                    Name = alias?.Name ?? g.First().Name,
                    Remembered = alias is not null,
                    Quantity = g.Sum(i => i.Quantity),
                    Price = g.Sum(i => i.Price),
                    Category = ItemCategory.Normalize(
                        alias?.Category ?? lastCategory.GetValueOrDefault(g.Key), codes),
                    IsDeposit = ReceiptText.IsDeposit(g.First().Name)
                };
            })
            .ToList();

        await Send.OkAsync(new ReceiptPreviewDTO
        {
            ReceiptID = receiptID,
            StoreName = ReceiptText.StoreName(receipt.Organization?.Name),
            IssuedAt = EKasaClient.IssuedAt(receipt.IssueDate),
            TotalCost = receipt.TotalPrice,
            Items = items,
            ImportedRecordID = imported?.ID,
            ImportedAt = imported?.CompletedAt
        }, ct);
    }
}
