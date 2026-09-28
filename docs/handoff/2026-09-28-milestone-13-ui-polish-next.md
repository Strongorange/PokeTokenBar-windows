# Handoff — Milestone 13: next UI-polish slice (reopen series; confirm choice first)

## Goal
The project was closed into maintenance mode at M11, then reopened by the user
for UI parity polish with the macOS original. M12 (sprites + full localization)
is done and personally verified. M13 is the next UI-polish slice from the
candidate list — confirm the slice with the user FIRST (see Decision Needed at
Start); do not start scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: M0–M9 unchanged (`f763205`…`b31cb0e`) · `a340745` docs M10
  handoff · `97f07c6` combat details (M10) · `0d4df7b` docs M11 handoff ·
  `7cd9f73` docs closure/maintenance · `eb3ce33` localization + sprites (M12) ·
  (M12 docs commit — see git log)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M12: dashboard/tray/pet/detail-window localization + sprites,
  version 0.11.0 published and user-verified.
- Localization: `src/Core/DashboardText.cs` holds every fixed UI string in the
  seven app languages (values copied from macOS `Localization.swift` where the
  concept exists). Engine emits localized event/notice texts (AddEvent),
  shop/bag labels and detail gender/move-method labels from `_state.Language`;
  `CompanionGameView` and `CompanionDetailSnapshot` carry `Language` so the UI
  localizes its own labels. Static labels are set once in
  `DashboardWindow.LocalizeStaticText` (language fixed per session; changing
  language requires restart — no picker exists yet).
- Sprites: `src/Ui/SpriteSlot.cs` (cached-first render, glyph placeholder,
  background fetch, version guard) reuses `SpriteStore`. Wired into the game
  tab active mon (animated; egg image while incubating), dex rows (static 28px
  thumbnails) and the detail window header (animated 72px). Unown dex rows use
  the generic sprite (DexRows do not carry the form).
- Evidence: 366 tests green (141 Core + 105 Providers + 49 Platform.Windows +
  71 Application) via `dotnet test PokeTokenBar.Windows.slnx`. New: 4
  DashboardText tests + 1 engine localization test; three older shop/event
  tests now set `Language=En` explicitly because engine texts localize from
  saved state (system-default language would otherwise make assertions
  culture-dependent).
- Manual smoke (user, 2026-09-28): Korean tray menu, game tab sprite + Korean
  labels, dex row sprites, detail window Korean sections + sprite, pet menu.

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2,
  Application references `Microsoft.Data.Sqlite` 10.0.12), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns
  paths/settings/diagnostics, Application owns engine + orchestration +
  provider IO that needs Platform types, UI consumes Application only. No
  MVVM framework. No provider-specific branches in aggregation/progression/UI.
- Pokemon base data stays the bundled schema-2 snapshot (offline-first);
  sprites remain the separate online-cached concern (`SpriteStore`).
- Windows keeps the tray + dashboard model; inline menu-bar text (macOS
  `NSStatusItem`) stays out of scope by design (documented in the plan).
- Packaging stays personal-use: single-file self-contained exe in
  `%LOCALAPPDATA%\Programs\PokeTokenBar` + Start Menu shortcut. Version in
  the Ui csproj, bumped with the slice that ships it (0.11.0 = M12).
  GitHub Releases are uploaded by hand (no release automation).
- App state/settings/sprites/diagnostics stay under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides). New local preferences go in `AppSettingsFile`
  (settings.json), never in the save.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- UI-polish slice (pick one; do not bundle). Rough value order:
  1. Dex grid view — sprite tiles instead of the text list (macOS Pokedex
     grid), detail window on tile double-click.
  2. Usage trend chart + model breakdown (needs aggregation surface — check
     what Application exposes vs. what macOS shows before promising).
  3. Settings window — language picker (live re-localize or restart note),
     scan folders, difficulty; remove sliders from the game tab.
  4. Shiny banner / evolution-line visuals / per-form Unown sprites in dex.
  5. Anything else the user wants (defects included).
