namespace TrebaNam.API.Receipts;

/// <summary>
/// Nahlad blocku pred nacitanim: co na nom je, uz prelozene do toho, ako by to vyzeralo
/// v nakupe. Neuklada sa nic - ulozi sa az to, co clovek v nahlade potvrdi.
/// </summary>
public class ReceiptPreviewDTO
{
    public required string ReceiptID { get; set; }

    public string? StoreName { get; set; }

    /// <summary>Kedy bol blocek vystaveny - podla toho sa nakup zaradi do mesiaca, nie podla dna skenovania.</summary>
    public DateTimeOffset? IssuedAt { get; set; }

    public decimal? TotalCost { get; set; }

    public List<ReceiptItemDTO> Items { get; set; } = [];

    /// <summary>Nakup, ktory z tohto blocku domacnost uz ma; null, ked este nie.</summary>
    public Guid? ImportedRecordID { get; set; }

    public DateTimeOffset? ImportedAt { get; set; }
}

/// <summary>
/// Riadok blocku. Rovnake veci su zlucene do jedneho - pokladna ich casto pipne kazdu zvlast.
/// Mnozstvo je cislo, ako ho dala pokladna; na text ho sklada klient, lebo zapis cisla je vec jazyka.
/// </summary>
public class ReceiptItemDTO
{
    /// <summary>Nazov z blocku (bez krikov velkymi pismenami) - podla neho sa riadok pamata.</summary>
    public required string ReceiptName { get; set; }

    /// <summary>Nazov, ako ho domacnost pise; zapamatany z minula, inak ten z blocku.</summary>
    public required string Name { get; set; }

    /// <summary>Name aj Category su z toho, co domacnost s tymto riadkom urobila naposledy.</summary>
    public bool Remembered { get; set; }

    public decimal Quantity { get; set; }

    /// <summary>Cena celeho riadku; len aby sa riadok dal na blocku najst, neuklada sa.</summary>
    public decimal Price { get; set; }

    /// <summary>Skupina, do ktorej domacnost tuto vec dala naposledy; inak "other".</summary>
    public required string Category { get; set; }

    /// <summary>Zaloha za obal - na blocku je, ale kupena vec to nie je.</summary>
    public bool IsDeposit { get; set; }
}
