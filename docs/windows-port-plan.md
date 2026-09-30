# Windows Port Plan

## Purpose

Build a personal-use Windows version of PokeTokenBar with feature parity where
it is practical, while keeping each functional area independently testable.
The initial target is a local development build, not a packaged public release.

The application will primarily run on Windows, but the user develops in WSL.
Usage must therefore include both Windows-native and WSL Claude Code/Codex
activity.

## Scope

### Initial providers

- Claude Code
- Codex
- OpenCode (added in M9; reads `opencode.db` under
  `%USERPROFILE%\.local\share\opencode` and each WSL distro's
  `/home/<user>/.local/share/opencode`)

### Initial features

- Local usage collection, aggregation, and caching
- Windows tray icon and a compact dashboard opened from it
- Pokemon progression, Pokedex, shop, and save import/export
- Local settings, sprite cache, and development diagnostics
- Floating pet, after the tray/dashboard vertical slice is stable

### Explicitly deferred

- Start automatically at sign-in — shipped in M23 after all (HKCU Run key)
- Credential Manager integration
- Automatic updates, MSIX, winget, and public distribution
- Remote error reporting or Sentry
- Windows crash-reporting service integration
- Exact macOS menu-bar text layout

Windows notification-area icons cannot provide the same persistent inline,
multi-line token text as macOS `NSStatusItem`. The Windows design will use a
tray icon, tooltip, context menu, and compact dashboard instead.

## Usage semantics

Two kinds of measurements must remain separate.

1. **Local token usage** is calculated from local Claude Code and Codex logs.
   Windows-native and WSL log events are included in the same daily/monthly
   total after deduplication.
2. **Official account limits** are account-wide values. They must never be
   summed across Windows and WSL. This is deferred until an intentional,
   non-Keychain authentication approach is selected.

## Architecture

The UI must consume application results, not parse logs or make platform calls.

| Module | Responsibility | Validation |
|---|---|---|
| `Core` | Usage models, time/date rules, costs, Pokemon progression, save format | Unit tests |
| `Providers` | Claude/Codex log discovery contracts and parsers | Sanitized fixture tests |
| `Platform.Windows` | Windows paths, WSL discovery, local settings and diagnostic logs | Integration tests |
| `Application` | Refresh scheduling, aggregation, cache, deduplication, display state | Unit and integration tests |
| `UI` | Tray, dashboard, settings, and floating pet | Uses application interfaces only |

Each provider returns a common usage snapshot. Shared aggregation must not
contain conditions such as `if provider == "claude"`; a future OpenCode provider
is added through the same provider interface and registration point.

## Windows and WSL collection

### Windows-native roots

Discover the actual Claude Code and Codex log roots under the active Windows
user profile. Do not assume that macOS `~/Library/...` paths exist. Keep
automatic discovery and user-configured additional roots separate.

### WSL roots

Discover installed WSL distributions and their Linux home directories. Adapt
their CLI log locations to Windows-readable UNC paths, for example:

`\\wsl.localhost\\<distribution>\\home\\<user>\\...`

The Windows app should read those paths as files; parsers should not need to
know whether an event originated in Windows or WSL. The source identity is
retained for diagnostics and optional per-environment detail views.

WSL logs may be actively written and Windows file-change notifications may not
be reliable across the boundary. Start with periodic incremental scanning plus
a persistent cache rather than relying on file watchers.

### Deduplication

Windows and WSL paths can expose the same data through more than one root.
Deduplicate before aggregation using a stable source/file identity and, where
available, provider session/event identifiers. Tests must cover duplicate roots
and the same event discovered from two roots.

## Local diagnostics

No external error-reporting service is required. Write rotating local logs under
the app's local application-data directory. Record at least:

- unhandled exceptions with stack information;
- provider parse failures and their safe context;
- inaccessible/disappearing scan roots;
- read failures caused by files being written or locked;
- refresh, cache, and network failures.

Never write credentials, session keys, authorization headers, or complete
unredacted log entries to diagnostics.

## Milestones

### 0. Windows research environment

Prepare a Windows 11 machine or snapshot-capable VM using the same Windows and
WSL user profile in which the app will be used. Install and authenticate the
actual target CLIs. Capture sanitized Claude Code and Codex fixtures from both
Windows and WSL, and verify actual paths, formats, and file-lock behavior.

