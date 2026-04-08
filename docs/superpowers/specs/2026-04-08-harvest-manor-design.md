# Harvest Manor Design

**Date:** 2026-04-08  
**Project Root:** `D:\game project\harvest-manor`  
**Status:** Approved design baseline for project setup and first implementation plan

---

## 1. Project Summary

`Harvest Manor` is a Windows desktop single-player farm and manor management game. The project starts as a focused, high-quality farming foundation and is designed to grow over time into a richer manor simulation with stronger world atmosphere.

The first development phase does not aim to be a content-heavy clone of *Stardew Valley*. Instead, it aims to make the core farming loop satisfying, readable, and expandable:

`prepare land -> plant seeds -> water crops -> wait through days -> harvest -> sell -> earn money/resources -> expand the farm -> unlock basic facilities -> repeat with stronger efficiency`

The game should feel:

- calm and immersive
- lightly strategic, but not punishing
- grounded and natural rather than pixel-stylized
- clearly expandable into a long-term formal project

---

## 2. Product Pillars

### 2.1 Farming First

The farm loop is the center of the game. Every early design choice should strengthen crop planting, tending, harvesting, selling, and reinvestment.

### 2.2 Visible Growth

The player should quickly feel that effort changes the farm in visible ways:

- more land
- more storage
- more useful facilities
- higher efficiency
- a clearer sense of ownership over the manor

### 2.3 Light Pressure, Not Harsh Punishment

The game includes planning through time, stamina, inventory, and money, but it should not create constant frustration or severe setbacks.

### 2.4 Small but Dense World

The world should not feel empty, but it should also not sprawl too early. Early areas should be compact, functional, and easy to polish.

### 2.5 Long-Term Expandability

The first milestone should feel complete on its own while leaving clean paths for later additions such as seasons, animal systems, processing chains, more facilities, and richer town life.

---

## 3. Confirmed Design Decisions

- Platform: Windows desktop game
- Genre: farm / manor simulation and management
- Project intent: long-term formal project, not a throwaway prototype
- Core fantasy: build up a farm into a growing manor
- Priority loop: crop planting and harvesting
- Initial scope: medium-sized foundational version, but architected for future expansion
- Camera / presentation: 2D top-down
- Art direction: between warm stylization and grounded realism; realistic enough to feel tangible, simplified enough to stay readable and affordable
- Time model: days advance, with time passing during the day
- Control model: keyboard movement with mouse-assisted interaction
- Player role: early game is hands-on character action; future phases may add light automation and stronger management tools
- Tone: light planning pressure without harsh punishment
- Expansion priority after the farm loop: economy, storage, land growth, facilities, later processing and broader world life
- Multiplayer stance: build for single-player first, but avoid data structures that would completely block future multiplayer exploration
- Engine recommendation: Godot 4 with C#
- First milestone world structure: farm area, home/storage area, town/shop area, one light gathering area
- First milestone social/world scope: small number of NPCs, light dialogue, light requests, no romance, no combat, no heavy plot
- First milestone season scope: one playable season on the surface, but internal design should support future four-season expansion
- Stamina: present but forgiving
- Inventory: limited but fairly generous
- Growth fantasy: land expansion and facilities are the main visible progression drivers
- Building placement: hybrid approach; core buildings anchored, some facilities placeable
- Save model: autosave plus manual save
- Parent workspace: `D:\game project`
- Project root: `D:\game project\harvest-manor`

---

## 4. Recommended Strategic Direction

The chosen direction is:

`Start with a farming-first foundation, then naturally extend into manor growth, while adding a small amount of world atmosphere.`

This corresponds to:

- Route 1 as the starting point: core farm loop first
- Route 3 as second-phase expansion: visible manor growth
- A small amount of Route 2 flavor: enough town life and NPC presence to keep the world from feeling empty

This means the project should prioritize in this order:

1. make the farm loop deeply satisfying
2. make farm growth visible and motivating
3. make the surrounding world feel alive without overpowering the farm

---

## 5. First Formal Milestone

### 5.1 Goal

Deliver a polished first milestone that proves the project is real and worth expanding.

By the end of this milestone, the player should be able to:

- prepare and use farmland
- buy seeds
- plant, water, and harvest crops
- sell crops for money
- store items in inventory and storage
- experience day progression and crop growth over time
- unlock or use a small number of facilities
- expand the farm in a visible way
- interact with a small town/shop loop and a few NPCs
- save progress reliably

### 5.2 Success Criteria

This milestone is successful when:

- the core farm loop feels smooth and satisfying
- progression from profit to expansion is obvious and motivating
- the farm visually changes as the player progresses
- the world feels small but intentional rather than empty
- saving, date advancement, crop growth, inventory, and economy are reliable

### 5.3 Explicit Non-Goals for Milestone 1

Do not make these major pillars of the first milestone:

- romance systems
- combat
- deep storylines
- large open-world exploration
- complex relationship simulation
- multi-season content breadth
- animal husbandry as a major system
- deep automation
- multiplayer

These may become later expansions, but they must not dilute the initial build.

---

## 6. Core Gameplay Loop

The first milestone loop should be:

