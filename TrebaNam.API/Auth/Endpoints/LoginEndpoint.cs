using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace TrebaNam.API.Auth.Endpoints;

public class LoginRequest
{
    /// <summary>Kam sa vratit po prihlaseni. Napriklad pozvankovy odkaz /app/join/ABCD-2345.</summary>
    public string? ReturnUrl { get; set; }
}

public class LoginEndpoint : Endpoint<LoginRequest>
{
    private const string DefaultRedirect = "/app";

    public override void Configure()
    {
        Get("login");
        Group<AuthGroup>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        await Send.ResultAsync(Results.Challenge(
            authenticationSchemes: [GoogleDefaults.AuthenticationScheme],
            properties: new AuthenticationProperties { RedirectUri = SafeRedirect(req.ReturnUrl) }));
    }

    /// <summary>
    /// Beriem len cesty v ramci tejto stranky. Bez toho by z /api/auth/login?returnUrl=...
    /// bola otvorena presmerovacia diera pouzitelna v phishingu.
    /// </summary>
    internal static string SafeRedirect(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return DefaultRedirect;

        // "//host" a "/\host" prehliadac chape ako inu domenu, aj ked zacinaju lomitkom.
        if (returnUrl[0] != '/' || returnUrl.Length > 1 && (returnUrl[1] == '/' || returnUrl[1] == '\\'))
            return DefaultRedirect;

        return returnUrl;
    }
}
