# Harvest Manor Milestone 1 Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first playable `Harvest Manor` vertical slice: one-season crop farming, day progression, stamina, inventory/storage, shop economy, basic farm expansion hooks, save/load, and a small amount of town-world support.

**Architecture:** Keep the deterministic game rules inside plain C# classes under `game/scripts/core` and `game/scripts/systems`, then let Godot scenes and node scripts act as a thin presentation and interaction layer. Store game content in JSON under `game/data`, keep saves as serializable snapshots, and use xUnit to validate the rule layer before wiring each feature into Godot scenes.

**Tech Stack:** Godot 4.6 .NET editor, C#, .NET 8 SDK, xUnit, System.Text.Json, PowerShell, Git

---

## Scope Guard

This plan only covers the first formal playable milestone described in the approved spec at `docs/superpowers/specs/2026-04-08-harvest-manor-design.md`.

This plan deliberately excludes:

- romance
- combat
- animal systems
- multi-season content breadth
- deep automation
- heavy story systems
- multiplayer

---

## Preflight

Run these setup steps before starting Task 1.

### Tooling Install

Run:

```powershell
winget install --id Microsoft.DotNet.SDK.8 -e --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity
winget install --id GodotEngine.GodotEngine.Mono -e --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity
```

Expected:

- `dotnet --info` shows an `8.0.x` SDK in the installed SDK list
- the .NET-capable Godot editor is installed

Run:

```powershell
$godot = Get-ChildItem "$env:LOCALAPPDATA\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_*" -Recurse -Filter "Godot*.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

[Environment]::SetEnvironmentVariable("GODOT4", $godot, "User")
$env:GODOT4 = $godot

dotnet --info
Write-Host $env:GODOT4
```

Expected:

- `dotnet --info` includes `.NET SDK 8.0`
- `Write-Host $env:GODOT4` prints a full path to the Godot .NET executable

### Project Bootstrap

Run:

```powershell
New-Item -ItemType Directory -Force -Path `
  "D:\game project\harvest-manor\game", `
  "D:\game project\harvest-manor\game\assets\art", `
  "D:\game project\harvest-manor\game\assets\audio", `
  "D:\game project\harvest-manor\game\assets\ui", `
  "D:\game project\harvest-manor\game\data\crops", `
  "D:\game project\harvest-manor\game\data\items", `
  "D:\game project\harvest-manor\game\data\shops", `
  "D:\game project\harvest-manor\game\data\requests", `
  "D:\game project\harvest-manor\game\scenes\ui", `
  "D:\game project\harvest-manor\game\scenes\world", `
  "D:\game project\harvest-manor\game\scripts\core\Content", `
  "D:\game project\harvest-manor\game\scripts\core\Economy", `
  "D:\game project\harvest-manor\game\scripts\core\Farming", `
  "D:\game project\harvest-manor\game\scripts\core\Inventory", `
  "D:\game project\harvest-manor\game\scripts\core\Progression", `
  "D:\game project\harvest-manor\game\scripts\core\Saves", `
  "D:\game project\harvest-manor\game\scripts\core\Time", `
  "D:\game project\harvest-manor\game\scripts\systems", `
  "D:\game project\harvest-manor\game\scripts\ui", `
  "D:\game project\harvest-manor\game\scripts\world", `
  "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\Content", `
  "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\Economy", `
  "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\Farming", `
  "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\Progression", `
  "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\Saves", `
  "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\Time" | Out-Null
```

Create `game/project.godot` with this content:

```ini
; Engine configuration file.
; It's best edited using the editor and not directly.
config_version=5

[application]
config/name="Harvest Manor"
run/main_scene="res://scenes/Main.tscn"
config/features=PackedStringArray("4.6", "C#", "Forward Plus")
```

Run:

```powershell
& $env:GODOT4 --editor --path "D:\game project\harvest-manor\game"
```

In the editor:

1. Accept the project import.
2. Create an empty `Node2D` scene and save it as `res://scenes/Main.tscn`.
3. Attach a C# script named `GameBootstrap.cs` at `res://scripts/world/GameBootstrap.cs`.
4. Save and trigger the build once from the Godot editor so it generates `HarvestManor.sln` and `HarvestManor.csproj`.

The Godot docs note that the first C# script generation creates the `.sln` and `.csproj`, and that Godot's .NET edition uses .NET 8+. Sources: [Godot C#/.NET docs](https://docs.godotengine.org/en/4.5/tutorials/scripting/c_sharp/), [Godot scripting languages docs](https://docs.godotengine.org/en/4.6/getting_started/step_by_step/scripting_languages.html).

---

## File Structure Map

### Root

- Create: `.gitignore`
- Create: `game/project.godot`
- Generated: `game/HarvestManor.csproj`
- Generated: `game/HarvestManor.sln`

### Content/Data

- Create: `game/data/crops/spring.json`
- Create: `game/data/items/items.json`
- Create: `game/data/shops/general-store.json`
- Create: `game/data/requests/request-board.json`

### Core Rule Layer

- Create: `game/scripts/core/Content/GrowthStageDefinition.cs`
- Create: `game/scripts/core/Content/CropDefinition.cs`
- Create: `game/scripts/core/Content/ItemDefinition.cs`
- Create: `game/scripts/core/Content/ContentCatalogLoader.cs`
- Create: `game/scripts/core/Time/Season.cs`
- Create: `game/scripts/core/Time/GameDate.cs`
- Create: `game/scripts/core/Time/DayClock.cs`
- Create: `game/scripts/core/Time/StaminaState.cs`
- Create: `game/scripts/core/Inventory/ItemStack.cs`
- Create: `game/scripts/core/Inventory/InventoryState.cs`
- Create: `game/scripts/core/Economy/Wallet.cs`
- Create: `game/scripts/core/Economy/ShopOffer.cs`
- Create: `game/scripts/core/Economy/ShopService.cs`
- Create: `game/scripts/core/Farming/CropInstance.cs`
- Create: `game/scripts/core/Farming/PlotState.cs`
- Create: `game/scripts/core/Farming/FarmGrid.cs`
- Create: `game/scripts/core/Farming/CropGrowthService.cs`
- Create: `game/scripts/core/Progression/UnlockState.cs`
- Create: `game/scripts/core/Progression/FarmExpansionService.cs`
- Create: `game/scripts/core/Progression/RequestDefinition.cs`
- Create: `game/scripts/core/Progression/RequestBoardService.cs`
- Create: `game/scripts/core/Saves/PlotSnapshot.cs`
- Create: `game/scripts/core/Saves/SaveGameSnapshot.cs`
- Create: `game/scripts/core/Saves/SaveGameStore.cs`

### Godot Integration Layer

- Create: `game/scripts/world/GameBootstrap.cs`
- Create: `game/scripts/world/PlayerController.cs`
- Create: `game/scripts/world/FarmPlotNode.cs`
- Create: `game/scripts/world/BedInteraction.cs`
- Create: `game/scripts/world/StorageInteraction.cs`
- Create: `game/scripts/world/ShopInteraction.cs`
- Create: `game/scripts/world/RequestBoardInteraction.cs`
- Create: `game/scripts/ui/HudController.cs`
- Create: `game/scripts/ui/InventoryPanelController.cs`
- Create: `game/scripts/ui/ShopPanelController.cs`
- Create: `game/scripts/ui/StoragePanelController.cs`

### Scenes

- Create: `game/scenes/Main.tscn`
- Create: `game/scenes/world/FarmScene.tscn`
- Create: `game/scenes/world/TownScene.tscn`
- Create: `game/scenes/ui/Hud.tscn`
- Create: `game/scenes/ui/InventoryPanel.tscn`
- Create: `game/scenes/ui/ShopPanel.tscn`
- Create: `game/scenes/ui/StoragePanel.tscn`

### Tests

- Create: `tests/HarvestManor.Game.Tests/HarvestManor.Game.Tests.csproj`
- Create: `tests/HarvestManor.Game.Tests/Content/ContentCatalogLoaderTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Time/DayClockTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Time/StaminaStateTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Economy/InventoryStateTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Economy/ShopServiceTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Farming/CropGrowthServiceTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Saves/SaveGameStoreTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Progression/FarmExpansionServiceTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Progression/RequestBoardServiceTests.cs`

---

### Task 1: Bootstrap the Content Layer and Test Harness

