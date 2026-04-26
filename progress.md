# Progress Log

## Session: 2026-04-25

### Phase 1: Planning Reset
- **Status:** complete
- Actions taken:
  - Reviewed the existing project against the design spec to identify the highest-impact missing core gameplay system
  - Decided that real, multi-tier farm expansion is the next milestone (entry to "Manor Growth")
  - Archived the prior `task_plan.md` / `findings.md` / `progress.md` set into `docs/archive/planning/2026-04-25-mainline-presentation-and-smoke-pass/`
  - Wrote the new root planning files for the expansion task
- Files created/modified:
  - `docs/archive/planning/2026-04-25-mainline-presentation-and-smoke-pass/` (new directory; old planning set moved here)
  - `task_plan.md` (rewritten)
  - `findings.md` (rewritten)
  - `progress.md` (rewritten)

### Phase 2: Failing Test Coverage First
- **Status:** complete
- Actions taken:
  - Added `tests/HarvestManor.Game.Tests/Progression/ExpansionTierServiceTests.cs` covering default unlocks, ring pricing, key-based lookup, and tier enumeration order
  - Added `tests/HarvestManor.Game.Tests/World/PlayerSceneLayoutTests.cs` covering the `Camera2D` node and smoothing flag
  - Extended `tests/HarvestManor.Game.Tests/World/FarmSceneLayoutTests.cs` to verify all 36 plots and assert backdrop polygons reach the deepest row
  - Updated `tests/HarvestManor.Game.Tests/World/GameBootstrapIntegrationTests.cs`:
    - `GetLockedPlotHint_ReturnsTierAwareUnlockPrompt` parametrized across 5 plots / 4 tiers
    - `TryHandleLockedPlotInteraction_UnlocksAcrossEveryTier` covering tiers 1-3
    - `TryHandleLockedPlotInteraction_ReturnsLockedMessageForPlotsOutsideAnyTier`
    - Renamed `CanTriggerDemoExpansionShortcut_*` -> `CanTriggerQuickExpansionShortcut_*`
    - Switched hover-message tests to the new `lookupUnlockCost` callback signature
  - Confirmed compilation failures and 32 pre-implementation test failures before writing any production code

### Phase 3: Implement Tiered Expansion Logic
- **Status:** complete
- Actions taken:
  - Added `game/scripts/core/Progression/ExpansionTierService.cs` with explicit `TierConfiguration(InclusiveMaxDistance, UnlockCost)` records and `CreateDefault()` returning ring 0 (free 2x2) plus four paid rings (120 / 280 / 600 / 1200 gold)
  - Refactored `GameBootstrap.cs` to inject `ExpansionTierService.CreateDefault()` and source `DefaultUnlockedPlotKeys` from the service
  - Updated `GameBootstrap.Flow.cs` `GetLockedPlotHint` and `TryHandleLockedPlotInteraction` to take an `ExpansionTierService` and use it for cost lookup
  - Updated `GameBootstrap.Status.cs` to rename the F7 helper to `CanTriggerQuickExpansionShortcut`
  - Updated `GameBootstrap.Runtime.cs` to call the new APIs and added `TryPurchaseCheapestLockedPlot()` so F7 unlocks the cheapest affordable plot
  - Updated `StatusMessageBuilder.BuildFarmPlotHoverStatusMessage` to take a `Func<int, int, int?> lookupUnlockCost` parameter

### Phase 4: Wire Godot Presentation
- **Status:** complete
- Actions taken:
  - Added a `Camera2D` node with `position_smoothing_enabled = true, position_smoothing_speed = 8.0` to `game/scenes/world/Player.tscn`
  - Extended `game/scenes/world/FarmScene.tscn` background, field, frame, fence, and bottom wall from y=696 down to y=996 to fit a 6-row grid
  - Resized the right-bottom collision wall (`wall_v_bot`) from 90 to 472 px and repositioned `WallRightBot` to keep the field sealed below the gate
  - Resized the left wall (`wall_v_full`) from 270 to 670 px and repositioned `WallLeft` to span the new height
  - Appended the 31 missing `FarmPlotNode` instances using a deterministic position formula (`x = 280 + col*140, y = 470 + row*88`)

