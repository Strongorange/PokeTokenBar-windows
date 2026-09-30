# Handoff — Milestone 25: next UI-parity slice (confirm choice first)

## Goal
UI parity polish with the macOS original continues. M12–M24 are done and
user-verified (see `docs/windows-port-plan.md` "Reopened" section). M24
(0.23.0) shipped the celebration animation package — the last Tier-2 item
besides the deprioritized incident banner. M25 candidates from
`docs/ui-parity-audit.md`: incident banner (user said "장애 배너까지는
굳이" at M23 kickoff — RECONFIRM before picking), Tier-3 dark mode (large:
token swap + title bar), or an engineering-quality slice (trend/provider/
shop render builders, bag/shop confirm-state machinery still in
`DashboardWindow.xaml.cs`; can also ride along with a UI slice touching the
same code). Do NOT scaffold before confirming the choice with the user.
NOTE: if more than one session works this same checkout in parallel, they
will conflict (repo pushes straight to `main`); coordinate landings.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2
  machine, target environment itself)
- Branch: `main` / Base: `main` (direct push, no PR flow)
- Commits: M24 = feat commit (celebration queue + engine enqueues + player
  + vector glyphs + 0.23.0 bump) + docs close-out (plan + audit + THIS
  handoff)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M24 (user-verified on 0.23.0):
  - Celebration package: hatch/evolve/ditto-reveal play white flash (0.85→0,
    0.8s ease-out) + spring pop (scale 0.6→1, ElasticEase) on the companion
    sprite tile; shiny hatch adds a gold sparkle burst (0.3s delay, 2.6s
    total), ditto reveal adds the mask pair (0.25s; +sparkle at 0.45s if
    shiny). Candy use pops the orange "+N XP" capsule above the sprite
    (1.3s); mint shimmers three gold sparkles (0.9s); egg ≥90% wiggles
    ±5° repeat-forever. Multiple flash kinds in one drain play sequentially;
    candy/mint overlays are independent/concurrent (macOS behavior).
  - Engine: Core `CompanionCelebration` record (Kind/Shiny/Amount) +
    `CelebrationQueue` (Enqueue/Drain/Clear, capacity 8, drops oldest);
    `CompanionEngine.DrainCelebrations()` mirrors `DrainNotices`. Enqueue
    sites: evolve (in ApplyGrowth loop), ditto reveal, hatch (AFTER
    carry-over ApplyGrowth, skipped when the overflow graduated the mon,
    shiny hidden while disguised — macOS ordering), UseRareCandy (amount =
    applied XP), UseMint. ImportSave clears pending.
  - UI: `CompanionCelebrationPlayer` (Ui) owns all playback — built from the
    named XAML overlay elements, wired in the DashboardWindow ctor;
    `UpdateGame` drains + feeds it and drives the wiggle via
    `SetEggImminent(!HasActive && EggProgress >= 0.9)`.
  - WPF renders emoji MONOCHROME (verified by experiment — even explicit
    `FontFamily="Segoe UI Emoji"` yields zero colored pixels; WPF has no
    color-font support). So ✨/🎭 are vector-drawn `CelebrationGlyphs`:
    curved four-point star (new Theme `SparkleBrush` #FFC83D + white glint
    dot ≥15px) and a comedy/tragedy mask pair (reuses RarityLegendary/
    RarityRareBrushes, Canvas-laid eyes/mouth). Fallback-only emoji (❔🥚🌿)
    stay as-is — real sprites cover them in practice.
  - No new DashboardText keys (macOS celebration uses no localized strings;
    "+N XP" is a literal via `TokenFormatter.Compact`).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 499 green (484 + 15 new:
  5 queue semantics, 10 engine enqueue/ordering/import). Smoke harness
  `%TEMP%\opencode\ptb-m24-smoke\Program.cs` (DispatcherSynchronizationContext
  + PushFrame pumping, window shown offscreen at Left=-2000, per-run
  `state-*` wipe — smoke state files otherwise leak across runs and silently
  skip the hatch) renders hatch-flash / shiny-burst / settled / candy-xp /
  mint-sparkle / egg-wiggle / ditto-reveal PNGs with numeric animation probes
  + `pixel-check.ps1`/`burst-color-check.ps1` color/diff assertions. Live
  species fetch allowed in smoke via a `/sprites/pokemon/` URL filter so the
  mint/ditto renders show real sprites. User approved the PNG checkpoint
  (incl. mint-cluster tightening to macOS-verbatim offsets scaled 64/76:
  (-9,-8)/(11,4)/(1,11)) and manual smoke on published 0.23.0.
