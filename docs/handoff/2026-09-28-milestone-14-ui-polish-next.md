# Handoff — Milestone 14: next UI-polish slice (confirm choice first)

## Goal
The project was closed into maintenance mode at M11, then reopened by the user
for UI parity polish with the macOS original. M12 (sprites + localization,
0.11.0) and M13 (dex sprite tile grid, 0.12.0) are done and user-verified.
M14 is the next UI-polish slice from the candidate list — confirm the slice
with the user FIRST (see Decision Needed at Start); do not start scaffolding
before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: M0–M9 unchanged (`f763205`…`b31cb0e`) · `a340745` docs M10
  handoff · `97f07c6` combat details (M10) · `0d4df7b` docs M11 handoff ·
  `7cd9f73` docs closure/maintenance · `eb3ce33` localization + sprites (M12) ·
  `da3f369` docs M13 handoff · `3685768` dex tile grid (M13) ·
  (M13 docs commit — see git log)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M13: dex text list replaced by a sprite tile grid, version 0.12.0
  published and user-verified (installed exe, grid readability confirmed
  after one size iteration).
- Grid: `DexList` is a ListBox with a WrapPanel items panel
  (`ScrollViewer.HorizontalScrollBarVisibility="Disabled"` forces wrapping);
  each tile (`CreateDexTile` in DashboardWindow.xaml.cs) is a fixed 84px
  Grid: `#id` caption (+ gray star when shiny caught), 64px static sprite
  with `BitmapScalingMode.NearestNeighbor`, centered name (11px, trimmed,
  `← ` prefix while raising), full old row info kept in the tooltip.
  Double-click (unchanged SelectedIndex path) opens the detail window.
- Layout: dashboard 580x720 (was 470x580 → 510x580 during iteration); dex
  column 1.15\* vs events 0.85\* → three 84px tiles per row.
- Evidence: 366 tests green via `dotnet test PokeTokenBar.Windows.slnx`
  (no pure-code change in M13, so no new tests) plus a headless UI smoke
  (see Hard-won Context).

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
  the Ui csproj, bumped with the slice that ships it (0.12.0 = M13).
  GitHub Releases are uploaded by hand (no release automation).
- App state/settings/sprites/diagnostics stay under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides). New local preferences go in `AppSettingsFile`
  (settings.json), never in the save.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- UI-polish slice (pick one; do not bundle). Rough value order:
  1. Usage trend chart + model breakdown (needs aggregation surface — check
     what Application exposes vs. what macOS shows before promising).
  2. Settings window — language picker (live re-localize or restart note),
     scan folders, difficulty; remove sliders from the game tab.
  3. Shiny banner / evolution-line visuals / per-form Unown sprites in the
     dex grid (DexRows would need to carry the form — engine change).
  4. Resizable dashboard window (`CanResize` + min sizes; WrapPanel/star
     layout already adapts).
  5. Anything else the user wants (defects included).
- The user may also stop the polish series at any time (back to maintenance).

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members, `View()` →
  `CompanionGameView` (ends with `AppLanguage Language`), `Detail(speciesID)`
  → `CompanionDetailSnapshot?`, `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. Engine texts localize from `_state.Language`.
- New fixed UI strings go in `src/Core/DashboardText.cs` (copy the macOS
  translation when the concept exists in `Localization.swift`; keep the same
  tone otherwise). UI never hardcodes display strings.
- Sprite rendering goes through `SpriteSlot` (cached-first; never block the UI
  thread on fetch). Static thumbnails for lists/grids, animated only for the
  single hero sprite per surface. Pixel-art thumbs >44px should set
  `RenderOptions.SetBitmapScalingMode(NearestNeighbor)`.
- Dex tiles: build via `CreateDexTile` in code-behind; keep tile info parity
  in the tooltip when a marker replaces visible text.
- Snapshot: schema 2 shape; loader keeps accepting schema 1. Regenerate only
  via `scripts/generate-pokemon-snapshot.ps1` under pwsh.
- Settings: `AppSettingsFile` for new local preferences. Packaging: version =
  Ui csproj `<Version>`; publish via `scripts/publish-windows.ps1`.

## Relevant Files
- `src/Ui/DashboardWindow.xaml`/`.cs` (WrapPanel dex grid, `CreateDexTile`,
  580x720 window, 1.15\*/0.85\* game-tab columns)
- `src/Ui/SpriteSlot.cs`, `src/Ui/SpeciesDetailWindow.xaml`/`.cs`,
  `src/Ui/App.xaml.cs`, `src/Ui/FloatingPetWindow.xaml.cs`
- `src/Core/DashboardText.cs`, `Tests/Core.Tests/DashboardTextTests.cs`
- `src/Application/CompanionEngine.cs`, `src/Application/SpriteStore.cs`
- `Sources/PokeTokenBar/Core/Localization.swift` — the translation source of
  truth for new strings; `Sources/PokeTokenBar/UI/CompanionView.swift` — the
  layout reference (`DexGridView`/`DexSpeciesCell` at ~line 895-1440 is the
  grid reference; footer pager/rarity filter are macOS-popover workarounds,
  not ported)
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
- WrapPanel inside a ListBox does NOT wrap unless
  `ScrollViewer.HorizontalScrollBarVisibility="Disabled"` — Auto/Visible gives
  it infinite width.
- ListBoxItem container adds ~6px per item (default Padding 4 + Border 1);
  the dex ItemContainerStyle sets `Padding=0`, count the 1px border when
  computing tiles-per-row.
- TabItem content is lazily realized: controls in a non-selected tab have an
  EMPTY visual tree until the tab is selected (headless checks must set
  `MainTabs.SelectedIndex` first).
- Headless UI smoke pattern (M13): temp csproj outside the repo referencing
  `src/Ui` + STA Main + `new Application()` + DispatcherTimer verify-then-
  shutdown; seed a sandbox via `PTB_STATE_DIR` + `CompanionStateFile.Save`
  with hand-built `DexEntry`s (Names dict per species: ko/en keys), fake
  `SpriteStore` fetch delegate returning a tiny PNG, `window.FindName(...)`
  for named controls. Assert items/panel type/actual widths, then exit code.
  (M13 harness lived in `%TEMP%\opencode\ptb-m13-smoke` — recreate, temp is
  disposable.)
- `SpriteSlot` per rebuilt tile is fine (memory/disk cache is synchronous
  on hit); async fetches keep their own version guard. Give the sprite host
  Grid a fixed Height so layout does not jitter while fetching.
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
  ListBox-level MouseDoubleClick, 40px tile sprites (user found them too
  small — 64px + NearestNeighbor passed).

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm the
  slice choice with the user first (this handoff's Decision Needed).
- Evidence: unit tests for pure parts; UI behavior manual by the user; the
  `PTB_STATE_DIR` sandbox + headless smoke harness for layout-level checks;
  `dotnet run file.cs` seeding/probe tricks for destructive checks.
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
- Dashboard fixed at 580x720; if the user keeps wanting more space, the
  resizable-window slice is the answer, not another size bump.

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
  continuity entries (M0–M13)

## Start Prompt
```text
Read docs/handoff/2026-09-28-milestone-14-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project was reopened for UI parity polish; M12 (sprites + localization) and M13 (dex tile grid) are done.
First confirm the M14 slice with the user (usage charts, settings window, shiny/evolution visuals, resizable window, or their own pick — see Decision Needed at Start).
Then implement that slice only, prove pure parts with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle other work into the slice.
```
