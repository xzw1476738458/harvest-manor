# Task Plan: Light Gathering Area

## Planning File Rules

- `docs/` holds long-term project documentation.
- Root `task_plan.md`, `findings.md`, and `progress.md` serve only the current active complex task.
- When a task is completed, a branch is merged, or the working context changes clearly, archive the current root planning files into `docs/archive/planning/` and create a new set.
- Long-term reference: `docs/project-workflow.md`

## Goal

Deliver milestone 1's still-missing world structure: a **Light Gathering Area** (spec §8.4). Add a fourth scene where the player can pick up small amounts of `wood` and `stone`, with respawning resource nodes that reset on day end. This finishes milestone 1's "farm / home-storage / town / gathering" four-zone shape and gives later facility/processing tasks a real material source.

## Current Phase

Phase 1 (planning)

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
- [ ] Add `GatheringNodeStateTests` covering harvest -> harvested -> reset cycle and the produced item id
- [ ] Add `GatheringServiceTests` covering "harvest one node", "cannot harvest twice", and "DayEndReset restores all nodes"
- [ ] Add `GatheringSceneLayoutTests` asserting `GatheringScene.tscn` exposes the expected resource node count and an `ExitGate` to town/farm
- [ ] Add `SaveStateTests` cases verifying the harvested set round-trips
- [ ] Confirm new tests fail before implementation

### Phase 3: Implement Core Gathering Logic
- [ ] Add `GatheringNode` record (Id, ItemId, IsHarvested) and `GatheringService` (harvest + reset) under `game/scripts/core/Gathering/`
- [ ] Extend `SaveState` with `HarvestedGatheringNodeIds`
- [ ] Wire `GatheringService.ResetForNewDay` into the existing day-end pipeline
- [ ] Surface "+1 wood" / "+1 stone" status messages in `StatusMessageBuilder`

### Phase 4: Wire Godot Presentation
- [ ] Author `scenes/world/GatheringScene.tscn` with backdrop, walls, and a handful of `ResourceNode` instances (Polygon2D-styled trees and rocks)
- [ ] Add `ResourceNode.cs` (subclass of `HoverableInteractionArea`) that emits a typed signal when clicked
- [ ] Add a new scene gate from `TownScene.tscn` (or `FarmScene.tscn`, TBD in Phase 4) to `GatheringScene`
- [ ] Register `GatheringSceneType` in `GameBootstrap` scene-switch tables
- [ ] Add a small wood/stone shop offer to `data/shop/shop_offers.json` (sell-only by default)

### Phase 5: Verification
- [ ] Run full `dotnet test` suite
- [ ] Run `dotnet build game/HarvestManor.csproj`
- [ ] Run Godot headless smoke launch
- [ ] Manual smoke pass: walk to gathering area, harvest, verify inventory + day reset + sell flow
- [ ] Update smoke checklist with the new zone

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
