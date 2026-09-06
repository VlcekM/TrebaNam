# Architecture: SvelteKit static client hosted by ASP.NET Core + Postgres

Reference architecture distilled from a working project (StretniSa). Use it as the blueprint for a
new app with the same shape: one Docker container running a .NET API that also serves a
pre-built SvelteKit client, backed by Postgres. Replace `MyApp` with the project name.

## 1. Shape at a glance

```
repo/
├── MyApp.API/          .NET 10, FastEndpoints, EF Core + Npgsql       -> the only process in prod
├── MyApp.Client/       SvelteKit 5, adapter-static, Tailwind 4        -> built to static files
├── Dockerfile          multi-stage: node build -> dotnet publish -> chiseled runtime
├── .github/workflows/  build image, push to Docker Hub, poke deploy webhook
├── .env.example        documented env vars for the server-side docker compose
└── CLAUDE.md           project rules for Claude Code
```

Single origin, always. The browser only ever talks to the API's port:

| Environment | `/api/*`      | everything else                                  |
|-------------|---------------|--------------------------------------------------|
| Development | FastEndpoints | YARP reverse-proxies to Vite dev server on :3000 |
| Production  | FastEndpoints | static files from `wwwroot` + SPA fallback       |

Because of single origin there is no CORS, cookies are first-party, and the client uses relative
`fetch('/api/...')` everywhere.

## 2. Client: SvelteKit as a static export with an SPA island

**Adapter**: `@sveltejs/adapter-static` with a `200.html` fallback and `strict: false`.

```js
// svelte.config.js
import adapter from '@sveltejs/adapter-static';

export default {
  kit: {
    adapter: adapter({ fallback: '200.html', strict: false }),
    prerender: {
      entries: ['*'],
      handleHttpError: ({ path, message }) => {
        if (path.startsWith('/api/')) return; // API does not exist at prerender time
        throw new Error(message);
      }
    }
  }
};
```

**Two zones in `src/routes`:**

- Public pages (landing, legal, ...): root `+layout.ts` exports `prerender = true` and
  `trailingSlash = 'always'`. Every page becomes `<path>/index.html`, so static hosting serves
  both `/path` and `/path/`.
- The app (`/app/**`): its `+layout.ts` sets `ssr = false; prerender = false`. It runs as a pure
  SPA served from `200.html` and loads all data from the API in the browser.

**Auth gate lives in the app layout load:**

```ts
// src/routes/app/+layout.ts
import { redirect } from '@sveltejs/kit';
import type { LayoutLoad } from './$types';

export const ssr = false;
export const prerender = false;

export const load: LayoutLoad = async ({ fetch }) => {
  const res = await fetch('/api/auth/me');
  if (res.status === 401) throw redirect(302, '/api/auth/login');
  const user = (await res.json()) as User;
  userStore.set(user);
  return user;
};
```

**Calling the API**: plain `fetch`, relative URL, JSON body, `credentials: 'include'`. No client
SDK and no generated types; DTO shapes are mirrored by hand in `src/lib/types.ts`.

```ts
const res = await fetch('/api/profile/setup', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  credentials: 'include',
  body: JSON.stringify(payload)
});
```

**Dev server**: `vite.config.ts` pins `server: { port: 3000 }` so the API's proxy target is stable.

**Localisation (optional, proven)**: paraglide-js Vite plugin. For several languages use
`strategy: ['url', 'cookie', 'baseLocale']` and URL patterns where the base locale has no prefix
and the others get `/<locale>/...`; `src/hooks.ts` then exports
`reroute = (r) => deLocalizeUrl(r.url).pathname` and `src/hooks.server.ts` wraps
`paraglideMiddleware` to stamp `%paraglide.lang%` into `app.html`. Both only run during prerender,
since there is no SvelteKit server at runtime.

TrebaNam itself ships one language (English), so it runs `strategy: ['baseLocale']` with no URL
patterns, no reroute hook and a hardcoded `lang="en"`. Paraglide stays purely as the string
catalogue, so a second language means adding a JSON file and switching the URL strategy back on.

**Theme without flash**: prerender stamps `<html class="dark">` from a cookie, and a tiny inline
script in `app.html` re-checks the cookie before first paint for cached HTML.

## 3. API: ASP.NET Core with FastEndpoints

### Packages (`MyApp.API.csproj`, `net10.0`)

