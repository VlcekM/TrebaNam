using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace TrebaNam.API.Items.Endpoints;

public class SetBoughtQuantityRequest
{
    public Guid ID { get; set; }

    /// <summary>Prazdna hodnota ciastocny nakup zrusi - polozka je zase cela nekupena.</summary>
    public string? Quantity { get; set; }
}

/// <summary>
/// Zapise, ze sa z polozky kupila len cast. Polozka ostava neodskrtnuta, lebo zvysok stale
/// treba; do historie sa pri ukonceni nakupu dostane prave toto mnozstvo.
/// </summary>
public class SetBoughtQuantityEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<SetBoughtQuantityRequest, ItemDTO>
{
    public override void Configure()
    {
        Put("{id}/bought");
        Group<ItemGroup>();
    }

    public override async Task HandleAsync(SetBoughtQuantityRequest req, CancellationToken ct)
    {
        var quantity = req.Quantity?.Trim();

        if (quantity?.Length > ItemEntity.QuantityMaxLength)
            ThrowError($"Keep the quantity under {ItemEntity.QuantityMaxLength} characters.");

        await using var context = await factory.CreateDbContextAsync(ct);

        var item = await ItemAccess.FindForCurrentUserAsync(User, req.ID, context, ct);

        if (item is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        item.BoughtQuantity = string.IsNullOrEmpty(quantity) ? null : quantity;

        // Cast a cela polozka su dva rozne stavy, takze jedno vylucuje druhe.
        if (item.BoughtQuantity is not null)
            item.IsChecked = false;

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(item.ToDTO(), ct);
    }
}