**Files:**
- Create: `.gitignore`
- Modify: `game/project.godot`
- Create: `game/data/crops/spring.json`
- Create: `game/data/items/items.json`
- Create: `game/scripts/core/Content/GrowthStageDefinition.cs`
- Create: `game/scripts/core/Content/CropDefinition.cs`
- Create: `game/scripts/core/Content/ItemDefinition.cs`
- Create: `game/scripts/core/Content/ContentCatalogLoader.cs`
- Modify: `game/scripts/world/GameBootstrap.cs`
- Create: `tests/HarvestManor.Game.Tests/HarvestManor.Game.Tests.csproj`
- Create: `tests/HarvestManor.Game.Tests/Content/ContentCatalogLoaderTests.cs`

- [ ] **Step 1: Write the failing content-loading tests**

```csharp
using HarvestManor.Core.Content;
using Xunit;

namespace HarvestManor.Game.Tests.Content;

public sealed class ContentCatalogLoaderTests
{
    [Fact]
    public void LoadCropCatalog_ReturnsExpectedCropById()
    {
        var loader = new ContentCatalogLoader();
        var cropPath = Path.Combine(AppContext.BaseDirectory, "game-data", "crops", "spring.json");

        var crops = loader.LoadCropCatalog(cropPath);

        var parsnip = Assert.Single(crops, crop => crop.Id == "parsnip");
        Assert.Equal("Parsnip", parsnip.DisplayName);
        Assert.Equal(4, parsnip.TotalGrowthDays);
        Assert.Equal("parsnip_seed", parsnip.SeedItemId);
        Assert.Equal("parsnip_crop", parsnip.HarvestItemId);
    }

    [Fact]
    public void LoadCropCatalog_ThrowsWhenGrowthStagesDoNotMatchTotalDays()
    {
        var loader = new ContentCatalogLoader();
        var invalidPath = Path.GetTempFileName();

        File.WriteAllText(
            invalidPath,
            """
            [
              {
                "id": "bad_turnip",
                "displayName": "Bad Turnip",
                "season": "Spring",
                "seedItemId": "bad_turnip_seed",
                "harvestItemId": "bad_turnip_crop",
                "purchasePrice": 10,
                "sellPrice": 18,
                "totalGrowthDays": 5,
                "growthStageDays": [2, 2]
              }
            ]
            """
        );

        var exception = Assert.Throws<InvalidDataException>(() => loader.LoadCropCatalog(invalidPath));
        Assert.Contains("bad_turnip", exception.Message);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~ContentCatalogLoaderTests
```

Expected:

- FAIL with missing type or missing project reference errors because the content classes do not exist yet

- [ ] **Step 3: Add git ignore rules and the xUnit project**

`.gitignore`

```gitignore
game/.godot/
game/.mono/
game/bin/
game/obj/
tests/**/bin/
tests/**/obj/
.vs/
.vscode/
```

`tests/HarvestManor.Game.Tests/HarvestManor.Game.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\game\HarvestManor.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <None Include="..\..\game\data\**\*.json">
      <Link>game-data\%(RecursiveDir)%(Filename)%(Extension)</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Implement the content records and loader**

`game/scripts/core/Content/GrowthStageDefinition.cs`

```csharp
namespace HarvestManor.Core.Content;

public sealed record GrowthStageDefinition(int DaysRequired);
```

`game/scripts/core/Content/CropDefinition.cs`

```csharp
using System.Linq;

namespace HarvestManor.Core.Content;

public sealed record CropDefinition(
    string Id,
    string DisplayName,
    string Season,
    string SeedItemId,
    string HarvestItemId,
    int PurchasePrice,
    int SellPrice,
    int TotalGrowthDays,
    IReadOnlyList<int> GrowthStageDays)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            throw new InvalidDataException("Crop id cannot be empty.");
        }

        if (GrowthStageDays.Count == 0 || GrowthStageDays.Sum() != TotalGrowthDays)
        {
            throw new InvalidDataException($"Crop '{Id}' has invalid growth stage totals.");
        }
    }
}
```

`game/scripts/core/Content/ItemDefinition.cs`

```csharp
namespace HarvestManor.Core.Content;

public sealed record ItemDefinition(
    string Id,
    string DisplayName,
    string Category,
    int MaxStack);
```

`game/scripts/core/Content/ContentCatalogLoader.cs`

```csharp
using System.Text.Json;

namespace HarvestManor.Core.Content;

public sealed class ContentCatalogLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<CropDefinition> LoadCropCatalog(string path)
    {
        using var stream = File.OpenRead(path);
        var crops = JsonSerializer.Deserialize<List<CropDefinition>>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Crop catalog '{path}' was empty.");

        foreach (var crop in crops)
        {
            crop.Validate();
        }

        return crops;
    }

    public IReadOnlyList<ItemDefinition> LoadItemCatalog(string path)
    {
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<List<ItemDefinition>>(stream, JsonOptions)
            ?? throw new InvalidDataException($"Item catalog '{path}' was empty.");
    }
}
```

`game/data/crops/spring.json`

```json
[
  {
    "id": "parsnip",
    "displayName": "Parsnip",
    "season": "Spring",
    "seedItemId": "parsnip_seed",
    "harvestItemId": "parsnip_crop",
    "purchasePrice": 20,
    "sellPrice": 35,
    "totalGrowthDays": 4,
    "growthStageDays": [1, 1, 2]
  },
  {
    "id": "potato",
    "displayName": "Potato",
    "season": "Spring",
    "seedItemId": "potato_seed",
    "harvestItemId": "potato_crop",
    "purchasePrice": 45,
    "sellPrice": 80,
    "totalGrowthDays": 6,
    "growthStageDays": [2, 2, 2]
  }
]
```

`game/data/items/items.json`

```json
[
  { "id": "parsnip_seed", "displayName": "Parsnip Seeds", "category": "Seed", "maxStack": 99 },
  { "id": "parsnip_crop", "displayName": "Parsnip", "category": "Crop", "maxStack": 99 },
  { "id": "potato_seed", "displayName": "Potato Seeds", "category": "Seed", "maxStack": 99 },
  { "id": "potato_crop", "displayName": "Potato", "category": "Crop", "maxStack": 99 },
  { "id": "wood", "displayName": "Wood", "category": "Material", "maxStack": 99 },
  { "id": "stone", "displayName": "Stone", "category": "Material", "maxStack": 99 }
]
```

- [ ] **Step 5: Wire the loader into the bootstrap script**

`game/scripts/world/GameBootstrap.cs`

```csharp
using Godot;
using HarvestManor.Core.Content;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    public override void _Ready()
    {
        var loader = new ContentCatalogLoader();
        var crops = loader.LoadCropCatalog(ProjectSettings.GlobalizePath("res://data/crops/spring.json"));
        var items = loader.LoadItemCatalog(ProjectSettings.GlobalizePath("res://data/items/items.json"));

        GD.Print($"Loaded {crops.Count} crops and {items.Count} items.");
    }
}
```

- [ ] **Step 6: Run the tests and build**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~ContentCatalogLoaderTests
dotnet build "D:\game project\harvest-manor\game\HarvestManor.csproj"
```

Expected:

- PASS for `ContentCatalogLoaderTests`
- BUILD SUCCEEDED for `HarvestManor.csproj`

- [ ] **Step 7: Commit**

```bash
git add .gitignore game/project.godot game/data/crops/spring.json game/data/items/items.json game/scripts/core/Content game/scripts/world/GameBootstrap.cs tests/HarvestManor.Game.Tests
git commit -m "feat: bootstrap content loading and test harness"
```

### Task 2: Add Time, Day Progression, and Stamina Rules

**Files:**
- Create: `game/scripts/core/Time/Season.cs`
- Create: `game/scripts/core/Time/GameDate.cs`
- Create: `game/scripts/core/Time/DayClock.cs`
- Create: `game/scripts/core/Time/StaminaState.cs`
- Create: `tests/HarvestManor.Game.Tests/Time/DayClockTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Time/StaminaStateTests.cs`
- Modify: `game/scripts/world/GameBootstrap.cs`

- [ ] **Step 1: Write the failing time and stamina tests**

`tests/HarvestManor.Game.Tests/Time/DayClockTests.cs`

```csharp
using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Time;

public sealed class DayClockTests
{
    [Fact]
    public void AdvanceMinutes_RollsToNextDayAfterDayEnd()
    {
        var clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);

        var rolled = clock.AdvanceMinutes(20 * 60);

        Assert.True(rolled);
        Assert.Equal(new GameDate(Season.Spring, 2), clock.Date);
        Assert.Equal(6 * 60, clock.CurrentMinuteOfDay);
    }
}
```

