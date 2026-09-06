using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Items;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Households.Endpoints;

public class CreateCategoryRequest
{
    public string? Name { get; set; }
}

/// <summary>
/// Prida domacnosti vlastnu skupinu. Zakladnych sest je len zaciatok - obchody nie su rovnake
/// a "u maziara" alebo "drogeria" vie povedat viac nez "ostatne".
/// </summary>
public class CreateCategoryEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<CreateCategoryRequest, HouseholdDTO>
{
    /// <summary>Zoznam sa este ma dat prejst ocami, nie prehladavat.</summary>
    private const int MaxCategories = 30;

    public override void Configure()
    {
        Post("me/categories");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(CreateCategoryRequest req, CancellationToken ct)
    {
        var name = req.Name?.Trim();

        if (string.IsNullOrEmpty(name))
            ThrowError(r => r.Name, "Give the group a name.");

        if (name!.Length > HouseholdCategoryEntity.NameMaxLength)
            ThrowError(r => r.Name, $"Keep the name under {HouseholdCategoryEntity.NameMaxLength} characters.");

        await using var context = await factory.CreateDbContextAsync(ct);

        var found = await HouseholdAccess.FindForCurrentUserAsync(User, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (_, household) = found.Value;

        var categories = await HouseholdAccess.CategoriesAsync(household.ID, context, ct);

        if (categories.Count >= MaxCategories)
        {
            await Send.ResultAsync(TypedResults.Conflict("That is as many groups as one household can keep."));
            return;
        }

        // Dve rovnako pomenovane skupiny by na zozname nikto nerozoznal; zakladne sa takto
        // porovnat nedaju, lebo ich nazov drzi preklad a nie databaza.
        var key = ItemName.Key(name);
        var duplicate = categories.FirstOrDefault(c => c.Name is not null && ItemName.Key(c.Name) == key);

        if (duplicate is not null)
        {
            await Send.ResultAsync(TypedResults.Conflict(new { name = duplicate.Name }));
            return;
        }

        context.HouseholdCategories.Add(new HouseholdCategoryEntity
        {
            HouseholdID = household.ID,
            Code = ItemCategory.Unique(ItemCategory.Slug(name), [.. categories.Select(c => c.Code)]),
            Name = ItemName.Display(name),
            // Na koniec, teda pred ostatne az vtedy, ked to domacnost sama prestavi.
            Position = categories.Count == 0 ? 0 : categories.Max(c => c.Position) + 1,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(household.ID, ChangeTopic.Household, ct);

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }
}
