using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Realtime;

namespace TrebaNam.API.Households.Endpoints;

public class RenameHouseholdRequest
{
    public string? Name { get; set; }
}

/// <summary>Premenuje domacnost. Meno je jedine, co sa na nej da zmenit - kod pozvanky je tajomstvo.</summary>
public class RenameHouseholdEndpoint(IDbContextFactory<DataContext> factory, HouseholdNotifier notifier)
    : Endpoint<RenameHouseholdRequest, HouseholdDTO>
{
    public override void Configure()
    {
        Put("me");
        Group<HouseholdGroup>();
    }

    public override async Task HandleAsync(RenameHouseholdRequest req, CancellationToken ct)
    {
        var name = req.Name?.Trim();

        if (string.IsNullOrEmpty(name))
            ThrowError(r => r.Name, "Give the household a name.");

        if (name!.Length > HouseholdEntity.NameMaxLength)
            ThrowError(r => r.Name, $"Keep the name under {HouseholdEntity.NameMaxLength} characters.");

        await using var context = await factory.CreateDbContextAsync(ct);

        var found = await HouseholdAccess.FindForCurrentUserAsync(User, context, ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var (_, household) = found.Value;

        household.Name = name;

        await context.SaveChangesAsync(ct);
        await notifier.ChangedAsync(household.ID, ChangeTopic.Household, ct);

        await Send.OkAsync(await household.DetailAsync(context, ct), ct);
    }
}
