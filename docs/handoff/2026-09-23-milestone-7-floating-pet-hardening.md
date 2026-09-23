# Handoff — Milestone 7: Floating pet, sprites, notifications, hardening

## Goal
Complete the personal-use surface: the floating pet (current mon on screen,
draggable, sprite-based), the pokemon sprite cache (animated where available,
static fallback, shiny variants), user notifications for companion events
(evolve / graduate / hatch / ditto reveal / release), and a hardening pass for
daily use (multi-monitor, DPI, sleep/wake, WSL disturbances, active/large
logs). Plan reference: `docs/windows-port-plan.md` "### 7."; the game shop /
difficulty surface was finished in M6.5.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2 machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR flow used so far)
- Commits so far: `f763205` docs research+fixtures (M0) · `8083930` pure core (M1) ·
  `55b7701` discovery+scan (M2) · `bb422fd` Claude slice (M3) · `113f7e0` Codex
  slice (M4) · `f1108dd` application+tray dashboard (M5) · `1765993`/`59bf551`
  handoff docs · `3a88556` game engine+persistence+dashboard game tab (M6) ·
  `1ac0a36` M6.5 handoff doc · `dce0620` shop/candy/mint/difficulty (M6.5)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M6.5 (`dce0620`): shop with wallet semantics
  (`availableTokens = max(0, UsedSinceInstall − SpentTokens)`, purchases raise
  `SpentTokens` only), rare candy plan/use routed through `ApplyGrowth`
  (ditto-disguise consumption capped at the reveal threshold), mint reroll to
  a different nature, tiered egg purchase (reached-forms-only release record
  into the dex, incubation reset, tier guarantee in `EggTier`), growth/shop
  difficulty with fraction-preserving `RescaleBankedGrowth` (incomplete stages
  can never complete via rounding).
- Difficulty storage (decided M6.5): `src/Platform.Windows/AppSettingsFile.cs`
  — `settings.json` in the same directory as the save (`%LOCALAPPDATA%\
  PokeTokenBar\`, `PTB_STATE_DIR` override honored). Difficulty is NOT part of
  the save; imported saves never change local difficulty. `App` loads it into
  `CompanionEngineOptions` and persists on slider change.
- Engine public surface added in M6.5 (`src/Application/CompanionEngine.cs`):
  `Buy(ItemKind)`, `CanBuy`, `BuyEgg(Rarity?)`, `CanBuyEgg`,
  `UseRareCandy(count=1)`, `PlanRareCandyUse(int)`, `MaxRareCandyUseCount()`,
  `UseMint()`, `SetGrowthDifficulty(double)`, `SetShopDifficulty(double)`,
  `Price(ShopEntry)`; `CandyUseResult` and `RareCandyUsePlan` records live in
  the same file. All threshold reads inside the engine are difficulty-scaled
  (`StageThreshold(mon)`, `EggHatchThresholdCore`) — never call
  `PokemonBalance.PhaseThreshold` / `EggHatchThreshold` directly from engine
  paths (macOS rule ported).
- View (`CompanionGameView`) now exposes `ShopRows` (price-ascending,
  purchased passives last), `Bag` (counts + CanUse), `AvailableTokens`,
  `GrowthDifficulty`, `ShopDifficulty`, scaled egg/stage thresholds.
- UI: Game tab has shop list + Buy selected, bag line, Use candy ×1 / Use all
  / Use mint buttons, growth/shop sliders (snap via
  `PokemonBalance.SnapDifficulty`), footer feedback line.
- Evidence: 277 tests green (104 Core + 42 Platform.Windows + 89 Providers +
  42 Application) via `dotnet test PokeTokenBar.Windows.slnx`. M6.5 evidence
  (a)-(f) all present in `Tests/Application.Tests/CompanionEngineTests.cs`,
  settings round-trip in `Tests/Platform.Windows.Tests/AppSettingsFileTests.cs`.
- Manual verification on this machine (2026-09-23, user-driven through the
  real UI against a `PTB_STATE_DIR` sandbox): mint×2 + candy×3 + charm +
  uncommon-egg purchases, candy use, egg purchase released the active into
  the dex, sliders persisted (growth 2.0 / shop 0.5), full restart restored
  wallet/inventory/dex/settings. Real progression untouched.
- Two UI crash fixes landed during that smoke (see Hard-won Context): the
  BAML `ValueChanged` construction trap and `ShutdownMode` tray exit.
- Not implemented: floating pet + sprites (this milestone), notifications
  (this milestone — deferred from M6.5 by decision), pokemon combat detail
  enrichment (base stats/abilities/moves — still unscheduled), OpenCode
  provider (later).

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf`), layering unchanged: Core pure, Providers parse,
  Platform.Windows owns paths/settings/diagnostics, Application owns engine +
  orchestration, UI consumes Application only. No MVVM framework.
