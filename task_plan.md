# Task Plan: Harvest Manor Milestone 1 Foundation Continuation

## Goal
Continue `Harvest Manor` milestone 1 on the existing worktree and branch, prioritizing real-runtime polish, save/load reliability, and session-recoverable project context.

## Current Phase
Phase 6

## Phases

### Phase 1: Context & Constraints Snapshot
- [x] Confirm the active worktree is `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation`
- [x] Confirm the active branch is `codex/milestone-1-foundation`
- [x] Re-read the approved design spec and implementation plan
- [x] Capture the current milestone guardrails in `findings.md`
- **Status:** complete

### Phase 2: Validation Baseline & Recent Fixes
- [x] Re-run `dotnet test tests/HarvestManor.Game.Tests/HarvestManor.Game.Tests.csproj`
- [x] Re-run `dotnet build game/HarvestManor.csproj`
- [x] Re-run the Godot 4.6.2 .NET smoke command
- [x] Commit the already-staged modal panel and unreadable save-slot repair changes
- **Status:** complete

### Phase 3: Runtime Polish Pass
- [x] Fix the highest-value panel/input/runtime issues exposed by real play
- [x] Add regression tests for new runtime behavior
- [x] Keep the worktree clean after each polished batch
- [x] Continue selecting the next best polish target from real-runtime behavior
- **Status:** complete

### Phase 4: Save/Load Compatibility & Display Consistency
- [x] Audit backward-compatible save loading behavior for future field additions
- [x] Verify restored unlock-state fallback behavior for legacy saves with no unlock history
- [x] Add focused regression coverage for load-path compatibility fixes
- [x] Run full-suite verification for the compatibility batch
- **Status:** complete

### Phase 5: Scene/Layout & Interaction Feedback
- [x] Audit click regions and scene readability for farm, town, and panel interactions
- [x] Tighten player-facing feedback for blocked or invalid actions
- [x] Verify panel open/close flow remains clear across mouse and keyboard paths
- **Status:** complete

### Phase 6: Milestone Verification & Handoff
- [x] Run the full automated suite for the current batch
- [x] Run the Godot smoke test after the current batch
- [x] Summarize the current batch, verification evidence, and next candidates
- **Status:** in_progress

## Key Questions
1. Which remaining runtime issue is the most player-visible problem in the current vertical slice?
2. Which save/load behaviors need compatibility hardening before more milestone work piles on?
3. Which interaction problems should get automated coverage versus smoke-only verification?

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Keep working only in `D:\game project\harvest-manor\.worktrees\codex-milestone-1-foundation` on `codex/milestone-1-foundation` | User explicitly required no new worktree and no new branch |
| Prioritize runtime polish over adding new systems | Current milestone already has a playable vertical slice; polish issues now give the highest player value |
| Verify each meaningful batch with `dotnet test`, `dotnet build`, and real Godot smoke | This project has already surfaced issues that only showed up at runtime |
| Treat `misc2` controller mapping warnings and the Vulkan registry warning as environment noise unless directly tied to a bug | User explicitly pre-triaged these warnings as non-actionable noise |
| Store persistent session context in worktree-root markdown files | New windows can recover state without relying on chat-only context |
| Default only later-added save progress collections during deserialization, while keeping inventory/storage/plot payloads strict | This preserves backward compatibility for older milestone saves without silently accepting badly broken core state |
| Fall back to the starting 2x2 unlocked plots when a legacy snapshot has no unlock history | An empty unlock list would otherwise make the loaded farm look completely broken and locked |
| Make storage panel edge-state copy item-specific instead of referring to a generic "selected item" | Single-direction and fully blocked transfer states were readable in code but still too vague in runtime, especially when the panel showed two concrete item candidates |
| Prefer surfacing a still-available shop sell action before explaining why buy is blocked | In real play, the actionable takeaway is "you can still sell this" rather than a purely negative buy-state warning |
| Put concise blocker reasons directly on disabled shop buttons while keeping price copy for enabled actions | Players should not need to read the panel body first just to understand why Buy or Sell is currently unavailable |
| Restore the latest shop/storage context after closing a panel instead of overwriting it with a generic close message | The main status label should stay close to the player's immediate intent and recent action flow |
| Prefer player-facing display names over internal item ids on panel surfaces | Inventory, storage, and shop panels are read-heavy UI, so raw ids break the game's intended readability more than they help development |
| Thread item catalog display names through the remaining global request/shop/storage status builders with optional catalog inputs | This finishes the player-facing readability pass without forcing every pure helper call site or regression test to load content catalogs |
| When request completion succeeds and an item catalog is available, report the delivered quantity and item display name instead of the internal request id | The final request-board confirmation should read like player-facing game feedback rather than a debug-facing identifier |
| Let a shop or storage hotspot close its own already-open panel while still blocking different world interactions during modal flow | This keeps the modal boundary intact for unrelated interactions, but removes an unintuitive runtime dead end where the same hotspot could not perform its implied toggle |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| `rg.exe` failed with `Access is denied` in this environment | 1 | Fell back to PowerShell `Get-Content` and `Select-String` for code inspection |
| `ScriptPathAttributeGenerator` warning during `dotnet test` (`GodotProjectDir` null or empty) | 1 | Treated as non-blocking test-environment noise because tests and builds still pass |
| `SaveGameStore` payload helper initially failed to compile because required namespaces were missing | 1 | Added the missing `HarvestManor.Core.Inventory` and `HarvestManor.Core.Time` usings |

## Notes
- Latest verified commits in this phase: `d51a0c0 fix: enforce modal panels and repair unreadable saves`, `801a1a4 fix: explain blocked world interactions`, `2f9fc50 docs: add persistent milestone recovery notes`
- Current worktree status was clean immediately after `801a1a4`
- Latest verified batch after those commits tightened storage panel one-way and fully blocked wording so the actionable and blocked item directions stay explicit
- Latest verified batch after that also tightened shop single-direction wording so sell-ready states stay visible even when buy is blocked by money or inventory space
- Latest verified batch after that made disabled shop buttons explain blocked buy/sell reasons directly and restored the latest panel-context status when shop or storage closes
- Latest verified batch after that threaded item display names into inventory, storage, and shop panel rendering so panel surfaces no longer expose internal ids
- Latest verified batch after that threaded player-facing item display names through request-board, shop, and storage world-status helpers so farm/town feedback no longer leaks internal ids outside the panels
- Latest verified batch after that also replaced the request-completion success copy so final turn-in feedback names the delivered crop instead of the internal request id
- Latest verified batch after that fixed an unreachable panel-toggle path so clicking the same shop or storage hotspot can now close its own panel instead of always hitting the generic modal-world blocker
- Latest verified batch after that made blocked shop/storage hover and click feedback panel-aware, so same-hotspot interactions now advertise "click again to close" while cross-service attempts explain which open panel must close first
- Latest verified batch after that made load-from-save startup copy farm-state-aware, so a restored session now surfaces harvest-ready or still-unwatered crops instead of always falling back to the same generic fresh-session prompt
- Latest verified batch after that let loaded saves with no urgent farm work fall through to request-board progress on the main startup status, so the first-screen farm label now agrees with the request label about turn-in readiness
- Latest verified batch after that removed `slot-1.json` from startup copy and let fully completed request boards surface on load too, so startup feedback now stays player-facing even when restored request work is already finished
- If a later session starts from a different cwd, recover by opening this worktree root first, then reading this file, `findings.md`, and `progress.md`
