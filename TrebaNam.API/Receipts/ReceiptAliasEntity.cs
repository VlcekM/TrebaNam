using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Items;

namespace TrebaNam.API.Receipts;

/// <summary>
/// Co si domacnost pod riadkom blocku predstavuje. Pokladna pise "MON.ZERO 0,5l", na zozname
/// stoji "Monster" - a ked to clovek raz sparoval alebo premenoval, nabuduce to uz vie appka.
/// Jeden riadok na domacnost a nazov z blocku; novsie rozhodnutie prepise starsie.
/// </summary>
public class ReceiptAliasEntity
{
    public Guid ID { get; set; }

    public Guid HouseholdID { get; set; }

    /// <summary>ItemName.Key nazvu z blocku (po upravach z ReceiptText.ItemName).</summary>
    [MaxLength(ItemEntity.NameMaxLength)]
    public required string ReceiptKey { get; set; }

    /// <summary>Nazov, ako ho pise domacnost.</summary>
    [MaxLength(ItemEntity.NameMaxLength)]
    public required string Name { get; set; }

    [MaxLength(ItemCategory.MaxLength)]
    public required string Category { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>Zapamatane parovania: citanie pre nahlad, zapis pri nacitani blocku aj pri ukonceni nakupu.</summary>
public static class ReceiptAliases
{
    /// <summary>Zapamatane parovania domacnosti podla kluca riadku z blocku.</summary>
    public static Task<Dictionary<string, ReceiptAliasEntity>> OfHouseholdAsync(
        Guid householdID, DataContext context, CancellationToken ct) =>
        context.ReceiptAliases
            .Where(a => a.HouseholdID == householdID)
            .ToDictionaryAsync(a => a.ReceiptKey, ct);

    /// <summary>
    /// Zapamata si, co z ktoreho riadku blocku domacnost spravila. Uklada sa spolu so zaznamom
    /// nakupu, takze to robi volajuci cez SaveChangesAsync.
    /// </summary>
    public static async Task RememberAsync(
        Guid householdID,
        IEnumerable<(string ReceiptName, string Name, string Category)> pairs,
        DataContext context,
        CancellationToken ct)
    {
        var latest = new Dictionary<string, (string Name, string Category)>();

        foreach (var (receiptName, name, category) in pairs)
        {
            var key = ItemName.Key(ReceiptText.ItemName(receiptName));

            if (key.Length == 0 || key.Length > ItemEntity.NameMaxLength || name.Length == 0)
                continue;

            latest[key] = (name, category);
        }

        if (latest.Count == 0)
            return;

        var keys = latest.Keys.ToList();
        var existing = await context.ReceiptAliases
            .Where(a => a.HouseholdID == householdID && keys.Contains(a.ReceiptKey))
            .ToDictionaryAsync(a => a.ReceiptKey, ct);
        var now = DateTimeOffset.UtcNow;

        foreach (var (key, (name, category)) in latest)
        {
            if (existing.TryGetValue(key, out var alias))
            {
                alias.Name = name;
                alias.Category = category;
                alias.UpdatedAt = now;
            }
            else
            {
                context.ReceiptAliases.Add(new ReceiptAliasEntity
                {
                    HouseholdID = householdID,
                    ReceiptKey = key,
                    Name = name,
                    Category = category,
                    UpdatedAt = now
                });
            }
        }
    }
}
