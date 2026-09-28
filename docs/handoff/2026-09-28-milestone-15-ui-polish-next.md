# Handoff — Milestone 15: next UI-polish slice (confirm choice first)

## Goal
The project was closed into maintenance mode at M11, then reopened by the user
for UI parity polish with the macOS original. M12 (sprites + localization,
0.11.0), M13 (dex sprite tile grid, 0.12.0) and M14 (usage trend chart +
model breakdown, 0.13.0) are done and user-verified. M15 is the next
UI-polish slice from the remaining candidates — confirm the slice with the
user FIRST (see Decision Needed at Start); do not start scaffolding before
that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: M0–M9 unchanged (`f763205`…`b31cb0e`) · `a340745` docs M10
  handoff · `97f07c6` combat details (M10) · `0d4df7b` docs M11 handoff ·
  `7cd9f73` docs closure/maintenance · `eb3ce33` localization + sprites (M12) ·
  `da3f369` docs M13 handoff · `3685768` dex tile grid (M13) · `51ed5e8` docs
  M14 handoff · `d8f6d85` usage trend chart (M14) · (M14 docs commit — see
  git log)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M14: Usage tab gained a month-to-date daily trend chart and a
  combined-today model breakdown, version 0.13.0 published and user-verified.
- Chart: top of the Usage tab inside `TrendSection`; caption row
  (DailyTrend label + hover readout + Peak label/value), bar row (today
  accented via `SystemColors.HighlightBrush`, zero days opacity 0.18, used
  days 0.45, min 1.5px baseline so empty days stay visible), weekend tick
  row, sparse axis (day 1, every 7 days, today; regular labels within 3 days
  of today suppressed). Transparent per-column hit borders drive the hover
  readout (`MouseEnter` day / `MouseLeave` today). Gate is semantic:
  `peak > 0`, else the whole section collapses.
- Geometry lives in `src/Core/DailyTrendMetrics.cs` (pure): BarHeight,
  AxisLabel, IsWeekend (Sat/Sun — all seven app languages share that
  weekend), DayStamp (per-language order + weekday names, e.g. ko
  "9. 28. (월)", en "Mon, 9/28", es "lun 28/09").
- Application surface: `UsageDisplayState` gained optional `MonthDaily`
  (zero-filled month-to-date `DailyUsage` list) and `TodayModels`
  (combined-today per-model token dict); `UsageRefreshService.RefreshAll`
  computes both from the combined entries it already had. Model rows render
  only when 2+ models, sorted desc, name = last `/` segment.
- Evidence: 376 tests green via `dotnet test PokeTokenBar.Windows.slnx`
  (366 + 10 new) plus a headless UI smoke (28-day series: bar heights/
  opacities, 8 weekend ticks, 5 axis labels, zero-gate collapse, model row
  order — harness pattern below).

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
  the Ui csproj, bumped with the slice that ships it (0.13.0 = M14).
  GitHub Releases are uploaded by hand (no release automation).
- App state/settings/sprites/diagnostics stay under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides). New local preferences go in `AppSettingsFile`
  (settings.json), never in the save.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- UI-polish slice (pick one; do not bundle). Rough value order:
  1. Settings window — language picker (live re-localize or restart note),
     scan folders, difficulty; remove sliders from the game tab.
  2. Shiny banner / evolution-line visuals / per-form Unown sprites in the
     dex grid (DexRows would need to carry the form — engine change).
  3. Resizable dashboard window (`CanResize` + min sizes; WrapPanel/star
     layout already adapts).
  4. Anything else the user wants (defects included).
