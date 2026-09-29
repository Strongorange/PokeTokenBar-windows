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
| ☐ Hatch/evolve celebration package: white flash + spring pop + ✨/🎭 burst, candy "+XP" capsule, mint sparkle, egg wiggle | CompanionView.swift 642-703 | todo | largest remaining Tier-2 item (celebration queue in engine + WPF storyboards) — deliberately kept as its own slice, not bundled into M23 |
| ☑ Catch log view: per-individual rows (rarity capsule, evolution-chain sprites, nature, caught-at, released badge) + rarity filter | `CollectionView.catchLog`, `DexEntryRow` 1460 | M23 | engine `BuildCatchRows` (active pinned first, caught-at desc, nulls last) + `CompanionCatchRow` view rows; dex-tab segment toggle (Pokédex / catch log), per-individual cards w/ chain sprites + relative caught-at (`RelativeTimes` buckets) |
| ☑ Companion status line (egg/idle/working/focus/tired/sleep/level-up) | CompanionView.swift `statusLine` 705 | M23 | Core `CompanionStatus.Compute` (pure port of `computeState`; burn tiers = macOS thresholds); engine approximates burn from refresh deltas; 4s level-up window on hatch/evolve/reveal/graduate; `tired` reachable but unused (no limits feature on Windows) |
| ☑ Detail window polish: type color capsules, individual picker (multi-catch), actual-stats section w/ IV | `PokemonDetailView` 1047 | M22 | no engine change; species-data section (capsules + value pairs + abilities), picker, XOR base/actual stats, `DisplayScaleMaximum` bars |
| ☐ Provider incident banner (statuspage.io, no auth) | `providerStatusBanner` 303 | todo | network fetch + privacy doc update; user deprioritized at M23 kickoff ("장애 배너까지는 굳이") |

## Tier 3 — constrained / large

| Item | macOS reference | Status |
|------|-----------------|--------|
| ✗ Official limit gauges (5h/weekly, pace marker, depletion forecast) | PopoverView.swift `limitsSection` | rejected by default: requires provider OAuth credentials ("never read credentials" rule); revisit only with an explicit user-designed opt-in |
| ☑ Shop item sprite images (PokeAPI items, emoji fallback) | `ItemIconView` | M23 | `ItemIconSlot` (cached-first, emoji fallback; mint has no sprite → 🌿 by design); `SpriteStore.Item`/`CachedItem` + `SpriteCatalog.ItemUrl` (`item-{name}.png` cache keys, macOS-compatible) |
| ☐ Dark mode | n/a (macOS free via materials) | large: token swap + title bar |
| ☑ Launch at login setting | SettingsView general group | M23 | `Platform.Windows.LoginItem` — HKCU Run key (registry access delegate-injected for tests), settings general-group toggle |

## Engineering quality (non-parity, user principle: every unit modular + individually testable)

| Item | Status |
|--------|--------|
| ☐ Extract pure UI-adjacent logic from `DashboardWindow.xaml.cs` into testable helpers in Core/Application so it gets unit coverage instead of headless-manual-only | ◐ M23: `CompanionPresentation` (rarity display order + hex colors, dex/catch-log visible-row filters, provider-chip selection fallback) + `RelativeTimes` (caught-at buckets) extracted and unit-tested; also fixed a pre-existing bug (dex double-click used the filtered index against the unfiltered list). Still in the code-behind: trend/provider/shop render builders, bag/shop confirm-state machinery |

## Done history (UI parity)

- M12 sprites + 7-language localization · M13 dex tile grid · M14 month
  trend chart · M15 settings window · M16 Unown per-form sprites · M17
  update notifications · M18 evolution line + ✨ shiny markers · M19 Fluent
  theme + resizable window + dex/shop tabs (macOS `ShopView` parity) ·
  M20 companion/dex/bag Tier-1 bundle · M21 representative Pokémon ★ +
  usage home redesign (today/week/month header, provider chips, token
  breakdown) · M22 detail-window polish (type capsules, individual picker,
  actual stats w/ IV, species-data section) · M23 catch-log view +
  companion status line + shop item sprites + launch at login.
