# Handoff — Milestone 24: next UI-parity slice (confirm choice first)

## Goal
UI parity polish with the macOS original continues. M12–M23 are done and
user-verified (see `docs/windows-port-plan.md` "Reopened" section). M23
(0.22.0) shipped a four-feature bundle picked by the user ("everything one
thread can handle, minus too-big items and the incident banner"): catch log
view, companion status line, shop/bag item sprites, launch at login, plus the
engineering-quality ride-along (`CompanionPresentation`, `RelativeTimes`, a
dex double-click filter-index fix). M24 is the next slice — confirm the choice
with the user FIRST; the leading candidate is the celebration animation
package (the last unclaimed Tier-2 item besides the deprioritized incident
banner). Do not start scaffolding before that confirmation.
NOTE: if more than one session works this same checkout in parallel, they will
conflict (repo pushes straight to `main`); coordinate so slices touching the
same files land sequentially.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2
  machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR
  flow used so far)
- Commits: M23 = feat commit (catch log + status line + item icons + launch at
  login + strings + 0.22.0 bump) + docs close-out (plan + audit + THIS
  handoff)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M23 (user-verified on 0.22.0):
  - Catch log view: dex tab has a segment toggle (Pokédex / 포획 로그,
    RadioButtons top-right of the dex header). Log mode lists one card per
    individual — solid rarity capsule, "키우는 중" (accent) / "놓아줌"
    (neutral) badge, ✨, nature right-aligned, reached evolution chain as
    40px sprites with 9pt names, caught-at relative line ("n분/시간/일 전") —
    currently-raised mon pinned first, then caught-at desc (nulls last;
    macOS `dexEntriesSorted` semantics). Per-individual rarity filter capsules
    share the dex tally builder (`RenderRarityTally`/`CreateRarityTallyCapsule`).
  - Companion status line: one-line mood under the game-tab progress bar.
    Core `CompanionStatus.Compute` is the pure port of macOS `computeState`
    (burn tiers ≤1k idle / <100k working / ≥100k focus at macOS thresholds);
    the engine approximates burn from consecutive same-day refresh deltas
    (delta tokens ÷ elapsed minutes; reset when no provider data) because
    Windows has no active-provider-block metric, and holds a 4s level-up
    window (`SetStatusLevelUpWindow`) after hatch/evolve/ditto-reveal/graduate
    with the evolved name for the "…(으)로 진화했어요!" wording. `Tired` is
    reachable in the machine but never produced (no limits feature on
    Windows; `limitWarning` always false).
  - Shop/bag item icons: `ItemIconSlot` (Ui) renders PokeAPI item sprites
    cached-first with emoji fallback; cache keys `item-{name}.png` match
    macOS. Rare candy + shiny charm fetch from
    `raw.githubusercontent.com/.../sprites/items/`; mint has NO sprite by
    design → 🌿 forever. Used in shop card headers (30px) and bag headers
    (24px); egg keeps the 🥚 emoji.
  - Launch at login: settings → general group toggle.
    `Platform.Windows.LoginItem` writes the HKCU Run key
    (`Software\Microsoft\Windows\CurrentVersion\Run`, value `PokeTokenBar`,
    quoted exe path); registry access is delegate-injected so
    encode/match logic is unit-tested without touching the real registry.
    `App.LaunchAtLoginEnabled` reads, `App.ApplyLaunchAtLogin` writes; the
    checkbox re-reads actual state after applying (silent revert on failure).
  - Ride-along: `Application/CompanionPresentation` (rarity display order,
    rarity hex WITHOUT '#', dex/catch visible-row filters, provider-chip
    selection fallback) and `Core/RelativeTimes` (caught-at buckets) are
    extracted pure helpers with tests; DashboardWindow's `ColorOf` now
    delegates to `CompanionPresentation.RarityHex`. Fixed a pre-existing bug:
    with a dex rarity filter active, double-click indexed the UNFILTERED
    `DexRows` (now `_visibleDexRows`).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 484 green (445 + 39 new:
  status machine/tiers, relative-time buckets, item URLs/keys, SpriteStore
  item fetch/cache, LoginItem, presentation helpers, catch-row build/order,
  status lifecycle + evolve-name via controllable clock, ~20 strings).
  Smoke harness `%TEMP%\opencode\ptb-m23-smoke\Program.cs` (deterministic
  clock closure; engine seeded with 5 dex entries incl. released + active mon
  + inventory) renders game-status / dex-mode / catch-log / shop-icons /
  settings-launch PNGs and probes header counts, badges, ago lines, natures,
  emoji placeholders. User verified the PNG checkpoint, then manual smoke on
  published 0.22.0.
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
  / `{DynamicResource}`; the only sanctioned hex-alpha tints follow the
  `ColorOf`/`#14`-prefix pattern without '#'). Dashboard resizable (min
  560x640, default 640x800); settings/detail windows fixed `CanMinimize`.
