using System.Globalization;
using System.Text;

namespace TrebaNam.API.Items;

/// <summary>
/// Nazov polozky tak, ako sa ma zapisat, a tak, ako sa ma porovnavat. Obe pravidla su tu spolu,
/// lebo duplicitu hlada zalozenie aj uprava a naseptavac sa musi trafit do toho isteho.
/// </summary>
public static class ItemName
{
    /// <summary>
    /// Zoznam citaju dvaja ludia, takze zaciatok vety patri velkemu pismenu aj vtedy, ked to
    /// clovek do mobilu naklepal malym. Zvysok nazvu nechavame tak, ako ho napisal.
    /// </summary>
    public static string Display(string name)
    {
        var trimmed = name.Trim();

        if (trimmed.Length == 0 || !char.IsLower(trimmed[0]))
            return trimmed;

        return string.Concat(char.ToUpper(trimmed[0], CultureInfo.InvariantCulture), trimmed[1..]);
    }

    /// <summary>
    /// Kluc na porovnanie: bez diakritiky, bez velkych pismen a bez zdvojenych medzier, takze
    /// "Banány" a "banany" su ta ista vec. Nikde sa neuklada, pocita sa pri kazdom porovnani.
    /// </summary>
    public static string Key(string name)
    {
        var decomposed = name.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastWasSpace = false;

        foreach (var character in decomposed)
        {
            // Diakritika je po rozklade samostatny znak, takze staci nebrat ju.
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsWhiteSpace(character))
            {
                if (!lastWasSpace && builder.Length > 0)
                    builder.Append(' ');

                lastWasSpace = true;
                continue;
            }

            lastWasSpace = false;
            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().TrimEnd().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Odpoved na duplicitu: samotny nazov tak, ako uz v zozname je. Vetu z toho sklada klient,
    /// lebo texty pre cloveka su jeho, nie nase.
    /// </summary>
    public static object AlreadyOnList(string name) => new { name };
}