**Exit criteria:** documented roots and sanitized fixtures for all four initial
sources: Windows Claude, WSL Claude, Windows Codex, and WSL Codex.

### 1. Pure core and local diagnostics

Implement usage models, date boundaries, aggregation, cost calculations,
Pokemon progression, and save encoding without a UI dependency. Add rotating
local diagnostic logging.

**Exit criteria:** core tests run without Windows UI APIs or real local logs.

### 2. Windows/WSL discovery and incremental scan foundation

Implement Windows profile discovery, WSL distribution/home discovery, explicit
additional scan roots, incremental scanning, cache persistence, and duplicate
root protection.

**Exit criteria:** test fixtures are found and scanned through both Windows and
UNC WSL paths; repeated refreshes do not double-count unchanged events.

### 3. Claude Code vertical slice

Implement a complete Claude provider: discover roots, parse logs, normalize
events, deduplicate, aggregate, cache, and expose a provider snapshot.

**Exit criteria:** verified token totals for Windows-only, WSL-only, and mixed
Windows+WSL fixture scenarios.

### 4. Codex vertical slice

Implement the same end-to-end path for Codex, including sessions and archived
data that exist in the observed Windows/WSL formats.

**Exit criteria:** verified token totals for Windows-only, WSL-only, mixed, and
duplicate-discovery scenarios.

### 5. Tray and compact dashboard

Build the tray icon, tooltip/context menu, compact dashboard, manual refresh,
and a safe way to open the local diagnostics folder. UI reads application
display state only.

**Exit criteria:** UI can present each provider and the combined total without
direct file parsing or platform-specific provider logic.

### 6. Game and persistence integration

Connect the validated aggregate to Pokemon growth, Pokedex, shop, settings,
sprite cache, and save import/export. Preserve and test the existing save
schema where feasible.

**Exit criteria:** progression and saves remain correct across restarts and
with Windows/WSL combined usage.

### 7. Floating pet and personal-use hardening

Add the floating pet after core use is reliable. Test multiple monitors, DPI,
sleep/wake, temporarily unavailable WSL distributions, active log writes, and
large logs.

**Exit criteria:** stable personal daily use with recoverable local diagnostic
evidence for failures.

### Personal packaging (added after M7)

Daily use no longer runs from the Debug build output. `scripts/publish-windows.ps1`
stops a running installed instance, publishes the UI project as a single-file
self-contained win-x64 exe (`src/Ui/Properties/PublishProfiles/win-x64.pubxml`),
verifies the staged exe version against the `<Version>` in the Ui csproj, installs
it to `%LOCALAPPDATA%\Programs\PokeTokenBar`, and refreshes a Start Menu shortcut.
Version lives in the Ui csproj and shows at the top of the tray context menu. App state, settings,
sprites, and diagnostics stay under `%LOCALAPPDATA%\PokeTokenBar` (`PTB_STATE_DIR`
to override), so installs never touch user data. Public distribution (MSIX,
winget, automatic updates) remains deferred.

### Later: OpenCode

Done in M9 (`OpenCodeUsageProvider` + `OpenCodeDbReader`). Storage research
established that OpenCode 1.x keeps everything in a WAL-mode SQLite
`opencode.db` (message rows carry tokens/cost in `data` JSON; the old
`storage/` JSON layout is legacy). Local disks are opened read-only directly;
UNC paths (WSL `\\wsl.localhost\...`) cannot host SQLite locks, so the reader
copies db+wal to a scratch folder under `%LOCALAPPDATA%\PokeTokenBar\opencode`
  gated by db+wal fingerprints. Sanitized fixtures live under
`docs/windows-port-research/fixtures/opencode/`. No generic totals, game
progression, or UI architecture changes were made.

### Later: combat-details enrichment

Done in M10. The bundled snapshot moved to schema 2: beside the existing
bases/lines/names it now carries per-species default-form combat data
(height, weight, base experience, gender rate, types, base stats, abilities,
and every move learnable in the black-2-white-2 version group) plus localized
type/ability/move display names for the app languages. `scripts/
generate-pokemon-snapshot.ps1` regenerates it from PokeAPI; the app stays
offline-first and never fetches pokemon base data at runtime. The engine fills
the deferred profile fields (gender/ability/moves) at hatch, evolution,
graduation, release, and startup migration, and the dashboard shows a combat
summary line for the active mon plus a per-species detail window (individuals
with computed stats and known moves, base stats, abilities, full move list)
opened by double-clicking a dex row.

