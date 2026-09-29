# Handoff — Milestone 19: next UI-polish slice (confirm choice first)

## Goal
The project was closed into maintenance mode at M11, then reopened by the
user for UI parity polish with the macOS original. M12 (sprites +
localization, 0.11.0), M13 (dex tile grid, 0.12.0), M14 (usage trend chart,
0.13.0), M15 (settings window, 0.14.0), M16 (per-form Unown sprites,
0.15.0), M17 (new-version notifications, 0.16.0) and M18 (evolution-line
sprites + ✨ shiny markers + README rewrite, 0.17.0) are done and
user-verified. M19 is the next slice from the remaining candidates —
confirm the choice with the user FIRST (see Decision Needed at Start); do
not start scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2
  machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR
  flow used so far)
- Commits so far: M0–M9 unchanged (`f763205`…`b31cb0e`) · M10–M17 see
  `git log --oneline -15` · `412b6a3` docs M18 handoff · `1bfa863` feat M18
  (evolution line + shiny markers) · docs M18 close-out commit (READMEs +
  plan + THIS handoff) directly on top
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M18 task 1: README rewrite. Root `README.md` / `README.ko.md` are
  now Windows-reality docs (tray + dashboard, Claude Code / Codex / OpenCode
  reading Windows + WSL logs, Releases exe install with SmartScreen note,
  `%LOCALAPPDATA%\PokeTokenBar` + `PTB_STATE_DIR`, seven languages, game
  loop incl. Unown 28 forms, M17 update notifications, data-sources and
  privacy tables, "Differences from the macOS original" section, fork
  attribution to chattymin/PokeTokenBar, badges = this repo's release +
  Win10/11 + .NET 10 + MIT, `assets/icon.png` header only — no screenshots).
  `README.ja.md` deleted; ja cross-links dropped. `CONTRIBUTING*` /
  `RELEASE.md` / `Package.swift` deliberately untouched (still macOS
  leftovers — `CONTRIBUTING.ja.md` and `RELEASE.md` reference the deleted
  README.ja.md; known, accepted).
