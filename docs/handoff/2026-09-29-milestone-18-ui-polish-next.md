# Handoff — Milestone 18: next UI-polish slice (confirm choice first)

## Goal
The project was closed into maintenance mode at M11, then reopened by the user
for UI parity polish with the macOS original. M12 (sprites + localization,
0.11.0), M13 (dex tile grid, 0.12.0), M14 (usage trend chart, 0.13.0), M15
(settings window, 0.14.0), M16 (per-form Unown sprites, 0.15.0) and M17
(new-version notifications, 0.16.0) are done and user-verified. M18 is the
next slice from the remaining candidates — confirm the choice with the user
FIRST (see Decision Needed at Start); do not start scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: M0–M9 unchanged (`f763205`…`b31cb0e`) · `a340745` docs M10
  handoff · `97f07c6` combat details (M10) · `0d4df7b` docs M11 handoff ·
  `7cd9f73` docs closure/maintenance · `eb3ce33` localization + sprites (M12) ·
  `da3f369` docs M13 handoff · `3685768` dex tile grid (M13) · `51ed5e8` docs
  M14 handoff · `d8f6d85` usage trend chart (M14) · `7685848` docs M15 handoff ·
  `d0fe59a` settings window (M15) · `3a9e908` docs M16 handoff · M16 feat +
  docs (`f367f6f`, `be71d9d`) · M17 feat + docs (see git log)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M17: new-version notifications (port of the macOS `UpdateChecker`),
  version 0.16.0 published and user-verified (manual checklist on this
  machine; the installed instance was relaunched after publishing).
- Core: `src/Core/VersionText.cs` — `IsNewer(a, b)` numeric per-dot compare
  (missing segments = 0, non-numeric segments = 0) and `Normalize` (strips ONE
  leading `v`/`V`). Faithful port of macOS `isNewer` + the `consider` tag
  strip; unit-tested in `Tests/Core.Tests/VersionTextTests.cs`.
- Application: `src/Application/UpdateChecker.cs` — state machine mirroring
  macOS: `Available` (banner target), `Skipped` (hidden from banner, still
  offered in Settings), `UpdateTarget = Available ?? Skipped`,
  `SettingsNotice` (Offer/Skipped/Current), `Changed` event. `CheckAsync(
  minIntervalSeconds = 1800)` debounces via an injected clock (failures count
  toward the interval, like macOS), runs the fetch on the thread pool, parses
  `tag_name` + `html_url` from GitHub `releases/latest`, and only accepts
  https + github.com URLs (`IsSafeReleaseUrl`). `Consider(tag, url)`,
  `SkipCurrent()`, `ShowSkippedAgain()` mutate state under a lock and persist
  the skip through injected `Read/WriteSkippedVersion` delegates. Every
  fetch/parse/network failure is silent. NO auto-download — apply means
  opening the release page in the browser.
- Platform.Windows: `AppSettings` + settings.json gained
  `skippedUpdateVersion` (string?, null round-trips = unskipped) and
  `updateNotificationsEnabled` (default true).
- UI: `App` creates the checker at startup (`CurrentVersion` from the
  assembly version), runs a debounced check on startup and on every
  `ShowDashboard`, and shows a tray balloon ONLY for automatic checks when a
  NEW available version appears (in-memory `_announcedUpdate` dedupe; manual
  checks never balloon). `DashboardWindow` has a banner row above the tabs
  (Grid row 0; accent-tinted Border, `UpdateAvailable` text, Skip/Update
  buttons; visible only when `Available != null && notifications enabled`).
  `SettingsWindow` (height 560 → 640) has an "업데이트" section: notifications
  toggle, "지금 확인" button (disabled + "…" while checking; result row
  afterwards: offer = UpdateFound + Update button, skipped = SkippedVersion +
  Show again + Update, current = UpToDate in gray). Install/Skip/Show-again
  go through App methods (`SkipCurrentUpdate`, `OpenReleasePage`,
  `ApplyUpdateNotifications`, `ManualUpdateCheckAsync`); windows reach App
  only via guarded `Application.Current is App` checks so headless tests work.