`tests/HarvestManor.Game.Tests/Time/StaminaStateTests.cs`

```csharp
using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Time;

public sealed class StaminaStateTests
{
    [Fact]
    public void TrySpend_ReturnsFalseWhenCostExceedsCurrentStamina()
    {
        var stamina = new StaminaState(maximum: 100, current: 10);

        var spent = stamina.TrySpend(12);

        Assert.False(spent);
        Assert.Equal(10, stamina.Current);
    }

    [Fact]
    public void RestoreFull_RefillsToMaximum()
    {
        var stamina = new StaminaState(maximum: 100, current: 25);

        stamina.RestoreFull();

        Assert.Equal(100, stamina.Current);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~Time
```

Expected:

- FAIL because the time and stamina classes do not exist

- [ ] **Step 3: Implement season, date, clock, and stamina**

`game/scripts/core/Time/Season.cs`

```csharp
namespace HarvestManor.Core.Time;

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}
```

`game/scripts/core/Time/GameDate.cs`

```csharp
namespace HarvestManor.Core.Time;

public readonly record struct GameDate(Season Season, int Day)
{
    public GameDate NextDay(int daysPerSeason = 28)
    {
        if (Day < daysPerSeason)
        {
            return this with { Day = Day + 1 };
        }

        var nextSeason = Season switch
        {
            Season.Spring => Season.Summer,
            Season.Summer => Season.Autumn,
            Season.Autumn => Season.Winter,
            _ => Season.Spring
        };

        return new GameDate(nextSeason, 1);
    }
}
```

`game/scripts/core/Time/DayClock.cs`

```csharp
namespace HarvestManor.Core.Time;

public sealed class DayClock
{
    private readonly int _dayStartMinute;
    private readonly int _dayEndMinute;

    public DayClock(GameDate date, int dayStartMinute, int dayEndMinute)
    {
        Date = date;
        CurrentMinuteOfDay = dayStartMinute;
        _dayStartMinute = dayStartMinute;
        _dayEndMinute = dayEndMinute;
    }

    public GameDate Date { get; private set; }

    public int CurrentMinuteOfDay { get; private set; }

    public bool AdvanceMinutes(int minutes)
    {
        CurrentMinuteOfDay += minutes;

        if (CurrentMinuteOfDay < _dayEndMinute)
        {
            return false;
        }

        Date = Date.NextDay();
        CurrentMinuteOfDay = _dayStartMinute;
        return true;
    }
}
```

`game/scripts/core/Time/StaminaState.cs`

```csharp
namespace HarvestManor.Core.Time;

public sealed class StaminaState
{
    public StaminaState(int maximum, int current)
    {
        Maximum = maximum;
        Current = current;
    }

    public int Maximum { get; }

    public int Current { get; private set; }

    public bool TrySpend(int amount)
    {
        if (amount > Current)
        {
            return false;
        }

        Current -= amount;
        return true;
    }

    public void RestoreFull()
    {
        Current = Maximum;
    }
}
```

- [ ] **Step 4: Surface the day state in the bootstrap**

`game/scripts/world/GameBootstrap.cs`

```csharp
using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Time;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    private DayClock? _clock;
    private StaminaState? _stamina;

    public override void _Ready()
    {
        var loader = new ContentCatalogLoader();
        _ = loader.LoadCropCatalog(ProjectSettings.GlobalizePath("res://data/crops/spring.json"));
        _ = loader.LoadItemCatalog(ProjectSettings.GlobalizePath("res://data/items/items.json"));

        _clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        _stamina = new StaminaState(100, 100);

        GD.Print($"Day {_clock.Date.Day} of {_clock.Date.Season}, stamina {_stamina.Current}/{_stamina.Maximum}");
    }
}
```

- [ ] **Step 5: Run the tests and build**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~Time
dotnet build "D:\game project\harvest-manor\game\HarvestManor.csproj"
```

Expected:

- PASS for the time and stamina tests
- BUILD SUCCEEDED

- [ ] **Step 6: Commit**

```bash
git add game/scripts/core/Time tests/HarvestManor.Game.Tests/Time game/scripts/world/GameBootstrap.cs
git commit -m "feat: add day progression and stamina state"
```

### Task 3: Add Inventory, Storage, Wallet, and Shop Rules

**Files:**
- Create: `game/scripts/core/Inventory/ItemStack.cs`
- Create: `game/scripts/core/Inventory/InventoryState.cs`
- Create: `game/scripts/core/Economy/Wallet.cs`
- Create: `game/scripts/core/Economy/ShopOffer.cs`
- Create: `game/scripts/core/Economy/ShopService.cs`
- Create: `game/data/shops/general-store.json`
- Create: `tests/HarvestManor.Game.Tests/Economy/InventoryStateTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Economy/ShopServiceTests.cs`

- [ ] **Step 1: Write the failing inventory and shop tests**

`tests/HarvestManor.Game.Tests/Economy/InventoryStateTests.cs`

```csharp
using HarvestManor.Core.Inventory;
using Xunit;

namespace HarvestManor.Game.Tests.Economy;

public sealed class InventoryStateTests
{
    [Fact]
    public void TryAdd_StacksIntoExistingSlotBeforeUsingNewSlot()
    {
        var inventory = new InventoryState(slotCapacity: 4, maxStackSize: 99);

        Assert.True(inventory.TryAdd("parsnip_seed", 10));
        Assert.True(inventory.TryAdd("parsnip_seed", 5));

        var stack = Assert.Single(inventory.Slots);
        Assert.Equal("parsnip_seed", stack.ItemId);
        Assert.Equal(15, stack.Quantity);
    }
}
```

`tests/HarvestManor.Game.Tests/Economy/ShopServiceTests.cs`

```csharp
using HarvestManor.Core.Economy;
using HarvestManor.Core.Inventory;
using Xunit;

namespace HarvestManor.Game.Tests.Economy;