## Closure (M11, 2026-09-28)

The user declared the project complete. The plan, every named nicety
(including M10 combat details), and personal verification are done; version
0.10.0 is the final shipped build. From here the project runs in maintenance
mode: no scheduled slices; defects are fixed as they appear during daily use.

Distribution stays manual: releases are uploaded by hand to GitHub Releases
(web UI) using a zip of the single-file exe produced by
`scripts/publish-windows.ps1` (e.g. `artifacts/PokeTokenBar-<version>-win-x64.zip`).
No release automation, installer, signing, or auto-update was added, per the
personal-use scope. The unused `OPENCODE_DATA_DIR` compatibility nicety was
checked and deliberately skipped: this machine does not set it anywhere
(Windows env and WSL shell rc files verified), so the fixed-path discovery
covers actual usage.

## Reopened: UI parity polish (M12+, 2026-09-28)

After closure the user reopened the project for UI polish toward the macOS
original. First slice (M12, version 0.11.0): dashboard/tray/pet/detail
localization via `src/Core/DashboardText.cs` (seven languages, values copied
from the macOS `Localization.swift` so both apps read identically; the engine
localizes events, notices, shop and bag labels from the saved language) and
sprites through a cached-first `SpriteSlot` helper reusing `SpriteStore`
(animated active mon + detail header, static dex row thumbnails, egg image).

Second slice (M13, version 0.12.0): the dex text list became a sprite tile
grid (WrapPanel tiles: number, 64px nearest-neighbor sprite, name; shiny star,
raising arrow, full-info tooltip; double-click still opens the detail window).
Dashboard grew to 580x720 with a wider dex column.

Third slice (M14, version 0.13.0): the Usage tab gained a month-to-date daily
trend chart (caption with hover readout and peak, accent bar for today, dimmed
zero days, weekend ticks, sparse date axis; pure geometry in
`src/Core/DailyTrendMetrics.cs`, gated on `peak > 0`) plus a combined-today
model breakdown under the chart (two-plus models only). The refresh service
now surfaces `MonthDaily` and `TodayModels` on `UsageDisplayState`.

Fourth slice (M15, version 0.14.0): a settings window (tray menu entry and a
dashboard footer button) holding the language picker (seven languages, live
re-localization of dashboard/tray/pet menus via `CompanionEngine.SetLanguage`,
persisted in the save), per-provider additional scan folders (`scanRoots` in
settings.json, applied live to `UsageRootOptions` and followed by a refresh),
difficulty sliders (percent readout) and the floating pet controls. The game
tab no longer carries sliders or pet controls.

Fifth slice (M16, version 0.15.0): per-form Unown sprites. The dex tile for
Unown shows the plain name plus a collected-form count ("Unown 2/28", tooltip
"Unown forms 2/28"); the detail window gained a 28-form picker grid (7
columns, static per-form thumbs with a shiny star, unowned forms dimmed with
"Not collected", click selects) that filters the individuals list and switches
the hero sprite to that form. Engine surfaces this via
`CompanionUnownFormStatus(Form, IsShiny)` lists on `CompanionGameView` and
`CompanionDetailSnapshot` plus `UnownForm` per detail individual, and the
active Unown dex row keeps a plain name.

Sixth slice (M17, version 0.16.0, user-verified): new-version notifications,
porting the macOS `UpdateChecker` — GitHub `releases/latest` check on startup
and dashboard open (30-minute in-memory debounce, manual check bypasses it),
tray balloon + dashboard banner with skip-this-version, a settings "Updates"
section with a manual check and an update-notifications toggle, and "open the
release page" as the apply action (no auto-download; https + github.com URL
validation before opening). Pure version compare in
`src/Core/VersionText.cs`, the checker service with injectable
fetch/clock/skip store in `src/Application/UpdateChecker.cs`, skip version +
notifications toggle in settings.json. Dormant but correct until releases
newer than the installed version are uploaded (releases so far use
`v<Version>` tags; only v0.10.0 exists today).

