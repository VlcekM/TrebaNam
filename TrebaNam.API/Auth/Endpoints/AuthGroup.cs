using FastEndpoints;

namespace TrebaNam.API.Auth.Endpoints;

public sealed class AuthGroup : Group
{
    public AuthGroup()
    {
        Configure("auth", ep =>
        {
            ep.Description(x => x.WithTags("Auth"));
        });
    }
}