- Pokemon data is the bundled snapshot (`assets/pokemon-snapshot.json`,
  offline-first); regenerate only via `scripts/generate-pokemon-snapshot.ps1`.
  The snapshot contains NO sprite data — sprites are a separate concern of
  this milestone (decide delivery below).
- Difficulty/settings live in `settings.json` (Platform.Windows), never in
  the save. Game tab stays one tab with sections; split only when cramped.
  Egg purchase keeps release-into-dex semantics (macOS `releasedDexEntry`).
- Engine stays a single class in `src/Application` with injected clock/RNG;
  candy XP routes through `ApplyGrowth` — `PickPlannedChild` rolls exist only
  inside the shared growth loop.
- macOS divergences preserved intentionally: keep-earliest (Codex) vs
  keep-max (Claude); do not "fix".
- Never read/copy credentials (`auth.json`, `.credentials.json`,
  `settings.json` secrets).
- Comments: none unless asked. English commits. Commit + push only when the
  user asks. Git remote `origin` = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
- Sprite sourcing & animation: macOS `SpriteLoader.swift` fetches animated
  GIFs (gen ≤649) / static artwork from
  `https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/...`
  with a disk cache keyed by species/shiny/animated/unown-form
  (`cacheKey`/`spriteURL` — pure functions, portable as-is). Windows options:
  1. Same online cache + `XamlAnimatedGif` (or `WpfAnimatedGif`) for GIF
     playback, static-PNG fallback when a fetch fails — macOS parity, small
     repo, but first sight of a species is offline-unfriendly until fetched
     (macOS prefetches at hatch roll time — port that). Recommended.
  2. Bundle static official-artwork PNGs via the snapshot generator —
     offline-first immediately, no animation, repo grows by tens of MB.
  3. Hybrid: online animated cache + bundled static fallback for the active
     mon only. Ask the user; option 1 matches macOS.
- Pet window tech: borderless WPF window (`WindowStyle=None`,
  `ShowInTaskbar=false`, `AllowsTransparency=true`, `Topmost=true`),
  left-drag to move with a click-vs-drag threshold (~4pt, macOS
  `FloatingPetPanel.clickThresholdSquared = 16`), double-click opens the
  dashboard. Confirm always-on-top behavior and whether the pet should hide
  when fullscreen apps are detected (macOS keeps it simple — recommend same).
- Notifications: real Windows toasts (`Microsoft.Toolkit.Uwp.Notifications`)
  need AUMID/MSIX plumbing for unpackaged apps — risk. Tray balloons via
  `TaskbarIcon.ShowBalloonTip` are zero-dependency but quaint. Recommend
  starting with tray balloons; only adopt the toast package if the user finds
  them insufficient.
