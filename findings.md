# Findings & Decisions

## Requirements

- Work from `D:\game project\harvest-manor`
- Treat `main` as the active branch
- Keep long-term documentation under `docs/`
- Use root `task_plan.md`, `findings.md`, and `progress.md` only for the current active complex task
- Archive root planning files when the task completes, the branch merges, or the working context changes clearly

## Research Findings

- The old root planning files had become inaccurate because they still referred to `.worktrees/codex-milestone-1-foundation` and `codex/milestone-1-foundation`
- The old milestone branch has already been merged into `main`
- The old milestone worktree has already been removed
- The repo is currently clean on `main`
- The current playable slice already has passing automated coverage plus a recent readability pass on farm, town, and UI scenes
- The project still has no real `game/assets` content, so current visual polish is being driven by scene composition and code-defined presentation rather than an external asset pipeline

## Technical Decisions

| Decision | Rationale |
|----------|-----------|
| Archive stale planning files instead of editing them in place | Their branch/worktree assumptions were already false, so preserving them as historical context is safer than rewriting history |
| Add `docs/project-workflow.md` | The rule about `docs/` vs root planning files needs a durable home |
| Keep a short copy of the planning-file rules at the top of root `task_plan.md` | New sessions are most likely to read this file first when planning-with-files is in use |

## Resources

- Long-term workflow rules: `D:\game project\harvest-manor\docs\project-workflow.md`
- Archived milestone-1 planning snapshot: `D:\game project\harvest-manor\docs\archive\planning\2026-04-10-milestone-1-foundation-context\`
- Design spec: `D:\game project\harvest-manor\docs\superpowers\specs\2026-04-08-harvest-manor-design.md`
- Milestone plan: `D:\game project\harvest-manor\docs\superpowers\plans\2026-04-08-harvest-manor-milestone-1-foundation.md`
- Smoke checklist: `D:\game project\harvest-manor\docs\testing\milestone-1-smoke-checklist.md`
- Current orchestration entry: `D:\game project\harvest-manor\game\scripts\world\GameBootstrap.cs`

## Current Context

- Current branch: `main`
- Current task: mainline presentation and smoke pass
- Latest verified visual polish commit: `778c94d feat: improve scene readability and ui atmosphere`
- Latest verified structure commit: `5d2e95e refactor: split game bootstrap by responsibility`