Seventh slice (M18, version 0.17.0): evolution-line visuals and shiny
sparkle parity in the game tab, porting the macOS `EvoLineView`. The engine
now exposes `EvoLine` (structured `EvoLineItem`s: species id or one mystery
cell for branch points, state Done/Current/Future — the Core record types
that had existed unused since the initial port) on `CompanionGameView` next
to the label-only `StageItems`. The dashboard's old text chain (`A → B → C`
in `StageLine`) became a horizontal row of 40px static sprite cells with
arrow separators, future stages at 0.32 opacity, an accent dot under the
current stage, a bold `?` cell (tooltip `unknownNextEvolution`) where a
line branches, whole-line shiny sprites when the individual is shiny, and
an overflow ScrollViewer. The progress caption shows `finalForm`
("최종 진화체"/"Final form") instead of "Stage i/k" at the last stage. Shiny
markers switched from a gray `★` suffix to macOS-style `✨` on the companion
name (tooltip `dexShinyLabel` — the previously dormant `ShinyLabel`, now
capitalized per the macOS table), dex tile stars, species-detail individual
headers, and Unown form thumbs. New macOS-verbatim strings: `FinalForm`,
`UnknownNextEvolution`; `ShinyLabel` values corrected to the macOS
capitalization.

Eighth slice (M19, version 0.18.0, user-verified): a general visual-quality
pass over the dashboard, settings and detail windows, moving from bare default
WPF controls to a Fluent/Windows-11 light theme. `src/Ui/Theme.xaml` (merged in
App.xaml AND per-window so headless tests resolve resources) holds the design
tokens (window/card/text/accent/rarity brushes, Segoe UI Variable) and implicit
styles for Button (+ keyed accent variant), ListBox/ListBoxItem, TabControl/
TabItem (pivot tabs with accent underline), ProgressBar, ToolTip, CheckBox,
ComboBox/ComboBoxItem, Slider and a flat thin ScrollBar, plus keyed Card/
SectionHeader/Caption styles. The dashboard game/usage sections, settings
sections and detail header/content are grouped into cards; the update banner
is accent-tinted. Code-built visuals (trend bars, evo cells, dex captions,
detail rows) read theme brushes through a `Token(key)` helper. During review
the user asked for a bigger dex and macOS-parity shop, so the slice also
grew: the window became 640x800 and resizable (min 560x640), the dex moved to
its own full-width tab (7 columns of 76px tiles) with the game tab keeping a
companion-events card, and the shop moved to its own tab modeled on the macOS
ShopView — wallet card with a big spendable number plus per-entry cards
(emoji icon, description, owned count, price, per-card buy with inline
confirm, egg cards with a send-off warning and a second shiny warning, tier
capsules in rarity colors). The game tab's old shop list became a "bag" card
with the candy/mint use buttons. New macOS-verbatim strings for the shop/bag
(passive-owned, egg confirm, shiny-discard warning, companion-events header).

Ninth slice (M20, version 0.19.0, user-verified): the Tier-1 bundle from
`docs/ui-parity-audit.md` (a living macOS-vs-Windows gap list introduced with
this slice). Companion card parity with the macOS CompanionHeader: rarity
capsule at the name (rarity brush, uppercase), growth-boost capsule, egg
"About to hatch!" wording at >=90% progress with an accent color, egg
guarantee capsule (new `EggGuarantee` field on `CompanionGameView` from the
stored `EggTier`), and macOS-style progress captions ("X to next evolution" /
"X to graduation" / "X to hatch" instead of raw used/threshold). The dex tab
gained rarity filter capsules (count per rarity, tap to toggle) and an
empty state (animated Pikachu + copy). The game tab's bag card became
per-item cards: candy with a +/- stepper, live plan preview via the
previously-unused `PlanRareCandyUse` ("expected to graduate" / carryover XP /
discarded XP) and an inline use confirm; mint with its effect hint and
confirm; passive shiny charm as a green "active" row; empty bag shows a
Snorlax. `RarityLabel` values corrected to macOS capitalization
("Rare"/"Legendary" — were lowercase). ~22 macOS-verbatim strings added.

