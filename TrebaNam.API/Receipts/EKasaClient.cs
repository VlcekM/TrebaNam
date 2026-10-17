using System.Globalization;

namespace TrebaNam.API.Receipts;

/// <summary>
/// Verejne vyhladavanie blockov Financnej spravy (eKasa) - to iste, co pouziva ich appka
/// "Over doklad". Ide cez server, nie z prehliadaca: z cudzej domeny ho CORS nepusti a ich
/// firewall odmieta poziadavky bez User-Agenta, ktory by vyzeral ako nejaky program.
/// </summary>
public class EKasaClient(HttpClient http, ILogger<EKasaClient> logger)
{
    public const string BaseAddress = "https://ekasa.financnasprava.sk/mdu/api/v1/opd/";

    /// <summary>
    /// Blocek podla jeho identifikatora; null, ked ho eKasa nepozna. Nepoznany blocek pride ako
    /// 200 s prazdnym "receipt", takze o vysledku rozhoduje telo, nie stavovy kod.
    /// </summary>
    public async Task<EKasaReceipt?> FindAsync(string receiptID, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync("receipt/find", new { receiptId = receiptID }, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("eKasa answered {Status} for {Receipt}", (int)response.StatusCode, receiptID);
            throw new EKasaUnavailableException();
        }

        // Firewall odmieta HTML strankou so stavom 200, takze aj to, ze to nie je JSON, je vypadok.
        if (response.Content.Headers.ContentType?.MediaType?.Contains("json") != true)
        {
            logger.LogWarning("eKasa answered {Type} for {Receipt}", response.Content.Headers.ContentType, receiptID);
            throw new EKasaUnavailableException();
        }

        var answer = await response.Content.ReadFromJsonAsync<EKasaAnswer>(ct);

        return answer?.Receipt;
    }

    /// <summary>Datum z blocku je miestny cas pokladne, "14.10.2026 17:51:51", a pokladne su na Slovensku.</summary>
    public static DateTimeOffset? IssuedAt(string? value)
    {
        if (!DateTime.TryParseExact(value, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var local))
            return null;

        var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava");

        return new DateTimeOffset(local, zone.GetUtcOffset(local)).ToUniversalTime();
    }
}

/// <summary>eKasa neodpovedala alebo odpovedala niecim, co nie je blocek ani "nepoznam".</summary>
public class EKasaUnavailableException : Exception;

public class EKasaAnswer
{
    public EKasaReceipt? Receipt { get; set; }
}

public class EKasaReceipt
{
    public string? ReceiptID { get; set; }

    public string? IssueDate { get; set; }

    public decimal? TotalPrice { get; set; }

    public List<EKasaItem> Items { get; set; } = [];

    public EKasaOrganization? Organization { get; set; }
}

public class EKasaItem
{
    public string? Name { get; set; }

    /// <summary>
    /// Druh riadku: K je predany tovar, ostatne (vratenie, zlava, oprava, vratenie obalov)
    /// nie su veci, ktore sa odniesli domov.
    /// </summary>
    public string? ItemType { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Cena celeho riadku, nie za kus - 4 plechovky za 9,96.</summary>
    public decimal Price { get; set; }
}

public class EKasaOrganization
{
    public string? Name { get; set; }
}
