# Handoff — Milestone 16: next UI-polish slice (confirm choice first)

## Goal
The project was closed into maintenance mode at M11, then reopened by the user
for UI parity polish with the macOS original. M12 (sprites + localization,
0.11.0), M13 (dex sprite tile grid, 0.12.0), M14 (usage trend chart + model
breakdown, 0.13.0) and M15 (settings window, 0.14.0) are done and
user-verified. M16 is the next UI-polish slice from the remaining candidates —
confirm the slice with the user FIRST (see Decision Needed at Start); do not
start scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: M0–M9 unchanged (`f763205`…`b31cb0e`) · `a340745` docs M10
  handoff · `97f07c6` combat details (M10) · `0d4df7b` docs M11 handoff ·
  `7cd9f73` docs closure/maintenance · `eb3ce33` localization + sprites (M12) ·
  `da3f369` docs M13 handoff · `3685768` dex tile grid (M13) · `51ed5e8` docs
  M14 handoff · `d8f6d85` usage trend chart (M14) · `7685848` docs M15 handoff ·
  `d0fe59a` settings window (M15) · (M16 docs commit — see git log)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M15: a settings window (opened from the tray menu "Settings" entry
  and a dashboard footer "Settings…" button) with four sections; version
  0.14.0 published and user-verified.
- Language: ComboBox of all seven `AppLanguage`s showing native labels
  (`한국어`, `English`, …). Selection calls `CompanionEngine.SetLanguage`
  (persists into the save, raises `Changed`) then `App.ApplyLanguage`, which
  rebuilds the tray context menu, re-localizes dashboard static text +
  re-renders the usage tab, and re-localizes the pet window menu. Live switch,
  no restart needed; survives restart (language lives in
  `companion-state.json`, NOT settings.json).
- Difficulty: growth/shop sliders moved out of the game tab into the settings
  window; values render as percents ("50%"). Same wiring as before — snap via
  `PokemonBalance.SnapDifficulty`, `SetGrowthDifficulty`/`SetShopDifficulty`,
  persisted via `App.ApplyDifficulty`.
- Scan folders: per-provider extra roots (Claude Code / Codex / OpenCode
  ComboBox + `OpenFolderDialog` + list + remove). Stored as
  `scanRoots: [{provider, path}]` in settings.json (`AppSettings.ScanRoots`,
  `ScanRootEntry`), applied live by `ScanRootSettings.Apply` mutating the
  shared `UsageRootOptions` (its `Extra*Roots` are now `set`-table), then a
  refresh is triggered. Path validation/dedup happens in discovery
  (`ExtraCandidate`/`Collapse`) and on load/save.
- Floating pet: enable checkbox + size slider moved into the settings window
  (`App.SetPetEnabled`/`ApplyPetSize` unchanged). The game tab has no sliders
  or pet controls anymore.
- Tray tooltip now localizes Today/Month labels from the engine language.
- Evidence: 385 tests green via `dotnet test PokeTokenBar.Windows.slnx`
  (376 + 9 new) plus a 15-check headless UI smoke (game tab sliders gone,
  footer settings button present, 7 language items, live engine language
  switch + self/dashboard re-localization, sliders drive engine difficulty,
  percent readout — harness pattern below).

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
  the Ui csproj, bumped with the slice that ships it (0.14.0 = M15).
  GitHub Releases are uploaded by hand (no release automation).
- App state/sprites/diagnostics under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides). Local preferences go in `AppSettingsFile`
  (settings.json) — difficulty, pet, scan roots. Language is the deliberate
  exception: it is saved-game state (import/export carries it), set via
  `CompanionEngine.SetLanguage`, never via settings.json.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- UI-polish slice (pick one; do not bundle). Rough value order:
  1. Shiny banner / evolution-line visuals / per-form Unown sprites in the
     dex grid (DexRows would need to carry the form — engine change).
  2. Resizable dashboard window (`CanResize` + min sizes; WrapPanel/star
     layout already adapts).
  3. Anything else the user wants (defects included).
- The user may also stop the polish series at any time (back to maintenance).

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `SetLanguage(AppLanguage)` (persists + raises `Changed`; texts localize
  from `_state.Language`), `View()` → `CompanionGameView` (ends with
  `AppLanguage Language`), `Detail(speciesID)` → `CompanionDetailSnapshot?`,
  `Changed`, `DrainNotices()`, `ExportSave`/`ImportSave`.
