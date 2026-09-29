# Handoff — Milestone 21: next UI-parity slice (confirm choice first)

## Goal
The project was reopened for UI parity polish with the macOS original.
M12–M19 are done and user-verified (see `docs/windows-port-plan.md`
"Reopened" section). M20 (0.19.0) shipped the Tier-1 bundle from
`docs/ui-parity-audit.md` — the living macOS-vs-Windows gap list that IS the
candidate list now. M21 is the next slice from that audit — confirm the
choice with the user FIRST (see Decision Needed at Start); do not start
scaffolding before that.

## Workspace
- Checkout: `C:\Users\USER\my-pjts\poketoken-bars-windows` (Windows 11 25H2
  machine, target environment itself)
- Branch: `main` / Base: `main` (this repo pushes directly to `main`; no PR
  flow used so far)
- Commits: M20 = feat commit (Tier-1 bundle) + docs close-out (plan + audit
  + THIS handoff) on top of `470da66` (M19 docs)
- Commit convention: English conventional commits (`feat:`, `fix:`, `docs:`)

## Current State
- Done — M20 Tier-1 bundle (all engine-data-ready parity items, user-verified
  on 0.19.0):
  - Companion card: rarity capsule at the name (rarity brush, uppercase,
    `RarityCapsule`/`RarityCapsuleText` in XAML), growth-boost capsule
    (`StatusCapsule`, orange tint), egg "곧 부화해요!" at `EggProgress >= 0.9`
    with accent foreground, egg guarantee capsule from the NEW
    `CompanionGameView.EggGuarantee` field (engine: `!hasActive ?
    _state.EggTier : null`, inserted before `UnownForms` — record still ends
    `UnownForms, AppLanguage Language`), captions switched to
    `ToNextEvolution`/`ToGraduation`/`EggToHatch` (threshold minus used).
    `CompanionDetail` now shows stage (+ raising nature via
    `RaisingNature()`), not rarity text.
  - Dex tab: rarity filter capsules (`RenderDexFilter`: Legendary → Common
    order, count>0 only, tap toggles `_dexRarityFilter` and re-renders;
    selected = solid rarity border + tinted bg) and empty state
    (`DexEmpty` Border with animated Pikachu `SpriteSlot` 25 + title/hint).
  - Bag card → per-item cards (`RenderBagCards`/`CreateBagCard`): candy
    stepper (`_candyCount`, clamp 1..`MaxRareCandyUseCount`) + plan preview
    from `PlanRareCandyUse` (Graduates→hint, DiscardedXP>0→orange warning,
    else Evolves→carryover) + inline use confirm (`_confirmingBagItem`,
    `UseOnCurrent`); mint hint + confirm; passive charm = green
    `ShinyCharmEffectHint`; empty bag = Snorlax 143 mini + `BagEmptyTitle`.
    Old `UseCandyButton`/`UseMintButton`/`UseAllButton`/`BagText` GONE
    (`UseItem()` is the shared action).
  - `RarityLabel` corrected to macOS capitalization ("Common"/"Uncommon"/
    "Rare"/"Legendary" — were lowercase; shop egg capsules already
    uppercased so only tooltips/detail text changed visibly).
- Strings: ~22 macOS-verbatim additions (egg incubating/imminent/to-hatch,
  to-next-evolution/to-graduation, growth-boost, egg-guarantee-hint,
  dex-filter-hint, dex-empty title/hint, bag-empty-title, use-item-label,
  use-on-current, use-after-hatch, use-needs-pokemon, candy graduates/
  carryover/discarded hints, mint-effect-hint, shiny-charm-effect-hint).
- Evidence: `dotnet test PokeTokenBar.Windows.slnx` 439 green (435 + 4: 1
  engine `EggGuaranteeSurfacesTierWhileIncubatingAndNullAfterHatch`, 1
  companion/dex polish strings test incl. capitalized RarityLabel, plus the
  M19-era count already included). Smoke harness
  `%TEMP%\opencode\ptb-m19-smoke\Program.cs` seeds dex entries (3 rarities),
  inventory (candy 3/mint 1/charm 1) and probes the new elements
  (RarityCapsule text, ProgressLabel caption, BagCards=3,
  DexRarityFilter capsules=3). Manual: 0.19.0 published, relaunched, user
  verified capsules/stepper/preview/filter. Egg-state visuals (imminent +
  guarantee capsule) not yet seen live — user has an active mon; logic is
  engine-tested + headless.
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
  / `{DynamicResource}`). Dashboard resizable (min 560x640, default
  640x800); settings/detail windows fixed `CanMinimize`.
- Official provider limits stay OUT (needs OAuth credentials; violates the
  no-credentials rule) unless the user explicitly designs an opt-in.
- Pokemon base data stays the bundled schema-2 snapshot (offline-first);
  sprites remain the separate online-cached concern (`SpriteStore`). Shop/
  bag item icons use `FallbackEmoji`.
