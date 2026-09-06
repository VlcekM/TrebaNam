using FastEndpoints;

namespace TrebaNam.API.Households;

public sealed class HouseholdGroup : Group
{
    public HouseholdGroup()
    {
        Configure("households", ep =>
        {
            ep.Description(x => x.WithTags("Households"));
        });
    }
}
