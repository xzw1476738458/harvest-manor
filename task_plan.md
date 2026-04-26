# Task Plan: Light Gathering Area

## Planning File Rules

- `docs/` holds long-term project documentation.
- Root `task_plan.md`, `findings.md`, and `progress.md` serve only the current active complex task.
- When a task is completed, a branch is merged, or the working context changes clearly, archive the current root planning files into `docs/archive/planning/` and create a new set.
- Long-term reference: `docs/project-workflow.md`

## Goal

Deliver milestone 1's still-missing world structure: a **Light Gathering Area** (spec §8.4). Add a fourth scene where the player can pick up small amounts of `wood` and `stone`, with respawning resource nodes that reset on day end. This finishes milestone 1's "farm / home-storage / town / gathering" four-zone shape and gives later facility/processing tasks a real material source.

## Current Phase

Phase 5 (verification complete)

## Scope Guard

This task delivers only the smallest viable gathering loop:

- A new `GatheringScene.tscn` connected via a scene gate to one existing scene (likely `TownScene`)
- A `ResourceNode` interaction (subclassed for tree / rock variants) that gives 1 item and turns "harvested"
- Day-end reset: every resource node respawns when the day rolls over
- `wood` / `stone` already live in `data/items/items.json`; this task wires them into real gameplay
- Optional shop offers so the player can sell wood/stone for a small price (no buying)
- Tests covering the resource node state machine and the day-end reset
- Save/load round-trip for the harvested set

This task explicitly does **not** include:

- Tools (axes, picks, durability)
- Stamina cost on gathering
- Random drop tables / rarity tiers
- Multiple gathering biomes
- Facility / building system (next task)
- NPCs in the gathering area
- New crops or seasonal gathering content

These are queued behind this task.

## Phases

### Phase 1: Planning Reset
- [x] Archive previous expansion planning files into `docs/archive/planning/2026-04-25-real-farm-expansion/`
- [x] Write new root planning files
- **Status:** complete

### Phase 2: Failing Test Coverage First
- [x] Add `GatheringStateTests` covering harvest -> harvested -> reset cycle and the produced item id
- [x] Add `GatheringServiceTests` covering "harvest one node", "cannot harvest twice", "InventoryFull", "UnknownNode", and "DayEndReset restores all nodes"
- [x] Add `GatheringSceneLayoutTests` asserting `GatheringScene.tscn` exposes the expected resource node count and an `ExitGate` to town
- [x] Extend `SaveGameStoreTests` to round-trip the harvested set and accept legacy payloads with no field
- [x] Confirm new tests fail before implementation
- **Status:** complete

### Phase 3: Implement Core Gathering Logic
- [x] Add `GatheringNodeDefinition` record, `GatheringState`, `GatheringHarvestResult`, and `GatheringService` under `game/scripts/core/Gathering/`
- [x] Extend `SaveGameSnapshot` and `SaveGameStore` with `HarvestedGatheringNodeIds` (legacy-compatible)
- [x] Hold a live `_gatheringService` on `GameBootstrap`; rehydrate from the snapshot on load
- [x] Surface gathering outcomes through `StatusMessageBuilder.BuildGatheringStatusMessage`
- **Status:** complete

### Phase 4: Wire Godot Presentation
- [x] Author `scenes/world/GatheringScene.tscn` with sky/forest backdrop, walls, four trees and three rocks
- [x] Add `ResourceNode.cs` (subclass of `HoverableInteractionArea`) plus `ResourceVisualTheme` for wood/stone polygons
- [x] Add `GateNorth` to `TownScene.tscn` (split `WallTop` into `WallTopLeft` + `WallTopRight`) targeting `gathering`
- [x] Register `GatheringSceneType` and town<->gathering spawn pairs in `GameBootstrap`; route `LoadScene` through `WireGatheringScene` + `RenderGatheringNodes`
- [x] Wire `OnResourceNodeInteracted` into `GatheringService.TryHarvest` (refresh HUD, panels, autosave) and reset every node in `EndDay`
- [x] Make `BuildSeasonShopOffers` keep `Material`-category items visible across all seasons
- [x] Add wood (4g) and stone (6g) sell offers to `data/shops/general-store.json`
- **Status:** complete

### Phase 5: Verification
- [x] Run full `dotnet test` suite (321/321 passing)
- [x] Run `dotnet build game/HarvestManor.csproj` (0 errors / 0 warnings)
- [x] Run Godot headless smoke launch (15 shop offers + save restored without warnings)
- [x] Update `docs/testing/milestone-1-smoke-checklist.md` with the gathering area + Tab inventory items
- [ ] Manual smoke pass through the new loop (waiting on user playtest)
- **Status:** core verification complete; manual smoke deferred to user

## Key Questions

1. Should the gate to gathering live on `TownScene` (compact world ring) or `FarmScene` (treats the farm as a hub)?
2. Should resource nodes share a single `ResourceNode` script with an exported `ItemId`, or specialize into `TreeNode` / `RockNode`?
3. Should the day reset mark every node "fresh" or stagger respawns over multiple days?
4. Should sold prices for wood/stone be high enough to short-circuit early farming, or low enough that gathering is a side income?

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| Gathering area is the next task instead of facilities or NPCs | Spec §8.4 lists gathering as one of milestone 1's four required zones; facilities (Phase 2) usually consume gathered materials, so this is the dependency root |
| Use a single generic `ResourceNode` driven by exported `ItemId` instead of separate scripts | Same Polygon2D pattern as `FarmPlotNode`; lets us add new resources by editing the scene only |
| Day-end fully resets all harvested nodes | Simplest viable rule that always works; staggered respawn is an enrichment we can add once playtesting demands it |
| `wood` / `stone` keep their existing item definitions | They are already in `items.json` from earlier milestones; reusing them keeps save format stable |

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| (none yet) | | |

## Notes

- Archived prior planning context to `docs/archive/planning/2026-04-25-real-farm-expansion/`
- Latest verified commit before this task: `ad2bd31 fix(ui): silence world hover hints while the inventory panel is open`
- Working branch: `main`
- Working directory: `D:\game project\harvest-manor`
