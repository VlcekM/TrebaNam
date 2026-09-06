using FastEndpoints;

namespace TrebaNam.API.ShoppingRecords;

public sealed class ShoppingRecordGroup : Group
{
    public ShoppingRecordGroup()
    {
        Configure("shopping-records", ep =>
        {
            ep.Description(x => x.WithTags("Shopping records"));
        });
    }
}
