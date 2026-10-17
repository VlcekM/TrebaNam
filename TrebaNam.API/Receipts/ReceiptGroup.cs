using FastEndpoints;

namespace TrebaNam.API.Receipts;

public sealed class ReceiptGroup : Group
{
    public ReceiptGroup()
    {
        Configure("receipts", ep =>
        {
            ep.Description(x => x.WithTags("Receipts"));
        });
    }
}
