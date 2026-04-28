# Progress Log

## Session: 2026-04-26

### Phase 1: Planning Reset
- **Status:** complete
- Actions taken:
  - Confirmed milestone-1 spec §8.4 still requires a Light Gathering Area; verified no scene/script/service exists today
  - Spotted that `wood` / `stone` are already declared in `data/items/items.json` but are reachable via no shop, request, or world object
  - Archived the previous farm-expansion planning files to `docs/archive/planning/2026-04-25-real-farm-expansion/`
  - Wrote new root `task_plan.md`, `findings.md`, `progress.md` describing the gathering task
- Files created/modified:
  - `docs/archive/planning/2026-04-25-real-farm-expansion/` (planning files moved into here)
  - `task_plan.md` (rewritten)
  - `findings.md` (rewritten)
  - `progress.md` (rewritten)

### Phase 2: Failing Test Coverage First
- **Status:** complete
- Actions taken:
  - Added `tests/HarvestManor.Game.Tests/Gathering/GatheringStateTests.cs` (7 cases) covering initial state, mark/idempotent guard, ResetForNewDay, hydrate-from-seed, and null/whitespace argument guards
  - Added `tests/HarvestManor.Game.Tests/Gathering/GatheringServiceTests.cs` (8 cases) covering Success, AlreadyHarvested, UnknownNode, InventoryFull, ResetForNewDay, Nodes lookup, and duplicate-id constructor guard
  - Extended `tests/HarvestManor.Game.Tests/Saves/SaveGameStoreTests.cs` to round-trip `HarvestedGatheringNodeIds` and confirm legacy payloads default it to empty
  - Updated two `GameBootstrapIntegrationTests` snapshot constructions for the new `HarvestedGatheringNodeIds` parameter
  - Confirmed the new tests fail before implementation (compile error + 1 runtime fail expected)

### Phase 3: Implement Core Gathering Logic
- **Status:** complete
- Actions taken:
  - Added `game/scripts/core/Gathering/GatheringNodeDefinition.cs`, `GatheringState.cs`, `GatheringHarvestResult.cs`, `GatheringService.cs`
  - Extended `SaveGameSnapshot`/`SaveGameStore` with `HarvestedGatheringNodeIds`, validating no nulls and defaulting to empty for legacy saves
  - Wired `_gatheringService` field onto `GameBootstrap`; `Autosave` forwards `State.HarvestedNodeIds.OrderBy(...)`
  - Added `StatusMessageBuilder.BuildGatheringStatusMessage` so each `GatheringHarvestOutcome` produces a player-facing field-notes line

### Phase 4: Wire Godot Presentation
- **Status:** complete
- Actions taken:
  - Authored `game/scenes/world/GatheringScene.tscn` with sky/distant-hills/forest-floor/path backdrops, perimeter walls, four trees + three rocks (each a `ResourceNode` Area2D), an `ExitGate` aimed at town, and a `Whispering Woods` title badge
  - Added `game/scripts/world/ResourceNode.cs` (subclass of `HoverableInteractionArea`) and `ResourceVisualTheme.cs` (pine + boulder polygons) so each node renders without art assets
  - Modified `game/scenes/world/TownScene.tscn` to split `WallTop` into `WallTopLeft`/`WallTopRight`, insert a `GateNorth` Area2D pointing at `gathering`, and add a `↑ to the woods` label
  - Registered `GatheringSceneType` and `TownFromGatheringSpawn`/`GatheringFromTownSpawn` in `GameBootstrap`; instantiated `_gatheringService` from `DefaultGatheringNodes` (4 wood + 3 stone) seeded from any saved set
  - Routed `LoadScene` through `WireGatheringScene` + `RenderGatheringNodes`; added `OnResourceNodeInteracted`, hover hints, item-display lookup, and `EndDay` -> `ResetForNewDay` + re-render
  - Extended `BuildSeasonShopOffers` to always pass through `Category="Material"` items (added a regression test); added wood (4g) and stone (6g) sell-only entries to `general-store.json`

### Phase 5: Verification
- **Status:** complete (manual smoke deferred to user playtest)
- Actions taken:
  - `dotnet test` -> 321/321 passing (296 baseline + 25 new gathering / shop / save / status cases)
  - `dotnet build game/HarvestManor.csproj` -> 0 errors / 0 warnings
  - Godot headless launch -> 10 crops, 22 items, 15 shop offers (was 13), 8 requests, save restored without warnings
  - Refreshed `docs/testing/milestone-1-smoke-checklist.md` with the Tab inventory toggle, hover-silence guard, and the gathering loop steps (gate north, harvest, dim node, day reset, material shop offer)