- Official provider limits stay OUT (needs OAuth credentials; violates the
  no-credentials rule) unless the user explicitly designs an opt-in.
- Pokemon base data stays the bundled schema-2 snapshot (offline-first);
  sprites remain the separate online-cached concern (`SpriteStore`).
- Packaging: version = Ui csproj, bumped with the shipping slice (0.22.0 =
  M23; M24 ships 0.23.0). Publish via `scripts\publish-windows.ps1` (kills
  the installed app; relaunch the exe yourself). Releases by hand,
  `v<Version>` tags; update checker wired to `Strongorange/PokeTokenBar-windows`.
  Release zip = `PokeTokenBar-<version>-win-x64.zip` containing ONLY
  PokeTokenBar.exe (pdbs excluded — README's install section names this exact
  zip); notes = changelog since v0.10.0 (see `docs/windows-port-plan.md`
  "Reopened" slices). `gh` CLI NOT authenticated and the GitHub API is 403
  rate-limited without a token, so the user uploads release + asset by hand
  in the browser.
- State under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR` overrides).
  Language is saved-game state via `SetLanguage`. README en+ko only.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
Pick from `docs/ui-parity-audit.md`. Remaining:
- Tier 2: **celebration animation package** (flash/pop/✨/🎭/+XP/mint
  sparkle/egg wiggle; needs a celebration queue in the engine + WPF
  storyboards) — the leading candidate and deliberately kept as its own
  slice. Incident banner (statuspage.io, no auth) — user deprioritized at M23
  kickoff ("장애 배너까지는 굳이"); reconfirm before picking.
- Tier 3: dark mode (large: token swap + title bar).
- Engineering quality (user principle: every unit modular and individually
  testable): ◐ partial since M23 (`CompanionPresentation`, `RelativeTimes`
  done). Still in `DashboardWindow.xaml.cs`: trend/provider/shop render
  builders, bag/shop confirm-state machinery, usage presentation decisions.
  Can ride along with a UI slice that touches the same code.

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `PlanRareCandyUse(int)`, `MaxRareCandyUseCount()`, `SetLanguage`,
  `SetRepresentative(int?, UnownForm?) -> bool`, `View()` →
  `CompanionGameView` (…, `EvoLine`, `DexRows`, `ShopRows`, `Bag`,
  `AvailableTokens`, `RecentEvents`, `EggGuarantee`, `RepresentativeSpeciesID`,
  `RepresentativeIsShiny`, `RepresentativeUnownForm`, then M23 additions
  `CompanionStatusKind Status`, `string? StatusEvolvedName`,
  `IReadOnlyList<CompanionCatchRow> CatchRows`, ends `UnownForms,
  AppLanguage Language`), `Detail(speciesID)`, `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. NEW VIEW FIELDS GO BEFORE `UnownForms`.
- New M23 records (Application): `CompanionChainNode(int SpeciesID, string
  Name)`; `CompanionCatchRow(string Key, int BaseID, Rarity, bool IsShiny,
  bool IsRaising, bool IsReleased, string Nature, UnownForm?,
  DateTimeOffset? CaughtAt, IReadOnlyList<CompanionChainNode> Chain)`. Active
  row Key = `"active-{baseID}-{currentID}"`, CaughtAt null; entries without
  stored `Names` render "#id" chain names.
- Core additions (M23): `CompanionStatusKind`/`CompanionBurnTier` +
  `CompanionStatus.Compute/BurnTier` (pure; thresholds 1k/100k/400k);
  `RelativeTimeBucket` + `RelativeTimes.Bucket/BucketValue`; `SpriteCatalog
  .ItemCacheKey/ItemUrl` + `ItemsBase`. Application: `SpriteStore.Item(name)/
  CachedItem(name)`; `CompanionPresentation` (RarityDisplayOrder/RarityHex/
  VisibleDexRows/VisibleCatchRows/ResolveSelectedProvider). Platform.Windows:
  `LoginItem` (EncodeCommand/MatchesCommand/IsEnabled/SetEnabled with
  delegate-injected registry).
- `Detail(speciesID)` returns null unless the snapshot has a `details` entry
  for that species (relevant for smoke fixtures).
- Usage: `UsageDisplayState(...)` and `ProviderUsageSummary(...)` —
  extensions appended as optional params only.
- UpdateChecker (M17, unchanged): `UpdateCheckerOptions`, `Available`/
  `Skipped`/`UpdateTarget`/`SettingsNotice`, `CheckAsync`, `Consider`/
  `SkipCurrent`/`ShowSkippedAgain`, static `IsSafeReleaseUrl`.