- Packaging: version = Ui csproj, bumped with the shipping slice (0.19.0 =
  M20; M21 ships 0.20.0). Publish via `scripts\publish-windows.ps1` (kills
  the installed app; relaunch the exe yourself). Releases by hand,
  `v<Version>` tags; update checker wired to `Strongorange/PokeTokenBar-windows`.
- State under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR` overrides).
  Language is saved-game state via `SetLanguage`. README en+ko only.
- Never read/copy credentials. Comments: none unless asked. English commits.
  Commit + push only when the user asks. Remote `origin` =
  `git@github-personal:Strongorange/PokeTokenBar-windows.git`.

## Decision Needed at Start (confirm with user before scaffolding)
Pick from `docs/ui-parity-audit.md` (each its own slice; user may pick
something new):
- Tier 1 remainder: representative Pokémon (★ set from dex; Core state +
  reconcile already exist unused).
- Tier 2: usage home redesign (big today number, week/month, provider chips,
  token-type breakdown) — biggest felt change; needs `ProviderUsageSummary`
  extension. Celebration animation package (flash/pop/✨/🎭/+XP/mint
  sparkle/egg wiggle; needs a celebration queue in the engine + WPF
  storyboards). Catch-log view (per-individual rows w/ evolution chains,
  natures, caught-at; Core `DexEntry` has the data). Companion status line.
  Detail-window polish (type capsules, individual picker, actual-stats).
  Incident banner (statuspage.io, no auth, network + privacy doc).
- Tier 3: shop item sprite images; dark mode; launch-at-login.
- Engineering quality (user principle from M20 close: every unit modular and
  individually testable): extract pure UI-adjacent logic from
  `DashboardWindow.xaml.cs` (~1200 lines — dex filter decision, capsule
  color mapping incl. `ColorOf` duplicating Theme brushes, bag/shop row
  presentation states) into Core/Application helpers with unit tests.
  Can ride along with a UI slice that touches the same code.

## Contracts
- Engine: `ApplyUsage(...)`, shop/candy/mint/difficulty members,
  `PlanRareCandyUse(int)`, `MaxRareCandyUseCount()`, `SetLanguage`,
  `View()` → `CompanionGameView` (…, `EvoLine`, `DexRows`, `ShopRows`,
  `Bag`, `AvailableTokens`, `RecentEvents`, `EggGuarantee`, ends
  `UnownForms, AppLanguage Language`), `Detail(speciesID)`, `Changed`,
  `DrainNotices()`, `ExportSave`/`ImportSave`.
- `CompanionShopRow(ItemKind? Item, Rarity? EggTier, string Label, long
  Price, bool CanBuy)` and `CompanionBagItem(Kind, Label, Count, CanUse)` —
  record value equality powers the confirm-state matching
  (`_confirmingShopRow`/`_confirmingBagItem`).
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
- `src/Ui/DashboardWindow.xaml(.cs)` — capsules, dex filter, bag cards,
  `Token`/`RarityBrush`/`HexBrush`/`ColorOf` helpers, 4 tab headers by
  index in `LocalizeStaticText`
- `src/Core/DashboardText.cs` + tests/Core.Tests/DashboardTextTests.cs
- `src/Application/CompanionEngine.cs` — `CompanionGameView.EggGuarantee`,
  `PlanRareCandyUse` (781)
- macOS references: `Sources/PokeTokenBar/UI/CompanionView.swift`
  (CompanionHeader 487-717, RarityTally, DexGridView, DexEntryRow),
  `BagView.swift`, `PopoverView.swift`, `ShopView.swift`
- `docs/handoff/2026-09-29-milestone-20-ui-polish-next.md` — M20 kickoff
  handoff (superseded by this file)

## Hard-won Context
- Run/test: `dotnet test PokeTokenBar.Windows.slnx` (~15s, 439 tests) —
  `MSB1009` if you type `.sln`. Kill DEBUG `PokeTokenBar.exe` before
  testing (`MSB3027`); a running INSTALLED exe does NOT block builds.
- Smoke apps (`dotnet run Program.cs`): need
  `#:property TargetFramework=net10.0-windows`, `UseWPF=true`,
  `PublishTrimmed=false` (NETSDK1168), `#:project <abs Ui csproj>`;
  x:Name fields are internal → `FindName`. Seeding: `engine.State.Dex`
  (`DexEntry(baseID, finalID, chain, rarity, caughtAt, isShiny: …)`),
  `engine.State.Inventory[ItemKinds.Raw(kind)]`; first `ApplyUsage(map,
  date, true)` only sets the baseline (wallet 0) — a second higher call
  creates spendable delta.
- Offscreen screenshots: arrange `window.Content`, `RenderTargetBitmap` at
  1.5x; tabs lazily realized — SelectedIndex then UpdateLayout, re-FindName
  after re-renders. `new SpriteStore(realCacheDir, _ => null)` = real
  cached sprites offline.
