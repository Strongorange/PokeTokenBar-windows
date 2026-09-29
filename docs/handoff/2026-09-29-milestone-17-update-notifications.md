# Handoff — Milestone 17: new-version notifications (port of macOS UpdateChecker)

## Goal
The project was reopened for UI parity polish after maintenance mode. M12
(sprites + localization, 0.11.0), M13 (dex tile grid, 0.12.0), M14 (usage
trend chart, 0.13.0), M15 (settings window, 0.14.0) and M16 (per-form Unown
sprites, 0.15.0) are done and user-verified. M17 is CONFIRMED with the user
(2026-09-29): new-version notifications. The macOS original ships a full
`UpdateChecker` (`Sources/PokeTokenBar/Core/UpdateChecker.swift`) that was
never ported; someone else will start using the app, which motivated this.
Port it adapted to Windows — do NOT redesign.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: M0–M9 unchanged (`f763205`…`b31cb0e`) · `a340745` docs M10
  handoff · `97f07c6` combat details (M10) · `0d4df7b` docs M11 handoff ·
  `7cd9f73` docs closure/maintenance · `eb3ce33` localization + sprites (M12) ·
  `da3f369` docs M13 handoff · `3685768` dex tile grid (M13) · `51ed5e8` docs
  M14 handoff · `d8f6d85` usage trend chart (M14) · `7685848` docs M15 handoff ·
  `d0fe59a` settings window (M15) · `3a9e908` docs M16 handoff · M16 feat +
  docs commits (see git log)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M16: per-form Unown sprites, version 0.15.0 published and
  user-verified (sandbox visual check on this machine).
- Dex grid: Unown keeps ONE tile with the default A sprite; the tile name
  shows the plain localized name plus " n/28"
  (`UnownFormsCollected` count; tile tooltip appends the same string). The
  active Unown dex row name no longer carries the "[B]" suffix
  (`BuildDexRows` uses `EvoLine.LocalizedName` directly for species 201).
- Detail window (`SpeciesDetailWindow`): a 28-form picker grid
  (`UniformGrid`, 7 columns, 28px static thumbs, NearestNeighbor) — owned
  forms full opacity + hand cursor + click selects, shiny forms show a ★
  overlay and a starred tooltip, unowned forms at Opacity 0.25 with a
  "Not collected" tooltip. Selection switches the hero sprite (animated,
  per-form shiny) and re-renders the individuals host filtered to that form.
  Default selection = first collected form. Individuals host is a dedicated
  StackPanel inside ContentRoot; Add* helpers take a target panel.
- Engine: `CompanionUnownFormStatus(UnownForm Form, bool IsShiny)` lists on
  `CompanionGameView.UnownForms` and `CompanionDetailSnapshot.UnownForms`
  (built by `BuildUnownForms()` via `OwnsSpecies`/`OwnsShinySpecies`),
  `CompanionDetailIndividual.UnownForm` per individual
  (`UnownForms.Resolved(speciesID, entry.UnownForm)`).
