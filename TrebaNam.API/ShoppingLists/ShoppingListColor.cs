namespace TrebaNam.API.ShoppingLists;

/// <summary>
/// Farby zoznamov. Su to kody, nie hexy: appka ma svetly aj tmavy rezim a farba z formulara
/// by v jednom z nich vzdy niekde zle sedela. Ktory odtien kod znamena, vie klient.
/// </summary>
public static class ShoppingListColor
{
    public const string Default = "green";

    public static readonly string[] All =
        [Default, "blue", "amber", "rose", "violet", "slate"];

    public static string Normalize(string? value)
    {
        var code = value?.Trim().ToLowerInvariant();

        return code is not null && All.Contains(code) ? code : Default;
    }
}
