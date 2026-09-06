namespace TrebaNam.API.Realtime;

/// <summary>
/// Mena, na ktorych sa musi zhodnut server aj klient. Su tu spolu, aby sa nedali zmenit
/// len na jednej strane - hub je jedina cast API, kde to prekladac nechyti.
/// </summary>
public static class HouseholdChannel
{
    /// <summary>Pod /api, aby ho SPA fallback ani vyvojarska proxy nezobrali za stranku.</summary>
    public const string Path = "/api/hub/household";

    /// <summary>Sprava, ktorou hub hovori "pozri sa na to znova".</summary>
    public const string Changed = "changed";

    /// <summary>Vsetci prihlaseni z jednej domacnosti - inde sa nic neposiela.</summary>
    public static string Group(Guid householdID) => $"household:{householdID}";
}

/// <summary>Co sa zmenilo. Klient si podla toho vie vybrat, ktore data znova nacita.</summary>
public static class ChangeTopic
{
    public const string Items = "items";

    public const string Lists = "lists";

    public const string Trips = "trips";

    public const string Household = "household";
}