- The audit doc `docs/ui-parity-audit.md` tracks every remaining item — keep
  it updated when slices land.

## Locked Decisions
- Stack C# / .NET 10 (`net10.0`, UI `net10.0-windows` WPF +
  `Hardcodet.NotifyIcon.Wpf` 2.0.1 + `XamlAnimatedGif` 2.3.2), layering
  unchanged: Core pure, Providers parse, Platform.Windows owns paths/
  settings/diagnostics, Application owns engine + orchestration, UI consumes
  Application only. No MVVM framework.
- Theme is Fluent light ONLY (`src/Ui/Theme.xaml` is the single color/font/
  radius source; no hardcoded hex in windows or code-behind — use `Token()`
  / `{DynamicResource}` / `SetResourceReference`; the only sanctioned
  hex-alpha tints follow the `ColorOf`/`#14`-prefix pattern without '#').
  Dashboard resizable (min 560x640, default 640x800); settings/detail
  windows fixed `CanMinimize`.
- Official provider limits stay OUT (needs OAuth credentials; violates the
  no-credentials rule) unless the user explicitly designs an opt-in.
- Pokemon base data stays the bundled schema-2 snapshot (offline-first);
  sprites remain the separate online-cached concern (`SpriteStore`).
- Packaging: version = Ui csproj, bumped with the shipping slice (0.23.0 =
  M24; M25 ships 0.24.0). Publish via `scripts\publish-windows.ps1` (kills
  the installed app; relaunch the exe yourself). Releases by hand,
  `v<Version>` tags; update checker wired to `Strongorange/PokeTokenBar-windows`.
  Release zip = `PokeTokenBar-<version>-win-x64.zip` containing ONLY
  PokeTokenBar.exe (pdbs excluded — README's install section names this exact
  zip); notes = changelog since v0.10.0. `gh` CLI NOT authenticated and the
  GitHub API is 403 rate-limited without a token, so the user uploads the
  release + asset by hand in the browser.
- State under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR` overrides).
  Language is saved-game state via `SetLanguage`. README en+ko only.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push when the milestone is verified (user's standing instruction:
  implement → validate → commit+push → write next handoff). Remote `origin`
  = `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
Pick from `docs/ui-parity-audit.md`. Remaining:
- Tier 2: incident banner (statuspage.io, no auth) — deprioritized at M23
  kickoff; reconfirm interest first.
- Tier 3: dark mode (large: token swap + title bar).
- Engineering quality (user principle: every unit modular and individually
  testable): ◐ — `CompanionPresentation`, `RelativeTimes`, and now
  `CompanionCelebrationPlayer`/`CelebrationGlyphs` are extracted. Still in
  `DashboardWindow.xaml.cs`: trend/provider/shop render builders, bag/shop
  confirm-state machinery, usage presentation decisions. Can ride along with
  a UI slice touching the same code.

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `PlanRareCandyUse(int)`, `MaxRareCandyUseCount()`, `SetLanguage`,
  `SetRepresentative(int?, UnownForm?) -> bool`, `View()` →
  `CompanionGameView` (…, `EvoLine`, `DexRows`, `ShopRows`, `Bag`,
  `AvailableTokens`, `RecentEvents`, `EggGuarantee`, `RepresentativeSpeciesID`,
  `RepresentativeIsShiny`, `RepresentativeUnownForm`, `Status`,
  `StatusEvolvedName`, `CatchRows`, ends `UnownForms, AppLanguage`),
  `Detail(speciesID)`, `Changed`, `DrainNotices()`,
  `DrainCelebrations()` (M24), `ExportSave`/`ImportSave`. NEW VIEW FIELDS GO
  BEFORE `UnownForms`.
- M24 additions (Core): `CompanionCelebrationKind` (Hatch/Evolve/DittoReveal/
  CandyXp/MintSparkle), `CompanionCelebration(Kind, Shiny = false, Amount =
  0)`, `CelebrationQueue` (Capacity 8; Enqueue/Drain/Clear). Application:
  `CompanionEngine.DrainCelebrations()`. Ui (internal):
  `CompanionCelebrationPlayer` (Play(IReadOnlyList<CompanionCelebration>),
  SetEggImminent(bool)) and `CelebrationGlyphs` (Sparkle(size),
  TheaterMasks(), FillMintCluster(grid)).
- `Detail(speciesID)` returns null unless the snapshot has a `details` entry
  for that species (relevant for smoke fixtures).
- Usage: `UsageDisplayState(...)` and `ProviderUsageSummary(...)` —
  extensions appended as optional params only.