public sealed class ShopServiceTests
{
    [Fact]
    public void TryPurchase_RemovesGoldAndAddsItems()
    {
        var inventory = new InventoryState(slotCapacity: 10, maxStackSize: 99);
        var wallet = new Wallet(200);
        var shop = new ShopService();

        var success = shop.TryPurchase(
            inventory,
            wallet,
            new ShopOffer("parsnip_seed", buyPrice: 20, sellPrice: 10),
            3
        );

        Assert.True(success);
        Assert.Equal(140, wallet.Gold);
        Assert.Equal(3, inventory.GetQuantity("parsnip_seed"));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~Economy
```

Expected:

- FAIL because the inventory and shop types do not exist yet

- [ ] **Step 3: Implement the inventory and wallet rules**

`game/scripts/core/Inventory/ItemStack.cs`

```csharp
namespace HarvestManor.Core.Inventory;

public sealed record ItemStack(string ItemId, int Quantity);
```

`game/scripts/core/Inventory/InventoryState.cs`

```csharp
using System.Linq;

namespace HarvestManor.Core.Inventory;

public sealed class InventoryState
{
    private readonly int _slotCapacity;
    private readonly int _maxStackSize;
    private readonly List<ItemStack> _slots = new();

    public InventoryState(int slotCapacity, int maxStackSize)
    {
        _slotCapacity = slotCapacity;
        _maxStackSize = maxStackSize;
    }

    public IReadOnlyList<ItemStack> Slots => _slots;

    public bool TryAdd(string itemId, int quantity)
    {
        for (var index = 0; index < _slots.Count; index++)
        {
            var stack = _slots[index];
            if (stack.ItemId == itemId && stack.Quantity < _maxStackSize)
            {
                var newQuantity = Math.Min(_maxStackSize, stack.Quantity + quantity);
                var consumed = newQuantity - stack.Quantity;
                quantity -= consumed;
                _slots[index] = stack with { Quantity = newQuantity };
            }
        }

        while (quantity > 0 && _slots.Count < _slotCapacity)
        {
            var nextQuantity = Math.Min(_maxStackSize, quantity);
            _slots.Add(new ItemStack(itemId, nextQuantity));
            quantity -= nextQuantity;
        }

        return quantity == 0;
    }

    public bool TryRemove(string itemId, int quantity)
    {
        if (GetQuantity(itemId) < quantity)
        {
            return false;
        }

        for (var index = _slots.Count - 1; index >= 0 && quantity > 0; index--)
        {
            var stack = _slots[index];
            if (stack.ItemId != itemId)
            {
                continue;
            }

            var removeAmount = Math.Min(stack.Quantity, quantity);
            quantity -= removeAmount;
            var remaining = stack.Quantity - removeAmount;

            if (remaining == 0)
            {
                _slots.RemoveAt(index);
            }
            else
            {
                _slots[index] = stack with { Quantity = remaining };
            }
        }

        return true;
    }

    public int GetQuantity(string itemId)
    {
        return _slots.Where(slot => slot.ItemId == itemId).Sum(slot => slot.Quantity);
    }
}
```

`game/scripts/core/Economy/Wallet.cs`

```csharp
namespace HarvestManor.Core.Economy;

public sealed class Wallet
{
    public Wallet(int gold)
    {
        Gold = gold;
    }

    public int Gold { get; private set; }

    public bool TrySpend(int amount)
    {
        if (amount > Gold)
        {
            return false;
        }

        Gold -= amount;
        return true;
    }

    public void Earn(int amount)
    {
        Gold += amount;
    }
}
```

`game/scripts/core/Economy/ShopOffer.cs`

```csharp
namespace HarvestManor.Core.Economy;

public sealed record ShopOffer(string ItemId, int BuyPrice, int SellPrice);
```

`game/scripts/core/Economy/ShopService.cs`

```csharp
using HarvestManor.Core.Inventory;

namespace HarvestManor.Core.Economy;

public sealed class ShopService
{
    public bool TryPurchase(InventoryState inventory, Wallet wallet, ShopOffer offer, int quantity)
    {
        var totalCost = offer.BuyPrice * quantity;
        if (!wallet.TrySpend(totalCost))
        {
            return false;
        }

        if (inventory.TryAdd(offer.ItemId, quantity))
        {
            return true;
        }

        wallet.Earn(totalCost);
        return false;
    }

    public bool TrySell(InventoryState inventory, Wallet wallet, ShopOffer offer, int quantity)
    {
        if (!inventory.TryRemove(offer.ItemId, quantity))
        {
            return false;
        }

        wallet.Earn(offer.SellPrice * quantity);
        return true;
    }
}
```

- [ ] **Step 4: Add initial shop data**

`game/data/shops/general-store.json`

```json
[
  { "itemId": "parsnip_seed", "buyPrice": 20, "sellPrice": 10 },
  { "itemId": "potato_seed", "buyPrice": 45, "sellPrice": 22 },
  { "itemId": "parsnip_crop", "buyPrice": 0, "sellPrice": 35 },
  { "itemId": "potato_crop", "buyPrice": 0, "sellPrice": 80 }
]
```

- [ ] **Step 5: Run the tests and build**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~Economy
dotnet build "D:\game project\harvest-manor\game\HarvestManor.csproj"
```

Expected:

- PASS for inventory and shop tests
- BUILD SUCCEEDED

- [ ] **Step 6: Commit**

```bash
git add game/scripts/core/Inventory game/scripts/core/Economy game/data/shops/general-store.json tests/HarvestManor.Game.Tests/Economy
git commit -m "feat: add inventory and shop domain rules"
```

### Task 4: Add Farm Plot State and Crop Growth Rules

**Files:**
- Create: `game/scripts/core/Farming/CropInstance.cs`
- Create: `game/scripts/core/Farming/PlotState.cs`
- Create: `game/scripts/core/Farming/FarmGrid.cs`
- Create: `game/scripts/core/Farming/CropGrowthService.cs`
- Create: `tests/HarvestManor.Game.Tests/Farming/CropGrowthServiceTests.cs`

- [ ] **Step 1: Write the failing crop growth tests**

```csharp
using HarvestManor.Core.Content;
using HarvestManor.Core.Farming;
using Xunit;

namespace HarvestManor.Game.Tests.Farming;

public sealed class CropGrowthServiceTests
{
    [Fact]
    public void AdvanceDay_GrowsWateredCropByOneDay()
    {
        var crop = new CropDefinition(
            "parsnip",
            "Parsnip",
            "Spring",
            "parsnip_seed",
            "parsnip_crop",
            20,
            35,
            4,
            new[] { 1, 1, 2 });

        var plot = PlotState.Tilled(0, 0).Plant(crop.Id).Water();
        var growth = new CropGrowthService(new Dictionary<string, CropDefinition> { [crop.Id] = crop });

        var next = growth.AdvanceDay(plot);

        Assert.Equal(1, next.Crop!.DaysGrown);
        Assert.False(next.IsWateredToday);
        Assert.False(next.IsHarvestReady);
    }

    [Fact]
    public void AdvanceDay_MarksCropHarvestReadyAtGrowthLimit()
    {
        var crop = new CropDefinition(
            "parsnip",
            "Parsnip",
            "Spring",
            "parsnip_seed",
            "parsnip_crop",
            20,
            35,
            4,
            new[] { 1, 1, 2 });

        var plot = PlotState.Tilled(0, 0).Plant(crop.Id).Water();
        plot = plot with { Crop = plot.Crop! with { DaysGrown = 3 } };

        var growth = new CropGrowthService(new Dictionary<string, CropDefinition> { [crop.Id] = crop });
        var next = growth.AdvanceDay(plot);

        Assert.True(next.IsHarvestReady);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~CropGrowthServiceTests
```

Expected:

- FAIL because the farming types do not exist

- [ ] **Step 3: Implement crop instances, plots, grid, and growth**

`game/scripts/core/Farming/CropInstance.cs`

```csharp
namespace HarvestManor.Core.Farming;

public sealed record CropInstance(string CropId, int DaysGrown);
```

`game/scripts/core/Farming/PlotState.cs`

```csharp
namespace HarvestManor.Core.Farming;

public sealed record PlotState(
    int X,
    int Y,
    bool IsTilled,
    bool IsLocked,
    bool IsWateredToday,
    bool IsHarvestReady,
    CropInstance? Crop)
{
    public static PlotState Wild(int x, int y) => new(x, y, false, false, false, false, null);

    public static PlotState Tilled(int x, int y) => new(x, y, true, false, false, false, null);

    public PlotState Till() => this with { IsTilled = true };

    public PlotState Plant(string cropId)
    {
        return this with
        {
            Crop = new CropInstance(cropId, 0),
            IsHarvestReady = false
        };
    }

    public PlotState Water() => this with { IsWateredToday = true };
}
```

`game/scripts/core/Farming/FarmGrid.cs`

```csharp
namespace HarvestManor.Core.Farming;

public sealed class FarmGrid
{
    private readonly Dictionary<(int X, int Y), PlotState> _plots = new();

    public FarmGrid(int width, int height)
    {
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                _plots[(x, y)] = PlotState.Wild(x, y);
            }
        }
    }

    public PlotState GetPlot(int x, int y) => _plots[(x, y)];

    public void SetPlot(PlotState plot)
    {
        _plots[(plot.X, plot.Y)] = plot;
    }

    public IReadOnlyCollection<PlotState> AllPlots => _plots.Values;
}
```

`game/scripts/core/Farming/CropGrowthService.cs`

```csharp
using HarvestManor.Core.Content;

namespace HarvestManor.Core.Farming;

public sealed class CropGrowthService
{
    private readonly IReadOnlyDictionary<string, CropDefinition> _crops;

    public CropGrowthService(IReadOnlyDictionary<string, CropDefinition> crops)
    {
        _crops = crops;
    }

    public PlotState AdvanceDay(PlotState plot)
    {
        if (plot.Crop is null || !plot.IsWateredToday)
        {
            return plot with { IsWateredToday = false };
        }

        var crop = _crops[plot.Crop.CropId];
        var nextDays = plot.Crop.DaysGrown + 1;

        return plot with
        {
            Crop = plot.Crop with { DaysGrown = nextDays },
            IsWateredToday = false,
            IsHarvestReady = nextDays >= crop.TotalGrowthDays
        };
    }
}
```

- [ ] **Step 4: Run the tests and the full suite so far**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj"
```

Expected:

- PASS for content, time, economy, and farming tests

- [ ] **Step 5: Commit**

```bash
git add game/scripts/core/Farming tests/HarvestManor.Game.Tests/Farming
git commit -m "feat: add farm plot and crop growth rules"
```

### Task 5: Add Save Snapshots, Land Unlocks, and Request Progression

**Files:**
- Create: `game/scripts/core/Progression/UnlockState.cs`
- Create: `game/scripts/core/Progression/FarmExpansionService.cs`
- Create: `game/scripts/core/Progression/RequestDefinition.cs`
- Create: `game/scripts/core/Progression/RequestBoardService.cs`
- Create: `game/scripts/core/Saves/PlotSnapshot.cs`
- Create: `game/scripts/core/Saves/SaveGameSnapshot.cs`
- Create: `game/scripts/core/Saves/SaveGameStore.cs`
- Create: `game/data/requests/request-board.json`
- Create: `tests/HarvestManor.Game.Tests/Saves/SaveGameStoreTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Progression/FarmExpansionServiceTests.cs`
- Create: `tests/HarvestManor.Game.Tests/Progression/RequestBoardServiceTests.cs`

- [ ] **Step 1: Write the failing persistence and progression tests**

`tests/HarvestManor.Game.Tests/Saves/SaveGameStoreTests.cs`

```csharp
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Saves;
using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Saves;

public sealed class SaveGameStoreTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTripsCoreProgress()
    {
        var snapshot = new SaveGameSnapshot(
            new GameDate(Season.Spring, 3),
            minuteOfDay: 420,
            gold: 180,
            stamina: 88,
            inventory: new List<ItemStack> { new("parsnip_seed", 8) },
            storage: new List<ItemStack> { new("wood", 12) },
            plots: new List<PlotSnapshot> { new(0, 0, true, false, true, "parsnip", 2) },
            unlockedPlotKeys: new List<string> { "0,0", "1,0" },
            completedRequests: new List<string> { "ship_5_parsnips" }
        );

        var json = SaveGameStore.Serialize(snapshot);
        var restored = SaveGameStore.Deserialize(json);

        Assert.Equal(snapshot.Date, restored.Date);
        Assert.Equal(180, restored.Gold);
        Assert.Single(restored.Inventory);
        Assert.Single(restored.Plots);
        Assert.Single(restored.CompletedRequests);
    }
}
```

`tests/HarvestManor.Game.Tests/Progression/FarmExpansionServiceTests.cs`

```csharp
using HarvestManor.Core.Progression;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class FarmExpansionServiceTests
{
    [Fact]
    public void UnlockPlot_AddsCoordinateKeyWhenGoldRequirementMet()
    {
        var unlocks = new UnlockState(new HashSet<string>());
        var expansion = new FarmExpansionService();

        var success = expansion.TryUnlockPlot(unlocks, "4,2", requiredGold: 120, currentGold: 200, out var updatedGold);

        Assert.True(success);
        Assert.True(unlocks.UnlockedPlotKeys.Contains("4,2"));
        Assert.Equal(80, updatedGold);
    }
}
```

`tests/HarvestManor.Game.Tests/Progression/RequestBoardServiceTests.cs`

```csharp
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class RequestBoardServiceTests
{
    [Fact]
    public void CompleteRequest_RemovesRequiredItemsAndMarksRequestDone()
    {
        var inventory = new InventoryState(slotCapacity: 10, maxStackSize: 99);
        inventory.TryAdd("parsnip_crop", 5);

        var service = new RequestBoardService();
        var request = new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120);

        var completed = service.TryComplete(request, inventory, out var reward);

        Assert.True(completed);
        Assert.Equal(120, reward);
        Assert.Equal(0, inventory.GetQuantity("parsnip_crop"));
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter "FullyQualifiedName~Saves|FullyQualifiedName~Progression"
```

Expected:

- FAIL because the save and progression types do not exist yet

- [ ] **Step 3: Implement save snapshot records and serializer**

`game/scripts/core/Saves/PlotSnapshot.cs`

```csharp
namespace HarvestManor.Core.Saves;

public sealed record PlotSnapshot(
    int X,
    int Y,
    bool IsTilled,
    bool IsLocked,
    bool IsHarvestReady,
    string? CropId,
    int DaysGrown);
```

`game/scripts/core/Saves/SaveGameSnapshot.cs`

```csharp
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Time;

namespace HarvestManor.Core.Saves;

public sealed record SaveGameSnapshot(
    GameDate Date,
    int MinuteOfDay,
    int Gold,
    int Stamina,
    List<ItemStack> Inventory,
    List<ItemStack> Storage,
    List<PlotSnapshot> Plots,
    List<string> UnlockedPlotKeys,
    List<string> CompletedRequests);
```

`game/scripts/core/Saves/SaveGameStore.cs`

```csharp
using System.Text.Json;

namespace HarvestManor.Core.Saves;

public static class SaveGameStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static string Serialize(SaveGameSnapshot snapshot)
    {
        return JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static SaveGameSnapshot Deserialize(string json)
    {
        return JsonSerializer.Deserialize<SaveGameSnapshot>(json, JsonOptions)
            ?? throw new InvalidDataException("Save payload was empty.");
    }
}
```

- [ ] **Step 4: Implement farm unlock and request services**

`game/scripts/core/Progression/UnlockState.cs`

```csharp
namespace HarvestManor.Core.Progression;

public sealed class UnlockState
{
    public UnlockState(HashSet<string> unlockedPlotKeys)
    {
        UnlockedPlotKeys = unlockedPlotKeys;
    }

    public HashSet<string> UnlockedPlotKeys { get; }
}
```

`game/scripts/core/Progression/FarmExpansionService.cs`

```csharp
namespace HarvestManor.Core.Progression;

public sealed class FarmExpansionService
{
    public bool TryUnlockPlot(UnlockState unlocks, string plotKey, int requiredGold, int currentGold, out int updatedGold)
    {
        if (currentGold < requiredGold || unlocks.UnlockedPlotKeys.Contains(plotKey))
        {
            updatedGold = currentGold;
            return false;
        }

        unlocks.UnlockedPlotKeys.Add(plotKey);
        updatedGold = currentGold - requiredGold;
        return true;
    }
}
```

`game/scripts/core/Progression/RequestDefinition.cs`

```csharp
namespace HarvestManor.Core.Progression;

public sealed record RequestDefinition(string Id, string RequiredItemId, int RequiredQuantity, int RewardGold);
```

`game/scripts/core/Progression/RequestBoardService.cs`

```csharp
using HarvestManor.Core.Inventory;

namespace HarvestManor.Core.Progression;

public sealed class RequestBoardService
{
    public bool TryComplete(RequestDefinition request, InventoryState inventory, out int rewardGold)
    {
        rewardGold = 0;

        if (!inventory.TryRemove(request.RequiredItemId, request.RequiredQuantity))
        {
            return false;
        }

        rewardGold = request.RewardGold;
        return true;
    }
}
```

`game/data/requests/request-board.json`

```json
[
  { "id": "ship_5_parsnips", "requiredItemId": "parsnip_crop", "requiredQuantity": 5, "rewardGold": 120 }
]
```

- [ ] **Step 5: Run the tests and the full suite**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj"
```

Expected:

- PASS for all rule-layer tests through saves and progression

- [ ] **Step 6: Commit**

```bash
git add game/scripts/core/Progression game/scripts/core/Saves game/data/requests/request-board.json tests/HarvestManor.Game.Tests/Saves tests/HarvestManor.Game.Tests/Progression
git commit -m "feat: add progression unlocks and save snapshots"
```

### Task 6: Build the First Playable Farm Vertical Slice in Godot

**Files:**
- Create: `game/scenes/world/FarmScene.tscn`
- Create: `game/scenes/ui/Hud.tscn`
- Create: `game/scripts/world/PlayerController.cs`
- Create: `game/scripts/world/FarmPlotNode.cs`
- Create: `game/scripts/world/BedInteraction.cs`
- Create: `game/scripts/ui/HudController.cs`
- Modify: `game/scenes/Main.tscn`
- Modify: `game/scripts/world/GameBootstrap.cs`

- [ ] **Step 1: Write the failing integration-oriented rule test for end-of-day crop updates**

```csharp
using HarvestManor.Core.Content;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Time;
using Xunit;

namespace HarvestManor.Game.Tests.Farming;

public sealed class DayEndFarmLoopTests
{
    [Fact]
    public void AdvancingPastDayEnd_GrowsWateredPlotAndResetsClock()
    {
        var crop = new CropDefinition(
            "parsnip",
            "Parsnip",
            "Spring",
            "parsnip_seed",
            "parsnip_crop",
            20,
            35,
            4,
            new[] { 1, 1, 2 });

        var growth = new CropGrowthService(new Dictionary<string, CropDefinition> { [crop.Id] = crop });
        var clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        var plot = PlotState.Tilled(0, 0).Plant(crop.Id).Water();

        var rolled = clock.AdvanceMinutes(20 * 60);
        var nextPlot = growth.AdvanceDay(plot);

        Assert.True(rolled);
        Assert.Equal(new GameDate(Season.Spring, 2), clock.Date);
        Assert.Equal(1, nextPlot.Crop!.DaysGrown);
    }
}
```

- [ ] **Step 2: Run the new test to verify it fails**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~DayEndFarmLoopTests
```

Expected:

- FAIL because the integration test file has not been added yet or the solution has not been rebuilt

- [ ] **Step 3: Create the Godot world scripts**

`game/scripts/world/PlayerController.cs`

```csharp
using Godot;

namespace HarvestManor.World;

public partial class PlayerController : CharacterBody2D
{
    [Export]
    public float MoveSpeed { get; set; } = 120.0f;

    public override void _PhysicsProcess(double delta)
    {
        var direction = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        Velocity = direction * MoveSpeed;
        MoveAndSlide();
    }
}
```

`game/scripts/world/FarmPlotNode.cs`

```csharp
using Godot;

namespace HarvestManor.World;

public partial class FarmPlotNode : Area2D
{
    [Export]
    public int GridX { get; set; }

    [Export]
    public int GridY { get; set; }

    [Signal]
    public delegate void PlotInteractedEventHandler(int gridX, int gridY);

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.PlotInteracted, GridX, GridY);
        }
    }
}
```

`game/scripts/world/BedInteraction.cs`

```csharp
using Godot;

