# Task Plan: Real Farm Expansion System

## Planning File Rules

- `docs/` holds long-term project documentation.
- Root `task_plan.md`, `findings.md`, and `progress.md` serve only the current active complex task.
- When a task is completed, a branch is merged, or the working context changes clearly, archive the current root planning files into `docs/archive/planning/` and create a new set.
- Long-term reference: `docs/project-workflow.md`

## Goal

Replace the current single-plot demo expansion with a real, data-driven farm expansion system so the "land expansion as the strongest visible growth signal" pillar from the design spec actually holds up over multiple in-game days. This is the first step of the **Manor Growth** phase that follows milestone 1.

## Current Phase

Phase 5 (in progress)

## Scope Guard

This task is intentionally narrow. It only delivers the systems needed for real land expansion:

- Camera follow on the player so a larger farm is reachable
- Full 6x6 grid (36 plots) physically present in `FarmScene.tscn`
- Tiered, data-driven expansion costs replacing the hard-coded `DemoExpansionPlotKey` / `DemoExpansionCost`
- Test and integration coverage matching the new behavior

This task explicitly does **not** include:

- Facility / building system
- Storage or inventory capacity upgrades
- NPCs or dialogue
- Gathering area / wood / stone production
- Crop multi-stage visuals
- Tool/equipment system
- Audio
- Main menu / multi-slot saves
- Art assets

These are queued behind this task and will be picked up in their own focused passes after this lands.

## Phases

### Phase 1: Planning Reset
- [x] Archive previous mainline-presentation planning files
- [x] Create new root planning files for the expansion task
- **Status:** complete

### Phase 2: Failing Test Coverage First
- [x] Add `PlayerSceneLayoutTests` asserting `Player.tscn` exposes a `Camera2D` child with smoothing
- [x] Update `FarmSceneLayoutTests` to assert all 36 `FarmPlotNode` instances cover `(0..5, 0..5)`
- [x] Add `FarmScene_BackdropExtendsToCoverTheFullSixRowGrid` asserting field reaches the deepest plot row
- [x] Add `ExpansionTierServiceTests` describing the four-tier ring pricing (120 / 280 / 600 / 1200) plus the free 2x2 starter
- [x] Update `GameBootstrapIntegrationTests` for tier-aware `GetLockedPlotHint`, `TryHandleLockedPlotInteraction`, and the new `lookupUnlockCost` callback in `BuildFarmPlotHoverStatusMessage`
- [x] Confirm the new tests fail before implementation
- **Status:** complete

### Phase 3: Implement Tiered Expansion Logic
- [x] Introduce `ExpansionTierService` (rule layer, no Godot deps) returning unlock cost by `(x, y)` and plot key with explicit `TierConfiguration` records
- [x] Replace `DemoExpansionPlotKey` / `DemoExpansionCost` usage in `GameBootstrap.*.cs` with tier service calls; add a `TryPurchaseCheapestLockedPlot` shortcut for F7 quick-unlock
- [x] Update `StatusMessageBuilder.BuildFarmPlotHoverStatusMessage` to take a `Func<int, int, int?> lookupUnlockCost` callback
- [x] Rename `CanTriggerDemoExpansionShortcut` to `CanTriggerQuickExpansionShortcut`
- **Status:** complete

### Phase 4: Wire Godot Presentation
- [x] Add `Camera2D` to `Player.tscn` with `position_smoothing_enabled = true`
- [x] Extend `FarmScene.tscn` field/frame/fence/walls vertically from y=696 to y=996 to fit the new grid
- [x] Add the missing 31 `FarmPlotNode` instances at deterministic positions (`x = 280 + col*140`, `y = 470 + row*88`)
- [x] Extend right-bottom collision wall so the field stays bounded after the grid drops below the original gate row
- **Status:** complete

### Phase 5: Verification
- [x] Run full `dotnet test` suite (246/246 passed)
- [x] Run `dotnet build game/HarvestManor.csproj` (0 errors, 0 warnings)
- [x] Run Godot headless smoke launch (loaded crops/items/offers/requests, no errors or warnings)
- [ ] Update `findings.md` and `progress.md` with summary notes
- [ ] Update `docs/testing/milestone-1-smoke-checklist.md` to describe the new multi-tier expansion behavior
- **Status:** in_progress

## Key Questions

1. What unlock tier shape best balances "fast first reinvestment" with "long-tail goldsink"?
2. Should the camera follow be locked to scene bounds or free-roaming?
3. Should the new plots use the same `Polygon2D` styling, or take this chance to differentiate ring boundaries visually?
4. How do we keep existing save files compatible after the unlock system changes?

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| Pick "real expansion" as the next milestone over polish/NPCs/facilities | The single-plot demo is the most broken design promise; expansion infrastructure is also a prerequisite for facilities and richer farm growth |
| Add a dedicated `ExpansionTierService` instead of expanding `FarmExpansionService` | Keeps the existing service focused on enforcement (gold + state) and isolates pricing rules in a unit-testable place |
| Keep camera follow simple (smoothing + scene-bound limits) | Smallest viable scope; we can revisit follow-cone, deadzone, or scripted shots later |
| Default unlock tiers: 4 free / 8 @ 120g / 12 @ 350g / 12 @ 800g | Free ring matches today's defaults; tier 2 matches today's demo cost; later tiers create meaningful late-day goldsinks |
| Existing saves stay compatible by treating unknown locked plots as ring-priced | No save migration needed; unlock state continues to be a set of plot keys |

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| (none yet) | | |

## Notes

- Archived prior planning context to `docs/archive/planning/2026-04-25-mainline-presentation-and-smoke-pass/`
- Latest verified commit before this task: `caaa857 fix(scene): make moon and stars actually visible at night`
- Working branch: `main`
- Working directory: `D:\game project\harvest-manor`
