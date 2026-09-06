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
start a shopping trip) and read-only cards summarising each list, the three most recent trips and
the household. Adding is a button beside the greeting on every width - never under it - because
it is the commonest reason to open the app at all; starting a trip sits below and only shows up
when exactly one list has something on it, because with two of them full there is no single trip
to start and picking one is what the list screen is for. Each card is one big link to the screen
it summarises: a tap anywhere on it opens that screen, and with a mouse an overlay fades in saying
where the click goes, because a card that is entirely clickable does not otherwise look it. It
holds no controls of its own beyond the add dialog, so nothing here has to be kept in step with
the screen that owns data.
Every screen bounces to `/app/household` for anyone without a household.
`/app/list/[[id]]` is one shopping list and `/app/shop/[[id]]` shop mode over that same list. The
id is optional and no id means the first list, so `/app/list` stays a single address worth saving
to a home screen while every list still has one of its own. `/app/history` is the past trips and
`/app/household` both the household screen and the onboarding form.
Everything under `/app` shares one loader, `src/routes/app/+layout.ts`, which fetches the user,
the household and the lists - the shell, the list switcher and the add dialog need them on every
screen; pages read them off layout data rather than fetching again, and a write is followed by
`invalidateAll()`. Data only one screen needs - the items, the records - is loaded by that
screen's own `+page.ts`, and home loads both because it shows both.

## Lists

A household keeps several lists (`ShoppingListEntity`, `/api/lists`): the weekly shop, the things
for the garden, what the cottage needs. They are not one heap because they are not bought in one
place or on one day. Each has a name, a colour and an optional note about what it is for; the
colour is a code from `ShoppingListColor` and the shade behind it lives on the client
(`src/lib/lists.ts`), because a hex picked in a form would sit wrong in one of the two themes.
The colour is a dot beside the name, never a whole tinted screen - it is there to be recognised,
not to be read.

Every household has at least one list: it is created with the household, named by the client so
it lands in the language the person creating it is reading, and the last one cannot be deleted
because a household with no list has nowhere to put the first thing. Deleting a list takes what
is still on it, since items hang off the list and nowhere else; finished trips stay, because they
are copies of what was bought.

The switcher is a row of chips under the list title, present even when there is only one - the
second one has to be discoverable from somewhere. Editing this list and starting a new one sit at
the end of that row rather than beside the title, where on a phone they would push the name onto
a second line. A new list opens straight away, because that is what it was made for.

An item can be moved between lists: the add dialog grows a list field once there is more than one
to choose from. That it will be bought at the cottage after all is something people find out
after writing it down.

## Shopping list

Items hang off the household and off one of its lists, never off a person;
`ItemEntity.AddedByUserID` is only there so the row can show who put it there. Adding and editing
share `ItemDialog.svelte` - the fields are the same, so an item passed in means edit and no item
means create. It is a native `<dialog>`: modality, the focus trap and Escape are the browser's
job, the component only keeps it in sync
with the page's state and adds the one dismissal the browser does not give - a click on the
backdrop, which is a click landing on the dialog itself because the form fills it edge to edge.

Categories are rows of the household (`HouseholdCategoryEntity`), not a fixed list in the code:
shops are not laid out alike and "the butcher" says more to two people than "other" does. Every
household starts with the six from `ItemCategory.Defaults` and can rename them, add its own and
drop the ones it does not use. Code is identity, name is a label - the same split as an item's
name and its key. A row with no name is one of the six and the client translates it
(`src/lib/categories.ts`); a row with a name is the household's own and reads as typed, in either
language, which is also what naming one is for. Renaming keeps the code, so nothing has to move.
Items still store the code (`ItemEntity.Category`), so a trip already finished never changes.

The order of the rows is the order the groups appear in on the list and in shop mode
(`PUT /api/households/me/categories`), because walking one shop twice is the thing the list is
meant to prevent. Adding, renaming and dropping go through the same group of endpoints. `other`
cannot be dropped - it is the floor everything else falls to, and dropping any other group moves
its items and usuals there rather than deleting them. An unknown code reads as `other` rather
than failing, and `byCategory` still shows what carries one, because a group changing underneath
is no reason for a thing to vanish off the list. Quantity is free text ("2 kg", "1 loaf"), because
units are not ours to invent. There is no price per item - money is counted once per trip, not
per row. An item can also carry a note ("the one in the blue pack"), shown under its name on the
list and in shop mode where it is actually needed; the note
belongs to the item on the list and never travels into a trip.

