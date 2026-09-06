using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;
using TrebaNam.API.ShoppingLists;

namespace TrebaNam.API.Households.Endpoints;

public class CreateHouseholdRequest
{
    public string? Name { get; set; }

    /// <summary>
    /// Ako sa ma volat prvy zoznam. Nazov posiela klient, lebo je to text pre cloveka a API
    /// nevie, v ktorom jazyku appku prave cita.
    /// </summary>
    public string? ListName { get; set; }
}

/// <summary>Zalozi domacnost a rovno do nej prida zakladatela.</summary>
public class CreateHouseholdEndpoint(IDbContextFactory<DataContext> factory)
    : Endpoint<CreateHouseholdRequest, HouseholdDTO>
{
    public override void Configure()
    {
        Post("");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(CreateHouseholdRequest req, CancellationToken ct)
    {
        var name = req.Name?.Trim();

        if (string.IsNullOrEmpty(name))
            ThrowError(r => r.Name, "Give the household a name.");

        if (name!.Length > HouseholdEntity.NameMaxLength)
            ThrowError(r => r.Name, $"Keep the name under {HouseholdEntity.NameMaxLength} characters.");

        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(User, context, ct);

        if (user is null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Jeden clovek, jedna domacnost - inak by nebolo jasne, do ktorej patri novy zoznam.
        if (user.HouseholdID is not null)
        {
            await Send.ResultAsync(TypedResults.Conflict("You are already in a household."));
            return;
        }

        var household = new HouseholdEntity
        {
            Name = name,
            InviteCode = await NextFreeCodeAsync(context, ct),
            CreatedByUserID = user.ID,
            CreatedAt = DateTimeOffset.UtcNow
        };

        context.Households.Add(household);
        user.HouseholdID = household.ID;

        // Domacnost zacina so zakladnymi skupinami a s jednym zoznamom - obe si potom premenuje
        // a preskladaju po svojom, ale prazdna appka by nemala kam pridat prvu vec.
        context.HouseholdCategories.AddRange(HouseholdCategoryEntity.Defaults(household.ID));
        context.ShoppingLists.Add(ShoppingListAccess.First(household.ID, req.ListName));

        await context.SaveChangesAsync(ct);

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }

    /// <summary>Kod je kratky, takze zrazku beriem ako realnu moznost a skusam znova.</summary>
    private static async Task<string> NextFreeCodeAsync(DataContext context, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = InviteCode.Generate();

            if (!await context.Households.AnyAsync(h => h.InviteCode == code, ct))
                return code;
        }

        throw new InvalidOperationException("Could not generate a free invite code.");
    }
}
