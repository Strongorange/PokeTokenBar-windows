# UI parity audit — macOS reference vs Windows port

Living document. Updated at each milestone close. Source of truth for
"which macOS-designed UI/UX is still missing on Windows". Scope rule: only
items where the macOS original already provides the design reference
(`Sources/PokeTokenBar/UI/*.swift`). Engine/data cost is noted per item so
slices can be planned honestly.

Legend: ☐ todo · ◐ in progress · ☑ done (milestone) · ✗ rejected (reason)

## Tier 1 — engine data already on Windows, pure UI port

| Item | macOS reference | Windows status | Data |
|------|-----------------|----------------|------|
| ☑ Dex rarity filter capsules (count per rarity, tap to filter) | CompanionView.swift `RarityTally`/`DexGridView.header` | M20 | `CompanionDexRow.Rarity` |
| ☑ Companion capsules: rarity chip at name, growth-boost chip | CompanionView.swift `CompanionHeader` 552-576 | M20 | `Rarity`, `HasGrowthBoost` |
| ☑ Egg state: imminent wording/color (>=90%), guarantee capsule | CompanionView.swift 583-613 | M20 | `EggProgress`; `EggTier` (needed view field) |
| ☑ Progress captions `다음 진화까지/졸업까지/부화까지` | CompanionView.swift 578-606 | M20 | Stage/Egg thresholds |
| ☑ Candy batch stepper + plan preview (graduate/carryover/discard XP) | BagView.swift 49-98 | M20 | `PlanRareCandyUse()` already implemented, was unused |
| ☑ Bag per-item cards w/ inline-use confirm; mint/charm hints | BagView.swift `ItemCard` | M20 | `Bag`, `CanUse` |
| ☑ Empty states: dex = animated Pikachu + copy, bag = Snorlax | CollectionView/BagView `emptyState` | M20 | sprites 25/143 |
| ☑ Representative Pokémon (★ set in dex, engine keeps selection valid) | `RepresentativeFooterButton`, settings row | M21 | `RepresentativeSpeciesID`/`ReconcileRepresentativeSelection` existed since initial port; engine `SetRepresentative` + view fields, dex ★ tile, detail toggle, settings row, floating pet follows representative |

## Tier 2 — small data extension + UI

| Item | macOS reference | Windows status | Needed |
|------|-----------------|----------------|--------|
| ☑ Usage home redesign: big today number + grouped + cost, week/month labels | PopoverView.swift `header` 150-224 | M21 | week totals via `UsageAggregation.Period` (Sunday start, invariant) in refresh service |
| ☑ Provider chip tab + per-provider detail (input/output/cache-write/cache-read, model rows) | `ProviderTabBar`, `providerRow` 203-281 | M21 | `ProviderUsageSummary` extended (today breakdown, week totals, models, cost coverages) |
| ☑ Hatch/evolve celebration package: white flash + spring pop + ✨/🎭 burst, candy "+XP" capsule, mint sparkle, egg wiggle | CompanionView.swift 642-703 | M24 | Core `CelebrationQueue` (drain-based, cap 8) + engine `DrainCelebrations()` enqueue at hatch (after carry-over growth, disguise hides shiny)/evolve/ditto-reveal/candy-XP/mint; UI `CompanionCelebrationPlayer` (flash+pop ElasticEase, delayed bursts, orange "+XP" capsule, mint cluster, repeating egg wiggle ≥90%). WPF renders emoji monochrome → ✨/🎭 are vector glyphs (`CelebrationGlyphs`: gold 4-point star, comedy/tragedy masks) |
| ☑ Catch log view: per-individual rows (rarity capsule, evolution-chain sprites, nature, caught-at, released badge) + rarity filter | `CollectionView.catchLog`, `DexEntryRow` 1460 | M23 | engine `BuildCatchRows` (active pinned first, caught-at desc, nulls last) + `CompanionCatchRow` view rows; dex-tab segment toggle (Pokédex / catch log), per-individual cards w/ chain sprites + relative caught-at (`RelativeTimes` buckets) |
| ☑ Companion status line (egg/idle/working/focus/tired/sleep/level-up) | CompanionView.swift `statusLine` 705 | M23 | Core `CompanionStatus.Compute` (pure port of `computeState`; burn tiers = macOS thresholds); engine approximates burn from refresh deltas; 4s level-up window on hatch/evolve/reveal/graduate; `tired` reachable but unused (no limits feature on Windows) |
| ☑ Detail window polish: type color capsules, individual picker (multi-catch), actual-stats section w/ IV | `PokemonDetailView` 1047 | M22 | no engine change; species-data section (capsules + value pairs + abilities), picker, XOR base/actual stats, `DisplayScaleMaximum` bars |
| ✗ Provider incident banner (statuspage.io, no auth) | `providerStatusBanner` 303 | rejected by user | deprioritized at M23 ("장애 배너까지는 굳이") and rejected outright at the 0.25.0 close ("다크모드랑 장애 배너는 굳이야", 2026-09-30) |
| ☑ Shop card inline-confirm parity: item two-step (idle price → confirm), egg shiny re-confirm, basic-egg card as egg | ShopView.swift `ShopItemCard.buyControls`/`EggCard.controls` | M25 | fixed two pre-existing bugs while extracting the shop builders to `ShopCards` (Ui): item cards rendered the confirm row unconditionally (cancel was a no-op), and the basic egg row (Item=null, EggTier=null) fell through to the item-card builder showing the rare-candy icon/description/owned count; egg confirm now follows the macOS three-stage flow (idle → confirm → shiny-discard warning) via Application `ShopFlow` |