- Done — M18 task 2: evolution-line visuals + shiny sparkle parity (the
  user's pick from the candidates). Version 0.17.0 published via
  `scripts\publish-windows.ps1` and relaunched; user visually confirmed the
  sprite row in the game tab (their live mon, base 436, 2-stage line).
  Shiny ✨ behavior was NOT verified live (user has no shiny mon — their
  one shiny hatch, base 594, was discarded via egg purchase on 09-23);
  verified headless instead, see Evidence.
- Engine: `CompanionGameView` gained `IReadOnlyList<EvoLineItem> EvoLine`
  right after `StageItems` (record still ends `UnownForms, Language` —
  keep that). `BuildEvoLineItems` in `CompanionEngine.cs` mirrors
  `BuildStageItems`: realized path → Done/Current with real species IDs,
  single-child chain → Future, branch point (>1 child) → ONE Mystery item
  (macOS `lineNodes` parity; branches never fan out). Uses the Core
  `EvoLineItem`/`EvoLineItemContent`/`EvoLineItemState` records from
  `src/Core/Evolution.cs` that had existed UNUSED since the initial port —
  they are now load-bearing. `StageItems` (labels) kept for tests/back-compat.
- UI: the game tab's text chain `StageLine` (`A → B → C`) is GONE. In its
  place `EvolutionScroll` (ScrollViewer, horizontal Auto) wrapping
  `EvolutionLine` (horizontal StackPanel) below the progress bar. Each cell
  (`CreateEvolutionCell` in DashboardWindow.xaml.cs): 40px static sprite via
  `SpriteSlot.Update(store, id, animated:false, shiny:view.IsShiny, form:
  view.ActiveUnownForm, "❔")`, mystery → bold gray `?` (FontSize 22,
  ToolTip `UnknownNextEvolution`), future → `Opacity 0.32` (macOS uses
  0.32 opacity + 0.4 saturation; WPF got opacity only), current → 4px
  accent-dot `Ellipse` (`#0078D4`) under the sprite (Hidden not Collapsed
  so layout is stable), `→` TextBlock arrows between cells. Egg → line
  hidden. Whole line uses shiny sprites when `view.IsShiny` (Unown form
  passed through). `RenderEvolutionLine` is called from BOTH branches of
  `UpdateGame` (active/egg).
- Progress caption: at the last stage (`StageIndex + 1 >= TotalForms > 0`)
  the label prefix becomes `DashboardText.FinalForm` ("최종 진화체" /
  "Final form") instead of `StageLabel` ("단계 i/k") — macOS `stage`/
  `finalForm` behavior.
- Shiny markers switched from gray `★` to `✨` everywhere: companion name
  suffix (`" ✨"`, ToolTip `ShinyLabel`), dex tile caption star (ToolTip
  `ShinyLabel`), SpeciesDetailWindow individual headers, Unown form-thumb
  overlay + thumb tooltip.
- Strings: `DashboardText.FinalForm` + `UnknownNextEvolution` added
  macOS-verbatim (7 languages, Localization.swift 667/669); `ShinyLabel`
  values corrected to macOS `dexShinyLabel` capitalization (en "Shiny", es
  "Variocolor", fr "Chromatique", de "Schillernd"; previously lowercase and
  dormant/unused — now wired to the ✨ tooltips).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 435 green (430 + 5:
  4 engine EvoLine tests — 3-stage states, final stage, branch→mystery via
  new `BranchSnapshot` const (base 133 → children 134/135), egg-empty; 1
  DashboardText evolution/shiny strings test) and a 19-check headless UI
  smoke (`%TEMP%\opencode\ptb-m18-smoke\Program.cs` — recreate if needed,
  temp is disposable): row visible/hidden, 3 cells + 2 arrows, arrow glyph,
  future 0.32 + done/current 1.0, current dot visible / other hidden,
  final-form caption en+ko, egg hides + clears, branch cell+arrow+mystery,
  `?` glyph + tooltip en/ko, ✨ name suffix + tooltip, dex tile ✨ + tooltip.
- Real-instance check: 0.17.0 published, relaunched, diagnostics log clean
  (only normal game events; the M17-era API 403 stays swallowed).

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
  the Ui csproj, bumped with the slice that ships it (0.17.0 = M18; M19
  ships 0.18.0). GitHub Releases uploaded by hand; `v<Version>` tags; the
  update checker is wired to `Strongorange/PokeTokenBar-windows`.
- App state/sprites/diagnostics under `%LOCALAPPDATA%\PokeTokenBar`
  (`PTB_STATE_DIR` overrides). Local preferences in settings.json —
  difficulty, pet, scan roots, skippedUpdateVersion,
  updateNotificationsEnabled. Language is saved-game state (import/export
  carries it), set via `CompanionEngine.SetLanguage`, never settings.json.
- README policy (from the M18 rewrite): en + ko only, no macOS screenshots
  (Windows captures may come in a later pass), fork attribution and this
  repo's badges. Do not "restore" ja or sponsor/trendshift badges.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
At M18 close (2026-09-29) the user volunteered: "전체적으로 UI가 좀 너무
임시티가 많이 난다 — macos 버전은 좀 이쁜데" (the overall UI looks rough
next to the pretty macOS original). So the LEADING candidate for M19 is a
general visual-quality pass over the dashboard/settings/detail windows
(cards, spacing, typography, colors, window chrome — toward macOS polish).
Others, each its own slice:
- Hatch/evolve celebration animation (macOS: white flash + spring pop +
  delayed ✨ burst on shiny hatch).
- Resizable dashboard window (fixed 580x720, `CanMinimize` today).
- Or the user's own pick (that is how M17 and M18 came about).
If the visual-quality pass is chosen, scope it EXPLICITLY with the user
first (which surfaces, what "pretty" means — macOS reference is
`Sources/PokeTokenBar/UI/*.swift`) and keep it one slice.

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `SetLanguage(AppLanguage)`, `View()` → `CompanionGameView` (…,
  `StageItems`, **`EvoLine`** = `IReadOnlyList<EvoLineItem>`, `DexRows`,
  …, ends `UnownForms`, `AppLanguage Language`), `Detail(speciesID)` →
  `CompanionDetailSnapshot?`, `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`.
- `EvoLineItem` (Core, readonly record struct): `Content` =
  `EvoLineItemContent(Species(id) | Mystery())`, `State` =
  Done | Current | Future. UI contract: mystery renders `?`, future dims,
  current gets the accent dot, whole line shiny when `view.IsShiny`.
- UpdateChecker (M17, unchanged): `UpdateCheckerOptions`, `Available`/
  `Skipped`/`UpdateTarget`/`SettingsNotice`, `CheckAsync`, `Consider`/
  `SkipCurrent`/`ShowSkippedAgain`, static `IsSafeReleaseUrl`.
- Settings: `AppSettings` incl. `SkippedUpdateVersion`, 
  `UpdateNotificationsEnabled`; windows reach App only via guarded
  `Application.Current is App` checks so headless tests work.
- New fixed UI strings go in `src/Core/DashboardText.cs` (copy the macOS
  translation when the concept exists in `Localization.swift`; macOS t()
  argument order ko/en/ja/es/fr/pt/de matches `DashboardText.T`).