- Language changes flow: UI calls `SetLanguage` → engine persists + `Changed`
  → `App.OnCompanionChanged` re-renders game views in the new language →
  UI then calls `App.ApplyLanguage(lang)` for static text (tray menu rebuild,
  `DashboardWindow.Relocalize`, usage re-render, `FloatingPetWindow.Relocalize`).
  New static surfaces must re-localize inside `ApplyLanguage`.
- Usage surfaces: `UsageDisplayState` (with `MonthDaily`/`TodayModels`) built
  by `UsageRefreshService`; pure chart geometry via `DailyTrendMetrics`;
  readout cost uses `UsageCost.Text(label, compact: true)`.
- Settings: `AppSettingsFile` for local prefs including
  `ScanRoots` (`ScanRootEntry(provider, path)`, providers
  claude|codex|opencode via `ScanRootSettings`). `App` owns the single
  `UsageRootOptions` instance passed to `UsageRefreshService`; runtime scan
  folder changes mutate it through `App.ApplyScanRoots` (save + apply +
  refresh). `SettingsWindow` reaches App only via guarded
  `Application.Current is App` checks so headless tests work.
- New fixed UI strings go in `src/Core/DashboardText.cs` (copy the macOS
  translation when the concept exists in `Localization.swift` — M15 copied
  settings/language/difficulty/scan-folder strings from there; keep the same
  tone otherwise). UI never hardcodes display strings.
- Sprite rendering goes through `SpriteSlot` (cached-first; never block the UI
  thread on fetch). Static thumbnails for lists/grids, animated only for the
  single hero sprite per surface. Pixel-art thumbs >44px should set
  `RenderOptions.SetBitmapScalingMode(NearestNeighbor)`.
- Dex tiles: build via `CreateDexTile` in code-behind; keep tile info parity
  in the tooltip when a marker replaces visible text.
- Snapshot: schema 2 shape; loader keeps accepting schema 1. Regenerate only
  via `scripts/generate-pokemon-snapshot.ps1` under pwsh.
- Packaging: version = Ui csproj `<Version>`; publish via
  `scripts/publish-windows.ps1`.

## Relevant Files
- `src/Ui/SettingsWindow.xaml`/`.cs` (new in M15 — the whole slice surface)
- `src/Ui/App.xaml.cs` (`ShowSettings`, `ApplyLanguage`, `ApplyScanRoots`,
  `_rootOptions` wiring, localized tray tooltip)
- `src/Platform.Windows/ScanRootSettings.cs`, `AppSettingsFile.cs`
  (`ScanRoots`/`ScanRootEntry`), `UsageRoots.cs` (Extra* now set-table),
  `Tests/Platform.Windows.Tests/ScanRootSettingsTests.cs`,
  `Tests/Platform.Windows.Tests/AppSettingsFileTests.cs`
- `src/Application/CompanionEngine.cs` (`SetLanguage`),
  `Tests/Application.Tests/CompanionEngineTests.cs`
- `src/Ui/DashboardWindow.xaml`/`.cs` (game tab without sliders/pet rows,
  footer `SettingsButton`, `Relocalize`),
  `src/Ui/FloatingPetWindow.xaml.cs` (`Relocalize`)
- `src/Core/DashboardText.cs`, `Tests/Core.Tests/DashboardTextTests.cs`
- `src/Core/DailyTrendMetrics.cs`, `src/Application/UsageRefreshService.cs`
  (M14 surfaces, unchanged in M15)
- `Sources/PokeTokenBar/UI/PopoverView.swift` — layout reference;
  `Sources/PokeTokenBar/Core/Localization.swift` — string source of truth
  (settings block around lines 145-230 and 395-411)
- `docs/windows-port-plan.md` — "Reopened: UI parity polish" section,
  `docs/handoff/2026-09-28-milestone-15-ui-polish-next.md` (this slice's
  predecessor), `docs/handoff/2026-09-28-milestone-11-next-or-close.md`
  (pre-reopen handoff with the full trap list — still canonical)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~10s, 385 tests) — wrong
  file error `MSB1009` if you type `.sln`. Kill any DEBUG `PokeTokenBar.exe`
  before testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- `scripts/publish-windows.ps1` KILLS the installed app and does NOT relaunch
  it — after publishing, start the exe yourself for manual checks (the user
  will otherwise report "not in tray").
- Engine-localized texts make text-asserting tests culture-dependent: any test
  that asserts event/notice/shop strings MUST pin a language different from
  the machine culture when asserting `Changed`-style side effects of
  `SetLanguage` — the M15 test picks
  `target = language == Ko ? En : Ko` because a ko-KR machine already boots
  the engine in Ko and `SetLanguage(Ko)` is a no-op.