- Strings: 11 new `DashboardText` members × 7 languages copied verbatim from
  macOS `Localization.swift` lines 916-944 (`UpdateAvailable`, `UpdateButton`,
  `SkipThisVersion`, `SkippedVersionText`, `ShowSkippedAgain`,
  `UpdateSectionTitle`, `UpdateNotificationsLabel`, `CheckForUpdatesLabel`,
  `CheckNowButton`, `UpdateFound`, `UpToDate`). The macOS `updating` string
  was deliberately NOT ported (no auto-update on Windows).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 430 green (390 + 40 new:
  14 VersionText, 1 DashboardText update-strings, 15 UpdateChecker, 4
  AppSettings update fields) and a 22-check headless UI smoke (banner exists
  and stays collapsed headless, ko/en localization of every new element,
  settings combo switch re-localizes + switches engine language; harness at
  `%TEMP%\opencode\ptb-m17-smoke` — recreate if needed, temp is disposable).
- Real-instance check: published 0.16.0 via `scripts\publish-windows.ps1`,
  relaunched, settings.json round-trips the new fields (a pet-enabled startup
  save triggers the write), diagnostics log clean — the API 403 is swallowed
  silently as designed.

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
  the Ui csproj, bumped with the slice that ships it (0.16.0 = M17; M18
  ships 0.17.0). GitHub Releases are uploaded by hand (no release
  automation); releases use `v<Version>` tags (confirmed 2026-09-29) — the
  update checker is wired to `Strongorange/PokeTokenBar-windows`.
- App state/sprites/diagnostics under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides). Local preferences go in `AppSettingsFile`
  (settings.json) — difficulty, pet, scan roots, and since M17
  `skippedUpdateVersion` + `updateNotificationsEnabled`. Language is the
  deliberate exception: it is saved-game state (import/export carries it),
  set via `CompanionEngine.SetLanguage`, never via settings.json.
- M17 update behavior is FINAL for the slice: release-page-open only, no
  auto-download or self-replacement (macOS brew path does not apply). If the
  user later wants auto-update, that is its OWN slice — do not slip it in.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
Remaining UI-parity candidates, in rough value order (each is its own slice):
- Shiny banner / evolution-line visuals (macOS parity in the game tab).
- Resizable dashboard window (currently fixed 580x720, `CanMinimize`).
- Or the user's own pick (that is how M17 came about — a second user started
  using the app).

## Contracts
- Engine (unchanged in M17): `ApplyUsage(...)`, shop/candy/mint/difficulty
  members, `SetLanguage(AppLanguage)`, `View()` → `CompanionGameView` (ends
  with `UnownForms`, `AppLanguage Language`), `Detail(speciesID)` →
  `CompanionDetailSnapshot?`, `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`.
- UpdateChecker (M17): `UpdateCheckerOptions { CurrentVersion, Repository,
  Fetch: Func<Uri,string?>, Clock, Read/WriteSkippedVersion }`;
  `Available`/`Skipped`/`UpdateTarget`/`SettingsNotice`/`CurrentVersion`;
  `CheckAsync(minIntervalSeconds)` (0 = manual bypass);
  `Consider(tag, url)` / `SkipCurrent()` / `ShowSkippedAgain()`;
  static `IsSafeReleaseUrl(string)`. State transitions raise `Changed`.
- Settings: `AppSettings` local prefs now include `SkippedUpdateVersion`
  (string?) and `UpdateNotificationsEnabled` (bool, default true);
  `SettingsWindow` reaches App only via guarded `Application.Current is App`
  checks so headless tests work.
- New fixed UI strings go in `src/Core/DashboardText.cs` (copy the macOS
  translation when the concept exists in `Localization.swift`).
- Sprite rendering goes through `SpriteSlot` (cached-first; never block the
  UI thread on fetch). Static thumbnails >44px set
  `RenderOptions.SetBitmapScalingMode(NearestNeighbor)`.
- Snapshot: schema 2 shape; loader keeps accepting schema 1. Regenerate only
  via `scripts/generate-pokemon-snapshot.ps1` under pwsh.