- UpdateChecker (M17, unchanged): `UpdateCheckerOptions`, `Available`/
  `Skipped`/`UpdateTarget`/`SettingsNotice`, `CheckAsync`, `Consider`/
  `SkipCurrent`/`ShowSkippedAgain`, static `IsSafeReleaseUrl`.
- Settings: `AppSettings` incl. `SkippedUpdateVersion`,
  `UpdateNotificationsEnabled`; guarded `Application.Current is App` checks;
  `App.LaunchAtLoginEnabled`/`ApplyLaunchAtLogin(bool)`.
- Fixed UI strings live in `src/Core/DashboardText.cs` macOS-verbatim (ko/en/
  ja/es/fr/pt/de order matches `DashboardText.T`); add a Core.Tests string
  test in the same slice. M24 added none.
- Theme resources via `{DynamicResource <Key>Brush}`, `Token(key)`, or code
  `SetResourceReference` (M24 lesson: do NOT use
  `Application.Current.FindResource` — smoke apps construct a bare
  `new Application()` without App.xaml resources). M24 key: `SparkleBrush`.
- Sprites through `SpriteSlot`; item icons through `ItemIconSlot`; static
  thumbnails >44px use NearestNeighbor.
- Snapshot schema 2 (loader accepts 1). Regenerate only via
  `scripts/generate-pokemon-snapshot.ps1` under pwsh.

## Relevant Files
- `docs/ui-parity-audit.md` — the candidate list (update per slice)
- `src/Core/CompanionCelebration.cs`, `src/Ui/CompanionCelebrationPlayer.cs`,
  `src/Ui/CelebrationGlyphs.cs` — all new in M24
- `src/Application/CompanionEngine.cs` — `_celebrations` field +
  `DrainCelebrations()` near `DrainNotices`; enqueues in `HatchIfReady`
  (after overflow growth), `ApplyGrowth` evolve branch, `RevealDitto`,
  `UseRareCandy`, `UseMint`; cleared in `ImportSave`
- `src/Ui/DashboardWindow.xaml` — companion tile wrapped in a 64x64 Grid:
  `CompanionTile` (ScaleTransform `CompanionScale` + RotateTransform
  `CompanionRotation`), overlays `CelebrationFlash` (white Border),
  `CelebrationShiny`/`CelebrationDitto` (burst hosts w/ ScaleTransforms),
  `CelebrationCandy` (+`CelebrationCandyText`/`CelebrationCandySlide`),
  `CelebrationMint` — glyph content filled from the DashboardWindow ctor
- `src/Ui/DashboardWindow.xaml.cs` — `UpdateGame` top: drain + play +
  `SetEggImminent`; `_celebrations` field