- WPF XAML parse order trap (M15): setting `Minimum="0.1"` on a Slider during
  InitializeComponent coerces Value and fires `ValueChanged` BEFORE the
  constructor body runs — value-changed handlers MUST null-guard
  (`_engine is null`) exactly like the old dashboard sliders did. Same for
  `PetSizeSlider` (Minimum 48).
- `SettingsWindow` depends on `App` only through guarded
  `Application.Current is App` checks — keep that pattern so the headless
  smoke harness can construct windows with just an engine.
- `App.ApplyLanguage` rebuilds the tray `ContextMenu` wholesale
  (`_trayIcon.ContextMenu = BuildMenu()`); `_petToggle` is reassigned inside
  BuildMenu so its check state survives. Pet window menu re-localizes via its
  public `Relocalize`.
- Scan-root live apply: `UsageRootOptions.Extra*Roots` are mutable and shared
  by reference into `DiscoveryUsageRootSource` — mutating the lists is enough,
  no service rebuild. Invalid/duplicate paths are dropped later in discovery
  (`PathNormalizer`/`Collapse`), so settings may store raw strings.
- DockPanel order trap (model rows): the right-docked element (tokens) must be
  added to `Children` FIRST — assert children[0]=tokens in smokes.
- WPF `MouseDoubleClick` is RoutingStrategy.Direct: attach via
  `ItemContainerStyle` + `EventSetter` on ListBoxItem, never on the ListBox.
- WrapPanel inside a ListBox does NOT wrap unless
  `ScrollViewer.HorizontalScrollBarVisibility="Disabled"` — Auto/Visible gives
  it infinite width.
- ListBoxItem container adds ~6px per item (default Padding 4 + Border 1);
  the dex ItemContainerStyle sets `Padding=0`, count the 1px border when
  computing tiles-per-row.
- TabItem content is lazily realized: controls in a non-selected tab have an
  EMPTY visual tree until selected (headless checks must set
  `MainTabs.SelectedIndex` first). Usage tab is index 0 (default); game tab
  index 1. `FindName` on XAML-named elements works regardless of tab state.
- Headless UI smoke pattern (M13-M15): temp csproj outside the repo referencing
  `src/Ui` + STA Main + `new Application()`; construct windows with a temp
  `CompanionEngine` (state file under `%TEMP%`); drive controls
  (`combo.SelectedIndex`, `slider.Value = …`) and assert structure/text;
  exit code. (M15 harness lived in `%TEMP%\opencode\ptb-m15-smoke` —
  recreate, temp is disposable.)
- `SpriteSlot` per rebuilt tile is fine (memory/disk cache is synchronous
  on hit); async fetches keep their own version guard. Give the sprite host
  Grid a fixed Height so layout does not jitter while fetching.
- XAML layout trap: a horizontal StackPanel gives children unlimited width —
  never put wrapping text content inside one (SpeciesDetailWindow splits
  HeaderRoot/ContentRoot; SettingsWindow hints live directly under the
  vertical StackPanel for this reason). Equal-width chart columns use a Grid
  with `*` columns (star sizing needs a finite-width parent).
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
- XamlAnimatedGif is hobby-maintained — static PNG fallback exists; do not
  build on animator internals.
- Snapshot size (3.9 MB) embedded in the exe; re-check only if the schema
  grows again.
- Dashboard fixed at 580x720; if the user keeps wanting more space, the
  resizable-window slice is the answer, not another size bump.
- Language lives in the save: an imported save switches language with its
  data (same as macOS export semantics) — intended, but worth remembering
  when a user reports a "surprise" language change after import.
- DayStamp/axis tests assume stable ICU weekday abbreviations across .NET
  updates (see Hard-won Context in the M14/M15 handoffs).

## Acceptance Criteria
- [ ] Slice confirmed with the user and implemented per its own plan
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (385 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (re-publish via `scripts\publish-windows.ps1`; bump `<Version>`
      minor/patch accordingly; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (closure + reopened UI polish)
- `docs/handoff/2026-09-28-milestone-15-ui-polish-next.md` — M15 handoff
  (settings window contracts)
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen handoff
  (full trap list, packaging/verification details)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session
  continuity entries (M0–M15)

## Start Prompt
```text
Read docs/handoff/2026-09-28-milestone-16-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project was reopened for UI parity polish; M12 (sprites + localization), M13 (dex tile grid), M14 (usage trend chart) and M15 (settings window) are done.
First confirm the M16 slice with the user (shiny/evolution visuals, resizable window, or their own pick — see Decision Needed at Start).
Then implement that slice only, prove pure parts with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle other work into the slice.
```