- The user may also stop the polish series at any time (back to maintenance).

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members, `View()` →
  `CompanionGameView` (ends with `AppLanguage Language`), `Detail(speciesID)`
  → `CompanionDetailSnapshot?` (ends with `Language`; individuals carry
  localized gender/nature/ability/moves; move methods already localized —
  "Lv. n"/"TM"/egg/tutor words), `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. Engine texts localize from `_state.Language`.
- New fixed UI strings go in `src/Core/DashboardText.cs` (copy the macOS
  translation when the concept exists in `Localization.swift`; keep the same
  tone otherwise). UI never hardcodes display strings.
- Sprite rendering goes through `SpriteSlot` (cached-first; never block the UI
  thread on fetch). Static thumbnails for lists/grids, animated only for the
  single hero sprite per surface (macOS CPU precedent: no mass GIF grids).
- Snapshot: schema 2 shape; loader keeps accepting schema 1. Regenerate only
  via `scripts/generate-pokemon-snapshot.ps1` under pwsh.
- Settings: `AppSettingsFile` for new local preferences. Packaging: version =
  Ui csproj `<Version>`; publish via `scripts/publish-windows.ps1`.

## Relevant Files
- `src/Core/DashboardText.cs`, `Tests/Core.Tests/DashboardTextTests.cs`
- `src/Ui/SpriteSlot.cs`, `src/Ui/DashboardWindow.xaml`/`.cs`,
  `src/Ui/SpeciesDetailWindow.xaml`/`.cs`, `src/Ui/App.xaml.cs`,
  `src/Ui/FloatingPetWindow.xaml.cs`
- `src/Application/CompanionEngine.cs` (localized AddEvent/labels, Language in
  view records), `src/Application/SpriteStore.cs`
- `Sources/PokeTokenBar/Core/Localization.swift` — the translation source of
  truth for new strings; `Sources/PokeTokenBar/UI/CompanionView.swift` — the
  layout reference for grid/chart/settings parity
- `docs/windows-port-plan.md` — "Reopened: UI parity polish" section (candidate
  list), `docs/handoff/2026-09-28-milestone-11-next-or-close.md` (pre-reopen
  handoff with the full trap list — still canonical)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~10s, 366 tests) — wrong
  file error `MSB1009` if you type `.sln`. Kill any DEBUG `PokeTokenBar.exe`
  before testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- Engine-localized texts make text-asserting tests culture-dependent: any test
  that asserts event/notice/shop strings MUST set `engine.State.Language =
  AppLanguage.En` first. New text assertions: do the same.
- WPF `MouseDoubleClick` is RoutingStrategy.Direct: attach via
  `ItemContainerStyle` + `EventSetter` on ListBoxItem, never on the ListBox.
- `SpriteSlot` per rebuilt list row is fine (memory/disk cache is synchronous
  on hit); async fetches keep their own version guard.
- XAML layout trap: a horizontal StackPanel gives children unlimited width —
  never put wrapping text content inside one (SpeciesDetailWindow splits
  HeaderRoot/ContentRoot for this reason).
- PowerShell generator: pwsh only (PS7 `-Parallel`); function-returned
  JsonObjects cannot be assigned into JsonObject indexers (PSObject wrapping)
  — build nodes inline.
- Snapshot regeneration ~4 min networked, byte-stable for identical upstream.
- Hardcodet tray events bypass `DispatcherUnhandledException` — wrap
  tray-sourced handler bodies entirely in try/catch. Balloon API:
  `ShowBalloonTip(title, message, BalloonIcon)`.
- XamlAnimatedGif: `AnimationBehavior.SetSourceStream` + keep the stream
  referenced (SpriteSlot does). Static PNG fallback exists for GIF failure.
- Install dir is wiped on every publish. Sprite disk cache grows unbounded by
  design. Pet window is manual-checklist only (drag/click/double/right-click/
  restart).
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI at runtime for BASE DATA, parsing `wsl.exe -l` output, version in
  the tray tooltip, opening WSL SQLite dbs over UNC directly (locked),
  ListBox-level MouseDoubleClick.

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm the
  slice choice with the user first (this handoff's Decision Needed).
- Evidence: unit tests for pure parts; UI behavior manual by the user; the
  `PTB_STATE_DIR` sandbox + `dotnet run file.cs` seeding/probe tricks for
  destructive checks and live verification.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- macOS parity slices can balloon: the macOS app is a popover with charts,
  grids and per-view polish; each Windows slice must stay scoped and text-
  first/simple WPF (no MVVM framework by decision).
- Language is fixed per session (no picker); a future settings slice should
  either live-relocalize or state the restart requirement.
- XamlAnimatedGif is hobby-maintained — static PNG fallback exists; do not
  build on animator internals.
- Snapshot size (3.9 MB) embedded in the exe; re-check only if the schema
  grows again.

## Acceptance Criteria
- [ ] Slice confirmed with the user and implemented per its own plan
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (366 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (re-publish via `scripts\publish-windows.ps1`; bump `<Version>`
      minor/patch accordingly)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (closure + reopened UI polish)
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen handoff
  (full trap list, packaging/verification details)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session
  continuity entries (M0–M12)

## Start Prompt
```text
Read docs/handoff/2026-09-28-milestone-13-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project was reopened for UI parity polish; M12 (sprites + localization) is done.
First confirm the M13 slice with the user (dex grid, usage charts, settings window, visuals, or their own pick — see Decision Needed at Start).
Then implement that slice only, prove pure parts with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle other work into the slice.
```
