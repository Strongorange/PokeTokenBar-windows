# Handoff — Milestone 26 done: series complete

## Goal
The UI-parity + engineering-quality series is COMPLETE (2026-09-30). M26
shipped the final slice: the game-tab render builders were extracted from
`DashboardWindow.xaml.cs` with no visual or behavior changes. The user
returned the project to MAINTENANCE MODE the same day (no scheduled
slices; fix defects found in daily use). This handoff is a close-out note
for any future session; there is no next slice queued. If the user
reopens work, re-confirm scope before scaffolding anything (see
`docs/ui-parity-audit.md` — everything is done or explicitly rejected).

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11
  25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (direct push, no PR flow)
- Commit convention: English conventional commits (`feat:`, `fix:`,
  `docs:`, `chore:`)

## Current State
- M26 (user-confirmed final slice, shipped as 0.25.1 — a PATCH per
  semver: internal-only refactor, no feature/behavior change):
  - Application `GameTabPresentation` (pure, unit-tested — 24 new tests):
    header name + shiny suffix/tooltip, rarity capsule label, stage·nature
    detail line, egg imminence, progress captions + value (clamped
    remainders), growth-boost/egg-guarantee badge decisions (`GameBadge`
    + `GameBadgeKind`), combat line assembly, dex/catch headers, tile
    tooltip/name, caught-ago bucket text.
  - Ui-internal `CompanionHeader` (header + progress cards, evolution
    line, companion sprite slot) and `DexTab` (dex/catch modes, rarity
    tally + filter state, tiles, catch cards, chain nodes, empty state;
    re-renders via a request callback like `ShopCards`).
  - `DashboardWindow.xaml.cs` 799→233 lines (orchestration, dialogs,
    event handlers remain). No XAML changes.
  - Evidence: 539 tests green (515 + 24). Before/after smoke harness
    `%TEMP%\opencode\ptb-m26-smoke\Program.cs` (label arg `before`/
    `after`): 24 logical-tree assertions pass on both builds with
    identical seen-values; PNGs pixel-identical except the imminent-egg
    pulse animation (reproduces within a single build — capture timing,
    not a regression).

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2), layering:
  Core pure, Providers parse, Platform.Windows owns paths/settings/
  diagnostics, Application owns engine + orchestration, UI consumes
  Application only. No MVVM framework.
- Theme is Fluent light ONLY (`src/Ui/Theme.xaml` single source;
  `Paint.` / `{DynamicResource}` / `SetResourceReference`; sanctioned
  alpha-tint hex without '#').
- Official provider limits stay OUT (no-credentials rule). Pokemon base
  data stays the bundled schema-2 snapshot; sprites the online-cached
  `SpriteStore` concern.
- Packaging: version = Ui csproj (0.25.1 current). Publish via
  `scripts\publish-windows.ps1` (kills the installed app; relaunch the
  exe yourself). It produces `artifacts\release\PokeTokenBar-<v>-win-x64.zip`
  (exe only) + bilingual release notes generated from the committed root
  `CHANGELOG.md` + `CHANGELOG.ko.md`, and REFUSES to publish when either
  newest section ≠ csproj version — add the version section to BOTH
  files with every shipping slice. `publish-windows.ps1` must stay UTF-8
  WITH BOM (Korean literals; PS 5.1 parses BOM-less as ANSI). Releases
  by hand, `v<Version>` tags; `gh` CLI NOT authenticated and the GitHub
  API is 403 rate-limited without a token — the user uploads the release
  + asset by hand in the browser.
- State under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR` overrides).
  Language is saved-game state via `SetLanguage`. README en+ko only.
- Never read/copy credentials. Comments: none unless asked. English
  commits. Commit + push when the milestone is verified. Remote `origin`
  = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `PlanRareCandyUse(int)`, `MaxRareCandyUseCount()`, `SetLanguage`,
  `SetRepresentative(int?, UnownForm?) -> bool`, `View()` →
  `CompanionGameView`, `Detail(speciesID)`, `Changed`, `DrainNotices()`,
  `DrainCelebrations()`, `ExportSave`/`ImportSave`. NEW VIEW FIELDS GO
  BEFORE `UnownForms`.
- Application: `ShopFlow` + `UsagePresentation` (M25),
  `GameTabPresentation` + `GameBadge`/`GameBadgeKind` (M26),
  `CompanionPresentation` (M23) are public pure helpers — UI renderers
  consume them and own no decisions.
- Ui-internal renderers: `UsageHomeRenderer`, `ShopCards`, `Paint`,
  `CompanionHeader`, `DexTab`. Pattern: FrameworkElement theme + element
  refs in ctor, `requestRender` callback for interactive state, pure
  decisions delegated to Application classes.
- `Detail(speciesID)` returns null unless the snapshot has a `details`
  entry for that species (relevant for smoke fixtures).
- Fixed UI strings live in `src/Core/DashboardText.cs` macOS-verbatim;
  add a Core.Tests string test in the same slice. M26 added none.
- Sprites through `SpriteSlot`; item icons through `ItemIconSlot`; static
  thumbnails >44px use NearestNeighbor.
- Snapshot schema 2 (loader accepts 1). Regenerate only via
  `scripts/generate-pokemon-snapshot.ps1` under pwsh.

## Relevant Files
- `src/Application/GameTabPresentation.cs`, `src/Ui/CompanionHeader.cs`,
  `src/Ui/DexTab.cs` — all new in M26
- `src/Ui/DashboardWindow.xaml.cs` — 233 lines: ctor wiring,
  localization, update banner, UpdateGame orchestration, events list,
  footer, dialogs, dex double-click
- `docs/ui-parity-audit.md` — closed (all items done/rejected)
- `docs/windows-port-plan.md` — canonical plan (M26 = fifteenth slice)
- `docs/handoff/2026-09-30-milestone-26-ui-polish-next.md` — M26 kickoff
  handoff (superseded by this file)

## Hard-Won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 539 tests) —
  `MSB1009` if you type `.sln`. Kill DEBUG `PokeTokenBar.exe` before
  testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
  NOTE: the solution's test projects do NOT reference Ui — compile Ui
  (or run a file-based smoke harness `#:project src/Ui/...csproj`) to
  catch Ui compile errors; M26's field-name collision (DexTab
  `_catchRarityFilter` panel vs filter state) surfaced only there.
