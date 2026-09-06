namespace TrebaNam.API.ShoppingRecords;

/// <summary>
/// Suma za nakup z formulara. Hranice aj zaokruhlenie su na jednom mieste, lebo ju zapisuje
/// ukoncenie nakupu aj neskorsia uprava zaznamu.
/// </summary>
public static class TripCost
{
    public static bool IsValid(decimal? cost) =>
        cost is null || (cost >= 0m && cost <= ShoppingRecordEntity.MaxTotalCost);

    /// <summary>Na centy - vsetko dalsie by aj tak bolo len sum v databaze.</summary>
    public static decimal? Round(decimal? cost) => cost is null
        ? null
        : Math.Round(cost.Value, ShoppingRecordEntity.CostScale, MidpointRounding.AwayFromZero);

    public const string OutOfRange = "Enter a total between 0 and 100000.";
}
