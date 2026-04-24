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
- Manual smoke confirms the current scene is still only in a lightweight readability-polish state, not a real art/UI pass
- The current window composition is heavily top-left clustered and leaves a large unused gray area in the viewport
- HUD, farm status text, and town/request text are visually colliding and competing for attention
- Farm plots, town services, and background layers are still obviously placeholder blocks rather than a cohesive visual language
- The next meaningful presentation step should prioritize layout composition, spacing, hierarchy, and scene framing before investing in finer art details
- A stronger first-pass composition can stay asset-light by treating the screen as three regions: a full-width HUD bar, a left farm card, and a right town/service card
- A fixed 1280x720 presentation target gives the scene enough room to breathe while keeping layout changes testable in `project.godot`
- The farm and town status text should live inside dedicated panels instead of floating directly in the scene
- The inventory, shop, and storage panels read better as a coordinated lower dock than as unrelated floating boxes

## Technical Decisions

| Decision | Rationale |
|----------|-----------|
| Archive stale planning files instead of editing them in place | Their branch/worktree assumptions were already false, so preserving them as historical context is safer than rewriting history |
| Add `docs/project-workflow.md` | The rule about `docs/` vs root planning files needs a durable home |
| Keep a short copy of the planning-file rules at the top of root `task_plan.md` | New sessions are most likely to read this file first when planning-with-files is in use |
| Treat the latest smoke screenshots as a signal to shift from readability tweaks to real layout/visual composition work | The current scene passes functional smoke but still fails the intended presentation bar |
| Target the next visual pass at layout framing first, not bespoke art assets | There is still no external asset pipeline, so the biggest gain comes from composition and panel hierarchy |
| Make the HUD a single top banner and dock panel UI along the lower band | This prevents farm, request, and resource text from competing in the same screen area |
| Put farm interactions on the left and town services on the right | The split gives each activity its own readable region without introducing scene swapping |

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
- Current beautification pass is uncommitted and has fresh verification on `main`