### Phase 5: Verification
- **Status:** in_progress
- Actions taken:
  - `dotnet test` -> 246/246 passing
  - `dotnet build game/HarvestManor.csproj` -> 0 errors, 0 warnings
  - Godot headless launch -> loaded 10 crops, 22 items, 13 shop offers, 8 requests, restored existing save (`Day 4 of Summer`) without warnings
- Remaining work:
  - Update `docs/testing/milestone-1-smoke-checklist.md` to describe the multi-tier expansion behavior

## Session: 2026-04-26 (UX follow-up)

### Phase 6: Pin scene UI to viewport
- **Status:** complete
- Actions taken:
  - Wrapped `TitleBadge` and `FarmStatusPanel` inside a new `SceneOverlay` `CanvasLayer` (layer = 4) in `game/scenes/world/FarmScene.tscn` so neither follows the camera
  - Re-anchored the title to `(24, 58)..(332, 124)` and the field-notes panel to `(24, 632)..(1256, 700)` against the 1280x720 viewport
  - Updated `GameBootstrap.OnFarmSceneReady` lookup paths to `SceneOverlay/FarmStatusPanel/...`
  - Adjusted `FarmSceneLayoutTests.FarmScene_UsesDedicatedStatusPanelAndWideSceneFrame` to assert the new parent path

### Phase 7: Tiered locked-plot visibility
- **Status:** complete
- Actions taken:
  - Added `ExpansionTierService.GetActiveTier(UnlockState)` returning the lowest-cost ring that still has at least one locked key, plus 4 new tests
  - Added `ResolveVisualState`/`Render` overload parameter `bool isInActiveTier = true` on `FarmPlotNode` so locked plots outside the next ring render with empty label and a darker fill
  - Updated `GameBootstrap.RenderFarmPlots` to compute the active tier's plot keys once per render and forward `isInActiveTier` per plot
  - Added 2 `FarmPlotNodeTests` covering the new visual branch and the backwards-compatible default

## Test Results

| Check | Result |
|------|--------|
| Pre-task `git status --short` clean | PASS (only untracked `start-game.cmd`) |
| Archived planning directory created | PASS |
| Root planning files rewritten | PASS |
| Failing tests confirmed before implementation | PASS (32 failures) |
| `dotnet test` after Phase 3-4 implementation | PASS (246/246) |
| `dotnet test` after Phase 6 (CanvasLayer) | PASS (246/246) |
| `dotnet test` after Phase 7 (active-tier visibility) | PASS (252/252) |
| `dotnet build game/HarvestManor.csproj` | PASS (0 errors / 0 warnings) |
| Godot headless smoke launch | PASS (no errors, save restored) |

## Error Log

| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|

## 5-Question Reboot Check

| Question | Answer |
|----------|--------|
| Where am I? | Phase 5 wrap-up of the Real Farm Expansion task |
| Where am I going? | Update the smoke checklist, then commit; next milestone candidate is the Facility System or Light Gathering Area |
| What's the goal? | Replace single-plot demo expansion with a real multi-tier farm growth system |
| What have I learned? | Chebyshev-distance rings give a clean ring-by-ring expansion curve without bespoke per-plot data; the `ExpansionTierService` is now the single source of truth for both pricing and the default unlocked corner |
| What have I done? | Wrote `ExpansionTierService` and 24 tier tests, refactored `GameBootstrap` and `StatusMessageBuilder` off the demo constants, added 31 plot nodes to `FarmScene.tscn`, attached a smoothed `Camera2D` to `Player.tscn`, extended scene framing/walls vertically, validated 246/246 tests + headless launch |
