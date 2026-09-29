# Handoff — Milestone 22: next UI-parity slice (confirm choice first)

## Goal
The project is reopened for UI parity polish with the macOS original.
M12–M21 are done and user-verified (see `docs/windows-port-plan.md`
"Reopened" section). M21 (0.20.0) shipped the last Tier-1 item (representative
Pokémon ★) plus the Tier-2 usage home redesign (today/week/month header,
provider chips, per-provider token breakdown) from `docs/ui-parity-audit.md`.
M22 is the next slice from that audit — confirm the choice with the user
FIRST (see Decision Needed at Start); do not start scaffolding before that.
NOTE: the user plans to finish the remaining audit items across MULTIPLE
threads. If more than one session works this same checkout in parallel, they
will conflict (repo pushes straight to `main`); coordinate so slices touching
`DashboardWindow.xaml.cs`/`DashboardText.cs` land sequentially.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2
  machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR
  flow used so far)
- Commits: M21 = feat commit (representative + usage home) + docs close-out
  (plan + audit + THIS handoff) on top of `8f255b1` (M21 kickoff docs)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M21 (all user-verified on 0.20.0):
  - Representative Pokémon (Tier 1 closed): `CompanionEngine.SetRepresentative(
    int?, UnownForm?)` validates `OwnsSpecies` (mirrors macOS
    `setRepresentativeSpeciesID`, false on unowned, nil clears), persists,
    raises `Changed`. Three new `CompanionGameView` fields inserted BEFORE
    `UnownForms` (record contract): `RepresentativeSpeciesID`,
    `RepresentativeIsShiny` (`OwnsShinySpecies`), `RepresentativeUnownForm`
    (`UnownForms.Resolved`). Floating pet follows the representative when set
    (egg only when following current), dex tile shows ★ in the number row +
    accent-tinted `Border` wrap + "대표" in tooltip, species detail window has
    a ★/☆ header toggle (Unown passes the selected form — form-aware match),
    settings general group has a representative row (selection text with ✨,
    follow-current reset, "choose in Pokédex…" → `App.OpenDashboardOnDexTab`
    → `DashboardWindow.SelectDexTab`, index 2). SettingsWindow subscribes
    `engine.Changed` to refresh the row (unsubscribed on Closed).
  - Usage home redesign (Tier 2): usage tab = Today card (caption
    `TodayTokensHeader`, big compact number Consolas 26 + grouped caption +
    cost right, `ThisWeekLabel`/`ThisMonthLabel` period row shown when
    week>0 || month>0, cost gated on `CostCoverage.HasKnown`) → month trend
    (unchanged) → provider card. Provider card: chip capsules when >1
    provider has usage (`_selectedProviderId` persists, falls back to first),
    selected-provider detail (name + today compact + cost, 입력/출력/캐시
    쓰기/캐시 읽기 rows, per-model rows when >1), unavailable providers as a
    muted "Name (없음)" line, card collapses when empty. Old `ProvidersList`
    + Combined card GONE. Chips re-render via `_lastUsageState`.
  - Data: `ProviderUsageSummary` += today input/output/cache-write/cache-read,
    `WeekTokens`/`WeekCost`, `TodayCostCoverage`/`WeekCostCoverage`,
    `TodayModels` (all optional params appended). `UsageDisplayState` +=
    `WeekTokens`/`WeekCost` + 3 cost coverages (optional). Refresh service
    computes week via `UsageAggregation.StartOfWeek(localNow,
    CultureInfo.InvariantCulture)` (Sunday start, deterministic) and per-
    provider today with `includeModels: true`.
  - Strings: 12 macOS-verbatim (representative label/follow/choose/set/badge,
    today-tokens, this-week/this-month, input/output/cache-write/cache-read).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 444 green (439 + 2 engine
  representative, 2 usage refresh week/breakdown, 1 strings). Smoke harness
  `%TEMP%\opencode\ptb-m21-smoke\Program.cs` (seeds dex 1/10/50 + shiny 10,
  representative 10, rich multi-provider usage incl. per-provider models +
  one unavailable provider) renders 5 PNGs (usage/dex/settings/detail/pet)
  and probes layout (usage tab bottom 477px < 800). Manual: 0.20.0
  published, relaunched, user verified chips/today header/★/pet-follow/
  settings row/persist-across-restart.