```
FastEndpoints
Microsoft.EntityFrameworkCore.Design (PrivateAssets=all)
Npgsql.EntityFrameworkCore.PostgreSQL
EFCore.NamingConventions                          -> snake_case tables/columns
Microsoft.AspNetCore.Authentication.Google        -> or whichever IdP
Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
Yarp.ReverseProxy                                 -> dev-only SPA proxy
```

### `Program.cs` skeleton, in the order that matters

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>();
// Only if compose uses prefixed vars like MYAPP_ConnectionStrings__DefaultConnection:
// builder.Configuration.AddEnvironmentVariables("MYAPP_");

// 1. Behind a tunnel/reverse proxy the app sees http. Trust forwarded headers,
//    otherwise OAuth builds http:// redirect URIs and the callback does not match.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                         | ForwardedHeaders.XForwardedProto
                         | ForwardedHeaders.XForwardedHost;
    o.KnownIPNetworks.Clear(); // the proxy has a random docker-network IP
    o.KnownProxies.Clear();
});

// 2. Dev only: everything that is not /api goes to Vite
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

// 3. EF Core: a factory everywhere, plus a scoped instance for libraries that want one
builder.Services.AddDbContextFactory<DataContext>(o => o
    .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IDbContextFactory<DataContext>>().CreateDbContext());

// 4. Data Protection keys in the DB, not on the container's disk.
//    Otherwise every redeploy rotates keys and every login cookie becomes a 401.
builder.Services.AddDataProtection()
    .SetApplicationName("MyApp") // must never change
    .PersistKeysToDbContext<DataContext>();

// 5. Cookie auth + external IdP. API calls must get 401/403, never a redirect.
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
        o.CallbackPath = "/api/signin-google"; // under /api so the SPA fallback never eats it
        o.Scope.Add("openid");
        o.Scope.Add("email");
        o.Scope.Add("profile");
        o.SaveTokens = false;
    });

builder.Services.AddAuthorization();
builder.Services.AddFastEndpoints();
// AddSingleton for domain services, AddHostedService for background workers

var app = builder.Build();

// 6. Migrate on startup: the container is the only thing that ever touches the schema
await using (var scope = app.Services.CreateAsyncScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DataContext>>();
    await using var db = await factory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
}

app.UseForwardedHeaders(); // first, before anything reads scheme or host

if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles(); // /foo/ -> /foo/index.html
    app.UseStaticFiles();  // wwwroot = SvelteKit build output
}

app.UseAuthentication();
app.UseAuthorization();

app.MapFastEndpoints(x =>
{
    x.Endpoints.RoutePrefix = "api";
    // optional global pre-processor: x.Endpoints.Configurator = ep => ep.PreProcessor<BanGuard>(Order.Before);
});

if (app.Environment.IsDevelopment())
{
    app.MapReverseProxy();
}
else
{
    var webRoot = Path.GetFullPath(app.Environment.WebRootPath);

    app.MapFallback(async ctx =>
    {
        // never serve HTML for an unknown API route
        if (ctx.Request.Path.StartsWithSegments("/api"))
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var relative = ctx.Request.Path.Value?.Trim('/') ?? string.Empty;
        var candidate = Path.GetFullPath(Path.Combine(webRoot, relative, "index.html"));

        var file = candidate.StartsWith(webRoot, StringComparison.Ordinal) && File.Exists(candidate)
            ? candidate                          // prerendered page
            : Path.Combine(webRoot, "200.html"); // SPA shell

        ctx.Response.ContentType = "text/html; charset=utf-8";
        await ctx.Response.SendFileAsync(file);
    });
}

