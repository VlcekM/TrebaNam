using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Items;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Households.Endpoints;

public class RenameCategoryRequest
{
    public Guid ID { get; set; }

    public string? Name { get; set; }
}

/// <summary>
/// Premenuje skupinu. Kod ostava, takze polozky nikam neprechadzaju - meni sa len to, ako sa
/// skupina cita. Pomenovana zakladna skupina tym prestava byt prekladom a od tej chvile sa cita
/// tak, ako si ju domacnost napisala; to je aj cielom, obchod je jeden pre oboch.
/// </summary>
public class RenameCategoryEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<RenameCategoryRequest, HouseholdDTO>
{
    public override void Configure()
    {
        Put("me/categories/{id}");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(RenameCategoryRequest req, CancellationToken ct)
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
        var category = categories.SingleOrDefault(c => c.ID == req.ID);

        if (category is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var key = ItemName.Key(name);

        var duplicate = categories.FirstOrDefault(
            c => c.ID != category.ID && c.Name is not null && ItemName.Key(c.Name) == key);

        if (duplicate is not null)
        {
            await Send.ResultAsync(TypedResults.Conflict(new { name = duplicate.Name }));
            return;
        }

        category.Name = ItemName.Display(name);

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(household.ID, ChangeTopic.Household, ct);

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }
}