Tenth slice (M21, version 0.20.0, user-verified): representative Pokémon —
the last Tier-1 item. `CompanionEngine.SetRepresentative(id, form?)` validates
ownership (mirrors the macOS `setRepresentativeSpeciesID`), three new
`CompanionGameView` fields surface the selection (species, shiny, Unown form;
inserted before `UnownForms` per the record contract), the floating pet now
follows the representative when one is set (egg otherwise), dex tiles show a
★ in the number row plus an accent-tinted card for the representative, the
species detail window gained a ★/☆ header toggle (Unown passes the selected
form), and the settings general group gained a representative row (current
selection, follow-current reset, "choose in Pokédex…" opens the dashboard on
the dex tab). Usage home redesign (Tier 2): the usage tab now leads with a
macOS-parity header — big compact today number + grouped caption + cost,
"this week"/"this month" period labels — followed by the month trend and a
provider card with chip tabs (when >1 provider has usage) and per-provider
today detail (input/output/cache-write/cache-read, per-model rows); the old
plain providers list and combined card are gone. Data: `ProviderUsageSummary`
extended (today breakdown, week totals, models, cost coverages) and
`UsageDisplayState` extended (combined week totals + coverages); week windows
use Sunday start via `UsageAggregation.StartOfWeek` (invariant culture).
~12 macOS-verbatim strings added.

Eleventh slice (M22, version 0.21.0, user-verified): detail-window polish.
The species detail window now mirrors the macOS `PokemonDetailView` layout.
A new "Species data" section carries the type capsules (localized, uppercase,
accent-tinted) plus height/weight/base-total value pairs and possible
abilities, replacing the plain header join line (the header keeps only name,
rarity, a ✨ shiny line, and an accent "Raising" line). When a species has
several catches (after Unown form filtering) an individual picker
("#N · Lv. X" combo) selects which individual to display, and the individual
card shows level/gender/nature value pairs, the ability block (hidden ability
suffix), an "Actual stats" section (bars scaled by the already-tested
`PokemonStatCalculator.DisplayScaleMaximum`, IV column), and known-move rows
with right-aligned levels. Base stats and actual stats are mutually exclusive
like on macOS (individual profile present → actual stats; otherwise base
stats at scale 300). No engine/data changes — `CompanionDetailSnapshot`
already carried everything; 8 macOS-verbatim strings added (`IndividualsTitle`
removed as unused).