## Tier 3 — constrained / large

| Item | macOS reference | Status |
|------|-----------------|--------|
| ✗ Official limit gauges (5h/weekly, pace marker, depletion forecast) | PopoverView.swift `limitsSection` | rejected by default: requires provider OAuth credentials ("never read credentials" rule); revisit only with an explicit user-designed opt-in |
| ☑ Shop item sprite images (PokeAPI items, emoji fallback) | `ItemIconView` | M23 | `ItemIconSlot` (cached-first, emoji fallback; mint has no sprite → 🌿 by design); `SpriteStore.Item`/`CachedItem` + `SpriteCatalog.ItemUrl` (`item-{name}.png` cache keys, macOS-compatible) |
| ✗ Dark mode | n/a (macOS free via materials) | rejected by user ("굳이", 2026-09-30); also large: token swap + title bar, and no macOS design reference to port |
| ☑ Launch at login setting | SettingsView general group | M23 | `Platform.Windows.LoginItem` — HKCU Run key (registry access delegate-injected for tests), settings general-group toggle |

## Engineering quality (non-parity, user principle: every unit modular + individually testable)

| Item | Status |
|--------|--------|
| ◑ Extract pure UI-adjacent logic from `DashboardWindow.xaml.cs` into testable helpers in Core/Application so it gets unit coverage instead of headless-manual-only | M23: `CompanionPresentation` (rarity display order + hex colors, dex/catch-log visible-row filters, provider-chip selection fallback) + `RelativeTimes` (caught-at buckets) extracted and unit-tested; also fixed a pre-existing bug (dex double-click used the filtered index against the unfiltered list). M25: bag/shop confirm-state machinery → Application `ShopFlow` (unit-tested, incl. the macOS three-stage egg confirm), usage-home decisions → `UsagePresentation` + `DailyTrendMetrics.Peak/ShowsCost/TodayUsage`, WPF builders → Ui-internal `UsageHomeRenderer`/`ShopCards`/`Paint`; code-behind 1,629→799 lines; two shop bugs fixed (item-card cancel no-op, basic-egg card rendered as rare-candy card). Remaining in the code-behind: game-tab builders (companion header, dex tiles, catch cards, evolution line) |

## Done history (UI parity)

- M12 sprites + 7-language localization · M13 dex tile grid · M14 month
  trend chart · M15 settings window · M16 Unown per-form sprites · M17
  update notifications · M18 evolution line + ✨ shiny markers · M19 Fluent
  theme + resizable window + dex/shop tabs (macOS `ShopView` parity) ·
  M20 companion/dex/bag Tier-1 bundle · M21 representative Pokémon ★ +
  usage home redesign (today/week/month header, provider chips, token
  breakdown) · M22 detail-window polish (type capsules, individual picker,
  actual stats w/ IV, species-data section) · M23 catch-log view +
  companion status line + shop item sprites + launch at login ·
  M24 celebration package (hatch/evolve/ditto flash+pop+bursts, candy "+XP"
  capsule, mint sparkle, egg wiggle) · M25 shop/usage extraction + shop
  confirm parity (item two-step, egg shiny re-confirm, basic-egg card fix).
