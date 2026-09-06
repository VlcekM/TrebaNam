# Handoff: TrebaNam — Household Shopping List

## Overview
Shared shopping-list app for a 2-adult household (Ana + Luka). Multiple lists, per-item quantity/price/notes, who-added-it attribution, recurring weekly staples, in-store "Store mode", checked-off history, and a monthly grocery budget (planned to grow into full household budgeting). Phone-first, plus a desktop web view.

## About the Design Files
The files in this bundle are **design references created in HTML** — prototypes showing intended look and behavior, NOT production code to copy directly. Recreate these designs in the target codebase's existing environment (React, Vue, SwiftUI, etc.) using its established patterns and libraries. If no environment exists yet, pick the most appropriate framework and implement the designs there.

`Shopping List Mockups.dc.html` renders three visual directions; **direction 1a ("Cozy cream") is the chosen one** — implement the "THE FLOW" section (screens 01–06) and the desktop view. Directions 1b/1c are reference only; ignore them.

## Fidelity
**High-fidelity.** Colors, typography, spacing, radii and copy are final (with the selected theme below). Recreate pixel-perfectly.

## Selected Theme (user's final tweak values — bake these in)
- Accent: **#C4553B** (terracotta)
- Font: **Sora** (Google Fonts; weights 400/600/700). Where mocks show weight 800, use Sora 700.
- Roundness: **16px** base radius (cards); small radius = 10px (inputs, chips); pills/checkboxes/avatars = 999px
- Note: the mock's static "1a" card shows Nunito — the live flow with the tweaks applied (Sora, 16px) is the source of truth.

## Design Tokens
- Accent: #C4553B; accent tint backgrounds: color-mix(in oklab, #C4553B 10–14%, #fff)
- App background: color-mix(in oklab, #C4553B 5%, #FBF8F3) ≈ warm cream #F8F1E9
- Card surface: #FFFFFF, shadow 0 1px 2px rgba(80,50,30,0.06)
- Ink: #2b2320; secondary: #9b9186; tertiary/disabled: #b3a99d; muted body: #6b655e
- Hairline dividers: 1px solid rgba(80,50,30,0.07)
- Member colors: Ana #D08C2E, Luka #4E7A9B (avatar circles, white 2px ring when stacked)
- Type scale: screen titles 30px/700; card titles 17px/700; row text 16px/700 (store mode 18px); meta 13–14px; section headers 11px caps, letter-spacing 0.08em, color #9b9186
- Currency: € with 2 decimals

## Screens
### 01 · Lists home
Wordmark "TREBANAM" (11px caps, accent) + stacked member avatars top-right. Title "Our lists" (30px). Vertical stack of list cards (16px radius, white): name + est total, meta "N to buy · N items", 6px accent progress bar on the active list. Budget card (accent 10% tint bg): "September budget", "€218 of €400", progress bar 54%, helper "On track — €182 left, 24 days to go". Bottom full-width accent button "+ New list" (50px tall).

### 02 · The list (main screen)
Header: back link "‹ OUR LISTS" (accent caps), est-total pill (white bg), title "Weekly groceries", meta "N to buy · N items". Items grouped by category (Produce, Bakery, Dairy & Eggs, Pantry, Household) in white cards; rows 52px min: 25px circular checkbox (2px accent border; checked = accent fill + white check), name, "qty · €price" (13px tertiary), 22px adder avatar. Checked rows: line-through, tertiary color. Bottom floating add bar: white pill "Add an item…" + 38px accent square (radius ~15px) with "+", shadow 0 4px 18px rgba(80,50,30,0.14).

### 03 · Add item
Modal screen with keyboard open. Header "Add item" + accent "Done". Search field (white card, 10px radius) with typed prefix "Ba" and accent caret. Autocomplete card: matches with prefix bold + rest tertiary, category label right, accent "+". "WEEKLY STAPLES" section: one-tap chips (accent text on 12% accent tint, 10px radius): + Milk 2%, + Eggs, + Coffee beans, + Oat milk.

### 04 · Item detail (bottom sheet)
Sheet over dimmed list (radius 16px top corners, grabber bar). Title "Greek yogurt" (24px) + category pill (accent on tint). Two half-width tiles (cream #FBF6EE bg, 10px radius): QUANTITY stepper (− / "1 tub" / + in white circles) and EST. PRICE "€2.40". NOTE tile with free text. Toggle row "Weekly staple / Re-added every Monday" (accent switch, on). Meta "Added by Ana · Tuesday". Buttons: "Remove" (ghost, 1.5px #e5dccf border) + "Save" (accent, 2:1 width ratio), 48px tall.

### 05 · Store mode
Accent header card (16px radius): "STORE MODE" caps + "Exit", "N of N in cart" (24px), running total "€X.XX · est €46.00", white-on-accent progress bar. Unchecked items grouped by category, bigger rows (62px min, 30px checkboxes, 18px names, price right). "IN CART · N" section: accent-8%-tint card, checked rows with filled check, line-through, price. Tapping rows toggles between sections.

### 06 · Household
Title "Our household", meta "2 members · since March 2026". Member cards: 44px avatar, name, "34 items added · 3 shops this month"; "You" pill (accent tint) on self. Settings card rows (52px, chevrons): Monthly budget €400 / Weekly staples 12 items / Checked-off history. Invite card: 1.5px dashed accent-45% border, "Invite someone", "Share code KUCA-42" (code mono, accent).

### Desktop (1240×780 in-browser)
Three columns on the same cream bg:
- Left sidebar 230px (right hairline): wordmark, nav rows (13.5px/700) with count badges — active row accent-12% tint; divider; Weekly staples / Budget / Household; member avatars + "Ana + Luka" bottom.
- Main column: title + meta, accent "+ Add item" button (38px) top-right; category-grouped white cards; rows 46px: checkbox 22px, name, qty (80px right-aligned), price (60px right), adder avatar.
- Right rail 280px (left hairline): budget card (as 01), "CHECKED OFF THIS WEEK" card (item struck-through + "Ana · Tue" style meta), "RUNNING LOW?" card ("Staples not bought in 10+ days: Oat milk, Butter").

## Interactions & Behavior
- Tapping a row's checkbox (or the whole row) toggles checked; checked state is shared across list, store mode and desktop in real time (household sync).
- Store mode moves checked items into "In cart" and updates count, running € total and progress bar.
- Add item: typeahead over catalog + past items; staple chips add in one tap; "Done" closes.
- Item detail opens as bottom sheet from row long-press/detail tap; Save persists, Remove deletes.
- Recurring staples: toggle per item; re-added automatically weekly (Monday).
- Budget: monthly cap set in Household; lists show est totals (sum of item est prices); month progress on home + desktop rail.
- Hover states (desktop): rows subtle cream tint; nav rows tint on hover.

## State Management
- items: {id, name, qty, category, price, addedBy, checked, isStaple, note}
- lists: {id, name, items[], estTotal}
- household: {members[{name, color, stats}], monthlyBudget, spentThisMonth, inviteCode}
- Derived: remainingCount, inCartCount, cartTotal, estTotal, budget %.
- Real-time sync across household members (checked state, adds, edits).

## Assets
No image assets. Icons are simple inline SVG (check mark, chevrons). Fonts from Google Fonts (Sora).

## Files
- `Shopping List Mockups.dc.html` — the design source (all screens; "THE FLOW" + desktop = spec)
- `ios-frame.jsx`, `browser-window.jsx`, `support.js` — mockup scaffolding only (device/browser chrome + preview runtime); NOT part of the app
