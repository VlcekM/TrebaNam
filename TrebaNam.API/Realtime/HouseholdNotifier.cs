using Microsoft.AspNetCore.SignalR;

namespace TrebaNam.API.Realtime;

/// <summary>
/// Jedina cesta, ktorou endpoint povie domacnosti, ze sa nieco zmenilo. Chyba spojenia sa
/// nikdy nesmie prejavit na zapise - ten uz je v databaze a druha strana ho v najhorsom
/// pripade uvidi az pri dalsom nacitani.
/// </summary>
public sealed class HouseholdNotifier(IHubContext<HouseholdHub> hub, ILogger<HouseholdNotifier> logger)
{
    public async Task ChangedAsync(Guid householdID, string topic, CancellationToken ct = default)
    {
        try
        {
            await hub.Clients
                .Group(HouseholdChannel.Group(householdID))
                .SendAsync(HouseholdChannel.Changed, topic, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not notify household {HouseholdID} about {Topic}.", householdID, topic);
        }
    }
}
