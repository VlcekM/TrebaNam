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

## App routes

`/app` is "Need to buy", the home screen, and it bounces to `/app/household` for anyone without
a household. `/app/household` is both the household screen and the onboarding form. Everything
under `/app` shares one loader, `src/routes/app/+layout.ts`, which fetches the user and the
household because the shell needs both; pages read them off layout data rather than fetching
again, and a write is followed by `invalidateAll()`.

## Households

One person belongs to at most one household (`UserEntity.HouseholdID`, nullable). Everything
shared - lists, budget - hangs off the household, never off a single person. Joining happens
through an invite link `/app/join/<code>`; the code is a random secret, so treat it like one and
never derive it from the household name. Someone already in another household is refused rather
than moved, because a silent move would empty their lists without explanation.

## Styling

The look comes from `design_handoff_trebanam/` - direction **1a "Cozy cream"**: white cards on
warm cream, Sora, one strong accent. `Shopping List Mockups.dc.html` is the source of truth for
screens and the handoff README lists every token, with one deliberate deviation: the accent is
**green `#5EA758`**, not the handoff terracotta, which was too close to another project of ours.
Read every `#C4553B` in the handoff as that green. Two consequences: the fill is light, so text on
it uses the dark `text-primary-foreground` rather than white, and accent-coloured text on light
surfaces uses `text-tn-primary-strong`, the same green darkened until it clears contrast.

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
- The name is a pun ("treba nam"), so `Wordmark.svelte` splits it: **Treba** in the accent,
  **Nam** in `text-foreground`. Render the name through that component, never as one string.
- Below `lg` the app is the handoff's phone screen: sticky `AppHeader` (brand left, stacked
  member avatars right, nav pills underneath), one column. From `lg` up it switches to the
  handoff's desktop view - `AppSidebar` (230px, right hairline, wordmark, nav rows with count
  badges, member avatars at the bottom) next to the content, and the header hides. The handoff's
  third column, the right rail, is not built yet because it is all budget and history data the
  app does not have.
- Both shells read their destinations from `src/lib/nav.ts`, so a new screen is one entry there
  plus a route, never two edits that can drift apart. Navigation only renders once there is a
  household - before that the only thing to do is create or join one.
- Everything else that used to sit in the corner (theme, sign out) lives in `MoreMenu.svelte`
  behind a three-dot button. The caller passes `panelClass` to say which way it opens.
- Anything the sidebar shows loads in `src/routes/app/+layout.ts`, not in a page load, so the
  panel and the screen never disagree. After a write, call `invalidateAll()` rather than keeping
  a local copy.
- Sora is self-hosted: the variable woff2 files live in `static/fonts/` with hand-written
  `@font-face` rules in `src/app.css` and both subsets are preloaded from `app.html`. The rules
  say `font-display: block`, so text waits for Sora instead of rendering in a fallback first -
  no other font is ever shown. That trio is what keeps the font from popping in; do not move the
  files under a hashed asset path or the preload URLs go stale. The mocks show weight 800 - use 700.

## Localisation

- The app is English only: one locale, no language in the URL, no language switcher.
- Every user-facing string still goes through paraglide (`m.some_key()`), never a literal in the
  markup, so adding a language later means adding `messages/<locale>.json` and turning the URL
  strategy back on in `vite.config.ts` - not rewriting the markup.
- New keys go into `messages/en.json`.
- DTO shapes are mirrored by hand in `src/lib/types.ts`; there is no generated client.