- `src/Ui/Theme.xaml` — `SparkleBrush` (#FFC83D)
- macOS references: `Sources/PokeTokenBar/UI/CompanionView.swift`
  (celebration 485-716 incl. playback 642-703 and mint/candy feedback),
  `CompanionStore.swift` (celebration members 20-38, fire sites 712/904/
  928/1340/1383, import reset 1523-1528)
- `docs/handoff/2026-09-30-milestone-24-ui-polish-next.md` — M24 kickoff
  handoff (superseded by this file)
- RELEASE.md / `docs/reference/release-workflow/` — macOS-original release
  runbook — NOT the Windows process; Windows releases are by hand per Locked
  Decisions

## Hard-Won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 499 tests) —
  `MSB1009` if you type `.sln`. Kill DEBUG `PokeTokenBar.exe` before
  testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- WPF emoji are MONOCHROME — never use emoji glyphs as primary visual
  design; draw vectors (`CelebrationGlyphs` pattern) or use sprites.
- WPF animation smoking: set
  `SynchronizationContext.SetSynchronizationContext(new
  DispatcherSynchronizationContext())` first (async void continuations must
  re-enter the dispatcher), `window.Show()` offscreen (Left=-2000) so
  animation clocks tick, then pump fixed durations via `DispatcherFrame` +
  Background-priority DispatcherTimer. Animated property getters return the
  CURRENT animated value — probe Opacity/ScaleX/Angle directly.
  RenderTargetBitmap captures mid-animation state. Wipe smoke `state-*`
  dirs per run (persistent engine state silently breaks hatch scenarios).
- Image verification without vision: this coding model cannot read images
  (Read tool returns no visual content in this environment). Substitute
  numeric animation probes + System.Drawing pixel checks (color counts,
  before/after diffs — see `%TEMP%\opencode\ptb-m24-smoke\*.ps1`). The user
  suggested "Muse Spark 1.3 Free" as an auxiliary image-recognition model
  (memory: preferences/user/image_recognition) but NO endpoint/CLI/skill is
  configured on this machine — if the user provides access, wire it in for
  PNG checkpoints; until then the user is the eyes.
- WPF gotchas (still true): no `StackPanel.Spacing`; `new X { ... } {
  Children = ... }` invalid; `HexBrush("#14" + hex)` — hex WITHOUT '#';
  `Math.Round(float)` ambiguous → cast double; no implicit long→int;
  CS0165 with `is { } y` stored in another bool; `Freeze()` is a method
  call, not an object-initializer property.
- `CompanionGameView` field order is contract — new view fields go before
  `UnownForms`; current tests build via `engine.View()`.
- Capsule/pill pattern: Border CornerRadius=half-height + Padding 6-8,1-3 +
  8-10pt Bold text; rarity colors via `CompanionPresentation.RarityHex`
  (hex WITHOUT '#') for alpha tints; `#26F7630C`-style orange tint for
  growth/XP chips.
- Usage week window: Sunday start via `CultureInfo.InvariantCulture` — do
  NOT use CurrentCulture there.
- Codex fixture vectors are (Input, Cached, Output, Total); per-entry usage
  comes from `last_token_usage`.
- GitHub API from this machine without a token is 403 rate-limited; manual
  update checks masquerade as "up to date". `gh` CLI NOT authenticated.
  Releases by hand in the browser: v0.22.0 was uploaded at M24 close
  (confirmed); v0.23.0 (`PokeTokenBar-0.23.0-win-x64.zip`, tag `v0.23.0`,
  exe only) still needs the user's manual upload — until then installed
  0.10.0–0.22.0 clients see no update.
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI for BASE DATA, macOS-card theme, 40px dex tiles,
  saturation-dimming, official limits via credentials, emoji-as-design
  glyphs (monochrome).

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the M25 choice with the user before building anything. Visual work gets a
  rendered sample/screenshot checkpoint BEFORE polishing everything
  (M24's mint-cluster + emoji-color corrections both came from exactly this
  review loop).
- Evidence: unit tests for pure parts; UI behavior manual by the user;
  `PTB_STATE_DIR` sandbox + offscreen screenshot harness for layout checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file; update `docs/ui-parity-audit.md` when a slice lands.

## Open Risks
- v0.23.0 release upload is manual (user, browser); installed clients see no
  update until then.
- Celebrations drain in `UpdateGame` — if the dashboard is CLOSED when an
  event fires, the queue holds and plays on the next open (window recreation
  calls UpdateGame; macOS-parity). If the window exists but is MINIMIZED the
  cues play unseen — accepted edge case, revisit only if the user notices.
- Status-line burn is a refresh-delta approximation; `Tired` never occurs
  (no limits feature). Fine for a mood line.
- Catch-log chain names for OLD dex entries show "#id" fallbacks.
- Egg imminent/guarantee visuals + guaranteed-tier egg purchase still never
  seen live by the user (active mon throughout M20–M24).
- Shiny ✨ surfaces — still never seen live on a real shiny since 09-23.
- Launch-at-login registry failures revert the checkbox silently.
- XamlAnimatedGif hobby-maintained — static PNG fallback exists.

## Acceptance Criteria
- [ ] M25 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (499 + new)
- [ ] `docs/ui-parity-audit.md` updated for the shipped items
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.24.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/ui-parity-audit.md` — candidate list (authoritative)
- `docs/windows-port-plan.md` — canonical plan (closure + reopened polish)
- `docs/handoff/2026-09-30-milestone-24-ui-polish-next.md` — M24 kickoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen
  handoff (full trap list, still canonical)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` —
  session continuity entries (M0–M24)

## Start Prompt
```text
Read docs/handoff/2026-09-30-milestone-25-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M24 are done (0.23.0
shipped: celebration package — flash/pop/bursts, candy "+XP" capsule, mint
sparkle, egg wiggle). The candidate list lives in docs/ui-parity-audit.md —
confirm the M25 slice with the user FIRST (Tier-2 incident banner needs
reconfirmation after the user deprioritized it; Tier-3 dark mode is large;
engineering-quality remainder can ride along). For visual work render a
sample/screenshot checkpoint before polishing everything. Implement that
slice only, prove pure parts with tests, update the audit doc, run the
manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do
not bundle other work into the slice. If other threads are working this same
checkout, coordinate landings — this repo pushes straight to main.
```