- Settings: `AppSettings` incl. `SkippedUpdateVersion`,
  `UpdateNotificationsEnabled`; guarded `Application.Current is App` checks;
  `App.LaunchAtLoginEnabled`/`ApplyLaunchAtLogin(bool)` (M23).
- New fixed UI strings go in `src/Core/DashboardText.cs` macOS-verbatim
  (ko/en/ja/es/fr/pt/de order matches `DashboardText.T`); add a Core.Tests
  string test in the same slice. Keys added in M23: CatchLogTitle,
  DexTotalCount, DexReleasedBadge, CaughtAgo(bucket, value), StatusEgg/
  Idle/Working/Focus/Tired/Sleep/Evolved/Grew + `StatusLine(lang, kind,
  evolvedName?)` resolver, LaunchAtLoginLabel.
- Theme resources via `{DynamicResource <Key>Brush}` or `Token(key)`; new
  windows merge Theme.xaml per-window AND in App.xaml.
- Sprites through `SpriteSlot`; item icons through `ItemIconSlot`; static
  thumbnails >44px use NearestNeighbor.
- Snapshot schema 2 (loader accepts 1). Regenerate only via
  `scripts/generate-pokemon-snapshot.ps1` under pwsh.

## Relevant Files
- `docs/ui-parity-audit.md` — the candidate list (update per slice)
- `src/Ui/DashboardWindow.xaml(.cs)` — dex tab segment toggle
  (`DexModeToggle`, `OnDexModeChanged`), `RenderDexTab` (mode switch + shared
  empty state), `RenderRarityTally`/`CreateRarityTallyCapsule` (shared by dex
  + catch filters), `CreateCatchCard`/`CreateChainNode`/`CaughtAgoText`,
  `CompanionStatusText` in the progress card, `CreateItemIcon`,
  `_visibleDexRows` (double-click), fields `_dexShowLog`/`_catchRarityFilter`
- `src/Application/CompanionEngine.cs` — `TrackStatusInputs` (burn from
  refresh deltas; called in `ApplyUsageCore`), `SetStatusLevelUpWindow` (4s,
  fired from hatch/evolve/graduate/reveal; reset on `ImportSave`),
  `BuildCatchRows`/`ChainNodeName`, status view fields
- `src/Core/CompanionStatus.cs`, `src/Core/RelativeTime.cs`,
  `src/Application/CompanionPresentation.cs`, `src/Platform.Windows/LoginItem.cs`,
  `src/Ui/ItemIconSlot.cs` — all new in M23
- `src/Application/SpriteStore.cs` — `Item`/`CachedItem` (`item-{name}.png`)
- `src/Ui/SettingsWindow.xaml(.cs)` + `src/Ui/App.xaml.cs` — launch-at-login
  row + app bridge
- `src/Core/DashboardText.cs` + Tests/Core.Tests/DashboardTextTests.cs
- macOS references: `Sources/PokeTokenBar/UI/CompanionView.swift`
  (celebration 485-716 incl. `statusLine` 705 and the celebration playback
  642-703, `DexEntryRow` 1457-1521, `CollectionView` 780-868, `RarityTally`
  719-747), `CompanionStore.swift` (`computeState` 1478, `dexEntriesSorted`
  361-374, celebration queue members), `PopoverView.swift`, `BagView.swift`,
  `ShopView.swift`, `SettingsView.swift`
- `docs/handoff/2026-09-29-milestone-23-ui-polish-next.md` — M23 kickoff
  handoff (superseded by this file)
- RELEASE.md / `docs/reference/release-workflow/` — macOS-original release
  runbook — NOT the Windows process; Windows releases are by hand per Locked
  Decisions

## Hard-Won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 484 tests) —
  `MSB1009` if you type `.sln`. Kill DEBUG `PokeTokenBar.exe` before
  testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- Smoke apps (`dotnet run Program.cs`): need
  `#:property TargetFramework=net10.0-windows`, `UseWPF=true`,
  `PublishTrimmed=false` (NETSDK1168), `#:project <abs Ui csproj>`;
  x:Name fields are internal → `FindName`. Seeding: `engine.State.Dex`
  (`DexEntry(...)`), `engine.State.Inventory[ItemKinds.Raw(kind)]`,
  `engine.State.Active = new MonState(...) { Nature = ..., Profile = ... }`.
- Deterministic status in smoke/tests: construct the engine with a clock
  closure (`var now = DateTimeOffset.UtcNow; ... Clock = () => now`) and
  advance `now` between `ApplyUsage` calls — burn needs minutes > 0 between
  observations (two immediate calls leave burn 0 → Idle), and the level-up
  window is 4s of engine-clock. Growth overflow: hatching with 245M overflow
  evolved through TWO stages (thresholds 125M/115M/…) — assert the actual
  stage, don't hand-compute.
