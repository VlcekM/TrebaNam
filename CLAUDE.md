# TrebaNam

SvelteKit 5 client (`TrebaNam.Client`) plus a .NET 10 FastEndpoints API (`TrebaNam.API`) on
Postgres/EF Core. In development the API reverse-proxies everything that is not `/api/*` to the
Vite dev server, so the client is always browsed through the API's port (`http://localhost:5000`).
Plain HTTP everywhere the app itself listens: in dev there is no TLS at all, in production TLS
terminates at the Cloudflare tunnel and the container only ever sees HTTP on 8080.
The full blueprint lives in `ARCHITECTURE.md`; read it before changing `Program.cs`, the
Dockerfile, or the SvelteKit adapter setup.

Production domain: `https://trebanam.martinvlcek.sk` (canonical link in the root layout,
OAuth redirect URI `https://trebanam.martinvlcek.sk/api/signin-google`; the dev one is
`http://localhost:5000/api/signin-google`).

## Running locally

```bash
npm --prefix TrebaNam.Client run dev
```

```bash
dotnet run --project TrebaNam.API
```

Secrets go through `dotnet user-secrets` (Google client id/secret), never into `appsettings.json`.

## API conventions

- One folder per feature holding `XEntity.cs`, `XDTO.cs`, `XGroup.cs` and an `Endpoints/` subfolder
  with one class per endpoint. Final route is `/api/<group>/<route>`.
- Endpoints take `IDbContextFactory<DataContext>` via primary constructor and open one context per
  request. Everything is authenticated by default; opt out with `AllowAnonymous()`.
- Schema changes go through EF migrations, committed to the repo and applied at startup:

```bash
dotnet ef migrations add Name --project TrebaNam.API -- "Host=localhost;Database=TrebaNam;Username=postgres;Password=..."
```

## Styling

The look comes from `design_handoff_trebanam/` - direction **1a "Cozy cream"**: white cards on
warm cream, Sora, one strong accent. `Shopping List Mockups.dc.html` is the source of truth for
screens and the handoff README lists every token, with one deliberate deviation: the accent is
**green `#5EA758`**, not the handoff terracotta, which was too close to another project of ours.
Read every `#C4553B` in the handoff as that green. Two consequences: the fill is light, so text on
it uses the dark `text-primary-foreground` rather than white, and accent-coloured text on light
surfaces uses `text-tn-primary-strong`, the same green darkened until it clears contrast (the
`wordmark` utility already does).

- **Never use `rounded-full` on buttons.** Use `rounded-lg` (10px, the handoff's small radius)
  instead. `rounded-full` stays fine for genuinely circular things - avatars, checkboxes, pills.
- Cards share one look: `rounded-2xl` (16px) `border border-tn-muted/40 bg-card p-6 shadow-xs`.
  Tinted cards (budget and similar) use `bg-tn-tint` and drop the border.
- Colours come from the `--tn-*` tokens in `src/app.css` (`bg-tn-primary`, `border-tn-muted/40`,
  `text-tn-meta` for meta lines, `text-tn-faint` for checked-off text), never hard-coded hex,
  so dark mode keeps working.
- Type scale from the handoff: screen titles `text-[30px]` bold, card titles `text-[17px]` bold,
  rows 16px bold, meta 13-14px, section headers 11px caps with `tracking-[0.08em]`.
- Light mode is the handoff cream `#FBF8F3` with warm neutral greys; no green in the page
  background, the green belongs to the accent alone. Dark mode is deliberately neutral - near
  black surfaces, grey text, green only as the accent. Check both.
- Sora is self-hosted: the variable woff2 files live in `static/fonts/` with hand-written
  `@font-face` rules in `src/app.css` (`font-display: optional`) and the latin file is preloaded
  from `app.html`. That trio is what keeps the font from popping in - do not move the files under
  a hashed asset path or the preload URL goes stale. The mocks show weight 800 - use 700.

## Localisation

- The app is English only: one locale, no language in the URL, no language switcher.
- Every user-facing string still goes through paraglide (`m.some_key()`), never a literal in the
  markup, so adding a language later means adding `messages/<locale>.json` and turning the URL
  strategy back on in `vite.config.ts` - not rewriting the markup.
- New keys go into `messages/en.json`.
- DTO shapes are mirrored by hand in `src/lib/types.ts`; there is no generated client.