- Packaging: version = Ui csproj `<Version>`; publish via
  `scripts/publish-windows.ps1` (kills the installed app and does NOT
  relaunch it — start the exe yourself after publishing).

## Relevant Files
- `Sources/PokeTokenBar/Core/UpdateChecker.swift` — macOS reference (kept in
  sync for any future checker change; note its brew auto-update path is
  intentionally not ported)
- `src/Core/VersionText.cs` + `Tests/Core.Tests/VersionTextTests.cs`
- `src/Application/UpdateChecker.cs` + `Tests/Application.Tests/UpdateCheckerTests.cs`
  (fake fetch/clock/skip-store Harness pattern)
- `src/Core/DashboardText.cs` + `Tests/Core.Tests/DashboardTextTests.cs`
  (`UpdateStringsFollowMacOSTranslations` asserts all 7 languages)
- `src/Platform.Windows/AppSettingsFile.cs` +
  `Tests/Platform.Windows.Tests/AppSettingsFileTests.cs`
- `src/Ui/App.xaml.cs` (checker wiring, balloon dedupe, `AutoCheckUpdateAsync`,
  `OpenReleasePage`), `src/Ui/DashboardWindow.xaml`/`.cs` (banner row Grid 0,
  `RefreshUpdateBanner`), `src/Ui/SettingsWindow.xaml`/`.cs` (update section,
  `RenderUpdateResult`)
- `docs/windows-port-plan.md` — "Reopened: UI parity polish" section
- `docs/handoff/2026-09-29-milestone-17-update-notifications.md` — this
  slice's kickoff handoff, `docs/handoff/2026-09-28-milestone-11-next-or-close.md`
  (pre-reopen handoff with the full trap list — still canonical)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 430 tests) — wrong
  file error `MSB1009` if you type `.sln`. Kill any DEBUG `PokeTokenBar.exe`
  before testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- GitHub API from this machine WITHOUT a token got HTTP 403 rate-limit on
  2026-09-29 (still 403 at M17 close; shared-IP exhaustion). The checker
  treats non-200 as "silently nothing" — a failed manual check therefore
  shows "최신 버전이에요" (macOS has the same flaw; ported faithfully). For
  manual testing prefer a seeded fake fetch or wait out the limit. `gh` CLI
  is NOT authenticated on this machine.
- Releases: only `v0.10.0` exists (`git ls-remote --tags origin`); 0.11–0.16
  were never uploaded. The checker is dormant-but-correct until the user
  uploads a release newer than the installed version. End-to-end banner test
  = upload a temporary `v0.17.0`-style release, see balloon + banner, delete.
- xunit 2.9.3: `Assert.NotNull` returns void — `var x = Assert.NotNull(y)`
  fails with CS0815; assign first, assert second.
- C# primary-constructor classes cannot reference `this` (instance fields
  like a skip-store dictionary) from field initializers (CS0236), and
  `dict[k] = v` cannot be an expression-bodied lambda (CS0815, void). Plain
  constructors + statement lambdas work.
- Balloon dedupe (`_announcedUpdate`) is in-memory per session: a pending
  update re-balloons once per app start until updated or skipped — intended.
- Engine-localized texts make text-asserting tests culture-dependent: pin
  `engine.State.Language = AppLanguage.En` in tests.
- Minimal-snapshot tests: `BuildEngine()` uses
  `PokemonLineSourceTests.MinimalSnapshot` which has NO details and NO
  species 201 — detail tests need `PokemonLineSourceTests.CombatSnapshot`
  (species 1/2/3 with details) or the M16 `UnownSnapshot` const in
  `CompanionEngineTests` (species 201 + details; reuse it).
- `dotnet run file.cs` seeding with `#:project <abs path>` works on this
  machine. `CompanionEngine.ImportSave` writes the state file BUT fails with
  `SaveTransferException: BackupFailed` if the target directory does not
  exist — create it first.