- The audit doc `docs/ui-parity-audit.md` tracks every remaining item with
  macOS file references — keep it updated when slices land.

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
  sprites remain the separate online-cached concern (`SpriteStore`). Shop/
  bag item icons use `FallbackEmoji`.
- Packaging: version = Ui csproj, bumped with the shipping slice (0.20.0 =
  M21; M22 ships 0.21.0). Publish via `scripts\publish-windows.ps1` (kills
  the installed app; relaunch the exe yourself). Releases by hand,
  `v<Version>` tags; update checker wired to `Strongorange/PokeTokenBar-windows`.
- State under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR` overrides).
  Language is saved-game state via `SetLanguage`. README en+ko only.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
Pick from `docs/ui-parity-audit.md` (each its own slice; user may pick
something new). Remaining:
- Tier 2: celebration animation package (flash/pop/✨/🎭/+XP/mint sparkle/
  egg wiggle; needs a celebration queue in the engine + WPF storyboards).
  Catch-log view (per-individual rows w/ evolution chains, natures,
  caught-at; Core `DexEntry` has the data). Companion status line (port
  macOS `computeState` machine). Detail-window polish (type capsules,
  individual picker, actual-stats). Incident banner (statuspage.io, no auth,
  network + privacy doc).
- Tier 3: shop item sprite images; dark mode; launch-at-login.
- Engineering quality (user principle: every unit modular and individually
  testable): extract pure UI-adjacent logic from `DashboardWindow.xaml.cs`
  (~1300 lines — dex filter decision, capsule/color mapping incl. `ColorOf`
  duplicating Theme brushes, bag/shop row presentation states, usage/provider
  presentation decisions added in M21) into Core/Application helpers with
  unit tests. Can ride along with a UI slice that touches the same code.

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `PlanRareCandyUse(int)`, `MaxRareCandyUseCount()`, `SetLanguage`,
  `SetRepresentative(int?, UnownForm?) -> bool`, `View()` →
  `CompanionGameView` (…, `EvoLine`, `DexRows`, `ShopRows`, `Bag`,
  `AvailableTokens`, `RecentEvents`, `EggGuarantee`, `RepresentativeSpeciesID`,
  `RepresentativeIsShiny`, `RepresentativeUnownForm`, ends `UnownForms,
  AppLanguage Language`), `Detail(speciesID)`, `Changed`, `DrainNotices()`,
  `ExportSave`/`ImportSave`. NEW VIEW FIELDS GO BEFORE `UnownForms`.
- `CompanionShopRow(ItemKind? Item, Rarity? EggTier, string Label, long
  Price, bool CanBuy)` / `CompanionBagItem(Kind, Label, Count, CanUse)` —
  record value equality powers confirm-state matching.
- Usage: `UsageDisplayState(AsOfUtc, Providers, Today*, Month*, MonthDaily?,
  TodayModels?, WeekTokens, WeekCost, Today/Week/MonthCostCoverage)` and
  `ProviderUsageSummary(…, Today/Month, today breakdown ×4, Week*,
  coverages, TodayModels?)` — extensions appended as optional params only.
- UpdateChecker (M17, unchanged): `UpdateCheckerOptions`, `Available`/
  `Skipped`/`UpdateTarget`/`SettingsNotice`, `CheckAsync`, `Consider`/
  `SkipCurrent`/`ShowSkippedAgain`, static `IsSafeReleaseUrl`.
- Settings: `AppSettings` incl. `SkippedUpdateVersion`,
  `UpdateNotificationsEnabled`; guarded `Application.Current is App` checks.
- New fixed UI strings go in `src/Core/DashboardText.cs` macOS-verbatim
  (ko/en/ja/es/fr/pt/de order matches `DashboardText.T`); add a Core.Tests
  string test in the same slice.
- Theme resources via `{DynamicResource <Key>Brush}` or `Token(key)`; new
  windows merge Theme.xaml per-window AND in App.xaml.
- Sprites through `SpriteSlot` (cached-first, never block UI); static
  thumbnails >44px use NearestNeighbor.
- Snapshot schema 2 (loader accepts 1). Regenerate only via
  `scripts/generate-pokemon-snapshot.ps1` under pwsh.

## Relevant Files
- `docs/ui-parity-audit.md` — the candidate list (update per slice)
- `src/Ui/DashboardWindow.xaml(.cs)` — usage today/provider cards, dex ★
  tile, `Token`/`RarityBrush`/`HexBrush`/`ColorOf` helpers, 4 tab headers by
  index in `LocalizeStaticText` (dex tab = index 2, see `SelectDexTab`)
- `src/Ui/SpeciesDetailWindow.xaml.cs` — ★/☆ representative toggle in
  header (form-aware for Unown), ctor takes engine
- `src/Ui/SettingsWindow.xaml(.cs)` — representative row,
  `RenderRepresentativeRow`, `engine.Changed` subscription
- `src/Ui/FloatingPetWindow.xaml.cs` — `Update` maps representative→subject
- `src/Core/DashboardText.cs` + Tests/Core.Tests/DashboardTextTests.cs
- `src/Application/CompanionEngine.cs` — `SetRepresentative` (near
  `SetLanguage`), representative fields in `BuildView`
- `src/Application/UsageDisplayState.cs` + `UsageRefreshService.cs` —
  week/breakdown/coverage extensions
- macOS references: `Sources/PokeTokenBar/UI/CompanionView.swift`
  (CompanionHeader 487-717, `RepresentativeFooterButton` 873,
  DexSpeciesCell 1334, PokemonDetailView 1047), `PopoverView.swift`
  (header 150-224, providerRow 203-281, ProviderTabBar 1041), `BagView.swift`,
  `ShopView.swift`, `SettingsView.swift` 145-180 (representative row)
- `docs/handoff/2026-09-29-milestone-21-ui-polish-next.md` — M21 kickoff
  handoff (superseded by this file)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 444 tests) —
  `MSB1009` if you type `.sln`. Kill DEBUG `PokeTokenBar.exe` before
  testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- Smoke apps (`dotnet run Program.cs`): need
  `#:property TargetFramework=net10.0-windows`, `UseWPF=true`,
  `PublishTrimmed=false` (NETSDK1168), `#:project <abs Ui csproj>`;
  x:Name fields are internal → `FindName`. Seeding: `engine.State.Dex`
  (`DexEntry(baseID, finalID, chain, rarity, caughtAt, isShiny: …)`),
  `engine.State.Inventory[ItemKinds.Raw(kind)]`; first `ApplyUsage(map,
  date, true)` only sets the baseline (wallet 0) — a second higher call
  creates spendable delta. Synthetic DexEntries without `Names` render
  "#id" fallback names — harmless smoke artifact.
