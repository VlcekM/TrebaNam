using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace TrebaNam.API.Items.Endpoints;

public class CheckItemRequest
{
    public Guid ID { get; set; }

    public bool IsChecked { get; set; }
}

/// <summary>
/// Odskrtnutie polozky v rezime nakupu. Samostatny endpoint, nie uprava - v obchode
/// sa odskrtava rychlo a nema zmysel posielat pritom cely obsah polozky.
/// </summary>
public class CheckItemEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<CheckItemRequest, ItemDTO>
{
    public override void Configure()
    {
        Put("{id}/checked");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(CheckItemRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var item = await ItemAccess.FindForCurrentUserAsync(User, req.ID, context, ct);

        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        item.IsChecked = req.IsChecked;

        // Cele mnozstvo aj ziadne prebijaju predchadzajucu ciastocnu poznamku - inak by
        // do historie islo aj to, co uz je v odskrtnutej polozke zapocitane.
        item.BoughtQuantity = null;

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(item.ToDTO(), ct);
    }
}