- Headless UI smoke pattern (M13-M17): temp csproj outside the repo
  (`%TEMP%\opencode\ptb-m17-smoke\`) referencing `src/Ui`, `[STAThread]
  Main`, `new Application()`, windows constructed with an engine whose
  StateFilePath is under %TEMP%. Access x:Named elements cross-assembly via
  `window.FindName("...")` (generated fields are internal). WPF
  `RaiseEvent(MouseLeftButtonUp)` on a Border fires its handler headless —
  good for click simulation. `SpriteStore(directory, _ => null)` keeps the
  smoke offline. `SettingsWindow.Relocalize` is private — drive language
  through the LanguageCombo SelectedIndex instead.
- PTB_STATE_DIR sandbox for manual user verification: seed a state dir,
  `Start-Process <exe> -Environment @{PTB_STATE_DIR = <dir>}`, kill the real
  instance first so the tray has ONE icon, restart the real instance after.
- WPF traps still true: value-changed handlers must null-guard (XAML parse
  order — the new notifications CheckBox follows the `_updating || _engine is
  null` pattern), TabItem content is lazily realized, WrapPanel in a ListBox
  needs `HorizontalScrollBarVisibility="Disabled"`, horizontal StackPanel
  gives children unlimited width (never wrap text inside one; the settings
  result row uses a DockPanel for this reason), shiny star overlays go INSIDE
  the sprite Grid.
- Hardcodet tray events bypass `DispatcherUnhandledException` — wrap
  tray-sourced handler bodies entirely in try/catch. Balloon API:
  `ShowBalloonTip(title, message, BalloonIcon)`.
- XamlAnimatedGif: `AnimationBehavior.SetSourceStream` + keep the stream
  referenced (SpriteSlot does). Static PNG fallback exists for GIF failure.
- Install dir is wiped on every publish. Sprite disk cache grows unbounded by
  design. Pet window is manual-checklist only.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI at runtime for BASE DATA, parsing `wsl.exe -l` output, version in
  the tray tooltip, opening WSL SQLite dbs over UNC directly (locked),
  ListBox-level MouseDoubleClick, 40px tile sprites (64px + NearestNeighbor
  passed), auto-download/self-update in M17 (release-page-open only).

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the M18 choice with the user before building anything.
- Evidence: unit tests for pure parts; UI behavior manual by the user; the
  `PTB_STATE_DIR` sandbox + headless smoke harness for layout-level checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- GitHub unauthenticated rate limit (40/hr/IP) — startup + 30-min debounce
  keeps usage trivial, but the 2026-09-29 403 shows shared-IP exhaustion is
  real; failures are invisible to the user by design (and masquerade as
  "up to date" in the manual check, same as macOS).
- Hand-uploaded releases may lag the installed version (0.11–0.16 were never
  uploaded) — the checker stays dormant until the user adopts release
  uploads; a `v<Version>`-tagged release ABOVE the installed version is
  required to see the banner/balloon.
- macOS parity slices can balloon — keep M18 to one confirmed slice.
- XamlAnimatedGif is hobby-maintained — static PNG fallback exists.
- Language lives in the save: an imported save switches language with its
  data — intended, but worth remembering when a user reports a "surprise"
  language change after import.
- DayStamp/axis tests assume stable ICU weekday abbreviations across .NET
  updates (see Hard-won Context in the M14-M16 handoffs).

## Acceptance Criteria
- [ ] M18 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (430 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (re-publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.17.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (closure + reopened UI polish)
- `docs/handoff/2026-09-29-milestone-17-update-notifications.md` — M17 kickoff
- `docs/handoff/2026-09-28-milestone-16-ui-polish-next.md` — M16 handoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen handoff
  (full trap list, packaging/verification details)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session
  continuity entries (M0–M17)

## Start Prompt
```text
Read docs/handoff/2026-09-29-milestone-18-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M17 are done (0.16.0 shipped,
update notifications live). First confirm the M18 slice with the user (shiny
banner/evolution-line visuals, resizable dashboard window, or their own pick),
then implement that slice only, prove pure parts with tests, run the manual
checklist, and verify with dotnet test PokeTokenBar.Windows.slnx.
Do not bundle other work into the slice.
```