- The user may also stop the polish series at any time (back to maintenance).

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members, `View()` →
  `CompanionGameView` (ends with `AppLanguage Language`), `Detail(speciesID)`
  → `CompanionDetailSnapshot?`, `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. Engine texts localize from `_state.Language`.
- Usage surfaces: `UsageDisplayState` (now with `MonthDaily`/`TodayModels`)
  built by `UsageRefreshService`; pure chart geometry via
  `DailyTrendMetrics`; readout cost uses `UsageCost.Text(label, compact:
  true)` and is appended only when some day in the series has known cost
  coverage.
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
- `src/Core/DailyTrendMetrics.cs`, `Tests/Core.Tests/DailyTrendMetricsTests.cs`
- `src/Application/UsageDisplayState.cs`, `src/Application/UsageRefreshService.cs`
- `src/Ui/DashboardWindow.xaml`/`.cs` (Usage tab `TrendSection` +
  `UpdateTrend`/`SetTrendReadout`; game tab unchanged)
- `src/Core/DashboardText.cs`, `Tests/Core.Tests/DashboardTextTests.cs`
- `src/Ui/SpriteSlot.cs`, `src/Ui/SpeciesDetailWindow.xaml`/`.cs`,
  `src/Ui/App.xaml.cs`, `src/Ui/FloatingPetWindow.xaml.cs`
- `src/Application/CompanionEngine.cs`, `src/Application/SpriteStore.cs`
- `Sources/PokeTokenBar/UI/PopoverView.swift` — the layout reference
  (`MonthDailyTrend`/`DailyTrendMetrics` at ~line 838-1040 is the chart
  reference; model rows at ~line 251; footer pager/rarity filter are
  macOS-popover workarounds, not ported)
- `docs/windows-port-plan.md` — "Reopened: UI parity polish" section (candidate
  list), `docs/handoff/2026-09-28-milestone-11-next-or-close.md` (pre-reopen
  handoff with the full trap list — still canonical)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~10s, 376 tests) — wrong
  file error `MSB1009` if you type `.sln`. Kill any DEBUG `PokeTokenBar.exe`
  before testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- `scripts/publish-windows.ps1` KILLS the installed app and does NOT relaunch
  it — after publishing, start the exe yourself for manual checks (the user
  will otherwise report "not in tray").
- Engine-localized texts make text-asserting tests culture-dependent: any test
  that asserts event/notice/shop strings MUST set `engine.State.Language =
  AppLanguage.En` first. New text assertions: do the same.
- `DayStamp` culture tests pin exact strings (ko "9. 28. (월)", es "lun
  28/09", …). They depend on ICU data; if a .NET/ICU update changes weekday
  abbreviations, re-verify with a scratch `dotnet run` one-liner before
  touching expectations (that is how the patterns were chosen in M14).
- DockPanel order trap (model rows): the right-docked element (tokens) must be
  added to `Children` FIRST — DockPanel processes children in order and the
  last (undocked) child fills the remaining space. Assert children[0]=tokens,
  children[1]=name in smokes.
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
  `MainTabs.SelectedIndex` first). The Usage tab is index 0 and selected by
  default, so chart checks skip this.
- Headless UI smoke pattern (M13/M14): temp csproj outside the repo referencing
  `src/Ui` + STA Main + `new Application()`; hand-build the state instead of
  reading real logs (M14 built `UsageDisplayState` directly with a synthetic
  series/models dict — no sandbox needed for the chart; M13 seeded a
  `PTB_STATE_DIR` sandbox via `CompanionStateFile.Save` for game-tab checks).
  `window.FindName(...)` for named controls; assert structure (column counts,
  bar heights/opacity, children order), then exit code. (M14 harness lived in
  `%TEMP%\opencode\ptb-m14-smoke` — recreate, temp is disposable.)
- `SpriteSlot` per rebuilt tile is fine (memory/disk cache is synchronous
  on hit); async fetches keep their own version guard. Give the sprite host
  Grid a fixed Height so layout does not jitter while fetching.
- XAML layout trap: a horizontal StackPanel gives children unlimited width —
  never put wrapping text content inside one (SpeciesDetailWindow splits
  HeaderRoot/ContentRoot for this reason). Equal-width chart columns use a
  Grid with `*` columns (star sizing needs a finite-width parent — true
  inside the tab).
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
- DayStamp/axis tests assume stable ICU weekday abbreviations across .NET
  updates (see Hard-won Context).

## Acceptance Criteria
- [ ] Slice confirmed with the user and implemented per its own plan
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (376 + new)
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
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen handoff
  (full trap list, packaging/verification details)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session
  continuity entries (M0–M14)

## Start Prompt
```text
Read docs/handoff/2026-09-28-milestone-15-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project was reopened for UI parity polish; M12 (sprites + localization), M13 (dex tile grid) and M14 (usage trend chart + model breakdown) are done.
First confirm the M15 slice with the user (settings window, shiny/evolution visuals, resizable window, or their own pick — see Decision Needed at Start).
Then implement that slice only, prove pure parts with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle other work into the slice.
```