Names are stored the way the list should read: a lowercase first letter is capitalised on the way
in (`ItemName.Display`), the rest is left alone. One list never holds the same thing twice -
`ItemName.Key` strips diacritics, case and doubled spaces, so "Banány" and "banany" are one thing,
and both adding and renaming answer 409 with the name already on the list for the dialog to show.
Another list of the same household may hold it perfectly well: bread for the week and bread for
the cottage are two errands.

The add dialog suggests what the household has bought before (`GET /api/items/suggestions`, built
from the past trips and never from the list being added to, so nothing can be added twice). One
field does both jobs: empty, it offers the most frequent things; typed into, it narrows them.
Picking a suggestion fills the quantity and category from the last time too, and everything stays
editable - it fills the form, it does not add the item.

A suggestion can be starred (`PUT /api/items/favourites`, one row per household and name key).
Starred things are **usuals**: they come first under their own heading however seldom they were
bought, because what a household knows it needs every week beats what the counter says. A usual
outlives the trips it came from and is offered even with no trip behind it at all - the one thing
that still hides it is being on the list already. Starring is a decision about the thing, not
about the form, so it saves on the spot and the dialog stays open.

`IsChecked` belongs to the item, not to one screen, so both of you see the same ticks while
shopping. Shop mode writes each tick straight to the API and flips the row before
the response lands, reverting it if the call fails; it is a sub-screen of the list, which is why
`isActive` keeps the list nav row lit there.

