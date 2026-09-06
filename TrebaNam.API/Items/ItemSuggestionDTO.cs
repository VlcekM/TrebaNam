namespace TrebaNam.API.Items;

/// <summary>
/// Vec, ktoru uz domacnost niekedy kupila. Sluzi na naseptavanie pri pridavani, takze nesie
/// aj mnozstvo a kategoriu z posledneho nakupu - clovek tak zvycajne nedopisuje uz nic.
/// </summary>
public class ItemSuggestionDTO
{
    public required string Name { get; set; }

    public string? Quantity { get; set; }

    public required string Category { get; set; }

    /// <summary>Kolkokrat sa uz kupila; podla toho su navrhy zoradene.</summary>
    public int Count { get; set; }
}
