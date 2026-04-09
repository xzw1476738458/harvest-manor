# Findings & Decisions

## Requirements
- Continue `Harvest Manor` on the existing worktree: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation`
- Continue on the existing branch: `codex/milestone-1-foundation`
- Do not create a new branch
- Do not create a new worktree
- Use the approved design spec and milestone plan as the baseline
- Prioritize real-runtime polish and stability over adding new systems
- Focus first on click regions, UI refresh, scene layout, interaction feedback, save/load display consistency, and panel flow
- Treat `misc2` controller mapping warnings and the Vulkan registry warning as environment noise unless they become directly relevant

## Research Findings
- The milestone already has a minimal playable loop: save restore, farm interaction, basic shop/storage/request flow, and a demo expansion hook
- A runtime configuration mismatch between `project.godot` and the C# project was previously the root cause of Godot assembly load failures, and that has already been fixed earlier in this branch
- Some important regressions only appeared in real Godot runtime, not from compile-only checks, so smoke verification is necessary after meaningful interaction changes
- Modal panels previously allowed confusing background interaction states; fixing those boundaries improved runtime clarity more than adding new features would have
- Silent blocking is still player-hostile even when technically correct, so blocked world interactions benefit from explicit status messaging
- The current branch history shows a real legacy-save shape difference: older `PlotSnapshot` payloads predate the later `isWateredToday` field
- The more immediate compatibility break in current code was that missing `unlockedPlotKeys` or `completedRequests` caused the whole save to be rejected as unreadable
- Even when deserialization succeeds, an empty unlock list in a legacy snapshot makes the entire farm appear locked unless the bootstrap layer restores the default starting 2x2 plots
- Panel visibility changes were technically working, but the farm status label did not explain when a panel opened or closed, which made the flow feel less deliberate during real play
- Scene inspection confirmed a separate layout problem: many interactive hotspots were effectively invisible because the scenes only exposed labels and collision shapes, not visible clickable surfaces
- After making hotspots visible, the next discoverability gap was hover affordance: there was still no change at all when the cursor moved across an active interaction
- After adding hover animation, the remaining clarity gap was pre-click intent: players could see a hotspot was active without always knowing what the click would do
- Generic hover intent text was still not enough in runtime because a highlighted plot could look actionable even when the player had no seeds, no inventory space, or not enough gold for the demo unlock
- Town-side actions still had a feedback gap after the panel polish passes: buy, sell, store, withdraw, and request turn-in refreshed local UI, but the main farm status label stayed stale and could make outcomes feel less definite
- Request board hover was still using the generic world-interaction helper even though request progress was already computed elsewhere, so the hover preview lagged behind the actual request state shown in the town panel
- The persistent request-status label itself was also too optimistic: it said "Click board to turn in" even when the player still needed more crops
- Shop and storage panels still had a smaller flow gap after the town-feedback fixes: opening a panel or cycling shop offers refreshed the panel body, but the main farm status label stayed on a generic open message instead of reflecting the currently selected actionable item
- Even after adding browse-state panel messages, shop/storage actions could still leave the main status label stuck on a one-off result string with no follow-up context about what the player could do next inside the still-open panel
- Storage panel still had one last clarity gap in edge cases: when only one transfer direction worked or both directions were blocked, the body/main-status copy fell back to a vague "selected item" phrasing and the disabled buttons still looked like live actions
- Shop panel had a parallel clarity gap: if the selected offer could still be sold but buying was blocked by money or inventory space, the status copy focused only on the buy-side blocker and hid the still-available sell action
- Shop panel still had a final button-level clarity gap after the wording passes: the body text explained why Buy or Sell was blocked, but the disabled buttons themselves still only showed generic price labels
- Panel close flow still had a context gap after the browse-feedback passes: closing shop or storage overwrote the richer recent panel context with a generic "Panels closed" message
- Panel surfaces still leaked internal item ids (`parsnip_seed`, `potato_crop`) even though the item catalog already contained player-facing display names
- Even after panel surfaces switched to display names, the main farm/request status text still leaked internal item ids during request-board, shop, and storage interactions
- After the broader status-text pass, request completion success copy still exposed the internal request id even though the rest of the request-board flow had already switched to player-facing item names
- Shop and storage hotspot handlers already contained toggle-style `nextMode` logic, but the shared modal blocker returned before that branch could run, so clicking the same hotspot could never actually close its own panel at runtime
- After the same-hotspot toggle fix, blocked hover/click feedback still lacked request context, so a shop or storage hotspot could be closable in code while its hover text still claimed the player had to close the panel first

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Keep `GameBootstrap` as the orchestration point for current milestone runtime behavior | The existing vertical slice is already concentrated there, so polish changes are cheapest and safest in one place for now |
| Add pure/static helpers for interaction-policy decisions where possible | Keeps tests fast and deterministic without requiring a full Godot scene harness |
| Use focused regression tests in `GameBootstrapIntegrationTests` for runtime policy changes | These tests already cover save/load and interaction behavior near the current problem area |
| Keep planning files in the worktree root | They are easy for later sessions to find and align with the exact branch/worktree the user wants continued |
| Deserialize save files through a payload shape that can supply defaults for later-added progress collections | This lets older milestone saves load instead of being forced into fresh-start fallback |
| Preserve strict validation for inventory, storage, plot data, gold, stamina, and date fields | This avoids papering over truly broken saves while still being friendly to version drift |
| Use the farm status label as the main lightweight feedback channel for panel open/close transitions | It keeps panel flow understandable without adding a larger HUD or modal overlay system |
| Add simple visible hotspot surfaces directly in `FarmScene.tscn` and `TownScene.tscn` for current milestone interactions | This improves click discoverability without introducing new systems or larger UI architecture |
| Standardize hover feedback through a tiny shared world helper instead of hand-tuning each interaction separately | It keeps the behavior consistent across bed, plots, shop, storage, and request board |
| Reuse `FarmStatusLabel` for world hover previews instead of adding a second transient tooltip system | It gives immediate pre-click guidance with very low implementation risk |
| Feed the current inventory and wallet state into plot hover previews | This keeps pre-click guidance honest when a highlighted interaction is blocked by missing seeds, full inventory, or insufficient gold |
| Reuse the farm status label for post-action town feedback too | This keeps the current vertical slice readable without adding a separate notification system |
| Drive request-board hover from the same request-progress data the town UI already knows about | This keeps hover guidance consistent with the actual turn-in state without inventing a new request-preview system |
| Make the persistent request-board label distinguish "not ready" from "ready to turn in" | This keeps town-side status text honest and consistent with the new hover preview |
| Use dedicated browse-status builders for shop and storage panel selection state | This lets panel-open and offer-navigation feedback stay specific without overloading the generic panel mode status helper |
| Combine action results with the current panel browse context when the panel stays open | This preserves immediate confirmation while keeping the next likely action visible |
| Keep storage transfer copy item-specific in blocked states for both the panel body and the main farm status | Players should be able to tell at a glance which concrete item can move and which direction is blocked without mentally decoding generic UI text |
| Let shop status messaging prioritize a still-available sell action when buy is blocked | The player-facing next step should stay obvious even when the selected offer cannot currently be purchased |
| Put concise blocker reasons on disabled shop buttons while preserving price copy when the action is available | The button surface itself should explain the immediate blocker without forcing the player to cross-reference the body text |
| Track the latest panel-context farm status separately from transient blocked/open-close copy | Closing a panel should restore the most relevant recent shop/storage context instead of flattening it into a generic close message |
| Thread item catalog display names into panel rendering without rewriting all global status builders in the same batch | This fixes the most visible player-facing readability issue first while keeping the current polish batch small and low-risk |
| Add optional `itemCatalog` inputs to the remaining pure global status builders instead of replacing their existing signatures outright | This preserves old raw-id behavior for baseline tests while letting runtime call sites opt into player-facing names |
| Keep the legacy request-completion success text only for helper calls with no catalog, but switch runtime/catalog-backed paths to a delivered-quantity display-name message | This preserves older baseline behavior while making actual turn-in confirmation read consistently with the rest of the polished town feedback |
| Allow same-service hotspot clicks to toggle their own panel closed, but keep different service hotspots blocked while a modal panel is open | This matches the existing handler intent, improves panel-open/close flow, and avoids weakening the current modal-interaction guardrails |
| Thread the requested panel mode into blocked shop/storage hover and click messaging | This keeps modal feedback honest after the toggle fix: the same hotspot can explain how to close itself, while a different hotspot can explain which open panel is currently in the way |

## Issues Encountered
| Issue | Resolution |
|-------|------------|
| `rg.exe` could not be used in this shell due to an access-denied failure | Inspected files with PowerShell instead |
| `dotnet test` emits a `ScriptPathAttributeGenerator` warning | Logged as non-blocking because automated tests still pass and runtime behavior is unaffected |
| Background world input felt broken when a panel was open because clicks were ignored without explanation | Added actionable farm-status messages and blocked the demo expansion shortcut while a panel is open |
| Legacy saves with no unlock history would currently restore with all plots locked | Fixed by falling back to the default starting unlocked plot keys during bootstrap |
| Opening and closing panels lacked explicit status feedback | Added panel-state messages for shop open, storage open, and returning to world interaction |
| Farm/town click regions were hard to discover because only labels were rendered | Added visible `Polygon2D` hotspot backgrounds for bed, plots, and town services |
| Visible hotspots still felt flat with no cursor-response | Added shared hover scaling and hotspot brightening to make active areas feel alive |
| Hover animation alone still left some interactions ambiguous | Added hover-preview status text that explains likely click outcomes and restores the last persistent status when the cursor leaves |
| Generic hover-preview text could still over-promise an action that would fail immediately | Made plot hover previews resource-aware for seed availability, harvest inventory space, and demo unlock affordability |
| Town actions could succeed or fail with only panel-local refreshes and no clear global confirmation | Added explicit farm-status results for shop buy/sell, storage transfer, and request board turn-in actions |
| Request board hover still looked static even after other interactions gained state-aware previews | Added a dedicated request-board hover message that reflects missing items, ready turn-ins, and completed-board state |
| Request progress label still implied the board was actionable before requirements were met | Updated the status text to show remaining quantity until the request is actually ready to turn in |
| Shop/storage panel flow still felt generic right after opening or changing offers | Added main-status summaries for the currently selected shop offer and current storage transfer candidates |
| Shop/storage action results still displaced the current panel context | Appended current browse-state summaries after buy/sell/store/withdraw result messages so the panel stays self-explanatory |
| Storage edge states still sounded too generic after the earlier browse-feedback pass | Named the blocked item and direction explicitly in storage status text, added blocked-state button labels, and made the main browse text distinguish between actionable versus fully blocked transfer states |
| Shop selection text still hid sell-ready states behind buy-side blockers | Reordered shop status messaging so the panel body and main farm status now keep "Ready to sell 1" visible before the missing-gold or inventory-full buy explanation |
| One new storage display-name regression test initially failed for the wrong reason because the source inventory was empty, so the helper correctly returned `none available` before it could hit the intended inventory-full branch | Restocked the test fixture with `stone` so the regression now exercises the intended blocked-withdraw path and still verifies display-name output |
| New panel-toggle regression tests initially failed at compile time because the panel-interaction helpers did not exist yet | Added focused `GameBootstrap` helpers, then routed the shop/storage hotspot handlers through them so the tests could drive the intended modal toggle behavior |
| New contextual blocked-message tests initially failed at compile time because `BuildBlockedWorldInteractionMessage` had no requested-panel overload | Added the optional requested-panel context and threaded it through shop/storage hover plus blocked-click paths |

## Resources
- Approved spec: `D:\game project\harvest-manor\docs\superpowers\specs\2026-04-08-harvest-manor-design.md`
- Approved implementation plan: `D:\game project\harvest-manor\docs\superpowers\plans\2026-04-08-harvest-manor-milestone-1-foundation.md`
- Main orchestration file: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\game\scripts\world\GameBootstrap.cs`
- Save deserialization file: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\game\scripts\core\Saves\SaveGameStore.cs`
- Main runtime behavior tests: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\tests\HarvestManor.Game.Tests\World\GameBootstrapIntegrationTests.cs`
- Save compatibility tests: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\tests\HarvestManor.Game.Tests\Saves\SaveGameStoreTests.cs`
- Scene layout tests: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\tests\HarvestManor.Game.Tests\World\FarmSceneLayoutTests.cs`, `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\tests\HarvestManor.Game.Tests\World\TownSceneLayoutTests.cs`
- Hover style helper: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\game\scripts\world\InteractionHoverStyle.cs`
- Hover preview wiring: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\game\scripts\world\GameBootstrap.cs`
- Hover style tests: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\tests\HarvestManor.Game.Tests\World\InteractionHoverStyleTests.cs`
- UI state tests: `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation\tests\HarvestManor.Game.Tests\UI\PanelControllerStateTests.cs`

## Visual/Browser Findings
- `FarmScene.tscn` currently exposes plots `(0,0)`, `(1,0)`, `(2,0)`, `(0,1)`, and `(1,1)` in-scene, matching the unlocked 2x2 start area plus the demo expansion plot
- `TownScene.tscn` currently places shop, storage, and request board click targets on a single lower row with labels directly above each target
- HUD currently exposes day, gold, stamina, and unlocked plot count in a compact top-left panel
- Inventory panel is hidden by default and becomes visible only in storage mode
- Shop and storage panels are hidden by default and rendered as separate control trees with explicit close buttons

---
Update this file whenever new runtime discoveries, risks, or decisions show up.