A row has two buttons: the circle takes the whole item, the one beside it opens `AmountsDialog`.
That dialog holds both amounts at once - how much is needed (the item's own quantity) and how much
actually made it into the cart (`ItemEntity.BoughtQuantity`, free text like the quantity itself,
`PUT /api/items/{id}/bought`). Both change in the shop for the same reason, that the shelf held
something other than what was expected, so they are one dialog and one save; only the field that
really changed is sent. The two states exclude each other, so ticking the circle
clears a partial amount and setting one unticks the circle. A partly bought item stays unchecked,
because the rest of it is still needed.

A trip belongs to one list, because that is how shopping happens - one shop, one outing. Finishing
it copies the checked items of that list into a `ShoppingRecordEntity` and deletes them from the
list - what stayed unchecked is what still needs buying. The record keeps the list's name and
colour as a copy (`ListName`, `ListColor`), so history says where the trip came from even after
the list is renamed or gone. A partly bought item goes into the record
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

The name can be changed (`PUT /api/households/me`); the invite code cannot, it is a secret and not
a label. Anyone can leave (`POST /api/households/me/leave`) and anyone can put someone else out
(`DELETE /api/households/members/{id}`) - a household has no head, everyone in it sees the same
things, and anyone can walk out on their own, so there is nothing for a rank to protect. Only
yourself you cannot remove: that is leaving, which also knows what to do with the empty house.
When the last member leaves, the household goes with them and takes the lists, the trips, the
groups and the usuals by cascade, because nobody is left to read them. Both are behind a
confirmation naming who or what goes, and neither returns anything to anyone's list.

## Live updates

Two people shop from one list at the same time, so a change on one phone shows up on the other
without anyone reloading. A SignalR hub at `/api/hub/household` (`TrebaNam.API/Realtime`) puts
every connection into the group of its household and sends one message: something changed, and
which of items, trips or household it was. No data travels over the hub - the client answers it
with `invalidateAll()` and the screens fill from the same loaders as always, so live never shows
anything a plain reload would not.

Endpoints announce through `HouseholdNotifier` after `SaveChangesAsync`, never before, and a hub
that is down is logged and ignored: the write is already committed and the other side sees it at
the next load. The client side is `src/lib/realtime.ts`, connected from the `/app` layout for as
long as there is a household - it keys on the household id, not on the loaded object, or
every refresh would tear the connection down and build it again. Nothing in the app waits for the
hub, and an optimistic override (the tick in shop mode) lives only for the length of its own
request, so it can never hide what the other person just did.

## Offline

The app lives on a phone home screen and the shop is where the signal is worst, so it has to open
and work with none. The service worker (`src/service-worker.ts`) keeps the shell - the scripts,
the styles, Sora, the icons, the public page - and answers a navigation with the SPA fallback
(`200.html`) when the network does not, so `/app/*` opens cold in flight mode. Nothing under
`/api` goes through it: an answer about the list is not a file, and a stale one served as fresh
would be a lie the screen cannot see through.

The data is the app's own business (`src/lib/offline`). Every loader reads through `loadJson`,
which puts each answer in IndexedDB under its own address and hands back the stored one when the
request fails - what the household saw last is closer to the truth than an error page. An address
that was never loaded and cannot be reached now reads as nothing rather than as a failure; a
server that answers badly is still an error, because it did answer.

Writes go through `write` (`src/lib/offline/queue.ts`). With a connection and an empty queue they
go straight out and nothing about them changes. Without one they wait in the outbox and the
stored answers are rewritten to what the server will say, so the list reads at once the way it
will read once it is sent. The order is kept: once something waits, everything after it waits
too, or a tick would arrive before the thing it ticks. A refusal from the server is never
swallowed - a duplicate name still lands in the dialog it came from.

The item and the trip get their id in the phone (`newID`), not on the server, and both endpoints
answer an id they already hold with the row they already wrote. The same write can therefore be
sent twice - the answer to the first one was lost on the way - without the thing appearing twice.

The queue empties when the connection returns, when the app is opened again, and otherwise every
twenty seconds while something waits (`watchNetwork`). A failed connection leaves it alone; an
answer moves it on, even a refusing one, because the server has said its piece and repeating it
changes nothing. Those are counted and said out loud in `OfflineBar`, the one strip above the
screen: a change that was not saved must not disappear quietly. What the whole household shares
beyond the items - the lists, the groups, the members - needs a connection and says so, because
those are not decisions anybody makes in a shop.

Signing out forgets both stores. On a shared phone one household's list is none of the next
person's business.

## Styling

The look comes from `design_handoff_trebanam/` - direction **1a "Cozy cream"**: white cards on
warm cream, Sora, one strong accent. `Shopping List Mockups.dc.html` is the source of truth for
screens and the handoff README lists every token, with one deliberate deviation: the accent is
**green `#5EA758`**, not the handoff terracotta, which was too close to another project of ours.
Read every `#C4553B` in the handoff as that green. Text and icons on the green fill are white
(`text-primary-foreground` is white in both themes); accent-coloured text on light surfaces
uses `text-tn-primary-strong`, the same green darkened until it clears contrast. `Logo.svelte`
is the app icon: a shopping list - a white sheet with three rows, two ticked in green and one
still open, the boxes red, blue and green and the items dark ink bars (red and blue are
literal hex, because `favicon.svg` and
the PNGs in `static/icons/` cannot read tokens and the two drawings must match) on the cream page
background. The icon is deliberately not a dark tile: iOS's dark
icon mode already darkens non-game icons on its own, so the source stays light and colourful.
The PNGs are rendered from `favicon.svg` with the sheet stripped and the rows scaled up on a
white tile, because the OS draws its own rounded tile and a sheet inside it read as a frame in a
frame; the maskable one keeps the rows inside the safe zone. Regenerate them whenever the SVG
changes.

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

- Two languages, English and Slovak (`messages/en.json`, `messages/sk.json`). English is the base
  locale: a new key goes there first, and a key missing from Slovak falls back to it rather than
  breaking.
- The language is never in the URL. A cookie decides, the browser's own preference decides before
  there is a cookie, and English is the last word - `strategy: ['cookie', 'preferredLanguage',
  'baseLocale']` in `vite.config.ts`. One address is what people put on their home screen, and a
  `/sk` prefix would break both those shortcuts and the return from Google sign-in.
- The switcher is a row in `MoreMenu`, next to the theme. It names the other language in that
  language ("Slovensky", "English"), because whoever is looking for their own tongue is not
  reading the one currently on screen. Switching reloads the page, so nothing is left half
  translated.
- Every user-facing string goes through paraglide (`m.some_key()`), never a literal in the markup.
- What the household typed is not a string of ours and is never translated: item names and notes,
  list names, and the name of a group it renamed. The six default groups are the exception only
  until someone renames one. The first list is named by the client on the way in, so it starts in
  the language of whoever created the household.
- Counts do not fit one ternary: English has 1 and the rest, Slovak also has 2 to 4 ("2 veci" but
  "5 vecí"). Anything that puts a number in front of a noun therefore has `_one`, `_few` and
  `_other` keys and is assembled in `src/lib/counts.ts`, which asks `Intl.PluralRules` and never
  the screen. English carries a `_few` that it never uses - the key has to exist for the call to
  compile. Wording that dodges the problem ("kúpené {count}×", "s {count} vecami") needs none of
  this and is the better answer where it fits.
- Dates and money follow the language too, through `formatLocale()` in `src/lib/locale.ts`:
  "24,90 €" and "nedeľa 6. septembra" in Slovak, "€24.90" and "Sunday, 6 September" in English.
  It is a separate map from the locale codes, because the tag for formatting (`sk-SK`) is not the
  same thing as the tag for strings (`sk`).
- The public page is prerendered in English and swaps to Slovak on hydration; `<html lang>` is
  corrected there too. That flicker is the price of keeping the language out of the URL, and it
  only touches `/` - everything under `/app` renders in the browser to begin with.
- DTO shapes are mirrored by hand in `src/lib/types.ts`; there is no generated client.