- New strings in `DashboardText`: `UnownFormsCollected(lang, n)` ("안농 글자
  n/28" …) and `UnownNotCollected(lang)` ("미수집" …) — copied from macOS
  `Localization.swift` `unownFormsCollected`/`unownNotCollected`.
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 390 green (385 + 5 new:
  3 engine Unown-view/detail tests + non-Unown regression + DashboardText
  unown strings) and a 17-check headless UI smoke (M16 harness lived in
  `%TEMP%\opencode\ptb-m16-smoke` — recreate if needed, temp is disposable).
- Published 0.15.0 via `scripts\publish-windows.ps1`; real instance
  relaunched and left running in the tray.

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
  the Ui csproj, bumped with the slice that ships it (0.15.0 = M16;
  M17 ships 0.16.0). GitHub Releases are uploaded by hand (no release
  automation) — M17 requires the user's releases to use `v<Version>` tags;
  CONFIRM the actual tag format (or that releases exist at all) before
  wiring the check.
- App state/sprites/diagnostics under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides). Local preferences go in `AppSettingsFile`
  (settings.json) — difficulty, pet, scan roots. Language is the deliberate
  exception: it is saved-game state (import/export carries it), set via
  `CompanionEngine.SetLanguage`, never via settings.json. M17: skip-version
  and update-notification toggle are LOCAL prefs → settings.json (macOS uses
  UserDefaults for the same).
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## M17 Scope (confirmed with the user 2026-09-29)
Port of macOS `UpdateChecker`, Windows-adapted:
- Check: GET `https://api.github.com/repos/Strongorange/PokeTokenBar-windows/releases/latest`
  (Accept `application/vnd.github+json`, 15s timeout), read `tag_name` +
  `html_url`; validate the URL is https + github.com before opening it
  (macOS does the same scheme-hijack guard). Debounce: skip if checked within
  30 min (macOS `minInterval = 1800`); manual check bypasses the debounce.
  Trigger on app start (and dashboard open, like macOS popover open) —
  never block the UI thread; silent failure on any error/non-200.
- Semver compare: pure helper in Core (e.g. `VersionText.IsNewer(a, b)`,
  numeric per-dot segments, `v` prefix stripped from tags) with unit tests.
- Surfaces: tray balloon (`ShowBalloonTip(title, message, BalloonIcon)` —
  wrap in try/catch, tray events bypass `DispatcherUnhandledException`) plus
  a dashboard banner row ("새 버전 x.y.z 사용 가능") with Update /
  Skip-this-version actions; a Settings "업데이트" section with a manual
  "업데이트 확인" button (spinner/"최신입니다" states per macOS
  SettingsView) and the update-notifications toggle.
- Skip-this-version: persists in settings.json (`skippedUpdateVersion`),
  hides the balloon/banner but Settings still offers install + "다시 보기".
- Apply: open the release page in the browser (`Process.Start` with
  UseShellExecute on the validated https github.com URL). NO auto-download
  or self-replacement in this slice (macOS brew path does not apply; its
  own fallback is exactly "open the releases page").
- Strings: copy from macOS `Localization.swift` (`updateSectionTitle`,
  `checkForUpdatesLabel`, `updateNotificationsLabel`, `updateButton`,
  `updateAvailable(version:current:)`, `skipThisVersion`, `showSkippedAgain`,
  up-to-date wording — find the exact keys around lines 926-941 and the
  SettingsView/PopoverView usage). All seven languages, `DashboardText`.
- Engine is NOT involved (no save-state change); this slice touches Core
  (compare + strings), Application (checker service, injectable fetch like
  `SpriteStore`'s `Func<Uri, byte[]?>`), Platform.Windows (settings.json
  fields), UI (App wiring + dashboard banner + settings section).

## Contracts
- Engine (unchanged in M17): `ApplyUsage(...)`, shop/candy/mint/difficulty
  members, `SetLanguage(AppLanguage)`, `View()` → `CompanionGameView` (ends
  with `UnownForms`, `AppLanguage Language`), `Detail(speciesID)` →
  `CompanionDetailSnapshot?` (has `UnownForms`, individuals carry `UnownForm`),
  `Changed`, `DrainNotices()`, `ExportSave`/`ImportSave`.
- Settings: `AppSettingsFile` for local prefs including M17 additions
  (`SkippedUpdateVersion`, `UpdateNotificationsEnabled` — exact names TBD by
  the existing settings.json style; `ScanRoots`/`ScanRootEntry` show the
  pattern). `SettingsWindow` reaches App only via guarded
  `Application.Current is App` checks so headless tests work.
- New fixed UI strings go in `src/Core/DashboardText.cs` (copy the macOS
  translation when the concept exists in `Localization.swift`; keep the same
  tone otherwise). UI never hardcodes display strings.