- Smoke-harness tree probing: use `LogicalTreeHelper` walks, NOT
  `VisualTreeHelper`, when asserting on code-built cards; beware
  `List<string>.Contains` (EXACT match) vs `anyString.Contains`
  (substring); re-find cards after every click. Multi-element captions
  (e.g. `#1` + `★`) are SEPARATE TextBlocks — don't assert joined text.
- Before/after visual equivalence: run the SAME harness (label arg
  `before`/`after`) against pre/post-refactor builds and pixel-compare
  (`Compare.cs` in the same dir; WPF `FormatConvertedBitmap`, threshold
  delta 8). Animation-clocked elements (imminent-egg pulse) vary between
  ANY two runs — confirm by comparing two runs of the SAME build.
- WPF emoji are MONOCHROME — never use emoji glyphs as primary visuals.
- WPF animation smoking: `DispatcherSynchronizationContext` first,
  offscreen `window.Show()` (Left=-2000), pump via `DispatcherFrame` +
  Background-priority timer; probe animated properties directly.
- Engine state in tests: direct `engine.State` edits are UNSAVED — fine
  within one engine instance. Basic egg price = 1,000,000,000 (2B is a
  good shop-test wallet). Shop prices SCALE with `_shopDifficulty` —
  never hardcode in probes; read from `view.ShopRows`.
- Image verification: a vision subagent IS configured on this machine
  (user, 2026-09-30) — but numeric probes + pixel compare proved
  sufficient for the M26 no-visual-change proof.
- WPF gotchas (still true): no `StackPanel.Spacing`; `new X { ... } {
  Children = ... }` invalid; `HexBrush("#14" + hex)` — hex WITHOUT '#';
  `Math.Round(float)` ambiguous → cast double; no implicit long→int;
  `Freeze()` is a method call, not initializer property.
- `CompanionGameView` field order is contract — new view fields go
  before `UnownForms`.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in
  `wsl -l -v`; SSH trap if push denied (`ssh-add -d <key>; ssh-add
  <key>`, verify `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI for BASE DATA, macOS-card theme, 40px dex tiles,
  saturation-dimming, official limits via credentials, emoji-as-design
  glyphs, dark mode, incident banner.

## Working Agreement
- Confirm any new slice with the user before building anything; visual
  work gets a rendered sample/screenshot checkpoint BEFORE polishing.
- Evidence: unit tests for pure parts; UI behavior manual by the user;
  `PTB_STATE_DIR` sandbox + offscreen screenshot harness for layout.
- Core/Providers/Application stay UI-free; UI references Application
  only.
- Tools & skills: `openviking` MCP — read/write session-continuity
  events (`viking://user/owner/memories/events/2026/09/`); model the
  next handoff on this file; update `docs/ui-parity-audit.md` when a
  slice lands (now closed — reopen only with user confirmation).

## Open Risks
- 0.25.0+ clients see update banners correctly; ≤0.24.0 clients never
  do (UA fix shipped in 0.25.0) — watch releases manually for those.
- v0.25.1 release upload is manual (user, browser) — zip + notes land
  in `artifacts\release\` when published.
- Celebrations drain in `UpdateGame` — dashboard CLOSED holds the queue
  (macOS-parity); MINIMIZED plays unseen (accepted edge case).
- Status-line burn is a refresh-delta approximation; `Tired` never
  occurs (no limits feature).
- Catch-log chain names for OLD dex entries show "#id" fallbacks.
- Egg imminent/guarantee visuals still never seen live by the user;
  smoke covers the flows logically (pulse animation variance noted).
- Shiny ✨ surfaces — still never seen live on a real shiny since 09-23.
- Launch-at-login registry failures revert the checkbox silently.
- XamlAnimatedGif hobby-maintained — static PNG fallback exists.

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all 539 tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for
  destructive checks; diagnostics log clean after runs

## Start Prompt
```text
Read docs/handoff/2026-09-30-milestone-26-series-complete.md end to end.
The UI-parity + engineering-quality series is complete; M26 (game-tab
extraction, no visual change) shipped as 0.25.1. There is no queued next
slice — if the user asks for new work, confirm scope first and re-open
docs/ui-parity-audit.md only with their say-so.
```
