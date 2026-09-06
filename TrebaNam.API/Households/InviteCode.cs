using System.Security.Cryptography;

namespace TrebaNam.API.Households;

/// <summary>
/// Kody pozvanok. Abeceda je bez znakov, ktore sa pri prepisovani z telefonu pletu
/// (0/O, 1/I/L), takze kod sa da nadiktovat aj cez stol, nielen poslat odkazom.
/// </summary>
public static class InviteCode
{
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private const int GroupLength = 4;

    private const int Groups = 2;

    public static string Generate()
    {
        var chars = new char[Groups * GroupLength + Groups - 1];
        var index = 0;

        for (var group = 0; group < Groups; group++)
        {
            if (group > 0)
                chars[index++] = '-';

            for (var i = 0; i < GroupLength; i++)
                chars[index++] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>
    /// Kod z URL moze prist v akejkolvek velkosti pismen a s medzerami navyse -
    /// porovnavame vzdy az normalizovanu podobu.
    /// </summary>
    public static string Normalize(string? code) =>
        new(code?.Where(c => !char.IsWhiteSpace(c)).Select(char.ToUpperInvariant).ToArray() ?? []);
}
