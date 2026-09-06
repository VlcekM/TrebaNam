using FastEndpoints;

namespace TrebaNam.API.Items;

public sealed class ItemGroup : Group
{
    public ItemGroup()
    {
        Configure("items", ep =>
        {
            ep.Description(x => x.WithTags("Items"));
        });
    }
}
