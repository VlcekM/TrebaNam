using System.Text.RegularExpressions;

namespace TrebaNam.API.Receipts;

/// <summary>
/// Co sa z textu blocku da urobit, aby sa citalo ako zoznam a nie ako pokladnicna paska.
/// Su to len navrhy - v nahlade ich clovek moze prepisat, kym nakup ulozi.
/// </summary>
public static partial class ReceiptText
{
    /// <summary>Identifikator online blocku z QR kodu: "O-" a 32 sestnastkovych znakov.</summary>
    public static bool IsReceiptID(string? value) => value is not null && ReceiptIDPattern().IsMatch(value);

    /// <summary>
    /// Nazov z pokladne: doplneny medzerami na pevnu sirku a casto cely velkymi pismenami.
    /// Velke pismena sa citaju ako krik, takze taky nazov ide na male a prve pismeno hore.
    /// Nazov, ktory male pismena ma, nechavame tak - niekto ho tak napisal schvalne.
    /// </summary>
    public static string ItemName(string? raw)
    {
        var name = Spaces().Replace(raw?.Trim() ?? string.Empty, " ");

        if (!name.Any(char.IsLower))
            name = name.ToLowerInvariant();

        return Items.ItemName.Display(name);
    }

    /// <summary>
    /// Firma z blocku je obchodne meno ("Lidl Slovenska republika, s.r.o."), nie to, co je nad
    /// dverami. Pravnu formu a krajinu odstrihneme; zvysok si clovek moze v nahlade prepisat.
    /// </summary>
    public static string? StoreName(string? organization)
    {
        var name = organization?.Split(',')[0].Trim();

        if (string.IsNullOrEmpty(name))
            return null;

        name = Country().Replace(LegalSuffix().Replace(name, string.Empty), string.Empty).Trim();

        return name.Length > 0 ? name : organization!.Trim();
    }

    /// <summary>
    /// Zaloha za plechovku alebo flasu je na blocku riadok ako kazdy iny, ale kupena vec to nie je.
    /// V nahlade ide preto neoznacena - kto ju chce v nakupe, oznaci si ju.
    /// </summary>
    public static bool IsDeposit(string name) => Items.ItemName.Key(name).Contains("zaloh");

    /// <summary>
    /// Predany tovar s uz odratanymi zlavami. Zlava (Z) je na blocku samostatny riadok hned pod
    /// tym, na ktory sa vztahuje, takze sa pripise k poslednemu predanemu riadku - nahlad potom
    /// ukaze 6,00 a nie 9,49. Vratenie ci oprava nie su veci, ktore sa odniesli domov.
    /// </summary>
    public static List<EKasaItem> SoldLines(IEnumerable<EKasaItem> lines)
    {
        var sold = new List<EKasaItem>();

        foreach (var line in lines)
        {
            if (line.ItemType == "K" && !string.IsNullOrWhiteSpace(line.Name))
            {
                sold.Add(new EKasaItem { Name = line.Name, ItemType = line.ItemType, Quantity = line.Quantity, Price = line.Price });
            }
            else if (line.ItemType == "Z" && sold.Count > 0)
            {
                // Znamienko zlavy sa medzi pokladnami lisi; zlava vzdy znizuje.
                var item = sold[^1];
                item.Price = Math.Max(0, item.Price - Math.Abs(line.Price));
            }
        }

        return sold;
    }

    [GeneratedRegex(@"^O-[0-9A-F]{32}$")]
    private static partial Regex ReceiptIDPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"\s+(spol\.\s*s\s*r\.\s*o\.|s\.\s*r\.\s*o\.|a\.\s*s\.|k\.\s*s\.|v\.\s*o\.\s*s\.)$",
        RegexOptions.IgnoreCase)]
    private static partial Regex LegalSuffix();

    [GeneratedRegex(@"\s+(slovensk[aá] republika|slovensko|slovakia)$", RegexOptions.IgnoreCase)]
    private static partial Regex Country();
}