namespace HarvestManor.World;

public partial class BedInteraction : Area2D
{
    [Signal]
    public delegate void DayEndRequestedEventHandler();

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.DayEndRequested);
        }
    }
}
```

`game/scripts/ui/HudController.cs`

```csharp
using Godot;

namespace HarvestManor.UI;

public partial class HudController : CanvasLayer
{
    [Export]
    public Label? DayLabel { get; set; }

    [Export]
    public Label? GoldLabel { get; set; }

    [Export]
    public Label? StaminaLabel { get; set; }

    public void SetDay(string text) => DayLabel!.Text = text;

    public void SetGold(int gold) => GoldLabel!.Text = $"Gold: {gold}";

    public void SetStamina(int current, int maximum) => StaminaLabel!.Text = $"Stamina: {current}/{maximum}";
}
```

- [ ] **Step 4: Create the basic farm and HUD scenes**

`game/scenes/world/FarmScene.tscn`

```text
[gd_scene load_steps=4 format=3]

[ext_resource type="Script" path="res://scripts/world/PlayerController.cs" id="1"]
[ext_resource type="Script" path="res://scripts/world/BedInteraction.cs" id="2"]
[ext_resource type="Script" path="res://scripts/world/FarmPlotNode.cs" id="3"]

[node name="FarmScene" type="Node2D"]

