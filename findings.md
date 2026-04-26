# Findings & Decisions

## Requirements

- Work from `D:\game project\harvest-manor`
- Treat `main` as the active branch
- Keep long-term documentation under `docs/`
- Use root `task_plan.md`, `findings.md`, and `progress.md` only for the current active complex task
- Archive root planning files when the task completes, the branch merges, or the working context changes clearly

## Research Findings (Pre-Task Audit)

- Design spec §8.4 lists a "Light Gathering Area" as one of milestone 1's four required world structures, but no scene, script, or service exists for it today
- `data/items/items.json` already declares `wood` and `stone` as `Material`-category stackables (max 99) - they were placeholders waiting for a real source
- No shop offer or request currently references `wood` or `stone`, so the materials are entirely unreachable in-game
- Scene transitions go through `SceneGate` (`TargetScene` string + `GateEntered` signal) and `EnterBuildingInteraction`, both already wired into `GameBootstrap.OnGateEntered`
- `GameBootstrap` resolves a scene type to a path via `ResolveScenePath` and computes spawn coordinates per `(target, source)` pair via `ResolveSpawnForTransition`; both will need a new `gathering` entry
- Day-end runs through `EndDay` -> `OnDayEndRequested` and already mutates farm state via `DayEndService`; resetting harvested resource nodes will hook in here
- `HoverableInteractionArea` is the shared base for all clickable world objects (`Bed`, `Shop`, `Storage`, `RequestBoard`, plot nodes); the gathering nodes should follow the same interaction pattern for input parity
- `SaveState` already persists farm grid, inventory, wallet, completed requests, and unlocked plots; adding a `HarvestedGatheringNodeIds` set follows that pattern
- `FarmPlotNode` builds its visuals at runtime via `Polygon2D` and `CropVisualTheme`; the same approach will keep gathering nodes art-asset-free for now

## Technical Decisions

| Decision | Rationale |
|----------|-----------|
| New core layer namespace `HarvestManor.Core.Gathering` | Mirrors `Farming` / `Progression` / `Inventory` package boundaries |
| One `ResourceNode` Godot script with an exported `ItemId` | Matches the data-driven pattern used for plots and avoids per-resource subclasses for milestone 1 |
| Day-end fully resets every resource node | Smallest reliable rule; staggered respawn would require day-counter metadata per node |
| Mount the gathering gate on `TownScene` first | Town has more spatial breathing room than the now-packed farm; world layout becomes farm <-> town <-> gathering |
| Sold prices: wood = 4g, stone = 6g (sell-only via shop offers, buy disabled) | Low enough to keep farming primary, high enough to make the side-trip feel rewarding |
| Use existing `Polygon2D` + theme color approach for tree/rock visuals | Keeps the task art-asset-free and consistent with current crop sprite scheme |
| Save format adds `HarvestedGatheringNodeIds: string[]` only | Backwards-compatible: missing field defaults to empty set |

## Resources

- Long-term workflow rules: `D:\game project\harvest-manor\docs\project-workflow.md`
- Design spec: `D:\game project\harvest-manor\docs\superpowers\specs\2026-04-08-harvest-manor-design.md` (§8.4 Light Gathering Area)
- Milestone-1 plan: `D:\game project\harvest-manor\docs\superpowers\plans\2026-04-08-harvest-manor-milestone-1-foundation.md`
- Smoke checklist: `D:\game project\harvest-manor\docs\testing\milestone-1-smoke-checklist.md`
- Archived previous task context: `D:\game project\harvest-manor\docs\archive\planning\2026-04-25-real-farm-expansion\`

## Implementation Notes (post-pass)

- `GatheringService.TryHarvest` is the only mutator: it short-circuits with typed outcomes (`UnknownNode`, `AlreadyHarvested`, `InventoryFull`) before touching state, so the inventory and harvested set never desync if any guard trips
- `DefaultGatheringNodes` lives on `GameBootstrap` (4 trees + 3 rocks) so the .tscn names and the service are the single source of truth - any extra node added to the scene must also be appended to that list, otherwise clicks return `UnknownNode`
- `BuildSeasonShopOffers` now accepts an optional `IReadOnlyDictionary<string, ItemDefinition>` and treats every entry whose `Category` equals `Material` (case-insensitive) as always-available - the design hook for future materials
- Godot's default `ui_focus_next` / `ui_focus_prev` were cleared in `project.godot` so `Tab` reaches `_UnhandledInput` reliably for the inventory toggle (and, by extension, future hotkeys we want to layer on top)
- `ShouldSilenceHoverPreview` only mutes hover hints in `PanelMode.Inventory`; `Shop` and `Storage` keep their click-driven blocked-interaction copy because that copy still helps when the player accidentally hovers another door
- `ResourceVisualTheme` uses single-polygon silhouettes (a stylized pine and a chunky boulder) so we can postpone real art without leaving the gathering area looking like a placeholder; the theme is keyed by `ItemId`, so adding new resources only requires a new visual entry
- Save migrations stay free: `SaveGameStore.Deserialize` defaults `HarvestedGatheringNodeIds` to an empty list, so legacy saves load identically to fresh starts

## Open Questions / Follow-Ups

- Should gathering nodes have a per-node respawn delay (e.g., regrow over 2 days) once base loop ships?
- Should rare materials (e.g., hardwood / iron ore) be added in a follow-up task, or wait for the facility system that consumes them?
- Should we expose a "gathering tally" badge in the HUD similar to the gold counter?

## Current Context

- Current branch: `main`
- Current task: Light Gathering Area (milestone 1's last missing world zone)
- Latest verified commit: `29341dc feat(gathering): wire the Whispering Woods scene into the world`
- Verification snapshot: 321/321 tests passing, 0 build warnings, Godot headless smoke launch clean (15 shop offers including wood/stone)
- Working directory before this update: gathering area shipped end-to-end; planning + smoke checklist refreshed; awaiting user playtest before archiving