1. wake up / begin the day
2. inspect farm status
3. choose actions based on time, stamina, inventory, money, and crop state
4. plant or maintain crops
5. harvest mature crops
6. visit town/shop to buy or sell
7. store materials and prepare the next cycle
8. optionally perform small gathering or utility tasks
9. end the day
10. progress crop growth, save, and start the next day

The loop must create three feelings:

- daily intention: "what should I do today?"
- payoff: "my work produced visible and economic results"
- reinvestment: "I can now improve my farm"

---

## 7. Core Systems

### 7.1 Farmland and Grid Logic

The game should appear natural and not overly board-like, but the underlying rules should be tile or plot based.

Design intent:

- visual presentation can soften the appearance of a strict grid
- system rules should still use explicit plots for planting, watering, occupancy, and persistence
- future expansion for facilities, automation, and save reliability depends on deterministic plot rules

Each plot should be able to express at minimum:

- locked / unavailable
- natural ground
- prepared farmland
- planted crop reference
- watered state
- blocked / occupied

### 7.2 Crop System

Milestone 1 should include a small number of crops with clear differences.

Each crop definition should support at minimum:

- id
- display name
- allowed season(s)
- seed item reference
- sell value
- purchase cost
- total growth duration in days
- growth stage count and stage timing
- harvest output

The first milestone should favor clarity over content volume. Crop variety is less important than the player quickly understanding trade-offs.

### 7.3 Time and Day Progression

The game uses:

- daytime with advancing time
- daily reset / transition
- one playable season for milestone 1

Internal structures should be written so future expansion can support:

- multiple seasons
- season-based crop restrictions
- seasonal shop changes
- seasonal map or visual variation

### 7.4 Stamina

Stamina exists to shape the day, not to over-punish the player.

Milestone 1 behavior:

- common actions consume stamina
- the player can still make meaningful daily progress
- poor planning slows growth rather than causing major failure

This system should support future expansion such as:

- better tools
- player progression
- facility bonuses
- food or recovery items

### 7.5 Inventory and Storage

The player has a limited but forgiving inventory and a more persistent farm storage solution.

Design goals:

- inventory management should exist
- it should not become tedious micromanagement
- storage should feel like an important farm upgrade

Item categories should be designed for future growth:

- seeds
- harvested crops
- gathered materials
- tools
- facility- or construction-related items
- future processed goods

### 7.6 Economy and Shop Loop

The economic loop is essential because it turns crops into visible growth.

Town/shop responsibilities:

- sell seeds and baseline supplies
- buy crops and possibly simple materials
- provide light guidance through requests or progression cues

Economy design principles:

- first profits should come quickly enough to feel rewarding
- growth purchases should feel meaningful
- the economy should encourage reinvestment instead of passive hoarding

### 7.7 Expansion and Facilities

The strongest growth signals in milestone 1 are:

- land expansion
- unlocking or upgrading basic facilities

Examples of early facilities:

- main storage improvements
- utility work area
- early farming support structures
- simple processing extension points for future phases

Placement model:

- anchor core world structures in stable locations
- allow selected facilities or interactables to be placeable by the player

### 7.8 Light World/NPC Layer

The world should feel inhabited without shifting into a narrative-heavy life sim.

Milestone 1 should include:

- a small number of recognizable NPCs
- short, readable dialogue
- light requests or objectives
- enough identity in the town to avoid a sterile management sim feeling

Milestone 1 should avoid:

- complex branching stories
- romance
- elaborate schedules requiring large simulation overhead

### 7.9 Save and Progress Persistence

Save reliability is critical.

Milestone 1 save behavior:

- autosave at day end
- autosave at carefully chosen key progression moments
- manual save from a clear player-controlled point or menu

Critical state that must persist correctly:

- current day / season
- player money
- player inventory
- storage contents
- plot states
- planted crops and growth progress
- unlocked land
- facility states
- relevant lightweight quest/progression flags

---

## 8. World Structure and Map Layout

Milestone 1 should use compact, high-value zones rather than a sprawling world.

### 8.1 Farm Area

The main stage of the game.

Responsibilities:

- starting farmland
- locked future land
- core structures or build anchors
- space for future manor growth
- immediate visual proof of player progress

### 8.2 Home / Main Storage Area

Responsibilities:

- rest / day transition context
- primary storage access
- possible manual save access
- a quiet pacing anchor between work cycles

### 8.3 Town / Shop Area

Responsibilities:

- buying seeds
- selling crops
- meeting key NPCs
- receiving light requests or milestones
- reinforcing that the farm exists in a lived-in setting

### 8.4 Light Gathering Area

Responsibilities:

- collecting a small amount of materials such as wood or stone
- supporting early utility upgrades and future building hooks
- adding variety without competing with farming as the main activity

### 8.5 World Expansion Strategy

Future growth should happen through new zones rather than turning milestone 1 into an oversized map.

Later examples may include:

- greenhouse area
- processing workshop zone
- larger manor grounds
- richer town district
- special seasonal areas

---

## 9. Art Direction

The project should aim for:

`soft stylization with grounded texture and form`

This means:

