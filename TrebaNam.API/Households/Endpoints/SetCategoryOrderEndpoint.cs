using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Households.Endpoints;

public class SetCategoryOrderRequest
{
    /// <summary>Skupiny v novom poradi. Co v nom chyba, ostava za nimi tak, ako bolo.</summary>
    public List<Guid>? Order { get; set; }
}

/// <summary>
/// Prestavi poradie skupin na zozname. Kazdy obchod ma oddelenia inak a chodit v nom hore-dolu
/// je strata casu, tak si poradie urcuje domacnost - je spolocne, rovnako ako zoznam.
/// </summary>
public class SetCategoryOrderEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<SetCategoryOrderRequest, HouseholdDTO>
{
    public override void Configure()
    {
        Put("me/categories");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(SetCategoryOrderRequest req, CancellationToken ct)
    {
        await using var context = await factory.CreateDbContextAsync(ct);

        var found = await HouseholdAccess.FindForCurrentUserAsync(User, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (_, household) = found.Value;

        var categories = await HouseholdAccess.CategoriesAsync(household.ID, context, ct);
        var wanted = (req.Order ?? []).Distinct().ToList();

        // Poradie skladame z toho, co domacnost naozaj ma: cudzie a nezname ID padaju a skupiny,
        // ktore v poziadavke nie su, sa poradia za ne. Zo zoznamu tak nemoze vypadnut ziadna,
        // ani ked medzitym niekto druhy nejaku pridal.
        var ordered = wanted
            .Select(id => categories.SingleOrDefault(c => c.ID == id))
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        ordered.AddRange(categories.Where(c => !ordered.Contains(c)));

        for (var index = 0; index < ordered.Count; index++)
            ordered[index].Position = index;

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(household.ID, ChangeTopic.Household, ct);

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }
}