- Hardening scope: multi-monitor + DPI change while running, sleep/wake,
  WSL distro stopped mid-run, actively-written logs, large logs (all from the
  plan's exit criteria). Combat-details enrichment stays OUT unless asked.

## Contracts
- Engine (all in place): `ApplyUsage(...)`, `ExportSave`/
  `SuggestedExportFileName`/`ImportSave`, `View() → CompanionGameView`,
  `Changed` event, `State` live reference, plus the M6.5 shop/candy/mint/
  difficulty members listed above. The pet should consume `View()` /
  `Changed` only — do not add engine→UI coupling.
- Core sprite seams: `PokemonAssets.HasAnimatedSprite(speciesID)`
  (true for 1–649) already exists in `src/Core/GameBalance.cs`; shiny odds
  via `PokemonOdds`. A new `SpriteCatalog` (URL/cache-key mapping) belongs in
  Core as pure functions (port `SpriteLoader.cacheKey`/`spriteURL`), with the
  HTTP fetch + disk cache in Application (offline after first fetch) —
  Core must not do IO.
- macOS reference for this milestone: `Sources/PokeTokenBar/UI/
  FloatingPetPanel.swift` (click-vs-drag, bubble sizing math
  `panelSize(petSize:showingBubble:)`, `bubbleMinWidth` 180),
  `SpriteAnimation.swift` (frame cadence), `SpriteLoader.swift` (URL scheme,
  cache layout, prefetch-on-hatch behavior).
- Settings: `AppSettingsFile` (`src/Platform.Windows/AppSettingsFile.cs`)
  currently holds growth/shop difficulty; extend it (not the save) for pet
  preferences (position, size, enabled) — same file, same rules.

## Relevant Files
- `src/Application/CompanionEngine.cs` — engine + view records (M6.5 members)
- `src/Platform.Windows/AppSettingsFile.cs` — settings.json service
- `src/Ui/App.xaml(.cs)` — tray, engine wiring, `ShutdownMode`
- `src/Ui/DashboardWindow.xaml(.cs)` — Game tab (shop/bag/sliders pattern)
- `src/Core/GameBalance.cs` — `PokemonAssets`, balance constants (read-only)
- `scripts/generate-pokemon-snapshot.ps1` — only if bundling sprites (option 2)
- `Sources/PokeTokenBar/UI/{FloatingPetPanel,SpriteAnimation,SpriteLoader}.swift`
  — semantic reference
- `Tests/Application.Tests/CompanionEngineTests.cs` — evidence pattern
  (SequenceRng, TickingClock, temp `_dir`, `MinimalSnapshot`)
- `docs/windows-port-plan.md` — "### 7." and "### Later: OpenCode"
- `docs/handoff/2026-09-23-milestone-6.5-shop-candy-mints.md` — previous handoff

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~3s, 277 tests) — wrong
  file error `MSB1009` if you type `.sln`.
- BAML event trap (cost one process-kill loop): the XAML loader attaches
  event handlers BEFORE applying dependency properties, so setting
  `Minimum="0.1"` on a Slider coerces Value 0→0.1 and fires `ValueChanged`
  synchronously DURING `InitializeComponent` — named elements declared after
  the slider are still null → NRE. Guard handlers with a construction-phase
  check (`_engine is null` works because the field is assigned after
  `InitializeComponent`) or null-check the referenced elements.
- Hardcodet tray events (`TrayLeftMouseUp`, context-menu clicks) arrive via a
  WndProc hook (`WindowMessageSink`) that BYPASSES
  `DispatcherUnhandledException` — an exception there kills the process
  silently; the only trace is Windows Event Log (Application, Id 1000/1026),
  NOT AppLog. Wrap tray-sourced handlers in try/catch (see `ShowDashboard`).
- WPF default `ShutdownMode=OnLastWindowClose` terminates a tray app when its
  only window (the dashboard) closes — set `ShutdownMode=
  "OnExplicitShutdown"` in App.xaml; the Exit menu already calls
  `Shutdown()`. Landed in M6.5 after the user X-closed the dashboard.
- File-based apps (`dotnet run file.cs`) DISABLE reflection-based
  System.Text.Json — `JsonSerializer.Serialize<T>` of anonymous types throws.
  Build JSON with `System.Text.Json.Nodes` (`JsonObject`) in any code that
  scripts/tests may drive this way (`AppSettingsFile.Save` already does).
- Chained difficulty rescales each preserve the earned fraction but floor
  rounding drifts by <1 token per step — harmless. Incomplete stages can
  never complete via rounding (`min(newThreshold−1, floor)`).
- Double math expectations: `4_999_999 / 5M * 10M` floors to `9_999_998`,
  not `9_999_999` — write assertions from observed double behavior (a test
  was corrected for this).
- QA sandbox trick: set `PTB_STATE_DIR` to a temp dir, seed
  `companion-state.json` + `settings.json` with a `dotnet run file.cs` script
  (`#:project` the Application csproj), launch the built
  `src/Ui/bin/Debug/net10.0-windows/PokeTokenBar.exe` with the same env var —
  full UI walkthrough without touching real progression. AppLog still writes
  to the real diagnostics log (process-global).
- Baseline refresh applies NO usage — first-apply expectations in tests are
  zero. The hatch check must run on EVERY apply; early-return paths fall
  through to `HatchIfReady()`. Read `PendingUnownForm` BEFORE clearing it.
- `Assert.Throws<T>` requires an EXACT exception type — use
  `Assert.ThrowsAny<JsonException>` for decode garbage.
- PowerShell snapshot-generator traps (if touching the script): retry loops
  need `break` after success; functions returning `JsonArray` get ENUMERATED
  (wrap with `,`); `@(a, $arr)` FLATTENS; `DeepClone()` before re-parenting
  nodes; parentheses around ternaries inside string holes.
