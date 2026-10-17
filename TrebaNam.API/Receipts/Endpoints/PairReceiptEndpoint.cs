using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.Items;

namespace TrebaNam.API.Receipts.Endpoints;

public class PairReceiptRequest
{
    /// <summary>Riadky blocku, ktore este nie su sparovane.</summary>
    public List<string> Lines { get; set; } = [];

    /// <summary>Odskrtnute veci z nakupu.</summary>
    public List<string> Items { get; set; } = [];
}

public class PairReceiptResponse
{
    public List<ReceiptPairDTO> Pairs { get; set; } = [];
}

/// <summary>Navrh paru: index riadku v Lines a index veci v Items z poziadavky.</summary>
public class ReceiptPairDTO
{
    public int Line { get; set; }

    public int Item { get; set; }
}

/// <summary>
/// Necha model navrhnut, ktory riadok blocku je ktora vec z nakupu. Nic neuklada - pary sa
/// zapamataju az s ukoncenim nakupu, ked ich clovek potvrdi. Pyta sa len prihlaseny clovek
/// s domacnostou, aby to nebola verejna brana k modelu na nase naklady.
/// </summary>
public class PairReceiptEndpoint(IDbContextFactory<DataContext> factory, OpenRouterClient openRouter)
    : Endpoint<PairReceiptRequest, PairReceiptResponse>
{
    private const int MaxLines = 100;

    private const int MaxItems = 200;

    public override void Configure()
    {
        Post("pair");
        Group<ReceiptGroup>();
    }

    public override async Task HandleAsync(PairReceiptRequest req, CancellationToken ct)
    {
        if (req.Lines.Count == 0 || req.Items.Count == 0)
        {
            await Send.OkAsync(new PairReceiptResponse(), ct);
            return;
        }

        if (req.Lines.Count > MaxLines || req.Items.Count > MaxItems
            || req.Lines.Concat(req.Items).Any(n => n is null || n.Length > ItemEntity.NameMaxLength))
            ThrowError("Too much to pair.");

        await using (var context = await factory.CreateDbContextAsync(ct))
        {
            var user = await CurrentUser.FindAsync(User, context, ct);

            if (user?.HouseholdID is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
        }

        if (!openRouter.IsConfigured)
        {
            await Send.ResultAsync(TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable));
            return;
        }

        List<(int Line, int Item)> pairs;

        try
        {
            pairs = await openRouter.PairAsync(req.Lines, req.Items, ct);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            await Send.ResultAsync(TypedResults.StatusCode(StatusCodes.Status502BadGateway));
            return;
        }

        await Send.OkAsync(new PairReceiptResponse
        {
            Pairs = pairs.Select(p => new ReceiptPairDTO { Line = p.Line, Item = p.Item }).ToList()
        }, ct);
    }
}
