# Harvest Manor

[简体中文](README.md) | **English**

[![Tests](https://github.com/xzw1476738458/harvest-manor/actions/workflows/test.yml/badge.svg)](https://github.com/xzw1476738458/harvest-manor/actions/workflows/test.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A 2D farming / life-simulation game built with Godot 4.6 + C#.

## Overview

In Harvest Manor you run your own farmstead: till the soil, plant and water crops, harvest them, trade goods at the town shop, gather wood and stone in the *Whispering Woods*, and complete orders from the request board. A **day / season loop** drives gameplay, with stamina and gold as core resources.

> The project is wrapping up Milestone 1. See `task_plan.md` and `docs/` for details.

## Features

- Full "till → plant → water → harvest" farming loop
- Day / season progression with end-of-day reset events
- Shop with season-aware buy/sell offers
- Storage chest and inventory panel (toggle with `Tab`)
- Multiple scenes: farm / town / cottage / barn / shop / gathering woods
- Resource gathering (trees and rocks respawn daily)
- Request board quests
- JSON save files (`user://saves/slot-1.json`)
- 321 xUnit tests covering the core systems

## Tech Stack

- **Engine**: Godot 4.6.2 (.NET / Mono)
- **Language**: C# / .NET 8
- **Testing**: xUnit
- **Data**: JSON config (`game/data/`)

## Quick Start

### 1. Get the Godot editor

Download the [Godot 4.6.2 mono win64](https://godotengine.org/download/windows/) portable build and extract it into the project:

```
harvest-manor/
└── tools/
    └── godot/
        └── Godot_v4.6.2-stable_mono_win64/
            └── Godot_v4.6.2-stable_mono_win64_console.exe
```

> `tools/godot/` is gitignored and will not be pushed to the repo.

### 2. Install the .NET SDK

You need [.NET 8 SDK](https://dotnet.microsoft.com/download).

### 3. Launch the game

Double-click `start-game.cmd` in the repo root, or from PowerShell:

```powershell
.\start-game.cmd
```

The script resolves Godot in this order:

1. `GODOT_CONSOLE_EXE` environment variable
2. Portable build under `tools/godot/` (recursive search for `*_console.exe`)
3. Default install path `C:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\`

## Controls

| Key | Action |
|-----|--------|
| `W` / `A` / `S` / `D` or arrow keys | Move |
| `E` / `Enter` | Interact (till, harvest, gates, dialogue) |
| `Tab` | Toggle inventory |
| `F7` | Unlock a farm plot (spends gold) |
| `Esc` | Close panels |
| Left mouse | Click interactable objects (plots, resource nodes, shop, etc.) |

## Project Layout

```
harvest-manor/
├── game/                       # Godot project root
│   ├── data/                   # JSON config (crops, items, shops, requests)
│   ├── scenes/                 # Godot scenes (.tscn)
│   ├── scripts/
│   │   ├── core/               # Game logic (Farming/Economy/Time/Saves/Gathering...)
│   │   ├── ui/                 # UI nodes
│   │   └── world/              # World nodes (player, plots, resource nodes, gates)
│   ├── themes/                 # UI themes
│   ├── HarvestManor.csproj
│   └── project.godot
├── tests/                      # xUnit test project
├── docs/                       # Long-term docs (specs, workflow, checklists)
├── tools/godot/                # Portable Godot (gitignored)
├── start-game.cmd              # Launch script
├── task_plan.md                # Current task plan
├── findings.md                 # Current task findings
└── progress.md                 # Current task progress
```

## Running Tests

```powershell
dotnet test tests/HarvestManor.Game.Tests/HarvestManor.Game.Tests.csproj
```

Or build only the main project:

```powershell
dotnet build game/HarvestManor.csproj
```

## Workflow

See `docs/project-workflow.md`. In short:

- `docs/` holds long-term documentation (specs, workflow, checklists, archived plans).
- Root-level `task_plan.md` / `findings.md` / `progress.md` are **working memory for the current task**; they get archived to `docs/archive/planning/` when the task is done.

## Roadmap

- [x] Milestone 1: core farming loop + town + gathering area
- [ ] Milestone 2: buildings / workshops, processing chains
- [ ] Milestone 3: NPCs and relationships
- [ ] Milestone 4: extended scenes and seasonal content

## License

Released under the [MIT License](LICENSE) — free to use, modify and redistribute, including commercially, as long as the copyright notice is preserved.