- Smoke catch-log specifics: direct-seeded DexEntries have no stored `Names`
  → chain nodes show "#id" (real saves store names at graduate/release).
  Probe with `Visibility == Visibility.Visible` filtering; tabs are lazily
  realized (`SelectedIndex` then `UpdateLayout`); `RenderTargetBitmap` 1.5x.
  Item icons in offline smoke show emoji placeholders — restrict the fetch
  delegate to `/items/` URLs if you want live item sprites in a PNG.
- WPF gotchas (still true): no `StackPanel.Spacing`; `new X { ... } {
  Children = ... }` is invalid C#; `HexBrush("#14" + hex)` — hex constants
  must NOT contain '#'; `Math.Round(float)` ambiguous → cast to double first;
  `long`→`int` implicit conversions don't exist; CS0165 with `is { } y`
  stored in another bool.
- `CompanionGameView` field order is part of the contract — new view fields
  go before `UnownForms`; tests construct positionally in theory, current
  tests build via `engine.View()`.
- Capsule/pill pattern: Border CornerRadius=half-height + Padding 6-8,1-3 +
  8-10pt Bold text; rarity colors in Theme and
  `CompanionPresentation.RarityHex` (hex WITHOUT '#') for alpha tints.
- Usage week window: Sunday start via `CultureInfo.InvariantCulture` — do
  NOT use CurrentCulture there.
- Codex fixture vectors are (Input, Cached, Output, Total); per-entry usage
  comes from `last_token_usage`.
- GitHub API from this machine without a token is 403 rate-limited; manual
  update checks masquerade as "up to date". `gh` CLI NOT authenticated.
  Releases: v0.10.0 uploaded; v0.21.0 was being uploaded by hand at M23
  kickoff — confirm its state in the browser before doing v0.22.0 (zip name
  `PokeTokenBar-0.22.0-win-x64.zip`, tag `v0.22.0`, exe only).
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI for BASE DATA, macOS-card theme, 40px dex tiles,
  saturation-dimming, official limits via credentials.

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the M24 choice with the user before building anything. Visual work gets a
  rendered sample/screenshot checkpoint BEFORE polishing everything (M13
  40px-tile rejection; M19/M20/M23 mid-review corrections all paid off).
- Evidence: unit tests for pure parts; UI behavior manual by the user;
  `PTB_STATE_DIR` sandbox + offscreen screenshot harness for layout checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file; update `docs/ui-parity-audit.md` when a slice lands.

## Open Risks
- v0.22.0 release upload is manual (user, browser); until it is uploaded,
  installed 0.10.0–0.21.0 clients see no update (v0.21.0 upload state
  unverified from this machine — check the releases page first).
- Status-line burn is a refresh-delta approximation, not macOS's active-block
  metric: with long refresh intervals the tier lags real coding pace by one
  interval, and `Tired` never occurs (no limits feature). Fine for a mood
  line; revisit only if the user notices.
- Catch-log chain names for OLD dex entries (pre-names saves) show "#id"
  fallbacks — same population the dex grid handles with "#id" fallback names.
- Egg imminent/guarantee visuals still never seen live (user had an active
  mon at M20–M23 close). First guaranteed-tier egg purchase (상점 희귀 알)
  is the live field test.
- Shiny ✨ surfaces (detail header M22, log rows M23) — still never seen live
  by the user on a real shiny (no shiny mon since 09-23).
- Launch-at-login: registry write failures revert the checkbox silently
  (logged only); no error text row (macOS shows one). Revisit if the user
  ever hits a failure.
- XamlAnimatedGif hobby-maintained — static PNG fallback exists.

## Acceptance Criteria
- [ ] M24 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (484 + new)
- [ ] `docs/ui-parity-audit.md` updated for the shipped items
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.23.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/ui-parity-audit.md` — candidate list (authoritative)
- `docs/windows-port-plan.md` — canonical plan (closure + reopened polish)
- `docs/handoff/2026-09-29-milestone-23-ui-polish-next.md` — M23 kickoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen
  handoff (full trap list, still canonical)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` —
  session continuity entries (M0–M23)

## Start Prompt
```text
Read docs/handoff/2026-09-30-milestone-24-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M23 are done (0.22.0
shipped: catch log view, companion status line, shop item sprites, launch at
login). The candidate list lives in docs/ui-parity-audit.md — confirm the
M24 slice with the user FIRST (leading candidate: celebration animation
package; incident banner deprioritized; Tier-3 dark mode large;
engineering-quality remainder can ride along). For visual work render a
sample/screenshot checkpoint before polishing everything. Implement that
slice only, prove pure parts with tests, update the audit doc, run the
manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx. Do
not bundle other work into the slice. If other threads are working this same
checkout, coordinate landings — this repo pushes straight to main.
```