app.Run();
```

### Endpoint conventions

- One folder per feature (`Auth/`, `Profile/`, `Matches/`, ...) holding `XEntity.cs`, `XDTO.cs`,
  an `XGroup : Group` that calls `Configure("profile", ...)`, and an `Endpoints/` subfolder with
  one class per endpoint. Final route is `/api/<group>/<route>`.
- Endpoints take `IDbContextFactory<DataContext>` via primary constructor and open one context per
  request: `await using var db = await factory.CreateDbContextAsync(ct);`.
- Everything is authenticated by default; opt out with `AllowAnonymous()`.
- The current user is the cookie's `ClaimTypes.NameIdentifier` (IdP subject) looked up in `Users`.
  First login creates the row.
- Validation inside the handler: `AddError(r => r.Field, "msg")` then `ThrowIfAnyErrors()`.
- Auth endpoints: `GET /api/auth/login` returns
  `Results.Challenge(authenticationSchemes: [GoogleDefaults.AuthenticationScheme], properties: new() { RedirectUri = "/app" })`,
  `GET /api/auth/logout` signs out, `GET /api/auth/me` returns the user DTO or 401.

```csharp
public class GetDefaultsEndpoint : EndpointWithoutRequest<ProfileDTO>
{
    public override void Configure()
    {
        Get("defaults");
        Group<ProfileGroup>();
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
        => await Send.OkAsync(ProfileDefaults.Create(), cancellation: ct);
}
```

### EF Core / Postgres conventions

- `DataContext : DbContext, IDataProtectionKeyContext` with `DbSet<DataProtectionKey> DataProtectionKeys`.
- Snake-case naming via `UseSnakeCaseNamingConvention()` everywhere a context is built.
- Relationships and indexes are declared in `OnModelCreating`; JSON columns via `OwnsOne(...).ToJson()`.
- Unique indexes double as locks for scheduled jobs (one run row per scheduled time).
- `DataContextDesignFactory : IDesignTimeDbContextFactory<DataContext>` reads the connection
  string from the first CLI arg or an env var, so `dotnet ef` works without booting the app:

```bash
dotnet ef migrations add Name --project MyApp.API -- "Host=localhost;Database=MyApp;Username=postgres;Password=..."
```

- Migrations are committed and applied automatically at startup. Nothing else runs them.

## 4. Configuration and secrets

`appsettings.json` holds shape and safe defaults: a localhost connection string, empty email
settings, placeholder OAuth ids. Real values come from, in order:

1. user secrets in development (`dotnet user-secrets set "Authentication:Google:ClientSecret" ...`),
2. environment variables in production from docker compose, using `__` for `:`
   (`ConnectionStrings__DefaultConnection`, `Authentication__Google__ClientId`, `Admin__Emails__0`).

Ship an `.env.example` next to the compose file listing every variable with a one-line comment.
Never commit OAuth client JSON files, `.env`, or any "secrets" folder.

## 5. Local development

```bash
npm --prefix MyApp.Client run dev
```

```bash
dotnet run --project MyApp.API
```

Vite listens on 3000, the API on `http://localhost:5000` with `ASPNETCORE_ENVIRONMENT=Development`
from `launchSettings.json`. Browse the API port only. It proxies HTML, assets and HMR websockets to
Vite and answers `/api/*` itself, so cookies and OAuth redirect URIs behave exactly as in
production. Register `http://localhost:5000/api/signin-google` and the production callback as
OAuth redirect URIs.

Local Postgres: any instance matching `DefaultConnection` in `appsettings.json`. A plain
`docker run postgres:18-alpine` is enough.

## 6. Dockerfile (multi-stage, one artifact)

```dockerfile
# syntax=docker/dockerfile:1

# 1) client
FROM node:24-alpine AS client
WORKDIR /src/client
COPY MyApp.Client/package.json MyApp.Client/package-lock.json ./
RUN npm ci
COPY MyApp.Client/ ./
RUN npm run build

# 2) api
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY MyApp.API/MyApp.API.csproj MyApp.API/
RUN dotnet restore MyApp.API/MyApp.API.csproj
COPY MyApp.API/ MyApp.API/
RUN dotnet publish MyApp.API/MyApp.API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# 3) GSSAPI libs Npgsql wants; the chiseled image has no apt
FROM ubuntu:noble AS krb5
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && mkdir /krb5 \
    && cp -L /usr/lib/*-linux-gnu/libgssapi_krb5.so.2 \
             /usr/lib/*-linux-gnu/libkrb5.so.3 \
             /usr/lib/*-linux-gnu/libk5crypto.so.3 \
             /usr/lib/*-linux-gnu/libcom_err.so.2 \
             /usr/lib/*-linux-gnu/libkrb5support.so.0 \
             /usr/lib/*-linux-gnu/libkeyutils.so.1 \
             /krb5/

# 4) runtime: distroless, non-root. "-extra" ships tzdata, needed for any IANA time zone.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS final
WORKDIR /app
COPY --from=krb5 /krb5/ /usr/local/lib/
ENV LD_LIBRARY_PATH=/usr/local/lib
COPY --from=api /app/publish ./
COPY --from=client /src/client/build ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "MyApp.API.dll"]
```

`.dockerignore` must exclude `**/bin`, `**/obj`, `**/node_modules`, `**/.svelte-kit`,
`MyApp.Client/build`, generated i18n output, `.git`, `.env`, `.claude`, and the compose plus
`db-data` folder.

Gotchas that cost time:

- The chiseled image has no shell. Debug with a non-chiseled tag and `--entrypoint`.
- Without the `-extra` variant `TimeZoneInfo.FindSystemTimeZoneById("Europe/Bratislava")` throws.
- Without the krb5 copies Npgsql fails to load a native library on connect.

## 7. Server: docker compose + pull-based redeploy

```yaml
name: myapp

services:
  db:
    image: postgres:18-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: MyApp
      POSTGRES_USER: myapp
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?set POSTGRES_PASSWORD in .env}
    volumes:
      # Postgres 18 wants the parent dir mounted, not .../data
      - ./db-data:/var/lib/postgresql
    healthcheck:
      test: ['CMD-SHELL', 'pg_isready -U myapp -d MyApp']
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s

  app:
    image: dockerhubuser/myapp:latest
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
    environment:
      ConnectionStrings__DefaultConnection: Host=db;Database=MyApp;Username=myapp;Password=${POSTGRES_PASSWORD}
      Authentication__Google__ClientId: ${GOOGLE_CLIENT_ID:?}
      Authentication__Google__ClientSecret: ${GOOGLE_CLIENT_SECRET:?}
      Admin__Emails__0: ${ADMIN_EMAIL:-}
    ports:
      # loopback only; a tunnel sits in front
      - '${BIND_ADDR:-127.0.0.1}:${APP_PORT:-8080}:8080'
    labels:
      com.centurylinklabs.watchtower.enable: 'true'

  watchtower:
    image: nickfedor/watchtower:1.21.0
    restart: unless-stopped
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      WATCHTOWER_HTTP_API_UPDATE: 'true'
      WATCHTOWER_HTTP_API_TOKEN: ${WATCHTOWER_TOKEN:?}
      WATCHTOWER_HTTP_API_PERIODIC_POLLS: 'false'
      WATCHTOWER_LABEL_ENABLE: 'true'
      WATCHTOWER_CLEANUP: 'true'
      REPO_USER: ${DOCKERHUB_USER:-}   # only for a private Docker Hub repo
      REPO_PASS: ${DOCKERHUB_TOKEN:-}
    ports:
      - '${BIND_ADDR:-127.0.0.1}:${WATCHTOWER_PORT:-8081}:8080'
```

Ingress: Cloudflare Tunnel (`cloudflared`) terminates TLS and forwards plain HTTP to the app on
loopback. That is why `UseForwardedHeaders` with cleared known proxies is mandatory.

Redeploy flow: CI pushes `:latest`, then POSTs a webhook on the server. The webhook receiver
calls `http://127.0.0.1:8081/v1/update` with the watchtower token, watchtower pulls the new image
and restarts only labelled containers. CI never gets SSH.

## 8. CI (`.github/workflows/docker.yml`)

```yaml
name: Build and redeploy

on:
  push:
    branches: [master]
  workflow_dispatch:

jobs:
  build:
    if: github.ref == 'refs/heads/master'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: docker build . -t dockerhubuser/myapp
      - uses: docker/login-action@v3
        with:
          username: ${{ vars.DOCKERHUB_USERNAME }}
          password: ${{ secrets.DOCKERHUB_TOKEN }}
      - run: docker push dockerhubuser/myapp

  deploy:
    needs: build
    if: github.ref == 'refs/heads/master'
    runs-on: ubuntu-latest
    steps:
      - env:
          DEPLOY_TOKEN: ${{ secrets.DEPLOY_TOKEN }}
        run: |
          curl --fail-with-body --silent --show-error \
            --retry 3 --retry-connrefused --max-time 120 \
            -X POST "https://hooks.example.com/hooks/myapp" \
            -H "X-Deploy-Token: $DEPLOY_TOKEN"
```

## 9. Checklist for a new project

1. `dotnet new web` + FastEndpoints, `npx sv create` with adapter-static, both in one `.slnx`.
2. Wire `Program.cs` in the order above: forwarded headers first, SPA fallback last.
3. `DataContext` implements `IDataProtectionKeyContext`; the first migration includes that table.
4. Root `+layout.ts`: `prerender = true; trailingSlash = 'always'`. App `+layout.ts`:
   `ssr = false; prerender = false` plus the `/api/auth/me` gate.
5. Vite on 3000, API on 5000 in dev and 8080 in the container. Register both OAuth callback URLs.
6. Dockerfile, `.dockerignore`, `.env.example`, compose and workflow from sections 6 to 8.
7. `CLAUDE.md` with the styling and i18n rules you want enforced.