- WPF gotchas: no `StackPanel.Spacing`; `new X { ... } { Children = ... }`
  is invalid C#; `HexBrush("#14" + hex)` — hex constants must NOT contain
  '#' (FormatException at render time, not compile time);
  `Math.Round(float)` is ambiguous → cast to double first; `long` → `int`
  implicit conversions don't exist (bag Count cast).
- `CompanionGameView` field order is part of the test contract — new view
  fields go before `UnownForms` and tests construct positionally; check
  `CompanionEngineTests` after touching the record.
- Capsule/pill pattern: Border CornerRadius=half-height + Padding 6-8,1 +
  8pt Bold text; rarity colors live in Theme (`RarityLegendaryBrush`
  #F7630C etc.) and `ColorOf` (hex WITHOUT '#') for alpha tints.
- Bag/ Shop confirm states are fields (`_confirmingBagItem`,
  `_confirmingShopRow`) reset by full re-render on every `UpdateGame` —
  acceptable today; revisit if refresh cadence changes.
- GitHub API from this machine without a token is 403 rate-limited; manual
  update checks masquerade as "up to date" (same as macOS). `gh` CLI NOT
  authenticated. Releases: only `v0.10.0` exists (0.11–0.19 never
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
  the M21 choice with the user before building anything. Visual work gets a
  rendered sample/screenshot checkpoint BEFORE polishing everything (M13
  40px-tile rejection; M19/M20 mid-review course corrections both paid off).
- Evidence: unit tests for pure parts; UI behavior manual by the user;
  `PTB_STATE_DIR` sandbox + offscreen screenshot harness for layout checks.
- Core/Providers/Application stay UI-free; UI references Application only.
- Tools & skills: `openviking` MCP — read/write session-continuity events
  (see `viking://user/owner/memories/events/2026/09/`); model the next
  handoff on this file; update `docs/ui-parity-audit.md` when a slice lands.

## Open Risks
- Egg imminent/guarantee visuals never seen live (user had an active mon at
  M20 close; engine-tested + headless). First guaranteed-tier egg purchase
  (상점 희귀 알) is the live field test.
- Rarity filter state (`_dexRarityFilter`) survives engine re-views; if the
  last mon of a rarity graduates away, counts drop to 0 and the capsule
  disappears — filter then hides everything until re-toggled. Cosmetic.
- Bag candy stepper resets to the clamped `_candyCount` on re-render —
  survives because the field persists; confirm-cancel resets nothing.
- Shiny ✨ still never seen live by the user (no shiny mon since 09-23).
- XamlAnimatedGif hobby-maintained — static PNG fallback exists.
- Update checker dormant until a release above the installed version is
  uploaded (0.11–0.19 were never uploaded).

## Acceptance Criteria
- [ ] M21 slice confirmed with the user before any code
- [ ] `dotnet test PokeTokenBar.Windows.slnx` fully green (439 + new)
- [ ] `docs/ui-parity-audit.md` updated for the shipped items
- [ ] Manual smoke on this machine incl. app restart; user confirms
      (publish via `scripts\publish-windows.ps1`; bump `<Version>` to
      0.20.0; relaunch the exe after publishing)
- [ ] Handoff for the next slice written and committed (if the series
      continues)

## Verification
- `dotnet test PokeTokenBar.Windows.slnx` — all tests pass
- Manual on this machine per slice; `PTB_STATE_DIR` sandbox for destructive
  checks; diagnostics log clean after runs

## Related Docs
- `docs/ui-parity-audit.md` — candidate list (authoritative)
- `docs/windows-port-plan.md` — canonical plan (closure + reopened polish)
- `docs/handoff/2026-09-29-milestone-20-ui-polish-next.md` — M20 kickoff
- `docs/handoff/2026-09-28-milestone-11-next-or-close.md` — pre-reopen
  handoff (full trap list, still canonical)
- OpenViking memory: `viking://user/owner/memories/events/2026/09/` —
  session continuity entries (M0–M20)

## Start Prompt
```text
Read docs/handoff/2026-09-29-milestone-21-ui-polish-next.md end to end.
Work in C:\Users\USER\my-pjts\poketoken-bars-windows, branch main, base main.
The project is reopened for UI parity polish; M12-M20 are done (0.19.0
shipped: Fluent theme, dex/shop tabs, companion/dex/bag parity bundle).
The candidate list lives in docs/ui-parity-audit.md — confirm the M21 slice
with the user FIRST (Tier-1 remainder: representative Pokémon; Tier-2:
usage home redesign, celebration animation package, catch-log view, status
line, detail polish, incident banner; Tier-3 constrained). For visual work
render a sample/screenshot checkpoint before polishing everything. Implement
that slice only, prove pure parts with tests, update the audit doc, run the
manual checklist, and verify with dotnet test PokeTokenBar.Windows.slnx.
Do not bundle other work into the slice.
```