- avoid pixel-art as the defining visual language
- avoid high-detail realism that would inflate cost and slow iteration
- use readable silhouettes and clean top-down readability
- keep crops, soil, paths, wood, and buildings tactile enough to feel physical
- favor warmth, natural materials, and calm environmental color choices

Milestone 1 art production approach:

- important spaces should already feel like the intended final direction
- core UI, main farm environment, essential crops, and key facilities should have cohesive visuals
- secondary content may use cohesive temporary assets when necessary

---

## 10. Technical Architecture

### 10.1 Engine and Language

Recommended stack:

- Godot 4
- C#

Reasoning:

- strong fit for a 2D top-down management game
- good iteration speed
- better long-term structure than an ad hoc script-heavy project
- suitable for clear data modeling across farm, crops, time, inventory, economy, and saves

### 10.2 Architectural Principles

- separate rules from presentation
- keep data structures explicit
- favor deterministic systems for world state
- keep content data-driven where possible
- keep files focused by responsibility

### 10.3 Key Layers

Suggested structural layers:

- core definitions and shared types
- gameplay systems
- world/scene behavior
- UI
- data/config content
- save/load and persistence

### 10.4 Future-Proofing for Possible Multiplayer

Do not design milestone 1 as a multiplayer game.

However:

- avoid burying authoritative state inside purely visual scene scripts
- keep player, item, crop, and plot data structured and serializable
- treat world state transitions as clear gameplay events

This does not guarantee future multiplayer, but it avoids unnecessary dead ends.

---

## 11. Project Directory Plan

Parent workspace:

- `D:\game project`

Project root:

- `D:\game project\harvest-manor`

Recommended structure:

```text
D:\game project\harvest-manor
+-- docs
|   +-- superpowers
|       +-- specs
+-- game
|   +-- assets
|   |   +-- art
|   |   +-- audio
|   |   \-- ui
|   +-- data
|   +-- scenes
|   \-- scripts
|       +-- core
|       +-- systems
|       +-- ui
|       \-- world
\-- tools
```

Intent of each area:

- `docs`: design docs, implementation plans, project notes
- `game`: Godot project root
- `game/assets`: art, UI, audio, and related resources
- `game/data`: crop definitions, items, shop data, progression data, future task data
- `game/scenes`: playable maps, UI scenes, interactable scenes
- `game/scripts/core`: shared types, constants, events, utility foundations
- `game/scripts/systems`: time, crop growth, economy, save/load, inventory, progression
- `game/scripts/world`: player interaction, farm plots, map objects, world logic
- `game/scripts/ui`: menus, HUD, inventory, shop, dialogue panels
- `tools`: optional support scripts for data prep or content workflows

---

## 12. Milestone 1 Validation Focus

When validating milestone 1, prioritize these questions:

### 12.1 Core Loop Quality

- Is planting fast and readable?
- Is watering satisfying and clear?
- Is crop growth easy to understand?
- Is harvesting rewarding?
- Does selling naturally lead to the next farming decision?

### 12.2 Growth Motivation

- Does the player quickly see why more money matters?
- Does expansion feel earned and visible?
- Do new facilities make the farm feel more capable?

### 12.3 Comfort and Flow

- Does the day structure feel good?
- Is stamina restrictive enough to matter but loose enough to stay fun?
- Is inventory management present without becoming annoying?

### 12.4 Reliability

- Are plot states stable after day changes?
- Are crops saved and restored correctly?
- Are money, inventory, and unlocks persistent and trustworthy?

### 12.5 World Presence

- Does town support the farm loop?
- Do the NPCs add warmth without slowing the game down?
- Does the world feel intentionally small rather than unfinished?

---

## 13. Risks and Guardrails

### 13.1 Primary Risks

- expanding scope too early into story, romance, combat, or broad life-sim systems
- overbuilding map size before content density exists
- adding too many crop rules before the base loop is proven
- letting art production block system validation
- mixing world state, visuals, and save logic too tightly

### 13.2 Guardrails

- every major addition must strengthen farming, growth, or world support for farming
- if a feature does not help milestone 1 quality, move it later
- prioritize system correctness over cosmetic polish when the two conflict
- keep first content set intentionally small
- validate the economic loop and land-growth motivation early

---

## 14. Expansion Roadmap

### Phase 2: Manor Growth

Primary focus:

- more land
- more facilities
- stronger progression
- early processing chains
- more deliberate farm layout planning

### Phase 3: Stronger World Life

Primary focus:

- richer town behavior
- more NPC variety
- more requests and ambient world identity
- broader sense of living in a small rural community

### Phase 4: Larger Simulation Layers

Potential later systems:

- additional seasons
- animal systems
- greenhouse
- deeper processing and production
- stronger automation
- broader economy
- more involved story threads
- future investigation of multiplayer feasibility

---

## 15. Final Design Summary

`Harvest Manor` should begin as a polished farming-first desktop game with a compact but intentional world. The first milestone should prove that the project is enjoyable at its core, visually cohesive at key touchpoints, technically stable, and clearly capable of growing into a much larger manor simulation.

The order of priorities is:

1. make farming feel good
2. make growth visible
3. make the world feel alive

If future choices conflict, this order should guide decisions.
