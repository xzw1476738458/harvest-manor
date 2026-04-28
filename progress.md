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

### Phase 6: Smoke Test Polish (previous session)
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

### Phase 7: Town Hover Wiring + Outdoor/Interior Visual Polish (this session)
- **Status:** complete (all changes verified by user during stepwise walkthrough)
- Actions taken:
  - **Town field-notes wiring:** `TownScene.tscn` was missing the `FarmStatusPanel` node entirely, so hover hints on the storage barn / shop door / etc. silently no-op'd. Added the `FarmStatusPanel` overlay (matching the farm/gathering layout), wired `OnWorldInteractionHovered` for the town interactables, and added a `TownSceneLayoutTests` regression case asserting the panel is part of the town tree.
  - **Request board hover routes through Guild Board:** `OnRequestBoardHovered/Ended` now writes through `RefreshRequestBoardStatus(override?)` -> the persistent `RequestStatusPanel`, so the same panel that shows `Active request: ...` also shows the hover preview. Eliminates the brief panel duplication when the player swept the cursor across the board.
  - **Field Notes + Guild Board are now mutually exclusive:** Both panels share the same y=632 footprint. `ShowFarmStatusPanel`/`ShowRequestStatusPanel` invoke `SuppressRequestStatusPanel`/`SuppressFarmStatusPanel` so only one is ever visible; when the transient `FarmStatusPanel` auto-hides in town, `HideFarmStatusPanel` calls `RefreshRequestBoardStatus()` so the persistent Guild Board returns instead of leaving the screen blank.
  - **Moon and stars dropped into the sky layer:** `Moon`/`MoonGlow`/`Stars` had `z_index = 11/12` so they rendered above mountains, clouds, and buildings. Lowered to `z_index = -6` in Town/Farm (same plane as `Sun` and `Cloud*`; tree order keeps `Cloud*`/`DistantBuildings*`/`DistantHill*` on top) and `z_index = -11` in Gathering (between `SkyBackdrop -12` and the `DistantHillsFar/Mid/Near` -10/-9/-8 cascade). Stars and the moon now read as part of the sky, occluded by the silhouette as expected.
  - **Pale-yellow moon, brighter than before:** Moon recolored from off-white `(0.96, 0.96, 0.90)` and cool-blue glow `(0.86, 0.92, 1)` to pale yellow `(1, 0.96, 0.74)` and warm yellow halo `(1, 0.94, 0.62)`. To stop `NightOverlay (z=10)` from dimming the moon back into a grey blob, trimmed all three outdoor `NightOverlay` polygons to start at `y=200` (horizon-ish) so the overlay only dims the ground; the sky already darkens via `GetSkyColor`/`SkyBackdrop`.
  - **Backdrop sky also tints at night:** Town and Farm have a wide background `BackdropSky` polygon at `z=-11` plus a smaller `SkyBackdrop` strip at `z=-7`. Only the latter was being recolored, so the area above the strip kept the daytime light blue and produced a bright cyan band at night. `UpdateOutdoorCelestials` now also writes `GetSkyColor(minute)` into `BackdropSky` whenever it is present (Gathering already covers the full sky with `SkyBackdrop`, so it needed no change).
  - **Interior `DayOnlyExtras` respect their authored alpha:** `UpdateInteriorWindow` was overwriting `WindowSunRay1/2.A` and `WindowCloud.A` with raw `dayStrength`, so the barn sun-ray polygons (authored at alpha 0.18 and 0.12) became fully opaque at noon and dominated the interior with a saturated yellow band. `DayOnlyExtras` is now a `(name, maxAlpha)` tuple array; the runtime alpha is `maxAlpha * dayStrength`, restoring the intended subtle daylight cue for both barn rays and the shop cloud.
  - `dotnet test` -> **345 / 345 passing**; manual smoke walkthrough re-run for hovers (storage/shop/cottage/farmer/request board), panel exclusivity, night sky, interior day/night windows, and shop hours.

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
| `dotnet test` after Phase 7 (town hover + visual polish) | PASS (345/345) |
| Manual smoke pass (Phase 7 stepwise walkthrough) | PASS (all 6+ items confirmed by user) |

## Error Log

| Timestamp | Error | Attempt | Resolution |
|-----------|-------|---------|------------|

## 5-Question Reboot Check

| Question | Answer |
|----------|--------|
| Where am I? | Phase 7 wrap-up: town hover wiring + outdoor/interior visual polish all merged into `main`; smoke walkthrough green; ready to retire the Light Gathering Area task and move on. |
| Where am I going? | Archive the root `task_plan.md` / `findings.md` / `progress.md` triple under `docs/archive/planning/` once the next task starts. Next candidate is the Facility System (which can now consume the wood/stone the gathering area produces). |
| What's the goal? | Ship milestone 1's last missing world zone (Light Gathering Area) with the polish pass that surfaces hover hints in town, lets day/night read correctly outdoors and through interior windows, and gates the shop to 09:00-18:00. |
| What have I learned? | Godot 2D `z_index` overrides scene-tree order, so layered backdrops still need disciplined z values; the `SkyBackdrop` strip alone is not enough to dim the sky at night when a wider `BackdropSky` exists at `z=-11`; trimming `NightOverlay` to the horizon plus modulating `BackdropSky` is what produces a uniform night sky; per-extras alpha needs to multiply by the authored max alpha, otherwise `dayStrength=1.0` blows out subtle effects like sun rays. |
| What have I done? | Phase 6 baseline (smoke polish + shop hours, 344/344) plus Phase 7 (Town field-notes wiring, request-board through Guild Board, panel exclusivity, moon/stars z-layering, pale-yellow moon, NightOverlay horizon trim, BackdropSky tint, `DayOnlyExtras` alpha scaling). Final state: 345/345 tests, smoke checklist refreshed, all changes committed on `main`. |