[node name="Player" type="CharacterBody2D" parent="."]
script = ExtResource("1")

[node name="Bed" type="Area2D" parent="."]
script = ExtResource("2")

[node name="Plot00" type="Area2D" parent="."]
script = ExtResource("3")
GridX = 0
GridY = 0
```

`game/scenes/ui/Hud.tscn`

```text
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/ui/HudController.cs" id="1"]

[node name="Hud" type="CanvasLayer"]
script = ExtResource("1")

[node name="DayLabel" type="Label" parent="."]
text = "Day 1"

[node name="GoldLabel" type="Label" parent="."]
position = Vector2(0, 24)
text = "Gold: 0"

[node name="StaminaLabel" type="Label" parent="."]
position = Vector2(0, 48)
text = "Stamina: 100/100"
```

`game/scenes/Main.tscn`

```text
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/world/GameBootstrap.cs" id="1"]

[node name="Main" type="Node2D"]
script = ExtResource("1")
```

- [ ] **Step 5: Wire bootstrap to instantiate the farm scene, update HUD, and process day end**

`game/scripts/world/GameBootstrap.cs`

```csharp
using System.Linq;
using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    private readonly ContentCatalogLoader _loader = new();
    private CropGrowthService? _growth;
    private DayClock? _clock;
    private StaminaState? _stamina;
    private Wallet? _wallet;
    private InventoryState? _inventory;
    private FarmGrid? _farmGrid;
    private HudController? _hud;

    public override void _Ready()
    {
        var crops = _loader.LoadCropCatalog(ProjectSettings.GlobalizePath("res://data/crops/spring.json"));
        _ = _loader.LoadItemCatalog(ProjectSettings.GlobalizePath("res://data/items/items.json"));

        _growth = new CropGrowthService(crops.ToDictionary(crop => crop.Id));
        _clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        _stamina = new StaminaState(100, 100);
        _wallet = new Wallet(200);
        _inventory = new InventoryState(12, 99);
        _farmGrid = new FarmGrid(6, 6);

        var farmScene = GD.Load<PackedScene>("res://scenes/world/FarmScene.tscn").Instantiate<Node2D>();
        AddChild(farmScene);

        _hud = GD.Load<PackedScene>("res://scenes/ui/Hud.tscn").Instantiate<HudController>();
        AddChild(_hud);

        RefreshHud();
    }

    private void EndDay()
    {
        if (_clock is null || _stamina is null || _growth is null || _farmGrid is null)
        {
            return;
        }

        _clock.AdvanceMinutes(20 * 60);
        foreach (var plot in _farmGrid.AllPlots.ToList())
        {
            _farmGrid.SetPlot(_growth.AdvanceDay(plot));
        }

        _stamina.RestoreFull();
        RefreshHud();
    }

    private void RefreshHud()
    {
        if (_clock is null || _stamina is null || _wallet is null || _hud is null)
        {
            return;
        }

        _hud.SetDay($"Day {_clock.Date.Day} ({_clock.Date.Season})");
        _hud.SetGold(_wallet.Gold);
        _hud.SetStamina(_stamina.Current, _stamina.Maximum);
    }
}
```

- [ ] **Step 6: Run tests, build, and perform a manual smoke check**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj"
dotnet build "D:\game project\harvest-manor\game\HarvestManor.csproj"
& $env:GODOT4 --path "D:\game project\harvest-manor\game"
```

Expected:

- PASS for the full test suite
- BUILD SUCCEEDED
- the game opens into the main scene, shows a HUD, and the farm scene loads without C# compilation errors

- [ ] **Step 7: Commit**

```bash
git add game/scenes/Main.tscn game/scenes/world/FarmScene.tscn game/scenes/ui/Hud.tscn game/scripts/world game/scripts/ui tests/HarvestManor.Game.Tests/Farming
git commit -m "feat: wire the first playable farm scene"
```

### Task 7: Add Storage, Shop, Requests, and Save Integration

**Files:**
- Create: `game/scenes/world/TownScene.tscn`
- Create: `game/scenes/ui/InventoryPanel.tscn`
- Create: `game/scenes/ui/ShopPanel.tscn`
- Create: `game/scenes/ui/StoragePanel.tscn`
- Create: `game/scripts/world/StorageInteraction.cs`
- Create: `game/scripts/world/ShopInteraction.cs`
- Create: `game/scripts/world/RequestBoardInteraction.cs`
- Create: `game/scripts/ui/InventoryPanelController.cs`
- Create: `game/scripts/ui/ShopPanelController.cs`
- Create: `game/scripts/ui/StoragePanelController.cs`
- Modify: `game/scripts/world/GameBootstrap.cs`

- [ ] **Step 1: Write the failing request-completion integration test**

```csharp
using HarvestManor.Core.Economy;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class RewardFlowTests
{
    [Fact]
    public void CompletingRequest_AddsRewardToWallet()
    {
        var inventory = new InventoryState(12, 99);
        inventory.TryAdd("parsnip_crop", 5);

        var wallet = new Wallet(0);
        var board = new RequestBoardService();
        var request = new RequestDefinition("ship_5_parsnips", "parsnip_crop", 5, 120);

        var completed = board.TryComplete(request, inventory, out var reward);
        if (completed)
        {
            wallet.Earn(reward);
        }

        Assert.True(completed);
        Assert.Equal(120, wallet.Gold);
    }
}
```

- [ ] **Step 2: Run the test to verify the integrated reward flow**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~RewardFlowTests
```

Expected:

- PASS after the test file is added, confirming the rule layer already supports the request-to-wallet flow

- [ ] **Step 3: Add the Godot interaction and panel controllers**

`game/scripts/world/StorageInteraction.cs`

```csharp
using Godot;