- PokéAPI quirks: Manaphy (490)/Phione (489) share chain 250 whose root is
  not the requested base — script re-roots; chains of ≤649 species include
  >649 members — filter at load (Eevee 7 branches).
- Engine smoke trick: `dotnet run file.cs` with `#:project <csproj>` drives
  the real engine without the WPF app.
- WPF name traps: `App : System.Windows.Application` fully qualified;
  `using System.IO;` for `Path`. Records containing `List<T>` lack structural
  equality. AppLog tests share one xUnit collection per project.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI at runtime for BASE DATA (snapshot chosen — sprites are a separate
  decision), parsing `wsl.exe -l` output.

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. M7 =
  floating pet + sprite cache + notifications + hardening pass ONLY (no
  OpenCode provider, no combat-details enrichment, no shop/game redesign).
  Confirm the four start decisions with the user first.
- Evidence (unit, pure parts only — pet window itself is manual):
  (a) sprite URL/cache-key mapping matches the macOS scheme for
      animated/shiny/unown variants; cache write/read round-trip; static
      fallback on fetch failure,
  (b) click-vs-drag threshold and bubble sizing math (port of
      `FloatingPetPanel` pure helpers),
  (c) notification trigger wiring fires exactly once per companion event
      (engine `Changed` → notify map, no duplicates on repeated refreshes),
  (d) pet preferences persist via `AppSettingsFile` round-trip.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file.

## Open Risks
- GIF animation on `net10.0-windows`: `XamlAnimatedGif` /
  `WpfAnimatedGif` package health on .NET 10 is unverified — prototype the
  choice FIRST before building the pet around it; static fallback must exist
  regardless.
- Toast packages on unpackaged WPF apps need AUMID registration; balloons are
  the safe default. Do not sink time into toast plumbing before the user
  asks for richer notifications.
- Per-monitor DPI (V2) with a borderless always-on-top window: sizing and
  drag coordinates across monitors with different scales is the classic
  failure mode — test on the real multi-monitor setup if available.
- Sprite disk cache growth is unbounded on macOS (per-species files only,
  small); keep that model, do not build a prune system unless asked.
- Do not couple the pet window to engine internals — it re-renders from
  `View()` on `Changed`, like the dashboard.
- Hardening pass may surface WSL timing quirks (distro stopping mid-scan);
  existing single-flight + regression-rebase paths should absorb them —
  verify, do not rewrite.

## Acceptance Criteria
- [ ] Sprite cache: animated sprite for species ≤649, static fallback,
      shiny + unown-form variants, offline after first fetch, unit-tested (a)
- [ ] Floating pet: shows active mon (or egg), draggable with click-vs-drag
      threshold, re-renders on `Changed`, per-monitor DPI sane, manual pass (b)
- [ ] Notifications for hatch/evolve/graduate/ditto-reveal/release via chosen
      mechanism, no duplicates, unit-tested trigger map (c)
- [ ] Pet preferences (enabled/position/size) persist via settings.json (d)
- [ ] Hardening: multi-monitor + DPI change, sleep/wake, WSL stopped,
      active-write logs, large logs — no crash/hang, diagnostics clean
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (277 + new)
- [ ] Manual smoke on this machine incl. app restart; user confirms visuals

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine: pet visible/draggable across the real monitors,
  events notify, sleep/wake + WSL-stop survive, diagnostics log clean; use
  the `PTB_STATE_DIR` sandbox for destructive checks

## Related Docs
- `docs/windows-port-plan.md` — canonical plan (milestones 0–7 + Later)
- `docs/handoff/2026-09-23-milestone-6.5-shop-candy-mints.md` — previous handoff (M6.5)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` — session continuity entries (M0–M7)

## Start Prompt
```text
Read docs/handoff/2026-09-23-milestone-7-floating-pet-hardening.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
First confirm the four start decisions with the user (sprite sourcing, pet window tech, notification mechanism, hardening scope).
Then implement Milestone 7: the pokemon sprite cache (port SpriteLoader URL/cache-key semantics to Core + Application with static fallback), the floating pet window (draggable, click-vs-drag threshold, renders from CompanionGameView on Changed), companion-event notifications, pet preference persistence in settings.json, and the personal-use hardening pass. Prove pure parts with tests (a)-(d), then run the manual multi-monitor/DPI/sleep/WSL checklist and a restart smoke. Do not add the OpenCode provider or combat-details enrichment. Verify with dotnet test PokeTokenBar.Windows.slnx.
```