- Sprite rendering goes through `SpriteSlot` (cached-first; never block the
  UI thread on fetch). Static thumbnails >44px set
  `RenderOptions.SetBitmapScalingMode(NearestNeighbor)` (40px evo cells and
  28px Unown thumbs use default interpolation — intentional).
- Snapshot: schema 2 shape; loader keeps accepting schema 1 (test snapshots
  are schema 1). Regenerate only via
  `scripts/generate-pokemon-snapshot.ps1` under pwsh.
- Packaging: version = Ui csproj `<Version>`; publish via
  `scripts/publish-windows.ps1` (kills the installed app and does NOT
  relaunch it — start the exe yourself after publishing).

## Relevant Files
- `Sources/PokeTokenBar/UI/CompanionView.swift` — macOS reference for
  EvoLineView (282–483), CompanionHeader shiny ✨ (555), celebration
  (643–669); `Sources/PokeTokenBar/Core/CompanionStore.swift` `lineNodes`
  (269–297) — keep in sync for future line changes
- `src/Core/Evolution.cs` — `EvoLineItem` types + `EvoLine.KeepingAnimatedSprites`
  (all species 1–649 count as animated, so test snapshots can use any ids
  in that range for branching trees)
- `src/Application/CompanionEngine.cs` — `CompanionGameView` record,
  `BuildView`, `BuildStageItems`, `BuildEvoLineItems`
- `src/Ui/DashboardWindow.xaml`/`.cs` — `EvolutionScroll`/`EvolutionLine`,
  `RenderEvolutionLine`/`CreateEvolutionArrow`/`CreateEvolutionCell`,
  `UpdateGame` (final-form caption, ✨ marker), `CreateDexTile` (✨)
- `src/Ui/SpeciesDetailWindow.xaml.cs` — ✨ in individuals/thumbs
- `src/Core/DashboardText.cs` — `FinalForm`, `UnknownNextEvolution`,
  `ShinyLabel` + Tests/Core.Tests/DashboardTextTests.cs
- `Tests/Application.Tests/CompanionEngineTests.cs` — EvoLine tests +
  `BranchSnapshot` const next to `UnownSnapshot`
- `README.md` / `README.ko.md` — the new Windows READMEs (see Current
  State task 1)
- `docs/windows-port-plan.md` — "Reopened: UI parity polish" section
- `docs/handoff/2026-09-29-milestone-18-ui-polish-next.md` — this slice's
  kickoff handoff (README decisions live there too),
  `docs/handoff/2026-09-28-milestone-11-next-or-close.md` (pre-reopen
  handoff with the full trap list — still canonical)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 435 tests) — wrong
  file error `MSB1009` if you type `.sln`. Kill any DEBUG `PokeTokenBar.exe`
  before testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- DashboardWindow.xaml.cs has `using System.IO` — adding
  `using System.Windows.Shapes` makes bare `Path` ambiguous (CS0104). Use
  fully-qualified `System.Windows.Shapes.Ellipse` etc. instead of the using.
- Headless smoke `dotnet run Program.cs` with `#:project <Ui csproj>`:
  the file-based app defaults to net10.0 → NU1201 against
  net10.0-windows; add `#:property TargetFramework=net10.0-windows` at the
  top, and `using PokeTokenBar.Ui;` for the window types.