namespace HarvestManor.World;

public partial class StorageInteraction : Area2D
{
    [Signal]
    public delegate void StorageRequestedEventHandler();

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.StorageRequested);
        }
    }
}
```

`game/scripts/world/ShopInteraction.cs`

```csharp
using Godot;

namespace HarvestManor.World;

public partial class ShopInteraction : Area2D
{
    [Signal]
    public delegate void ShopRequestedEventHandler();

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.ShopRequested);
        }
    }
}
```

`game/scripts/world/RequestBoardInteraction.cs`

```csharp
using Godot;

namespace HarvestManor.World;

public partial class RequestBoardInteraction : Area2D
{
    [Signal]
    public delegate void RequestBoardRequestedEventHandler();

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            EmitSignal(SignalName.RequestBoardRequested);
        }
    }
}
```

`game/scripts/ui/InventoryPanelController.cs`

```csharp
using System.Linq;
using Godot;
using HarvestManor.Core.Inventory;

namespace HarvestManor.UI;

public partial class InventoryPanelController : Control
{
    [Export]
    public RichTextLabel? BodyLabel { get; set; }

    public void Render(InventoryState inventory)
    {
        BodyLabel!.Text = string.Join("\n", inventory.Slots.Select(slot => $"{slot.ItemId} x{slot.Quantity}"));
    }
}
```

`game/scripts/ui/ShopPanelController.cs`

```csharp
using System.Linq;
using Godot;
using HarvestManor.Core.Economy;

namespace HarvestManor.UI;

public partial class ShopPanelController : Control
{
    [Export]
    public RichTextLabel? BodyLabel { get; set; }

    public void Render(IEnumerable<ShopOffer> offers)
    {
        BodyLabel!.Text = string.Join("\n", offers.Select(offer => $"{offer.ItemId} buy:{offer.BuyPrice} sell:{offer.SellPrice}"));
    }
}
```

`game/scripts/ui/StoragePanelController.cs`

```csharp
using System.Linq;
using Godot;
using HarvestManor.Core.Inventory;

namespace HarvestManor.UI;

public partial class StoragePanelController : Control
{
    [Export]
    public RichTextLabel? BodyLabel { get; set; }

    public void Render(InventoryState storage)
    {
        BodyLabel!.Text = string.Join("\n", storage.Slots.Select(slot => $"{slot.ItemId} x{slot.Quantity}"));
    }
}
```

- [ ] **Step 4: Create minimal UI scenes and a minimal town scene**

`game/scenes/ui/InventoryPanel.tscn`

```text
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/ui/InventoryPanelController.cs" id="1"]

[node name="InventoryPanel" type="Control"]
script = ExtResource("1")

[node name="BodyLabel" type="RichTextLabel" parent="."]
size = Vector2(320, 240)
```

`game/scenes/ui/ShopPanel.tscn`

```text
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/ui/ShopPanelController.cs" id="1"]

[node name="ShopPanel" type="Control"]
script = ExtResource("1")

[node name="BodyLabel" type="RichTextLabel" parent="."]
size = Vector2(320, 240)
```

`game/scenes/ui/StoragePanel.tscn`

```text
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/ui/StoragePanelController.cs" id="1"]

[node name="StoragePanel" type="Control"]
script = ExtResource("1")

[node name="BodyLabel" type="RichTextLabel" parent="."]
size = Vector2(320, 240)
```

`game/scenes/world/TownScene.tscn`

```text
[gd_scene load_steps=4 format=3]

[ext_resource type="Script" path="res://scripts/world/ShopInteraction.cs" id="1"]
[ext_resource type="Script" path="res://scripts/world/StorageInteraction.cs" id="2"]
[ext_resource type="Script" path="res://scripts/world/RequestBoardInteraction.cs" id="3"]

[node name="TownScene" type="Node2D"]

[node name="Shop" type="Area2D" parent="."]
script = ExtResource("1")

[node name="Storage" type="Area2D" parent="."]
script = ExtResource("2")