Twelfth slice (M23, version 0.22.0, user-verified): a four-feature bundle
("everything one thread can handle" per the user's kickoff pick). Catch log
view: the dex tab gained a segment toggle (Pokédex / catch log); the log lists
one card per individual — rarity capsule, "Raising"/"Released" badge, ✨,
nature on the right, the reached evolution chain as sprites with names, and a
relative caught-at line ("n일 전") — with the currently-raised mon pinned
first and per-individual rarity filter capsules (engine `BuildCatchRows` +
`CompanionCatchRow` view rows; macOS `dexEntriesSorted` ordering). Companion
status line: a one-line mood under the progress bar (egg / idle / working /
focus / tired / sleep / level-up) ported as the pure `CompanionStatus.Compute`
machine with macOS burn thresholds; the engine approximates burn rate from
consecutive refresh deltas (macOS reads active provider blocks) and holds a 4s
"Evolved into X!" window after hatch/evolve/reveal. Shop/bag item icons:
`ItemIconSlot` renders PokeAPI item sprites cached-first (`item-{name}.png`,
macOS-compatible keys) with emoji fallback (mint has no sprite by design).
Launch at login: settings general-group toggle backed by
`Platform.Windows.LoginItem` (HKCU Run key, registry access delegate-injected
so encoding/matching stay unit-tested). Engineering quality rode along:
`CompanionPresentation` + `RelativeTimes` extracted from the dashboard
code-behind with tests, and a pre-existing dex-filter double-click misindex
was fixed. ~20 macOS-verbatim strings added.

Thirteenth slice (M24, version 0.23.0, user-verified): the celebration
animation package (the last unclaimed Tier-2 item besides the deprioritized
incident banner). Hatch/evolve/ditto-reveal play a white flash fade + spring
pop (ElasticEase) over the companion sprite with delayed bursts — a gold
sparkle for shiny hatches, a comedy/tragedy mask pair for ditto reveals —
candy use pops an orange "+XP" capsule above the sprite, mint re-rolls
shimmer three gold sparkles, and an imminent egg (≥90%) wiggles back and
forth. Engine-side: Core `CelebrationQueue` (drain-based, capacity 8) +
`DrainCelebrations()` with enqueues at hatch (after carry-over growth,
disguise hides shiny — macOS ordering), evolve, ditto reveal, candy XP
amount, and mint; import clears pending celebrations. UI-side:
`CompanionCelebrationPlayer` (code-behind playback, sequential for
flash+pop kinds, concurrent candy/mint overlays) — and because WPF renders
emoji monochrome (verified: even explicit Segoe UI Emoji yields no color
glyphs), the ✨/🎭 cues are vector-drawn `CelebrationGlyphs` (curved
four-point star in the new Theme `SparkleBrush`, mask pair reusing rarity
brushes) instead of emoji text. No new localized strings (macOS celebration
uses none). 15 new tests (queue semantics + engine enqueue/ordering/import).

Fourteenth slice (M25, version 0.24.0, user-verified): engineering-quality
extraction with ride-along shop parity fixes. `DashboardWindow.xaml.cs`
shrank 1,629→799 lines: the bag/shop confirm-state machinery, candy stepper,
and use/purchase feedback moved to Application `ShopFlow` (egg confirm is now
the macOS three-stage flow: confirm → shiny-discard warning), the
today/provider/trend render decisions moved to `UsagePresentation` +
`DailyTrendMetrics` (Peak/ShowsCost/TodayUsage), and the WPF builders moved
to Ui-internal `UsageHomeRenderer` / `ShopCards` / `Paint` (brush helper).
Two pre-existing bugs fixed while moving the shop code: item cards rendered
their confirm row unconditionally (cancel was a visual no-op; macOS uses an
inline two-step idle price → confirm), and the BASIC egg row (Item=null,
EggTier=null) fell through to the item-card builder and rendered with the
rare-candy icon/description/owned count. Bag card background switched from
opaque #F8F8F8 to the macOS-verbatim #0F000000 alpha tint (theme-blind hex
removed). 16 new tests (ShopFlow confirm flows incl. shiny escalation,
UsagePresentation, trend metrics). Smoke harness evidence in
`%TEMP%\opencode\ptb-m25-smoke\` (5 PNGs + numeric probes).

Fifteenth slice (M26, version 0.25.1, final of the series): the
engineering-quality finish — the game-tab render builders left in the
code-behind after M25. Pure decisions moved to Application
`GameTabPresentation` (unit-tested, 24 new tests: header name + shiny
suffix/tooltip, rarity capsule label, stage·nature detail line, egg
imminence wording, progress captions (next-evolution/graduation/hatch,
clamped remainders) + stage/egg progress value, growth-boost and
egg-guarantee badge decisions, combat line assembly, dex/catch headers,
dex tile tooltip/name, caught-ago bucket text) and the WPF builders to
Ui-internal `CompanionHeader` (header card + progress card + evolution
line + companion sprite slot) and `DexTab` (dex/catch modes, headers,
rarity tally capsules + filter state, tiles, catch cards, chain nodes,
empty state — owns the mode/filter state, re-renders via a request
callback like `ShopCards`). `DashboardWindow.xaml.cs` shrank 799→233
lines (orchestration, dialogs and event handlers remain). No visual or
behavior changes: verified with a before/after smoke harness
(`%TEMP%\opencode\ptb-m26-smoke\`) — 24 logical-tree assertions pass on
both builds with identical values, and the captured PNGs (game tab
active/egg/egg-imminent, dex tiles/filtered, catch log) are
pixel-identical between builds (the imminent-egg pulse animation is the
only run-to-run variance and reproduces within a single build).

Remaining UI-parity candidates live in `docs/ui-parity-audit.md` — the
series is now complete: every Tier-1/Tier-2/Tier-3 parity item is done
or explicitly rejected by the user (incident banner, dark mode) or by
the no-credentials rule (official limits), and the engineering-quality
extraction is finished. The user returned the project to MAINTENANCE
MODE on 2026-09-30 (as after M11): no scheduled slices; fix defects
found in daily use, re-confirm scope before any new feature. Windows
keeps the tray + dashboard model — inline menu-bar text stays out of
scope by design.
