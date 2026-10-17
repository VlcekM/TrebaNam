using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TrebaNam.API.Receipts;

/// <summary>
/// Parovanie riadkov blocku s vecami zo zoznamu cez jazykovy model na OpenRouteri. Pokladna
/// skracuje ("KOR.KURCA AZIA THYMOS 25") a co je co, uhadne model lepsie nez porovnanie textu.
/// Je to len navrh pre obrazovku s parovanim - clovek ho vidi a moze ho zmenit.
///
/// Kluc je v konfiguracii (OpenRouter:ApiKey, v developmente cez user-secrets); bez neho
/// parovanie modelom jednoducho nie je. Model je predvolene "openrouter/free", ktory si sam
/// vyberie niektory z bezplatnych.
/// </summary>
public class OpenRouterClient(HttpClient http, IConfiguration configuration, ILogger<OpenRouterClient> logger)
{
    public const string BaseAddress = "https://openrouter.ai/api/v1/";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(configuration["OpenRouter:ApiKey"]);

    private string Model => configuration["OpenRouter:Model"] is { Length: > 0 } model ? model : "openrouter/free";

    private const string Instructions =
        """
        You pair lines from a Slovak shop receipt with items on the household's shopping list.
        Receipt lines are abbreviated by the till (e.g. "MON.ZERO 0,5l" is a Monster Zero energy
        drink, "Kur.prsné rezne" is chicken breast, "PARAD.PASIR." is passata). List items are
        written by people, in Slovak or English, often generic ("Chicken", "Energy drink").
        Pair a receipt line with an item only when the line is plausibly that item. Several
        lines may pair with the same item; a line pairs with at most one item. Leave out lines
        that match nothing - deposits, bags and things that were not on the list stay unpaired.
        Answer with JSON only: {"pairs":[{"line":<line number>,"item":<item number>}]}.
        """;

    /// <summary>Navrhnute pary (cislo riadku, cislo veci); prazdne, ked model nic nenasiel.</summary>
    public async Task<List<(int Line, int Item)>> PairAsync(
        IReadOnlyList<string> lines, IReadOnlyList<string> items, CancellationToken ct)
    {
        var prompt = "Receipt lines:\n"
                     + string.Join('\n', lines.Select((line, i) => $"{i}: {line}"))
                     + "\n\nShopping list items:\n"
                     + string.Join('\n', items.Select((item, i) => $"{i}: {item}"));

        var body = new JsonObject
        {
            ["model"] = Model,
            ["temperature"] = 0,
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = Instructions },
                new JsonObject { ["role"] = "user", ["content"] = prompt }
            },
            ["response_format"] = JsonNode.Parse(
                """
                {
                  "type": "json_schema",
                  "json_schema": {
                    "name": "pairs",
                    "strict": true,
                    "schema": {
                      "type": "object",
                      "properties": {
                        "pairs": {
                          "type": "array",
                          "items": {
                            "type": "object",
                            "properties": { "line": { "type": "integer" }, "item": { "type": "integer" } },
                            "required": ["line", "item"],
                            "additionalProperties": false
                          }
                        }
                      },
                      "required": ["pairs"],
                      "additionalProperties": false
                    }
                  }
                }
                """)
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration["OpenRouter:ApiKey"]);

        using var response = await http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("OpenRouter answered {Status}: {Body}", (int)response.StatusCode,
                await response.Content.ReadAsStringAsync(ct));
            throw new HttpRequestException($"OpenRouter answered {(int)response.StatusCode}");
        }

        var answer = await response.Content.ReadFromJsonAsync<JsonNode>(ct);
        var content = answer?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();

        return Parse(content, lines.Count, items.Count);
    }

    /// <summary>
    /// Nie kazdy bezplatny model drzi schemu - niektory JSON zabali do ```, iny pred neho nieco
    /// napise. Berieme prvy objekt v texte a z neho len cisla, ktore naozaj existuju.
    /// </summary>
    private static List<(int Line, int Item)> Parse(string? content, int lineCount, int itemCount)
    {
        var start = content?.IndexOf('{') ?? -1;
        var end = content?.LastIndexOf('}') ?? -1;

        if (start < 0 || end <= start)
            return [];

        try
        {
            var pairs = JsonNode.Parse(content![start..(end + 1)])?["pairs"]?.AsArray() ?? [];

            return pairs
                .Select(p => (Line: p?["line"]?.GetValue<int>() ?? -1, Item: p?["item"]?.GetValue<int>() ?? -1))
                .Where(p => p.Line >= 0 && p.Line < lineCount && p.Item >= 0 && p.Item < itemCount)
                .DistinctBy(p => p.Line)
                .ToList();
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException)
        {
            return [];
        }
    }
}
