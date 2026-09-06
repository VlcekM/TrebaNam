using FastEndpoints;
using Microsoft.AspNetCore.Authentication;

namespace TrebaNam.API.Auth.Endpoints;

public class LogoutEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Post("logout");
        Group<AuthGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await HttpContext.SignOutAsync("AppCookie");
        await Send.OkAsync(cancellation: ct);
    }
}
