# Handoff — Milestone 8: next slice (scope to confirm: OpenCode / combat details / packaging)

## Goal
Milestones 0–7 of the Windows port are complete (pure core through floating
pet, all personally verified). The canonical plan has one remaining section —
"### Later: OpenCode" — plus one unscheduled nicety (combat-details
enrichment). Daily use currently runs from the Debug build output with no
packaging story. Confirm the slice with the user FIRST (see Decision Needed at
Start); do not start scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `f763205` docs research+fixtures (M0) · `8083930` pure core (M1) ·
  `55b7701` discovery+scan (M2) · `bb422fd` Claude slice (M3) · `113f7e0` Codex
  slice (M4) · `f1108dd` application+tray dashboard (M5) · `1765993`/`59bf551`
  handoff docs · `3a88556` game engine+persistence+dashboard game tab (M6) ·
  `1ac0a36` M6.5 handoff doc · `dce0620` shop/candy/mint/difficulty (M6.5) ·
  `056297c` M7 handoff doc · `6675770` floating pet/sprite cache/notifications (M7)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M7 (`6675770`): Core `SpriteCatalog` (macOS `SpriteLoader`
  cacheKey/URL scheme ported exactly: `25-a.gif`, `25-shs.png`,
  `201-question-a.gif`, egg key `egg`), Core `FloatingPetGeometry`
  (click-threshold 16pt², bubble headroom 72 / min width 180 / padding 8 /
  content 164 / line-limit 2, `PanelSize`, `PanelXInset`), Application
  `SpriteStore` (injected sync fetch, memory LRU 64, disk cache with atomic
  write at `<state-dir>\sprites`, subject fallback order animated→static,
  shiny→normal — macOS `SpriteLoader.image` port), engine notice drain
  (`DrainNotices()` — exactly-once per hatch/evolve/graduate/ditto/release,
  cleared by `ImportSave`; kinds in `NoticeKinds`), `CompanionGameView` gained
  `ActiveSpeciesID`/`ActiveUnownForm` for the pet.
- Pet window (`src/Ui/FloatingPetWindow.xaml(.cs)`): borderless transparent
  topmost, GIF via `XamlAnimatedGif` 2.3.2 `AnimationBehavior.SetSourceStream`
  (stream kept alive), static PNG path otherwise, placeholder 🥚/❔ while
  fetching, sync cached render first + background fetch swap-in (version
  guard), drag with 4pt click-vs-drag threshold, manual double-click
  detection (`user32 GetDoubleClickTime`), double-click → dashboard,
  right-click menu (dashboard / hide), `SystemEvents.DisplaySettingsChanged`
  re-clamp into the virtual screen, position persist on drag end.
- Notifications: tray balloons (`ShowBalloonTip(title, message, BalloonIcon)`)
  from `App.NotifyPendingEvents`, multiple notices joined into one balloon,
  try/catch'd.
- Pet prefs in `settings.json` (Platform.Windows `AppSettings`): `petEnabled`
  (default false), `petX`/`petY` (nullable), `petSize` 48–384 snap-8 default
  96. Surfaces: tray "Floating pet" checkable item + Game tab section
  (checkbox + size slider, guarded with the `_updatingDifficulty` pattern).
- Evidence: 325 tests green (137 Core + 89 Providers + 46 Platform.Windows +
  53 Application) via `dotnet test PokeTokenBar.Windows.slnx`. M7 evidence
  (a)-(d): `SpriteCatalogTests` (URL/key scheme), `SpriteStoreTests`
  (round-trip, static + shiny fallback, >649 guard, egg, LRU eviction),
  `FloatingPetGeometryTests`, notices tests + view sprite coordinates in
  `CompanionEngineTests`, pet prefs round-trip in `AppSettingsFileTests`.
- Manual verification on this machine (2026-09-23, user through the real UI
  against `PTB_STATE_DIR` sandboxes): pet display + GIF animation, drag /
  click / double-click / right-click, size slider + restart persistence,
  evolve balloon (sandbox Whismur near-threshold + candy seeded —
  `Temp\opencode\ptb-m7-smoke\balloon-run.bat` pattern), hardening informal
  pass (multi-monitor, DPI, sleep/wake, WSL — "fine"; no failures observed).
- Automated sandbox smoke proof: seeded Buneary(427) evolved to Lopunny(428)
  from real usage delta while the app ran — pet re-rendered and both sprites
  were fetched/cached without crash, diagnostics log clean.