- Headless UI smoke pattern (M13-M18): temp csproj outside the repo
  (`%TEMP%\opencode\ptb-m18-smoke\`), `[STAThread] Main`,
  `new Application()`, `new DashboardWindow(engine, store)` with an engine
  whose StateFilePath is under %TEMP%; drive state directly
  (`engine.State.Active = new MonState(base, [path], null, stageIndex, used,
  rarity, totalForms, isShiny: …)`), call `window.UpdateGame(engine.View())`,
  read elements via `window.FindName(...)`. Re-render recreates elements —
  re-FindName after every UpdateGame before asserting on children.
  `new SpriteStore(dir, _ => null)` keeps it offline.
- GitHub API from this machine WITHOUT a token is 403 rate-limited
  (shared-IP exhaustion, true since 2026-09-29). Manual update-check tests
  masquerade as "up to date" — same as macOS. Prefer seeded fakes. `gh` CLI
  is NOT authenticated here.
- Releases: only `v0.10.0` exists (`git ls-remote --tags origin`); 0.11–0.17
  were never uploaded. The checker stays dormant-but-correct until a
  release ABOVE the installed version goes up.
- xunit 2.9.3: `Assert.NotNull` returns void (CS0815); C# primary
  constructors cannot use `this` in field initializers (CS0236) and
  `dict[k] = v` is not expression-bodied (CS0815).
- Minimal-snapshot tests: `PokemonLineSourceTests.MinimalSnapshot` has NO
  details and NO species 201; detail tests need `CombatSnapshot` (species
  1/2/3 with details) or `CompanionEngineTests.UnownSnapshot` (201). NEW:
  `CompanionEngineTests.BranchSnapshot` (133→134/135) for branch/mystery
  behavior. Fake ids must stay within 1–649 (see KeepingAnimatedSprites
  note under Relevant Files).
- `dotnet run file.cs` seeding with `#:project <abs path>` works.
  `CompanionEngine.ImportSave` fails with `SaveTransferException:
  BackupFailed` if the target directory does not exist — create it first.
- PTB_STATE_DIR sandbox for manual user verification: seed a state dir,
  `Start-Process <exe> -Environment @{PTB_STATE_DIR = <dir>}`, kill the real
  instance first so the tray has ONE icon, restart the real instance after.
- WPF traps still true: value-changed handlers must null-guard (XAML parse
  order), TabItem content is lazily realized, WrapPanel in a ListBox needs
  `HorizontalScrollBarVisibility="Disabled"`, horizontal StackPanel gives
  children unlimited width (never wrap text inside one), shiny star
  overlays go INSIDE the sprite Grid, keep the evo-line current-stage dot
  `Hidden` (not `Collapsed`) so cell widths stay stable.
- Hardcodet tray events bypass `DispatcherUnhandledException` — wrap
  tray-sourced handler bodies entirely in try/catch. Balloon API:
  `ShowBalloonTip(title, message, BalloonIcon)`.
- XamlAnimatedGif: `AnimationBehavior.SetSourceStream` + keep the stream
  referenced (SpriteSlot does). Static PNG fallback exists for GIF failure.
- Install dir is wiped on every publish. Sprite disk cache grows unbounded
  by design. Pet window is manual-checklist only.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI at runtime for BASE DATA, parsing `wsl.exe -l` output, version in
  the tray tooltip, opening WSL SQLite dbs over UNC directly (locked),
  ListBox-level MouseDoubleClick, 40px tile sprites (64px + NearestNeighbor
  passed), auto-download/self-update in M17 (release-page-open only),
  macOS-style saturation-dimming for future evo stages (opacity-only on
  WPF, accepted).

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the M19 choice with the user before building anything.
- Evidence: unit tests for pure parts; UI behavior manual by the user; the
  `PTB_STATE_DIR` sandbox + headless smoke harness for layout-level checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- Shiny ✨ visuals were never seen live by the user (no shiny mon at M18
  close; headless smoke covers the logic). First real shiny hatch is the
  field test — 1/64 (1/48 with charm) odds.
- A visual-quality pass (the leading M19 candidate) has NO objective
  done-line — scope surfaces + acceptance examples with the user up front
  or it will balloon; the M13 40px-tile rejection shows visual taste needs
  an early user look (iterate small, publish, look, adjust).
- GitHub unauthenticated rate limit (40/hr/IP) — startup + 30-min debounce
  keeps usage trivial; failures are invisible by design (and masquerade as
  "up to date" in the manual check, same as macOS).
- Hand-uploaded releases may lag the installed version (0.11–0.17 were
  never uploaded) — the checker stays dormant until the user adopts release
  uploads; a `v<Version>`-tagged release ABOVE the installed version is
  required to see the banner/balloon.
- XamlAnimatedGif is hobby-maintained — static PNG fallback exists.
- Language lives in the save: an imported save switches language with its
  data — intended, but worth remembering when a user reports a "surprise"
  language change after import.
- DayStamp/axis tests assume stable ICU weekday abbreviations across .NET
  updates (see Hard-won Context in the M14-M16 handoffs).

## Acceptance Criteria
- [ ] M19 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (435 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (re-publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.18.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (closure + reopened UI polish)
- `docs/handoff/2026-09-29-milestone-18-ui-polish-next.md` — M18 kickoff
  (README scoping decisions)
- `docs/handoff/2026-09-28-milestone-16-ui-polish-next.md` — M16 handoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen handoff
  (full trap list, packaging/verification details)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session
  continuity entries (M0–M18)

## Start Prompt
```text
Read docs/handoff/2026-09-29-milestone-19-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M18 are done (0.17.0 shipped,
evolution-line sprites + shiny sparkle markers live, READMEs rewritten).
The user hinted the overall UI looks rough next to the macOS original —
confirm the M19 slice with them FIRST (leading candidate: a general
visual-quality pass over dashboard/settings/detail windows; alternatives:
hatch/evolve celebration animation, resizable dashboard window, or their own
pick). If the visual pass is chosen, scope surfaces and acceptance with the
user before coding. Implement that slice only, prove pure parts with tests,
run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx.
Do not bundle other work into the slice.
```