- Sprite rendering goes through `SpriteSlot` (cached-first; never block the
  UI thread on fetch). Static thumbnails >44px set
  `RenderOptions.SetBitmapScalingMode(NearestNeighbor)`.
- Snapshot: schema 2 shape; loader keeps accepting schema 1. Regenerate only
  via `scripts/generate-pokemon-snapshot.ps1` under pwsh.
- Packaging: version = Ui csproj `<Version>`; publish via
  `scripts/publish-windows.ps1` (kills the installed app and does NOT
  relaunch it — start the exe yourself after publishing).

## Relevant Files
- `Sources/PokeTokenBar/Core/UpdateChecker.swift` — THE reference (check
  debounce, tag parse, skip semantics, isNewer, apply fallback)
- `Sources/PokeTokenBar/UI/PopoverView.swift` lines ~89-102 — banner UI;
  `Sources/PokeTokenBar/UI/SettingsView.swift` lines ~330-372 — settings UI
- `Sources/PokeTokenBar/Core/Localization.swift` — update strings source of
  truth (~lines 926-941 plus `updateAvailable`; grep before writing)
- `src/Ui/App.xaml.cs` (startup wiring point, tray balloon, `ApplyLanguage`),
  `src/Ui/SettingsWindow.xaml`/`.cs` (new section), 
  `src/Ui/DashboardWindow.xaml`/`.cs` (banner row + `Relocalize` hook)
- `src/Platform.Windows/AppSettingsFile.cs` + 
  `Tests/Platform.Windows.Tests/AppSettingsFileTests.cs`
- `src/Core/DashboardText.cs` + `Tests/Core.Tests/DashboardTextTests.cs`
  (M16 added `UnownFormStringsFollowMacOSTranslations` — follow that style)
- New: Core version-compare helper + Application `UpdateChecker` service +
  tests (Application.Tests pattern: injectable fetch/clock)
- `docs/windows-port-plan.md` — "Reopened: UI parity polish" section,
  `docs/handoff/2026-09-28-milestone-16-ui-polish-next.md` (this slice's
  predecessor with the M16 detail), `docs/handoff/2026-09-28-milestone-11-next-or-close.md`
  (pre-reopen handoff with the full trap list — still canonical)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~10s, 390 tests) — wrong
  file error `MSB1009` if you type `.sln`. Kill any DEBUG `PokeTokenBar.exe`
  before testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- GitHub API from this machine WITHOUT a token got HTTP 403 rate-limit on
  2026-09-29 (`Invoke-RestMethod` check). The checker must treat non-200 as
  "silently nothing"; for manual testing prefer a seeded fake fetch or wait
  out the limit. `gh` CLI is NOT authenticated on this machine.
- Releases are uploaded by hand; tag format unverified (403 blocked the
  check). Confirm with the user that a `v0.15.0`-style release exists before
  shipping the checker, otherwise the feature is untestable end-to-end.
- Engine-localized texts make text-asserting tests culture-dependent: pin
  `engine.State.Language = AppLanguage.En` in tests.
- Minimal-snapshot tests: `BuildEngine()` uses
  `PokemonLineSourceTests.MinimalSnapshot` which has NO details and NO
  species 201 — detail tests need `PokemonLineSourceTests.CombatSnapshot`
  (species 1/2/3 with details) or the M16 `UnownSnapshot` const in
  `CompanionEngineTests` (species 201 + details; reuse it).
- Seeding dex entries directly in tests/smokes: `DexEntry(201, 201, [201],
  Rarity, caughtAt, isShiny:, profile: PokemonProfile.Generate(n), names:…,
  unownForm:)` — WITHOUT a `names` dictionary the dex row name falls back to
  "#201", so name-asserting harnesses must seed names per language.
- `dotnet run file.cs` seeding with `#:project <abs path>` works on this
  machine (M16 manual-check seed script). `CompanionEngine.ImportSave` writes
  the state file BUT fails with `SaveTransferException: BackupFailed` if the
  target directory does not exist — create it first.
