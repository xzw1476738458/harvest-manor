# Task Plan: Mainline Presentation And Smoke Pass

## Planning File Rules

- `docs/` holds long-term project documentation.
- Root `task_plan.md`, `findings.md`, and `progress.md` serve only the current active complex task.
- When a task is completed, a branch is merged, or the working context changes clearly, archive the current root planning files into `docs/archive/planning/` and create a new set.
- Long-term reference: `docs/project-workflow.md`

## Goal

Continue `Harvest Manor` on `main`, focusing on manual smoke coverage and the first real scene/UI beautification pass now that milestone-1 work has been merged and the bootstrap orchestration has been split by responsibility.

## Current Phase

Phase 4

## Phases

### Phase 1: Planning Reset After Merge
- [x] Archive the stale root planning files that still referenced the deleted milestone worktree
- [x] Write a long-term workflow document under `docs/`
- [x] Recreate root planning files for the current `main` branch task
- **Status:** complete

### Phase 2: Verify The Mainline Experience
- [x] Run a manual smoke pass against the current playable slice
- [x] Record any UX, layout, or readability issues that only show up in interactive play
- [x] Decide the highest-value follow-up polish target from the smoke pass
- **Status:** complete

### Phase 3: First Beautification Pass
- [x] Reframe the main window so the composition uses a deliberate widescreen layout
- [x] Separate HUD, farm status, and request status into dedicated visual regions
- [x] Keep automated layout and behavior coverage aligned with the new presentation structure
- **Status:** complete

### Phase 4: Visual Review And Follow-Up
- [ ] Review the new live layout in a normal game window
- [ ] Record any remaining high-impact presentation gaps after the wider composition pass
- [ ] Decide whether the next step is another art/UI pass or a return to gameplay/system work
- **Status:** in_progress

## Key Questions

1. Which issues only show up in manual play and not in the automated suite?
2. Which visual improvements most increase clarity without introducing asset-pipeline overhead?
3. How far should the current polish pass go before starting the next gameplay/system milestone?
4. After the first beautification pass, which remaining visual rough edges still feel prototype-grade in live play?

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| `main` is now the active source of truth | Milestone-1 work has already been merged; the old feature branch/worktree no longer exists |
| Root planning files were reset instead of extended | The old files had become inaccurate and no longer described the current working context |
| The workflow rule is stored both here and in `docs/project-workflow.md` | New sessions should encounter the rule quickly, while `docs/` keeps the durable version |
| The current active task is framed as mainline smoke + presentation polish | That matches the repo state after merge and the most recent verified work on scene readability |
| First beautification work should prioritize layout composition over repair polish | The smoke feedback showed the real problem was framing, spacing, and information hierarchy rather than isolated missing details |
| The presentation target is now a 1280x720 widescreen composition with a top HUD, a left farm card, and a right town card | This uses the screen intentionally and separates the most important information zones before deeper art work starts |

## Errors Encountered

| Error | Attempt | Resolution |
|-------|---------|------------|
| Previous root planning files referenced a deleted worktree and branch | 1 | Archived the old files and regenerated a fresh root set for `main` |

## Notes

- Archived prior planning context to `docs/archive/planning/2026-04-10-milestone-1-foundation-context/`
- Latest verified commits before this planning reset: `778c94d feat: improve scene readability and ui atmosphere`, `5d2e95e refactor: split game bootstrap by responsibility`
- Current repo state should stay rooted in `D:\game project\harvest-manor` on branch `main`
- Current uncommitted work is the first beautification pass plus its new layout coverage
