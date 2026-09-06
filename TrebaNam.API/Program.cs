using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using TrebaNam.API;
using TrebaNam.API.Auth;
using TrebaNam.API.Realtime;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

// 1. Za Cloudflare tunelom prichadza request cez HTTP, https vie len proxy.
//    Bez tohto by Google OAuth poskladal redirect_uri s http:// a callback by nesedel.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                         | ForwardedHeaders.XForwardedProto
                         | ForwardedHeaders.XForwardedHost;

    // cloudflared ma v docker sieti nahodnu adresu, nedokazeme ju vymenovat
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// 2. Len v developmente: vsetko mimo /api ide na Vite dev server
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddReverseProxy().LoadFromMemory(
        routes:
        [
            new RouteConfig
            {
                RouteId = "spa-catchall",
                ClusterId = "web",
                Match = new RouteMatch { Path = "/{**catchall}" }
            }
        ],
        clusters:
        [
            new ClusterConfig
            {
                ClusterId = "web",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["d1"] = new() { Address = "http://localhost:3000/" }
                }
            }
        ]);
}

// 3. EF Core: factory vsade, plus scoped instancia pre kniznice, ktore ju chcu
builder.Services.AddDbContextFactory<DataContext>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<DataContext>>().CreateDbContext());

// 4. Klucenka Data Protection v databaze, nie na disku kontajnera.
//    Inak by kazdy redeploy zahodil kluce a vsetky prihlasovacie cookies by skoncili na 401.
//    SetApplicationName sa nikdy nesmie zmenit.
builder.Services.AddDataProtection()
    .SetApplicationName("TrebaNam")
    .PersistKeysToDbContext<DataContext>();

// 5. Cookie auth + Google. API volania dostanu 401/403, nikdy redirect.
builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultScheme = "AppCookie";
        o.DefaultAuthenticateScheme = "AppCookie";
        o.DefaultSignInScheme = "AppCookie";
        o.DefaultChallengeScheme = "AppCookie";
    })
    .AddCookie("AppCookie", o =>
    {
        o.LoginPath = "/api/auth/login";
        o.LogoutPath = "/api/auth/logout";
        o.SlidingExpiration = true;
        o.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                else
                    ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/api"))
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                else
                    ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle(o =>
    {
        o.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        o.CallbackPath = "/api/signin-google"; // pod /api, aby ho SPA fallback nikdy nezjedol
        o.Scope.Add("openid");
        o.Scope.Add("email");
        o.Scope.Add("profile");
        o.SaveTokens = false;
        o.ClaimActions.MapJsonKey("picture", "picture", "url");
    });

builder.Services.AddAuthorization();
builder.Services.AddFastEndpoints();

// 5b. Realny cas: hub len oznamuje zmeny, data si klient aj tak tiahne cez /api.
builder.Services.AddSignalR();
builder.Services.AddSingleton<HouseholdNotifier>();

builder.Services.AddSingleton<AdminEmails>();

var app = builder.Build();

// 6. Migracie pri starte: kontajner je jedina vec, ktora sa kedy dotkne schemy
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

// musi byt uplne prve, nez si ktorykolvek middleware precita schemu alebo host
app.UseForwardedHeaders();

// mimo developmentu servuje API aj staticky build klienta z wwwroot
if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

// SignalR pouziva websocket, ked ho spojenie zvladne; inak si sam siahne po dlhom dopyte.
app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

app.MapFastEndpoints(x =>
{
    x.Endpoints.RoutePrefix = "api";
});

app.MapHub<HouseholdHub>(HouseholdChannel.Path);

if (app.Environment.IsDevelopment())
{
    app.MapReverseProxy();
}
else
{
    // WebRootPath je null, ked wwwroot este neexistuje (lokalny beh v Production bez buildu klienta).
    var webRoot = Path.GetFullPath(
        app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"));

    app.MapFallback(async ctx =>
    {
        // /api/* patri backendu - nikdy mu nepodstrkavame HTML
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var relative = ctx.Request.Path.Value?.Trim('/') ?? string.Empty;
        var candidate = Path.GetFullPath(Path.Combine(webRoot, relative, "index.html"));

        var file = candidate.StartsWith(webRoot, StringComparison.Ordinal) && File.Exists(candidate)
            ? candidate                          // predgenerovana stranka
            : Path.Combine(webRoot, "200.html"); // SPA shell

        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.SendFileAsync(file);
    });
}

app.Run();
