using System.Text.Json;
using System.Text.Json.Nodes;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Messages;

namespace TrebaNam.API.Receipts;

/// <summary>
/// Parovanie riadkov blocku s vecami zo zoznamu cez Claude Haiku na API Anthropicu. Pokladna
/// skracuje ("KOR.KURCA AZIA THYMOS 25") a co je co, uhadne model lepsie nez porovnanie textu.
/// Je to len navrh pre obrazovku s parovanim - clovek ho vidi a moze ho zmenit.
///
/// Kluc je v konfiguracii (Anthropic:ApiKey, v developmente cez user-secrets); bez neho
/// parovanie modelom jednoducho nie je. Model je predvolene "claude-haiku-5-5".
/// </summary>
public class ClaudeClient(IConfiguration configuration, ILogger<ClaudeClient> logger)
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);

    private string? ApiKey => configuration["Anthropic:ApiKey"];

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    private string Model => configuration["Anthropic:Model"] is { Length: > 0 } model ? model : "claude-haiku-5-5";

    private const string Instructions =
        """
        You pair lines from a Slovak shop receipt with items on the household's shopping list.
        Receipt lines are abbreviated by the till (e.g. "MON.ZERO 0,5l" is a Monster Zero energy
        drink, "Kur.prsné rezne" is chicken breast, "PARAD.PASIR." is passata). List items are
        written by people, in Slovak or English, often generic ("Chicken", "Energy drink").
        Pair a receipt line with an item only when the line is plausibly that item. Several
        lines may pair with the same item; a line pairs with at most one item. Leave out lines
        that match nothing - deposits, bags and things that were not on the list stay unpaired.
        Answer with {"pairs":[{"line":<line number>,"item":<item number>}]}.
        """;

    private static readonly Dictionary<string, JsonElement> Schema =
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
            """
            {
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
            """)!;

    /// <summary>Navrhnute pary (cislo riadku, cislo veci); prazdne, ked model nic nenasiel.</summary>
    public async Task<List<(int Line, int Item)>> PairAsync(
        IReadOnlyList<string> lines, IReadOnlyList<string> items, CancellationToken ct)
    {
        var prompt = "Receipt lines:\n"
                     + string.Join('\n', lines.Select((line, i) => $"{i}: {line}"))
                     + "\n\nShopping list items:\n"
                     + string.Join('\n', items.Select((item, i) => $"{i}: {item}"));

        AnthropicClient client = new() { ApiKey = ApiKey };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(Timeout);

        Message response;

        try
        {
            response = await client.Messages.Create(new MessageCreateParams
            {
                Model = Model,
                MaxTokens = 16000,
                System = Instructions,
                OutputConfig = new OutputConfig
                {
                    Effort = Effort.Low,
                    Format = new JsonOutputFormat { Schema = Schema }
                },
                Messages = [new UserMessageParam { Content = prompt }]
            }, timeout.Token);
        }
        catch (AnthropicApiException e)
        {
            logger.LogWarning(e, "Anthropic refused the pairing request");
            throw new HttpRequestException("Anthropic refused the pairing request", e);
        }

        if (response.StopReason != "end_turn")
        {
            logger.LogWarning("Anthropic stopped pairing with {StopReason}", response.StopReason);
            return [];
        }

        var content = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));

        return Parse(content, lines.Count, items.Count);
    }

    /// <summary>Schema drzi tvar odpovede; berieme z nej len cisla, ktore naozaj existuju.</summary>
    private static List<(int Line, int Item)> Parse(string content, int lineCount, int itemCount)
    {
        try
        {
            var pairs = JsonNode.Parse(content)?["pairs"]?.AsArray() ?? [];

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
