# Harvest Manor

**简体中文** | [English](README.en.md)

[![Tests](https://github.com/xzw1476738458/harvest-manor/actions/workflows/test.yml/badge.svg)](https://github.com/xzw1476738458/harvest-manor/actions/workflows/test.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

一款使用 Godot 4.6 + C# 开发的 2D 田园经营 / 生活模拟游戏。

## 项目简介

玩家在 Harvest Manor 经营自己的农场：开垦土地、种植作物、灌溉收获、前往小镇商店买卖道具，去 *低语森林（Whispering Woods）* 采集木材与石头，并完成请求板上的订单。游戏采用 **日 / 季节循环系统**，体力与金币是核心资源。

> 项目目前处于里程碑 1 收尾阶段，详见 `task_plan.md` 与 `docs/`。

## 功能特性

- 完整的"开垦 → 播种 → 浇水 → 收获"农耕循环
- 日 / 季节推进，每日重置事件
- 商店买卖系统，季节性物品轮换
- 储物箱与背包面板（`Tab` 键打开）
- 多场景切换：农场 / 小镇 / 屋内 / 谷仓 / 商店 / 采集森林
- 资源采集系统（树木、石头每日重生）
- 请求板任务
- JSON 存档（`user://saves/slot-1.json`）
- 321 个 xUnit 单元测试覆盖核心逻辑

## 技术栈

- **引擎**：Godot 4.6.2（.NET / Mono）
- **语言**：C# / .NET 8
- **测试**：xUnit
- **数据**：JSON 配置 (`game/data/`)

## 快速开始

### 1. 准备 Godot 编辑器

下载 [Godot 4.6.2 mono win64](https://godotengine.org/download/windows/) 便携版，解压后放入项目目录：

```
harvest-manor/
└── tools/
    └── godot/
        └── Godot_v4.6.2-stable_mono_win64/
            └── Godot_v4.6.2-stable_mono_win64_console.exe
```

> `tools/godot/` 已加入 `.gitignore`，不会被推送到仓库。

### 2. 安装 .NET SDK

需要 [.NET 8 SDK](https://dotnet.microsoft.com/download)。

### 3. 启动游戏

双击根目录的 `start-game.cmd`，或在 PowerShell 执行：

```powershell
.\start-game.cmd
```

脚本会按以下优先级查找 Godot：

1. 环境变量 `GODOT_CONSOLE_EXE`
2. `tools/godot/` 项目内便携版（自动递归查找 `*_console.exe`）
3. 系统默认路径 `C:\Program Files (x86)\Godot_v4.6.2-stable_mono_win64\`

## 操作说明

| 按键 | 功能 |
|------|------|
| `W` / `A` / `S` / `D` 或方向键 | 移动 |
| `E` / `Enter` | 交互（开垦、收获、传送门、对话）|
| `Tab` | 打开 / 关闭背包 |
| `F7` | 解锁一块新农田（消耗金币）|
| `Esc` | 关闭面板 |
| 鼠标左键 | 与可点击对象交互（农田、资源点、商店等）|

## 项目结构

```
harvest-manor/
├── game/                       # Godot 项目根
│   ├── data/                   # JSON 配置（作物、道具、商店、请求）
│   ├── scenes/                 # Godot 场景（.tscn）
│   ├── scripts/
│   │   ├── core/               # 业务逻辑（Farming/Economy/Time/Saves/Gathering...）
│   │   ├── ui/                 # 界面节点
│   │   └── world/              # 世界节点（玩家、农田、资源点、传送门）
│   ├── themes/                 # UI 主题
│   ├── HarvestManor.csproj
│   └── project.godot
├── tests/                      # xUnit 测试工程
├── docs/                       # 长期文档（设计规格、工作流、测试清单）
├── tools/godot/                # 便携版 Godot（gitignored）
├── start-game.cmd              # 启动脚本
├── task_plan.md                # 当前任务计划
├── findings.md                 # 当前任务发现
└── progress.md                 # 当前任务进度
```

## 运行测试

```powershell
dotnet test tests/HarvestManor.Game.Tests/HarvestManor.Game.Tests.csproj
```

或单独构建主项目：

```powershell
dotnet build game/HarvestManor.csproj
```

## 开发流程

详见 `docs/project-workflow.md`。简言之：

- `docs/` 存放长期文档（规格、工作流、测试清单、归档计划）
- 根目录的 `task_plan.md` / `findings.md` / `progress.md` 是**当前任务**的临时工作笔记，任务完成后归档至 `docs/archive/planning/`

## 路线图

- [x] 里程碑 1：基础农耕循环 + 小镇 + 采集区
- [ ] 里程碑 2：建筑 / 工坊系统、加工链
- [ ] 里程碑 3：NPC 与人际关系
- [ ] 里程碑 4：扩展场景与季节性内容

## 许可证

本项目采用 [MIT 协议](LICENSE) 开源 — 你可以自由使用、修改、再分发，包括商用，唯一要求是保留版权声明。
