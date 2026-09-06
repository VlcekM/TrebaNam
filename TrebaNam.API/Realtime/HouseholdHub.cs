using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API.Auth;

namespace TrebaNam.API.Realtime;

/// <summary>
/// Spojenie, ktorym sa druhy telefon dozvie o zmene skor, nez si ho niekto obnovi. Nic sa
/// cez neho neposiela ani nemeni - hub len povie, ze sa nieco stalo, a klient si data
/// dotiahne bezne cez API. Tak nema realny cas vlastnu cestu k datam, ktoru by bolo treba
/// zvlast zabezpecovat.
/// </summary>
[Authorize]
public class HouseholdHub(IDbContextFactory<DataContext> factory) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var ct = Context.ConnectionAborted;

        await using var context = await factory.CreateDbContextAsync(ct);

        var user = await CurrentUser.FindAsync(Context.User!, context, ct);

        // Bez domacnosti nie je co pocuvat; skupinu dostane az ked si nejaku zalozi,
        // co znamena nove nacitanie stranky a s nim aj nove spojenie.
        if (user?.HouseholdID is not null)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId, HouseholdChannel.Group(user.HouseholdID.Value), ct);
        }

        await base.OnConnectedAsync();
    }
}
