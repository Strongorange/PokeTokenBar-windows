# Handoff — Shop buy-button stretch fix + egg sprite parity (standalone worktree)

## Goal
Fix the shop (and bag) action-button full-stretch regression reported against
the 2026-10-02 11:20 screenshot, restore the real egg sprite on shop egg
cards, and land both as independently testable modules (new
`tests/Ui.Tests`). This work lives in its own Orca worktree
(`shop-button-fix`) because other work (M27 heatmap) is in progress in the
main checkout — do not touch the main checkout while this runs.

## Investigation (moved from the main-checkout session)

### Screenshot findings (2026-10-02 11:20:10)
- Buy buttons fill ~87–90% of the card width (~500px) with the 2-glyph
  "구매" label centered — text is ~19x narrower than the button. Height
  (25–26px, `MinHeight=26`) is fine; only the WIDTH is wrong.
- Egg rows (포켓몬 알 / 고급 알 / 허귀 알) show a glyph that reads as "0"
  in the 30x30 icon box. Root cause per the M24 note: WPF renders emoji
  MONOCHROME, so the fallback `🥚` renders as an outline egg ≈ "0". Item
  rows (candy, charm) show real sprites, so the list looks inconsistent.

### Root cause — WPF `DockPanel.LastChildFill` trap
`DockPanel.LastChildFill` defaults to `true`: the LAST child ignores its
`Dock` property and fills all remaining space. `src/Ui/ShopCards.cs` added
the buy button (Dock.Right) as the last child in four places, so the button
stretched to fill the row:

| Site | Pattern | Effect |
|---|---|---|
| `ShopCards.cs:313-328` item card idle | price(Left) then buy(Right) LAST | buy fills row (screenshot) |
| `ShopCards.cs:380-427` egg card idle | same | same |
| `ShopCards.cs:215-222` AddBagUseButton idle | hint text then button LAST | bag use button also stretches |
| `ShopCards.cs:331-338` egg card !CanBuy | locked text LAST | stretched bounds (less visible) |

Confirm rows are ordered correctly (buttons docked Right first, text fills)
which is why the two-step confirm state rendered normally — the bug only
hits the idle rows.

### macOS original composition (`Sources/PokeTokenBar/UI/ShopView.swift`)
- Idle row (L211-234): `HStack { price(caption2/tertiary/monospacedDigit) →
  Spacer() → Button(l.buy) }` with `.buttonStyle(.bordered).controlSize(.small)`
  — a small text-hugging secondary button pinned to the right edge.
- Confirm row (L189-208): message + Spacer + buy(`.borderedProminent` small)
  + cancel(`.borderless` small).
- Card (L120-166): padding 10, `Color.secondary.opacity(0.06)`, corner 10.
- Egg card (L284-375): REAL egg sprite `SpriteView(speciesID: nil, size: 26)`
  inside a 30x30 frame, rarity capsule badge, `eggReleaseNote` line under the
  description when a companion is active.

### Parity gaps found
1. Buy/use button width stretch (critical) — the four DockPanel sites above.
   The port's `MinWidth=70 / MinHeight=26` intent already matches the macOS
   small button; only the stretch defeats it.
2. Egg card icon: macOS uses the real egg sprite; the port hardcodes the
   `🥚` emoji (`ShopCards.cs:352-356`) even though `SpriteStore.Egg()`
   (`src/Application/SpriteStore.cs:127`) and `SpriteSlot.UpdateEgg()`
   (`src/Ui/SpriteSlot.cs:59`) already exist. Fix: cached-first egg sprite
   with the emoji as pre-load fallback, 26px sprite inside the 30px cell.
3. (Minor, NOT in this slice) `eggReleaseNote` line under egg descriptions
   exists only on macOS.

## Fix design (modular principle)
- New `src/Ui/ShopActionRow.cs`: the single module that builds
  "leading fill content + trailing action" rows. The trailing element is
  ALWAYS added first (Dock.Right) so `LastChildFill` can never stretch it;
  the optional leading element is added last and fills. Every call site in
  `ShopCards.cs` goes through this module — the trap is removed by
  construction, and the layout contract is unit-testable.
- New `tests/Ui.Tests` (net10.0-windows, UseWPF, xUnit): STA-thread layout
  tests arrange `ShopActionRow` rows at fixed widths and assert the
  trailing control keeps its natural width (regression test for exactly
  this bug class). `InternalsVisibleTo` opens `PokeTokenBar.Ui` internals
  to the test project.
- Egg cards: `CreateEggIcon()` builds a `SpriteSlot` cell
  (`UpdateEgg(sprites, "🥚")`) mirroring `CreateItemIcon`, 26px sprite in
  the 30x30 header cell (macOS parity).

## Validation
- `dotnet test PokeTokenBar.Windows.slnx` in this worktree: 562 green
  (558 existing + 4 new Ui.Tests layout regressions).
- Latent repo bug found while building on the Linux worktree: the slnx
  referenced `tests/...` (lowercase) while git tracks `Tests/...` —
  invisible on case-insensitive Windows, but MSB3202 on the WSL worktree.
  Fixed by normalizing the slnx paths to `Tests/` (no effect on Windows).
- Manual: run the worktree build, open the Shop tab — idle rows show a
  small right-aligned buy button; egg rows show the real egg sprite;
  confirm rows unchanged.

## Workspace
- Orca worktree: `shop-button-fix` (branch `shop-button-fix`, base
  `origin/main` @ 0889321), path
  `\\wsl.localhost\Ubuntu-24.04\home\ubuntu\orca\workspaces\poketoken-bars-windows\shop-button-fix`
- Main checkout `C:\Users\USER\my-pjts\poketoken-bars-windows` has OTHER
  in-progress work — do not build/commit there.
- Commit convention: English conventional commits (`fix:`, `test:`).