- Offscreen screenshots: arrange `window.Content`, `RenderTargetBitmap` at
  1.5x; tabs lazily realized — SelectedIndex then UpdateLayout, re-FindName
  after re-renders. `new SpriteStore(realCacheDir, _ => null)` = real
  cached sprites offline (works for FloatingPetWindow too).
- WPF gotchas: no `StackPanel.Spacing`; `new X { ... } { Children = ... }`
  is invalid C#; `HexBrush("#14" + hex)` — hex constants must NOT contain
  '#' (FormatException at render time, not compile time);
  `Math.Round(float)` is ambiguous → cast to double first; `long` → `int`
  implicit conversions don't exist (bag Count cast); pattern variable from
  `x is { } y` is NOT definitely-assigned when the test result is stored in
  another bool used later (CS0165) — assign `var id = x;` then `id is null`
  checks instead.
- `CompanionGameView` field order is part of the contract — new view fields
  go before `UnownForms`; tests construct positionally in theory, current
  tests build via `engine.View()`.
- Capsule/pill pattern: Border CornerRadius=half-height + Padding 6-8,1 +
  8pt Bold text; rarity colors live in Theme (`RarityLegendaryBrush`
  #F7630C etc.) and `ColorOf` (hex WITHOUT '#') for alpha tints. Provider
  chips follow the dex rarity-filter capsule pattern.
- Usage week window: Sunday start via `CultureInfo.InvariantCulture` — do
  NOT use CurrentCulture there (test determinism across machines).
- Codex fixture vectors are (Input, Cached, Output, Total) and per-entry
  usage comes from `last_token_usage` — check before asserting breakdowns.
- Bag/shop confirm states are fields reset by full re-render on every
  `UpdateGame`; provider chip selection `_selectedProviderId` survives
  refreshes, falls back to first provider with usage.
- GitHub API from this machine without a token is 403 rate-limited; manual
  update checks masquerade as "up to date" (same as macOS). `gh` CLI NOT
  authenticated. Releases: only `v0.10.0` exists (0.11–0.20 never
  uploaded).
- Normal and unrelated: git CRLF warnings; `docker-desktop` in `wsl -l -v`;
  SSH trap if push denied (`ssh-add -d <key>; ssh-add <key>`, verify
  `ssh -T git@github-personal` greets `Strongorange`).
- Already tried and rejected: file watchers over the WSL boundary, live
  PokeAPI for BASE DATA, macOS-card theme (user picked Fluent from a
  rendered comparison), 40px dex tiles, saturation-dimming, official
  limits via credentials.

## Working Agreement
- Slice per milestone; report after the milestone or when blocked. Confirm
  the M22 choice with the user before building anything. Visual work gets a
  rendered sample/screenshot checkpoint BEFORE polishing everything (M13
  40px-tile rejection; M19/M20 mid-review course corrections both paid off).
- Evidence: unit tests for pure parts; UI behavior manual by the user;
  `PTB_STATE_DIR` sandbox + offscreen screenshot harness for layout checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file; update `docs/ui-parity-audit.md` when a slice lands.

## Open Risks
- Representative ★ never seen on a REAL dex with many tiles (smoke had 5
  tiles); the accent Border wrap adds ~4px width to the representative tile
  — watch the WrapPanel flow when the rep sits at a row edge.
- Provider card collapses to hidden when NO provider has usage and none
  are unavailable (fresh install with empty logs) — today card still shows
  zeros; verify this reads fine on a fresh `PTB_STATE_DIR` sandbox.
- Unavailable-provider line is Windows-only (macOS hides them entirely) —
  kept for parity with the old list info; revisit if the user dislikes it.
- Egg imminent/guarantee visuals still never seen live (user had an active
  mon at M20 close; still active at M21 close). First guaranteed-tier egg
  purchase (상점 희귀 알) is the live field test.
- Shiny ✨ still never seen live by the user (no shiny mon since 09-23);
  representative row does show ✨ from dex shiny state.
- XamlAnimatedGif hobby-maintained — static PNG fallback exists.
- Update checker dormant until a release above the installed version is
  uploaded (0.11–0.20 were never uploaded).

## Acceptance Criteria
- [ ] M22 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (444 + new)
- [ ] `docs/ui-parity-audit.md` updated for the shipped items
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.21.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/ui-parity-audit.md` — candidate list (authoritative)
- `docs/windows-port-plan.md` — canonical plan (closure + reopened polish)
- `docs/handoff/2026-09-29-milestone-21-ui-polish-next.md` — M21 kickoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen
  handoff (full trap list, still canonical)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` —
  session continuity entries (M0–M21)

## Start Prompt
```text
Read docs/handoff/2026-09-29-milestone-22-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M21 are done (0.20.0
shipped: representative Pokémon ★, usage home redesign with provider chips
and token breakdown). The candidate list lives in docs/ui-parity-audit.md —
confirm the M22 slice with the user FIRST (Tier-2: celebration animation
package, catch-log view, status line, detail polish, incident banner; Tier-3
constrained; engineering-quality extraction can ride along). For visual work
render a sample/screenshot checkpoint before polishing everything. Implement
that slice only, prove pure parts with tests, update the audit doc, run the
manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx.
Do not bundle other work into the slice. If other threads are working this
same checkout, coordinate landings — this repo pushes straight to main.
```
