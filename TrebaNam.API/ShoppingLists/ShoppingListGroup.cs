using FastEndpoints;

namespace TrebaNam.API.ShoppingLists;

public sealed class ShoppingListGroup : Group
{
    public ShoppingListGroup()
    {
        Configure("lists", ep =>
        {
            ep.Description(x => x.WithTags("Shopping lists"));
        });
    }
}
