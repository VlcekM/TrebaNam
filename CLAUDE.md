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

`/app` is the home screen: a greeting, the two things people open the app for (add an item,
start a shopping trip) and read-only cards summarising the list, the three most recent trips and
the household. Each card is one big link to the screen it summarises: a tap anywhere on it opens
that screen, and with a mouse an overlay fades in over the card saying where the click leads,
because a card that is entirely clickable does not otherwise look it. It holds no controls of its
own beyond the add dialog, so nothing here has to be kept in step with the screen that owns data.
Every screen bounces to `/app/household` for anyone without a household.
`/app/list` is the shopping list, `/app/shop` shop mode over that same list, `/app/history` the
past trips and `/app/household` both the household screen and the onboarding form.
Everything under `/app` shares one loader, `src/routes/app/+layout.ts`, which fetches the user and
the household because the shell needs both; pages read them off layout data rather than fetching
again, and a write is followed by `invalidateAll()`. Data only one screen needs - the items, the
records - is loaded by that screen's own `+page.ts`, and home loads both because it shows both.

## Shopping list

Items hang off the household, never off a person; `ItemEntity.AddedByUserID` is only there so the
row can show who put it there. Adding and editing share `ItemDialog.svelte` - the fields are the
same, so an item passed in means edit and no item means create. It is a native `<dialog>`:
modality, the focus trap and Escape are the browser's job, the component only keeps it in sync
with the page's state and adds the one dismissal the browser does not give - a click on the
backdrop, which is a click landing on the dialog itself because the form fills it edge to edge.

Categories are fixed codes, listed in `ItemCategory.All` and mirrored in `src/lib/categories.ts`
in the same order - that order is also the order of the groups on screen, and the human-readable
names live only on the client. An unknown code reads as `other` rather than failing. Quantity is
free text ("2 kg", "1 loaf"), because units are not ours to invent. There is no price per item -
money is counted once per trip, not per row. An item can also carry a note ("the one in the blue
pack"), shown under its name on the list and in shop mode where it is actually needed; the note
belongs to the item on the list and never travels into a trip.

Names are stored the way the list should read: a lowercase first letter is capitalised on the way
in (`ItemName.Display`), the rest is left alone. The same list never holds the same thing twice -
`ItemName.Key` strips diacritics, case and doubled spaces, so "Banány" and "banany" are one thing,
and both adding and renaming answer 409 with the name already on the list for the dialog to show.

The add dialog suggests what the household has bought before (`GET /api/items/suggestions`, built
from the past trips and never from the current list, so nothing can be added twice). One field
does both jobs: empty, it offers the most frequent things; typed into, it narrows them. Picking a
suggestion fills the quantity and category from the last time too, and everything stays editable -
it fills the form, it does not add the item.

`IsChecked` belongs to the item, not to one screen, so both of you see the same ticks while
shopping. Shop mode (`/app/shop`) writes each tick straight to the API and flips the row before
the response lands, reverting it if the call fails; it is a sub-screen of the list, which is why
`isActive` keeps the list nav row lit there.

A row has two buttons: the circle takes the whole item, the one beside it opens a dialog for the
part that actually made it into the cart (`ItemEntity.BoughtQuantity`, free text like the quantity
itself, `PUT /api/items/{id}/bought`). The two states exclude each other, so ticking the circle
clears a partial amount and setting one unticks the circle. A partly bought item stays unchecked,
because the rest of it is still needed.

Finishing a trip copies the checked items into a `ShoppingRecordEntity` and deletes them from the
list - what stayed unchecked is what still needs buying. A partly bought item goes into the record
with the amount that was bought and stays on the list with the note cleared; the remainder is never
computed, because free-text quantities cannot be subtracted. The record's rows are copies, not
references, so the trip does not change when someone later renames an item.

A finished trip carries one optional total (`ShoppingRecordEntity.TotalCost`, euros - the app has
one currency and does not ask). It is offered when the trip is finished and can be filled in or
corrected later from `/app/history` (`PUT /api/shopping-records/{id}/total`), because the receipt
is not always to hand at the till. The total is the only thing on a finished trip that changes;
its rows never do.

A trip can be deleted (`DELETE /api/shopping-records/{id}`, rows go with it), always behind a
confirmation naming the date, because the button sits next to the total and the delete cannot be
undone. Deleting a trip does not put its items back on the list - the trip was a copy of what was
bought, not the items themselves.

In the interface a finished shopping is a **trip**, never a "shop" - a shop is a place, and this
is the outing. The code still says `ShoppingRecord`, so the database and the endpoints keep their
names; only the copy in `messages/en.json` carries the word.

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
Read every `#C4553B` in the handoff as that green. Text and icons on the green fill are white
(`text-primary-foreground` is white in both themes); accent-coloured text on light surfaces
uses `text-tn-primary-strong`, the same green darkened until it clears contrast. `Logo.svelte`
keeps a literal dark ink instead, because `favicon.svg` cannot read tokens and the two drawings
must match.

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
- Motion is small and everywhere the same. Dialogs open with a short fade and rise, backdrop
  included - that lives in `app.css` on the `dialog` element itself (`@starting-style` plus
  `allow-discrete`, which is why no dialog carries a `backdrop:` utility any more). Rows that
  come and go use Svelte's `fade`/`slide` with `animate:flip`, and every duration goes through
  `ms()` in `src/lib/motion.ts`, which returns 0 when the system asks for less motion; the same
  preference also flattens CSS transitions in `app.css`. Nothing waits on an animation to work.
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