- Headless UI smoke pattern (M13-M16): temp csproj outside the repo
  (`%TEMP%\opencode\ptb-m16-smoke\`) referencing `src/Ui`, STA Main,
  `new Application()`, windows constructed with an engine whose
  StateFilePath is under %TEMP%. Access x:Named elements cross-assembly via
  `window.FindName("...")` (generated fields are internal). WPF
  `RaiseEvent(MouseLeftButtonUp)` on a Border fires its handler headless —
  good for click simulation. `SpriteStore(directory, _ => null)` keeps the
  smoke offline (placeholders stay up).
- PTB_STATE_DIR sandbox for manual user verification: seed a state dir (see
  seed script trick above), `Start-Process <exe> -Environment @{PTB_STATE_DIR
  = <dir>}`, kill the real instance first so the tray has ONE icon, and
  restart the real instance afterwards (Start Menu shortcut works).
- WPF traps still true: value-changed handlers must null-guard (XAML parse
  order), TabItem content is lazily realized (set `MainTabs.SelectedIndex`
  before walking the visual tree), WrapPanel in a ListBox needs
  `HorizontalScrollBarVisibility="Disabled"`, horizontal StackPanel gives
  children unlimited width (never wrap text inside one), shiny star overlays
  go INSIDE the sprite Grid (fixed Height) so layout does not jitter.
- `SpeciesDetailWindow` Add* helpers now take a target StackPanel — keep new
  content going through them; the individuals host is rebuilt by
  `RenderIndividuals()` on form selection.
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
- Slice per milestone; report after the milestone or when blocked. The M17
  choice is already confirmed — do not re-litigate it, but do confirm the
  release tag format question above before finalizing.
- Evidence: unit tests for pure parts (version compare, tag parse, settings
  round-trip, checker state machine with fake fetch/clock); UI behavior
  manual by the user; the `PTB_STATE_DIR` sandbox + headless smoke harness
  for layout-level checks; `dotnet run file.cs` seeding/probe tricks for
  destructive checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- GitHub unauthenticated rate limit (40/hr/IP) — startup + 30-min debounce
  keeps usage trivial, but the 2026-09-29 403 shows shared-IP exhaustion is
  real; failures must be invisible to the user.
- Hand-uploaded releases may not exist yet / tag format may differ — the
  checker is dead weight until the user adopts `v<Version>` tags.
- macOS parity slices can balloon: the macOS checker auto-updates via brew;
  Windows deliberately does not (this slice). If the user later wants
  auto-update, that is its OWN slice (download asset, verify, swap exe
  behind a detached script like macOS does) — do not slip it in here.
- XamlAnimatedGif is hobby-maintained — static PNG fallback exists.
- Language lives in the save: an imported save switches language with its
  data — intended, but worth remembering when a user reports a "surprise"
  language change after import.
- DayStamp/axis tests assume stable ICU weekday abbreviations across .NET
  updates (see Hard-won Context in the M14/M15/M16 handoffs).

## Acceptance Criteria
- [ ] Release tag format confirmed with the user; checker wired to
      `Strongorange/PokeTokenBar-windows`
- [ ] Startup check (debounced) + manual check + skip/undo + notifications
      toggle + balloon/banner + release-page open, all localized (7 langs)
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (390 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (re-publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.16.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (closure + reopened UI polish)
- `docs/handoff/2026-09-28-milestone-16-ui-polish-next.md` — M16 handoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen handoff
  (full trap list, packaging/verification details)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session
  continuity entries (M0–M16)

## Start Prompt
```text
Read docs/handoff/2026-09-29-milestone-17-update-notifications.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M16 are done (0.15.0 shipped).
M17 is already confirmed with the user: port the macOS UpdateChecker as new-version
notifications (GitHub releases/latest check, tray balloon + banner, skip-version,
settings section, release-page open; NO auto-download). First confirm the GitHub
release tag format with the user, then implement that slice only, prove pure parts
with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx.
Do not bundle other work into the slice.
```
