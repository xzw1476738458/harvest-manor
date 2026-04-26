# Findings & Decisions

## Requirements

- Work from `D:\game project\harvest-manor`
- Treat `main` as the active branch
- Keep long-term documentation under `docs/`
- Use root `task_plan.md`, `findings.md`, and `progress.md` only for the current active complex task
- Archive root planning files when the task completes, the branch merges, or the working context changes clearly

## Research Findings (Pre-Task Audit)

- The design spec (`docs/superpowers/specs/2026-04-08-harvest-manor-design.md`) lists "land expansion" as the strongest growth signal of milestone 1
- The current implementation only allows unlocking **one** plot (`2,0`) for **120g** via `DemoExpansionPlotKey` / `DemoExpansionCost` constants in `game/scripts/world/GameBootstrap.cs`
- `FarmGrid` is sized 6x6 (36 plots) but only 4 are unlocked by default and only 1 is unlockable, leaving 31 plots permanently inaccessible
- `FarmScene.tscn` contains only 5 `FarmPlotNode` instances, so even if expansion logic were richer, the scene would not render the new plots
- `Player.tscn` has no `Camera2D` node, so the viewport is locked to the scene origin; an enlarged farm cannot be reached
- The `FarmExpansionService` already enforces "spend gold and mark plot unlocked"; what's missing is the pricing layer and a visible field of plots
- `StatusMessageBuilder.BuildFarmPlotHoverStatusMessage` already takes `expansionPlotKey` and `expansionCost` arguments, so the call sites can be retargeted at a service-driven cost lookup without rewriting the message format
- `GameBootstrapIntegrationTests.cs` exercises the demo expansion path explicitly; those assertions will need to be retargeted at the new tier service rather than removed
- No NPC, facility, dialogue, audio, or gathering systems exist in the code; those are out of scope for this task but tracked as the next backlog candidates

## Technical Decisions

| Decision | Rationale |
|----------|-----------|
| Add a new `ExpansionTierService` in `game/scripts/core/Progression/` | Keeps pricing logic out of Godot scripts and out of the gold-spending service; makes pricing rules unit-testable in isolation |
| Define tiers as plain code data inside `ExpansionTierService` rather than JSON for now | Tier shape is design-driven, not content-driven; a JSON file would be over-engineered for 4 tiers |
| Default tier shape: ring 0 free (4), ring 1 @120g (8), ring 2 @350g (12), ring 3 @800g (12) | Ring 0 matches today's free defaults; ring 1 keeps today's demo price as the "first reinvestment" hook; rings 2 and 3 create late-day goldsinks |
| Camera follow uses `position_smoothing_enabled = true` and scene limits | Cheapest fix that prevents motion sickness and prevents leaving the painted background |
| Keep `FarmExpansionService` as the gold-spending gate | Single responsibility; pricing flows in via the service, enforcement stays unchanged |
| Save format does not change | `UnlockState` is still a set of plot keys; no migration needed |

## Resources

- Long-term workflow rules: `D:\game project\harvest-manor\docs\project-workflow.md`
- Archived previous task context: `D:\game project\harvest-manor\docs\archive\planning\2026-04-25-mainline-presentation-and-smoke-pass\`
- Design spec: `D:\game project\harvest-manor\docs\superpowers\specs\2026-04-08-harvest-manor-design.md`
- Milestone-1 plan: `D:\game project\harvest-manor\docs\superpowers\plans\2026-04-08-harvest-manor-milestone-1-foundation.md`
- Smoke checklist: `D:\game project\harvest-manor\docs\testing\milestone-1-smoke-checklist.md`

## Implementation Notes (post-pass)

- `ExpansionTierService.CreateDefault()` ships with five rings using Chebyshev distance: free 2x2 corner, then four paid rings at 120 / 280 / 600 / 1200 gold (5 / 7 / 9 / 11 plots respectively, totaling 32 paid plots out of 36)
- Ring 0 plots are produced by the same `EnumeratePlotKeysWithinDistanceBand` enumerator as paid rings, which means the default unlocked set is now fully driven by the tier configuration instead of a separate constant
- `GameBootstrap.TryPurchaseCheapestLockedPlot()` walks `EnumerateLockedTiers()` in increasing cost order, so the F7 quick-unlock always grabs the cheapest reachable plot rather than the previous hard-coded `(2,0)` shortcut
- `Player.tscn` now mounts a `Camera2D` with `position_smoothing_speed = 8.0`; no scene-bound limits were added yet, so the camera can pan slightly past the field edges - a small follow-up if it feels off in playtesting
- The `FarmStatusPanel` is still parented under `FarmScene` (a `Node2D`), so it scrolls with the camera; converting it into a CanvasLayer is queued as a follow-up if the scrolling notice board feels disorienting
- Save files remain forward-compatible: the `UnlockState` is still a flat set of plot keys, so existing saves restore correctly and new plot unlocks are appended without migration

## Open Questions / Follow-Ups

- Should the right-bottom collision wall and the cottage door (`Bed` at y=396) be moved to align with the grid extent, or left where they are as a stylistic asymmetric layout?
- Should `FarmStatusPanel` be promoted into a CanvasLayer so it stays viewport-anchored as the camera moves?
- Should the gate to town (`GateEast` at y=470) get an additional southern exit or path hint now that the field extends much further south?

## Current Context

- Current branch: `main`
- Current task: Real Farm Expansion System (first step of Manor Growth phase)
- Latest verified commit: `caaa857 fix(scene): make moon and stars actually visible at night`
- Verification snapshot: 246/246 tests passing, 0 build warnings, Godot headless smoke launch clean
- Working directory before commit: planning files updated, `ExpansionTierService.cs` added, `Player.tscn` and `FarmScene.tscn` extended, smoke checklist refreshed
