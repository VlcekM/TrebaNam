# TrebaNam

A shared shopping list for a household. Sign in with Google, add what you need, tick it off when
it lands in the basket.

Built as one Docker container: a .NET 10 API that also serves a prebuilt SvelteKit client, backed
by Postgres. Runs at [trebanam.martinvlcek.sk](https://trebanam.martinvlcek.sk).

## Layout

| Folder            | What it is                                                    |
|-------------------|---------------------------------------------------------------|
| `TrebaNam.API`    | .NET 10, FastEndpoints, EF Core and Npgsql. The only process in production. |
| `TrebaNam.Client` | SvelteKit 5, adapter-static, Tailwind 4. Built to static files. |

There is a single origin. The browser only ever talks to the API's port. In development the API
reverse-proxies everything that is not `/api/*` to the Vite dev server; in production it serves
the built client from `wwwroot`. `ARCHITECTURE.md` has the full blueprint.

## Installable

The client is a PWA. Launching it from the home screen icon opens `/app` directly, never the
landing page: the manifest sets `start_url` to `/app`, and the landing page also forwards anyone
who arrives there in standalone mode, which covers icons added before that was true.

It ships `static/manifest.webmanifest`, maskable icons and a service worker
(`TrebaNam.Client/src/service-worker.ts`) that precaches the build and falls back to the SPA shell
when the network is gone. `/api/*` is never cached, so nothing stale is ever shown as list data.
The service worker only exists in a production build, not under the Vite dev server.

## Running it locally

You need the .NET 10 SDK, Node 24, and a Postgres instance matching the connection string in
`TrebaNam.API/appsettings.json`. A plain `docker run postgres:18-alpine` is enough.

Start the client:

```bash
npm --prefix TrebaNam.Client run dev
```

Start the API in a second terminal:

```bash
dotnet run --project TrebaNam.API
```

Then open <http://localhost:5000> and browse only that port. The API proxies pages, assets and
hot reload to Vite, and answers `/api/*` itself, so cookies and OAuth redirects behave exactly as
they do in production.

Google sign-in needs your own OAuth client. Register `http://localhost:5000/api/signin-google` as
a redirect URI and store the credentials outside the repo:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "..." --project TrebaNam.API
```

## Deploying

Pushing to `master` builds the image, pushes it to Docker Hub and pokes a webhook on the server,
where watchtower pulls the new image and restarts the container. Migrations are applied at
startup, so nothing else ever touches the schema. Copy `.env.example` to `.env` next to
`docker-compose.yml` on the server and fill it in.

## License

MIT. See [LICENSE](LICENSE).