- Not implemented anywhere yet: OpenCode provider (plan "Later" — needs
  Windows/WSL storage research + sanitized fixtures), combat-details
  enrichment (base stats/abilities/moves), packaging/release.

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns
  paths/settings/diagnostics, Application owns engine + orchestration, UI
  consumes Application only. No MVVM framework.
- M7 start decisions (all taken 2026-09-23): sprite sourcing = online cache +
  GIF with static fallback (macOS parity, option 1); pet = always topmost, NO
  fullscreen-app hiding; notifications = tray balloons (toast package only if
  the user finds them insufficient); hardening = full checklist, no
  combat-details.
- Pokemon base data stays the bundled snapshot (`assets/pokemon-snapshot.json`,
  offline-first); sprites are the separate online-cached concern
  (`SpriteStore`) — keep that split.
- Difficulty + pet prefs live in `settings.json` (Platform.Windows), never in
  the save. macOS divergences (keep-earliest vs keep-max) stay unfixed.
- Never read/copy credentials (`auth.json`, `.credentials.json`,
  `settings.json` secrets). Comments: none unless asked. English commits.
  Commit + push only when the user asks. Git remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- Slice choice (pick one; do not bundle):
  1. OpenCode provider — plan's "Later" section. Gate: Windows/WSL storage
     locations and sanitized fixtures must be established first (research
     spike). Must use the existing provider contract; no changes to generic
     totals, progression, or UI architecture.
  2. Combat-details enrichment (base stats / abilities / moves from
     PokeAPI) — snapshot regeneration + view surface; unscheduled so far.
  3. Packaging / release story — single-file self-contained publish,
     versioning, an install/shortcut path so daily use no longer runs from
     `src/Ui/bin/Debug/...` (macOS repo has a release workflow to mirror).
- If the user wants something else entirely (defects, UI polish, metrics),
  follow their direction — the plan is done.

## Contracts
- Engine: everything through M7 — `ApplyUsage(...)`, shop/candy/mint/
  difficulty members, `View()` → `CompanionGameView` (now includes
  `ActiveSpeciesID`/`ActiveUnownForm`), `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. UI/pet consume `View()`/`Changed`/`DrainNotices`
  only.
- Sprite seams: Core `SpriteCatalog` (pure), Application `SpriteStore`
  (fetch+cache; `Subject`/`CachedSubject`/`Egg`/`CachedEgg`). New sprite
  kinds (items) would follow the same pattern — Core pure mapping first.
- Settings: `AppSettingsFile` (`src/Platform.Windows/AppSettingsFile.cs`)
  holds growth/shop difficulty + pet prefs; extend it (not the save) for any
  new local preference.
- macOS reference: `Sources/PokeTokenBar/` stays the semantic source for any
  ported behavior (e.g. `CodexUsage*.swift` provider patterns for OpenCode).

## Relevant Files
- `src/Core/SpriteCatalog.cs`, `src/Core/FloatingPetGeometry.cs` — M7 pure ports
- `src/Application/SpriteStore.cs` — fetch/cache/fallback ordering
- `src/Application/CompanionEngine.cs` — engine + notices + view records
- `src/Platform.Windows/AppSettingsFile.cs` — settings.json service
- `src/Ui/FloatingPetWindow.xaml(.cs)` — pet window (drag/click/sprite)
- `src/Ui/App.xaml.cs` — tray, balloons, pet toggle wiring
- `src/Ui/DashboardWindow.xaml(.cs)` — Game tab incl. pet section
- `Tests/{Core,Application,Platform.Windows}.Tests/` — M7 evidence files
- `docs/windows-port-plan.md` — "### Later: OpenCode" is the last section
- `docs/handoff/2026-09-23-milestone-7-floating-pet-hardening.md` — previous handoff

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~4s, 325 tests) — wrong
  file error `MSB1009` if you type `.sln`. Kill any running `PokeTokenBar.exe`
  BEFORE building the UI project — a live instance locks the bin output
  (`MSB3027` "being used by another process").
- BAML event trap (M6.5, re-applied M7): XAML attaches handlers BEFORE
  dependency properties — `Minimum="48"` on the pet-size slider fires
  `ValueChanged` during `InitializeComponent`. Guard handlers with a
  construction-phase check (`_engine is null`) or null-check referenced
  elements. Any new slider/checkbox must follow this.
- Hardcodet tray events (`TrayLeftMouseUp`, context-menu clicks) arrive via a
  WndProc hook that BYPASSES `DispatcherUnhandledException` — wrap tray-sourced
  handler bodies ENTIRELY in try/catch (see `SetPetEnabled`, `ShowDashboard`).
- Hardcodet 2.0.1 balloon API is `ShowBalloonTip(string title, string
  message, BalloonIcon icon)` — three args, NO timeout overload (the 1.x
  `int timeout` form is gone; compile error CS1503 if you try).
- XamlAnimatedGif 2.3.2 verified on net10.0-windows (prototype
  2026-09-23): needs STA + a running dispatcher; `AnimationBehavior.
  SetSourceStream(image, stream)` then `GetAnimator(image)` → `Play()`;
  keep the stream referenced for as long as the animation is attached.
- WPF `SystemParameters` has no `DoubleClickTime` — P/Invoke
  `user32!GetDoubleClickTime` (see FloatingPetWindow).
- `HttpContent` has no sync `ReadAsByteArray()` on net10 — use
  `Http.Send(request)` + `ReadAsStream()` + `CopyTo` (see `SpriteStore.
  HttpFetch`). Sync-over-async avoided on purpose; fetch runs on background
  threads.
- File-based apps (`dotnet run file.cs`): top-level args are LOWERCASE
  `args` (`Args` → CS0103); `#:project` resolves relative to the SCRIPT file
  (absolute path is safest); reflection-based STJ serialization of anonymous
  types is disabled — build JSON with `JsonObject` (AppSettingsFile.Save
  already does).