[node name="RequestBoard" type="Area2D" parent="."]
script = ExtResource("3")
```

- [ ] **Step 5: Extend bootstrap to hold storage, shop offers, and autosave**

`game/scripts/world/GameBootstrap.cs`

```csharp
using System.Linq;
using System.Text.Json;
using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.Core.Saves;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    private readonly ContentCatalogLoader _loader = new();
    private readonly ShopService _shopService = new();
    private readonly RequestBoardService _requestBoardService = new();

    private CropGrowthService? _growth;
    private DayClock? _clock;
    private StaminaState? _stamina;
    private Wallet? _wallet;
    private InventoryState? _inventory;
    private InventoryState? _storage;
    private FarmGrid? _farmGrid;
    private HudController? _hud;
    private IReadOnlyList<ShopOffer> _shopOffers = Array.Empty<ShopOffer>();
    private IReadOnlyList<RequestDefinition> _requests = Array.Empty<RequestDefinition>();

    public override void _Ready()
    {
        var crops = _loader.LoadCropCatalog(ProjectSettings.GlobalizePath("res://data/crops/spring.json"));
        _ = _loader.LoadItemCatalog(ProjectSettings.GlobalizePath("res://data/items/items.json"));

        _shopOffers = JsonSerializer.Deserialize<List<ShopOffer>>(
            File.ReadAllText(ProjectSettings.GlobalizePath("res://data/shops/general-store.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        _requests = JsonSerializer.Deserialize<List<RequestDefinition>>(
            File.ReadAllText(ProjectSettings.GlobalizePath("res://data/requests/request-board.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        _growth = new CropGrowthService(crops.ToDictionary(crop => crop.Id));
        _clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        _stamina = new StaminaState(100, 100);
        _wallet = new Wallet(200);
        _inventory = new InventoryState(12, 99);
        _storage = new InventoryState(24, 99);
        _farmGrid = new FarmGrid(6, 6);

        AddChild(GD.Load<PackedScene>("res://scenes/world/FarmScene.tscn").Instantiate<Node2D>());
        AddChild(GD.Load<PackedScene>("res://scenes/world/TownScene.tscn").Instantiate<Node2D>());

        _hud = GD.Load<PackedScene>("res://scenes/ui/Hud.tscn").Instantiate<HudController>();
        AddChild(_hud);

        RefreshHud();
        Autosave();
    }

    private void Autosave()
    {
        if (_clock is null || _wallet is null || _stamina is null || _inventory is null || _storage is null || _farmGrid is null)
        {
            return;
        }

        var snapshot = new SaveGameSnapshot(
            _clock.Date,
            _clock.CurrentMinuteOfDay,
            _wallet.Gold,
            _stamina.Current,
            _inventory.Slots.ToList(),
            _storage.Slots.ToList(),
            _farmGrid.AllPlots.Select(plot => new PlotSnapshot(
                plot.X,
                plot.Y,
                plot.IsTilled,
                plot.IsLocked,
                plot.IsHarvestReady,
                plot.Crop?.CropId,
                plot.Crop?.DaysGrown ?? 0)).ToList(),
            new List<string>(),
            new List<string>());

        var saveDir = ProjectSettings.GlobalizePath("user://saves");
        Directory.CreateDirectory(saveDir);
        File.WriteAllText(Path.Combine(saveDir, "slot-1.json"), SaveGameStore.Serialize(snapshot));
    }

    private void RefreshHud()
    {
        if (_clock is null || _stamina is null || _wallet is null || _hud is null)
        {
            return;
        }

        _hud.SetDay($"Day {_clock.Date.Day} ({_clock.Date.Season})");
        _hud.SetGold(_wallet.Gold);
        _hud.SetStamina(_stamina.Current, _stamina.Maximum);
    }
}
```

- [ ] **Step 6: Run the full test suite, build, and manual smoke test**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj"
dotnet build "D:\game project\harvest-manor\game\HarvestManor.csproj"
& $env:GODOT4 --path "D:\game project\harvest-manor\game"
```

Expected:

- PASS for the full test suite
- BUILD SUCCEEDED
- the game opens, saves `user://saves/slot-1.json`, and shows no missing-script errors for the new panels and world interactions

- [ ] **Step 7: Commit**

```bash
git add game/scenes/world/TownScene.tscn game/scenes/ui game/scripts/ui game/scripts/world game/data/shops/general-store.json game/data/requests/request-board.json
git commit -m "feat: add town interactions, storage, and autosave"
```

### Task 8: Add Land Expansion Hooks and Final Milestone QA Pass

**Files:**
- Modify: `game/scripts/world/GameBootstrap.cs`
- Modify: `game/scripts/ui/HudController.cs`
- Modify: `game/scripts/core/Progression/FarmExpansionService.cs`
- Modify: `tests/HarvestManor.Game.Tests/Progression/FarmExpansionServiceTests.cs`
- Create: `docs/testing/milestone-1-smoke-checklist.md`

- [ ] **Step 1: Extend the failing land-expansion test with a repeated unlock guard**

```csharp
using HarvestManor.Core.Progression;
using Xunit;

namespace HarvestManor.Game.Tests.Progression;

public sealed class FarmExpansionServiceTests
{
    [Fact]
    public void TryUnlockPlot_ReturnsFalseWhenPlotAlreadyUnlocked()
    {
        var unlocks = new UnlockState(new HashSet<string> { "4,2" });
        var expansion = new FarmExpansionService();

        var success = expansion.TryUnlockPlot(unlocks, "4,2", requiredGold: 120, currentGold: 200, out var updatedGold);

        Assert.False(success);
        Assert.Equal(200, updatedGold);
    }
}
```

- [ ] **Step 2: Run the test to verify the guard exists**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj" --filter FullyQualifiedName~FarmExpansionServiceTests
```

Expected:

- PASS after adding the extra test case because the current service already rejects repeated unlocks

- [ ] **Step 3: Surface expansion status in the HUD and keep bootstrap ready for expansion purchases**

`game/scripts/ui/HudController.cs`

```csharp
using Godot;

namespace HarvestManor.UI;

public partial class HudController : CanvasLayer
{
    [Export]
    public Label? DayLabel { get; set; }

    [Export]
    public Label? GoldLabel { get; set; }

    [Export]
    public Label? StaminaLabel { get; set; }

    [Export]
    public Label? GrowthLabel { get; set; }

    public void SetDay(string text) => DayLabel!.Text = text;

    public void SetGold(int gold) => GoldLabel!.Text = $"Gold: {gold}";

    public void SetStamina(int current, int maximum) => StaminaLabel!.Text = $"Stamina: {current}/{maximum}";

    public void SetGrowth(string text) => GrowthLabel!.Text = text;
}
```

`game/scripts/world/GameBootstrap.cs`

```csharp
using System.Linq;
using System.Text.Json;
using Godot;
using HarvestManor.Core.Content;
using HarvestManor.Core.Economy;
using HarvestManor.Core.Farming;
using HarvestManor.Core.Inventory;
using HarvestManor.Core.Progression;
using HarvestManor.Core.Saves;
using HarvestManor.Core.Time;
using HarvestManor.UI;

namespace HarvestManor.World;

public partial class GameBootstrap : Node2D
{
    private readonly ContentCatalogLoader _loader = new();
    private readonly ShopService _shopService = new();
    private readonly RequestBoardService _requestBoardService = new();
    private readonly FarmExpansionService _expansionService = new();
    private readonly UnlockState _unlockState = new(new HashSet<string> { "0,0", "1,0", "0,1", "1,1" });

    private CropGrowthService? _growth;
    private DayClock? _clock;
    private StaminaState? _stamina;
    private Wallet? _wallet;
    private InventoryState? _inventory;
    private InventoryState? _storage;
    private FarmGrid? _farmGrid;
    private HudController? _hud;
    private IReadOnlyList<ShopOffer> _shopOffers = Array.Empty<ShopOffer>();
    private IReadOnlyList<RequestDefinition> _requests = Array.Empty<RequestDefinition>();

    public override void _Ready()
    {
        var crops = _loader.LoadCropCatalog(ProjectSettings.GlobalizePath("res://data/crops/spring.json"));
        _ = _loader.LoadItemCatalog(ProjectSettings.GlobalizePath("res://data/items/items.json"));

        _shopOffers = JsonSerializer.Deserialize<List<ShopOffer>>(
            File.ReadAllText(ProjectSettings.GlobalizePath("res://data/shops/general-store.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        _requests = JsonSerializer.Deserialize<List<RequestDefinition>>(
            File.ReadAllText(ProjectSettings.GlobalizePath("res://data/requests/request-board.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        _growth = new CropGrowthService(crops.ToDictionary(crop => crop.Id));
        _clock = new DayClock(new GameDate(Season.Spring, 1), 6 * 60, 26 * 60);
        _stamina = new StaminaState(100, 100);
        _wallet = new Wallet(200);
        _inventory = new InventoryState(12, 99);
        _storage = new InventoryState(24, 99);
        _farmGrid = new FarmGrid(6, 6);

        AddChild(GD.Load<PackedScene>("res://scenes/world/FarmScene.tscn").Instantiate<Node2D>());
        AddChild(GD.Load<PackedScene>("res://scenes/world/TownScene.tscn").Instantiate<Node2D>());

        _hud = GD.Load<PackedScene>("res://scenes/ui/Hud.tscn").Instantiate<HudController>();
        AddChild(_hud);

        RefreshHud();
        Autosave();
    }

    private void TryUnlockDemoPlot()
    {
        if (_wallet is null)
        {
            return;
        }

        if (_expansionService.TryUnlockPlot(_unlockState, "2,0", 120, _wallet.Gold, out var updatedGold))
        {
            _wallet = new Wallet(updatedGold);
            RefreshHud();
            Autosave();
        }
    }

    private void Autosave()
    {
        if (_clock is null || _wallet is null || _stamina is null || _inventory is null || _storage is null || _farmGrid is null)
        {
            return;
        }

        var snapshot = new SaveGameSnapshot(
            _clock.Date,
            _clock.CurrentMinuteOfDay,
            _wallet.Gold,
            _stamina.Current,
            _inventory.Slots.ToList(),
            _storage.Slots.ToList(),
            _farmGrid.AllPlots.Select(plot => new PlotSnapshot(
                plot.X,
                plot.Y,
                plot.IsTilled,
                plot.IsLocked,
                plot.IsHarvestReady,
                plot.Crop?.CropId,
                plot.Crop?.DaysGrown ?? 0)).ToList(),
            _unlockState.UnlockedPlotKeys.ToList(),
            new List<string>());

        var saveDir = ProjectSettings.GlobalizePath("user://saves");
        Directory.CreateDirectory(saveDir);
        File.WriteAllText(Path.Combine(saveDir, "slot-1.json"), SaveGameStore.Serialize(snapshot));
    }

    private void RefreshHud()
    {
        if (_clock is null || _stamina is null || _wallet is null || _hud is null)
        {
            return;
        }

        _hud.SetDay($"Day {_clock.Date.Day} ({_clock.Date.Season})");
        _hud.SetGold(_wallet.Gold);
        _hud.SetStamina(_stamina.Current, _stamina.Maximum);
        _hud.SetGrowth($"Unlocked plots: {_unlockState.UnlockedPlotKeys.Count}");
    }
}
```

- [ ] **Step 4: Add a smoke checklist for the full milestone**

`docs/testing/milestone-1-smoke-checklist.md`

```markdown
# Harvest Manor Milestone 1 Smoke Checklist

- [ ] Launch the game and confirm the main scene loads.
- [ ] Confirm HUD shows day, gold, stamina, and unlocked plot count.
- [ ] Click a farm plot and verify there is no missing-script error in the console.
- [ ] End the day and confirm the day increments.
- [ ] Verify `user://saves/slot-1.json` is created or updated.
- [ ] Load the request data and confirm the request board scene instantiates.
- [ ] Confirm the town scene, shop interaction, and storage interaction load without missing-resource warnings.
```

- [ ] **Step 5: Run the final suite and manual smoke pass**

Run:

```powershell
dotnet test "D:\game project\harvest-manor\tests\HarvestManor.Game.Tests\HarvestManor.Game.Tests.csproj"
dotnet build "D:\game project\harvest-manor\game\HarvestManor.csproj"
& $env:GODOT4 --path "D:\game project\harvest-manor\game"
```

Expected:

- PASS for the full test suite
- BUILD SUCCEEDED
- manual smoke checklist can be completed without missing-script or save/load regressions

- [ ] **Step 6: Commit**

```bash
git add game/scripts/world/GameBootstrap.cs game/scripts/ui/HudController.cs tests/HarvestManor.Game.Tests/Progression/FarmExpansionServiceTests.cs docs/testing/milestone-1-smoke-checklist.md
git commit -m "feat: add expansion hooks and milestone smoke coverage"
```