### Phase 6: Smoke Test Polish (this session)
- **Status:** complete (awaiting user manual reverification of polished bits)
- Actions taken:
  - **F7 feedback:** Added `BuildQuickExpansionShortcutFailureMessage` helper and routed `TryPurchaseCheapestLockedPlot` failure path through `SetFarmStatus` so insufficient-gold (or no-locked-plot) attempts now print to the field-notes panel instead of staying silent.
  - **Panel exclusivity:** `GameBootstrap.Status.cs` now keeps Storage and Inventory panels mutually exclusive to stop the overlapping-panel bug.
  - **Gathering scene visuals:** Added the missing `NightOverlay`, `Stars`, `Sun*`/`Moon*`, and `FarmStatusPanel` nodes to `GatheringScene.tscn`; pruned the static `SkyGradient` strip that was creating a daytime seam at dusk; lowered the moon-glow polygon size + alpha in `FarmScene.tscn` and `TownScene.tscn` to match.
  - **Resource hover text:** Hover on a harvested `ResourceNode` now reads `already gathered today` instead of `click to gather` (`GameBootstrap.Runtime.cs:233-239`).
  - **Interior window day/night:** Refactored `UpdateCottageWindow` -> generic `UpdateInteriorWindow(scene, minute, InteriorWindowConfig)` driven by per-interior `Cottage/Shop/BarnWindowConfig`; added `WindowMoon` + `WindowStars(WStar1..WStar6)` to `ShopInterior.tscn` and `BarnInterior.tscn`; sun/cloud/sun-rays now fade with day strength while moon/stars fade in at night for all three interiors.
  - **Shop opening hours:** Added `ShopOpenMinute` (09:00) / `ShopCloseMinute` (18:00) and helpers `IsShopOpen` / `FormatShopHours` to `TimeOfDayController`; `OnGateEntered` now blocks `ShopInteriorSceneType` outside hours and posts `BuildShopClosedAttemptStatusMessage`; the Shop hover label uses `BuildShopClosedHoverStatusMessage` whenever the store is closed.
  - **Tests:** Added `Time/TimeOfDayControllerTests.cs` (9 cases for `IsShopOpen` / `FormatShopHours` / `FormatClock`), plus 2 `StatusMessageBuilder` cases for the closed-store strings, and the F7 / panel-mode regression cases noted in earlier phases.
  - `dotnet test` -> **344 / 344 passing**.

## Test Results

| Check | Result |
|------|--------|
| Pre-task `git status --short` clean | PASS (only untracked `start-game.cmd`) |
| Archived planning directory created | PASS |
| Root planning files rewritten | PASS |
| `dotnet test` after Phase 3 (core domain) | PASS (312/312) |
| `dotnet test` after Phase 4 (scene + integration) | PASS (321/321) |
| `dotnet build game/HarvestManor.csproj` | PASS (0 errors / 0 warnings) |
| Godot headless smoke launch | PASS (15 shop offers, save restored, no warnings) |
| `dotnet test` after Phase 6 (smoke polish + shop hours) | PASS (344/344) |

## Error Log

| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|

## 5-Question Reboot Check

| Question | Answer |
|----------|--------|
| Where am I? | Phase 5 wrap-up of the Light Gathering Area task; all automated checks green, manual playtest pending |
| Where am I going? | Wait for the user's manual smoke pass; next milestone candidate is the Facility System (which can now consume gathered wood/stone) |
| What's the goal? | Ship milestone 1's last missing world zone: a small gathering area that produces wood and stone, resets daily, and integrates with inventory/save/shop |
| What have I learned? | A pure-domain `GatheringState`/`Service` mirrors the farm/expansion split well; making `BuildSeasonShopOffers` aware of `Category="Material"` lets future tasks add new always-available materials without touching the scene; clearing Godot's default Tab binding was needed before `Tab` could reach our handler reliably |
| What have I done? | Built the gathering core (4 files), the `ResourceNode`/`ResourceVisualTheme` Godot layer, `GatheringScene.tscn` with 7 resource nodes, town `GateNorth`, scene-switch + day-end + autosave wiring, wood/stone shop offers, and 25 new tests bringing the suite to 321/321 |