- Seeding sandboxes: script pattern in
  `Temp\opencode\ptb-m7-smoke\seed-balloon.cs` — construct engine with
  explicit `StateFilePath`, ApplyUsage baseline+hatch, mutate
  `State.Inventory`/`State.Active.UsedAtStage`, persist via a +1 ledger
  ApplyUsage. Balloon-on-demand: seed mon to `threshold − 60M` with candies,
  then "Use candy" in the dashboard.
- Double math expectations: write assertions from observed behavior
  (`4_999_999 / 5M * 10M` floors to `9_999_998`). `Assert.Throws<T>` needs
  the EXACT type. Baseline refresh applies NO usage. Hatch check runs on
  EVERY apply; read `PendingUnownForm` BEFORE clearing it.
- PowerShell traps (snapshot script): retry loops need `break` after
  success; functions returning `JsonArray` get ENUMERATED (wrap with `,`);
  `@(a, $arr)` FLATTENS; `DeepClone()` before re-parenting nodes.
- PokéAPI quirks: Manaphy (490)/Phione (489) share chain 250 whose root is
  not the requested base — script re-roots; chains of ≤649 species include
  >649 members — filter at load.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI at runtime for BASE DATA, parsing `wsl.exe -l` output.

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the slice choice with the user first (this handoff's Decision Needed).
- Evidence: unit tests for pure parts; UI behavior manual by the user; the
  `PTB_STATE_DIR` sandbox + `dotnet run file.cs` seeding tricks for
  destructive checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- OpenCode: storage locations on Windows/WSL unknown until the research
  spike; do NOT wire a provider before fixtures exist (same rule that made
  Claude/Codex slices safe).
- XamlAnimatedGif is a hobby-maintained package — if GIF playback ever
  breaks after a .NET upgrade, the static PNG fallback path already exists;
  do not build more on top of animator internals.
- Sprite disk cache grows unbounded by design (per-species files, small) —
  keep, no prune system unless asked.
- Pet window is not covered by unit tests (WPF) — changes to drag/click
  logic need the manual checklist (drag / click no-op / double-click /
  right-click / restart persistence).
- Balloon tips are best-effort (Windows may suppress them under Focus
  Assist); if the user misses events, revisit toast plumbing THEN, not
  before.

## Acceptance Criteria
- [ ] Slice confirmed with the user and implemented per its own plan
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (325 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms
- [ ] Handoff for the next slice written and committed

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (milestones 0–7 + Later)
- `docs/handoff/2026-09-23-milestone-7-floating-pet-hardening.md` — previous handoff (M7)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M8)

## Start Prompt
```text
Read docs/handoff/2026-09-23-milestone-8-opencode-combat-details-packaging.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
Milestones 0-7 are complete and verified. First confirm the M8 slice with the user (OpenCode research spike vs combat-details enrichment vs packaging/release story — see Decision Needed at Start; follow the user if they name something else).
Then implement that slice only, prove pure parts with tests, run the manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do not bundle the other candidates into this slice.
```
