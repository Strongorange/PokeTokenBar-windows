# Changelog

PokeTokenBar for Windows. One section per released version, newest first.
Release notes for a version are generated from its section here (see
`scripts/publish-windows.ps1`, output in `artifacts\release\`). The Korean
edition lives in `CHANGELOG.ko.md` — keep both in sync; the publish script
refuses to release when either one is missing the current version.

## v0.26.0 — Game tab modularization

- Internal: the companion header, progress caption, combat text, dex
  tiles, rarity filter capsules, catch-log cards and evolution line moved
  out of the dashboard code-behind into individually tested units (pure
  decisions in `GameTabPresentation`, WPF builders in renderer classes).
  The code-behind shrank to a quarter of its size. No visual or behavior
  changes — verified by pixel-identical before/after captures of the
  affected views.

## v0.25.0 — Update checks fixed

- Fixed the update checker: its requests to the GitHub API carried no
  User-Agent header, so GitHub rejected them (403) and the checker's
  silent-failure policy made every client since the first release report
  "up to date". New-version banners and tray balloons now actually fire
  when a release newer than the installed version is published.

## v0.24.0 — Shop confirm parity + dashboard modularization

- Shop item cards now use the macOS-style inline two-step confirm: a price
  row with a Buy button first, then the confirm row — Cancel actually
  returns to the price row (previously the confirm row was always shown and
  Cancel did nothing visible).
- Fixed: the basic "Pokémon Egg" shop card rendered with the rare-candy
  icon, description and owned count. It is now a proper egg card.
- Buying an egg while raising a shiny companion now shows the plain confirm
  first and only then the shiny-discard warning (two deliberate clicks,
  matching the macOS original).
- Bag card background uses the same subtle tint as the rest of the cards.
- Internal: the bag/shop confirm flow, usage-home rendering and trend
  metrics were extracted into individually tested units; the dashboard
  code-behind shrank by half. No other behavior changes.

## v0.23.0 — Celebration animations

- Hatch, evolution and Ditto-reveal moments now celebrate on the companion
  sprite: a white flash with a spring pop, a gold sparkle burst for shiny
  hatches, and a mask-pair burst when a Ditto disguise is revealed (with an
  extra sparkle if it turns out shiny).
- Rare candy pops an orange "+N XP" capsule above the sprite showing the
  applied XP; mint shimmers with three gold sparkles.
- Eggs past 90% incubation wiggle until they hatch.
- Back-to-back moments (a hatch followed by a carry-over evolution) queue
  up and play one after another.

## v0.22.0 — Catch log, status line, item icons, launch at login

- Catch log: the Pokédex tab gains a Pokédex / catch-log toggle. The log
  lists one card per individual Pokémon — rarity capsule, "Raising" /
  "Released" badge, ✨, nature, the reached evolution chain as sprites with
  names, and a relative caught-at line ("3 days ago") — with the current
  companion pinned first and per-rarity filter capsules.
- Companion status line: a one-line mood under the progress bar (hatching
  soon / keeping quiet / working / focus mode / sleeping, plus a brief
  "Evolved into …!" line right after evolution) driven by your coding pace.
- Shop and bag items now show their real item sprites (rare candy, shiny
  charm) with emoji fallback when offline.
- New "Launch at login" setting (per-user, no admin).

## v0.21.0 — Detail window polish

- Species detail window rebuilt to match the macOS original.
- New "Species data" section: type capsules (uppercase, accent-tinted),
  height / weight / base-total pairs, possible abilities.
- Individual picker for species with multiple catches (`#N · Lv. X`) —
  shows the selected catch only; the header sprite follows the selection.
- "Actual stats" section with level/gender/nature, ability (hidden-ability
  label), scaled stat bars and an IV column; base stats shown only when the
  species has no caught profile.
- Header gains a "✨ Shiny" line and an accent "Raising" line.

## v0.20.0 — Representative Pokémon + usage home

- Representative Pokémon: star a species in the Pokédex (or from its detail
  window / settings); the floating pet follows the representative, dex tile
  shows ★, and the selection survives restarts.
- Usage tab redesign: big today's-tokens number with cost, "this week" /
  "this month" period labels, month trend, and a provider card with chip
  tabs plus per-provider today detail (input / output / cache write / cache
  read, per-model rows).

## v0.19.0 — Companion, dex & bag bundle

- Companion card parity: rarity capsule, growth-boost capsule, egg
  "about to hatch" wording, egg guarantee capsule, macOS-style progress
  captions.
- Dex rarity filter capsules (count per rarity) and an empty state.
- Bag became per-item cards: rare candy +/− stepper with live plan preview
  (carryover / discarded XP), mint with confirm, shiny charm active row,
  Snorlax empty state.

## v0.18.0 — Fluent theme + dex/shop tabs

- Windows 11 Fluent light theme across the dashboard, settings and detail
  windows; dashboard now resizable.
- Dedicated Pokédex tab (7-column sprite tile grid) and Shop tab: wallet
  card with spendable total, per-entry cards with inline buy confirm, egg
  send-off and shiny-discard warnings, rarity tier capsules.

## v0.17.0 — Evolution line + shiny markers

- Visual evolution line: sprite cells, current-stage accent dot, mystery
  cell at branch points, overflow scrolling, whole-line shiny sprites.
- macOS-style ✨ shiny markers on names, dex tiles, detail individuals and
  Unown form thumbnails.

## v0.16.0 — Update notifications

- New-version checks against GitHub releases on startup and dashboard open;
  tray balloon + dashboard banner with skip-this-version; "Updates" section
  in settings with manual check and toggle. Opens the release page in the
  browser (no auto-download).

## v0.15.0 — Unown forms

- Per-form Unown sprites everywhere: dex tile shows a collected-form count
  ("Unown 2/28"), detail window gains a 28-form picker grid that filters
  individuals and switches the hero sprite; shiny stars per form.

## v0.14.0 — Settings window

- New settings window: language picker (7 languages, live
  re-localization), per-provider additional scan folders, difficulty
  sliders, floating pet controls.

## v0.13.0 — Month trend chart

- Usage tab month-to-date daily chart with hover readout, peak marker,
  weekend ticks and a sparse date axis; combined-today model breakdown.

## v0.12.0 — Dex tile grid

- The Pokédex text list became a sprite tile grid with shiny stars,
  raising arrows and full tooltips; double-click opens details.

## v0.11.0 — Localization + sprites

- Full localization (English, 한국어, 日本語, Español, Français, Português,
  Deutsch) across dashboard, tray, floating pet and detail window.
- Pokémon sprites (online-cached, animated where sensible).

## v0.10.0 — Initial Windows port

- Token dashboard for Claude Code / Codex / OpenCode usage logs with the
  companion game (egg → Pokémon → evolution → graduation), Pokédex, shop,
  floating pet and tray.
