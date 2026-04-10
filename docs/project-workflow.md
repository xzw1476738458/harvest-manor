# Project Workflow

## Planning File Rules

This project uses two different kinds of documentation:

### Long-Term Documentation

Everything under `docs/` is long-lived project documentation.

Use `docs/` for:
- approved design specs
- milestone plans
- testing checklists
- workflow conventions
- archived planning files from completed phases

### Root Planning Files

The root-level planning files:
- `task_plan.md`
- `findings.md`
- `progress.md`

are **temporary working memory** for the **current active complex task only**.

Use the root planning files for:
- the current multi-step implementation task
- current discoveries and risks
- current session logs and verification notes

Do **not** use the root planning files as permanent project documentation.

## Lifecycle Rules

When a complex task is active:
- create or refresh the root `task_plan.md`, `findings.md`, and `progress.md`
- keep them aligned with the current branch and current working directory

When the task is completed, the branch is merged, or the working context changes clearly:
- archive the current root planning files into `docs/archive/planning/`
- create a fresh root set for the next active complex task

## Practical Rule of Thumb

- `docs/` holds project memory that should remain useful months later
- root planning files hold the working memory needed right now

If a root planning file starts referring to an old branch, deleted worktree, or finished phase, it should be archived rather than extended.
