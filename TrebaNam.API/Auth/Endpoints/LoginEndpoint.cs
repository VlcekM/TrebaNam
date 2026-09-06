using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace TrebaNam.API.Auth.Endpoints;

public class LoginEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("login");
        Group<AuthGroup>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.ResultAsync(Results.Challenge(
            authenticationSchemes: [GoogleDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties { RedirectUri = "/app" }));
    }
}
