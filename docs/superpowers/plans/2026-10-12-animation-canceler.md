# Animation Canceler Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a Stardew Valley 1.6 / SMAPI 4.x mod that performs continuous, automated tool and weapon animation canceling while holding the Space bar (or configured key).

**Architecture:** Pure SMAPI event-driven state machine listening to `UpdateTicked`. When the hold key is pressed, it initiates the tool/weapon action, monitors the sprite animation until the impact/hit frame executes, instantly interrupts the recovery animation via `completelyStopAnimatingOrDoingAction()`, and loops cleanly on the next frame while enforcing a 10 stamina protection buffer.

**Tech Stack:** C# 10, .NET 6.0 (`net6.0`), SMAPI 4.x, `Pathoschild.Stardew.ModBuildConfig` (4.4.1), Generic Mod Config Menu API.

**Spec:** [docs/superpowers/specs/2026-10-12-animation-canceler-design.md](file:///c:/Users/User/Documents/Discord%20project/StardewValley/docs/superpowers/specs/2026-10-12-animation-canceler-design.md)

## Global Constraints

- Target Framework: `net6.0`
- SMAPI Minimum Version: `4.0.0`
- Stardew Valley Version: `1.6.0+`
- Package: `Pathoschild.Stardew.ModBuildConfig` (version `4.4.1` or compatible 4.x)
- Default Hold Key: `SButton.Space`
- Default Minimum Stamina: `10.0`
- No bytecode patching or Harmony transpilers; use SMAPI lifecycle events and game methods.

---

### Task 1: Project Scaffolding & SMAPI Manifest

**Files:**
- Create: `AnimationCanceler/AnimationCanceler.csproj`
- Create: `AnimationCanceler/manifest.json`

**Interfaces:**
- Produces: Base project structure and SMAPI mod metadata recognized by .NET SDK and SMAPI.

- [ ] **Step 1: Create `manifest.json`**

```json
{
  "Name": "Animation Canceler",
  "Author": "User",
  "Version": "1.0.0",
  "Description": "Automatically cancels tool and weapon swing animations while holding the Space bar.",
  "UniqueID": "User.AnimationCanceler",
  "EntryDll": "AnimationCanceler.dll",
  "MinimumApiVersion": "4.0.0",
  "UpdateKeys": []
}
```

- [ ] **Step 2: Create `AnimationCanceler.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableHarmony>false</EnableHarmony>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Pathoschild.Stardew.ModBuildConfig" Version="4.4.1" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Run build to verify project scaffolding compiles**

Run: `dotnet build AnimationCanceler/AnimationCanceler.csproj`
Expected: Build succeeds (or reports missing ModEntry, which will be added in Task 4).

- [ ] **Step 4: Commit**

```bash
git add AnimationCanceler/AnimationCanceler.csproj AnimationCanceler/manifest.json
git commit -m "chore: scaffold AnimationCanceler project and manifest"
```

---

### Task 2: Configuration Model & GMCM API Interface

**Files:**
- Create: `AnimationCanceler/ModConfig.cs`
- Create: `AnimationCanceler/Integrations/IGenericModConfigMenuApi.cs`

**Interfaces:**
- Produces: `ModConfig` class with `HoldKey`, `PreventExhaustion`, `MinimumStamina`, `CancelTools`, `CancelWeapons`.
- Produces: `IGenericModConfigMenuApi` interface for optional in-game configuration UI.

- [ ] **Step 1: Create `ModConfig.cs`**

```csharp
using StardewModdingAPI;

namespace AnimationCanceler;

public sealed class ModConfig
{
    public SButton HoldKey { get; set; } = SButton.Space;
    public bool PreventExhaustion { get; set; } = true;
    public float MinimumStamina { get; set; } = 10f;
    public bool CancelTools { get; set; } = true;
    public bool CancelWeapons { get; set; } = true;
}
```

- [ ] **Step 2: Create `Integrations/IGenericModConfigMenuApi.cs`**

```csharp
using System;
using StardewModdingAPI;

namespace AnimationCanceler.Integrations;

public interface IGenericModConfigMenuApi
{
    void Register(
        IManifest mod,
        Action reset,
        Action save,
        bool titleScreenOnly = false
    );

    void AddSectionTitle(
        IManifest mod,
        Func<string> text,
        Func<string>? tooltip = null
    );

    void AddKeybind(
        IManifest mod,
        Func<SButton> getValue,
        Action<SButton> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null
    );

    void AddBoolOption(
        IManifest mod,
        Func<bool> getValue,
        Action<bool> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        string? fieldId = null
    );

    void AddNumberOption(
        IManifest mod,
        Func<float> getValue,
        Action<float> setValue,
        Func<string> name,
        Func<string>? tooltip = null,
        float? min = null,
        float? max = null,
        float? interval = null,
        Func<float, string>? formatValue = null,
        string? fieldId = null
    );
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build AnimationCanceler/AnimationCanceler.csproj`
Expected: Build passes.

- [ ] **Step 4: Commit**

```bash
git add AnimationCanceler/ModConfig.cs AnimationCanceler/Integrations/IGenericModConfigMenuApi.cs
git commit -m "feat: add ModConfig and GMCM API interface"
```

---

### Task 3: Animation Canceler Core Engine

**Files:**
- Create: `AnimationCanceler/Services/AnimationCancelerService.cs`

**Interfaces:**
- Consumes: `ModConfig`, `IModHelper`, `IMonitor`.
- Produces: `AnimationCancelerService.Update()` called on every game tick.

- [ ] **Step 1: Implement `AnimationCancelerService.cs`**

```csharp
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace AnimationCanceler.Services;

public sealed class AnimationCancelerService
{
    private readonly ModConfig _config;
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;

    public AnimationCancelerService(ModConfig config, IModHelper helper, IMonitor monitor)
    {
        _config = config;
        _helper = helper;
        _monitor = monitor;
    }

    public void Update()
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        if (Game1.paused || Game1.activeClickableMenu != null)
            return;

        Farmer player = Game1.player;
        if (player.isRidingHorse())
            return;

        if (!_helper.Input.IsDown(_config.HoldKey))
            return;

        Item? currentItem = player.CurrentItem;
        if (currentItem is not Tool tool)
            return;

        bool isWeapon = tool is MeleeWeapon;
        if (isWeapon && !_config.CancelWeapons)
            return;
        if (!isWeapon && !_config.CancelTools)
            return;

        // Stamina check
        if (_config.PreventExhaustion && !isWeapon && !(tool is Scythe))
        {
            if (player.Stamina <= _config.MinimumStamina)
                return;
        }

        // Check if currently performing action
        if (player.UsingTool)
        {
            TryCancelToolAnimation(player, tool, isWeapon);
        }
        else if (player.CanMove)
        {
            // Initiate the swing
            TriggerToolSwing(player, tool, isWeapon);
        }
    }

    private void TriggerToolSwing(Farmer player, Tool tool, bool isWeapon)
    {
        if (isWeapon && tool is MeleeWeapon weapon)
        {
            if (weapon.type.Value == MeleeWeapon.dagger)
            {
                // Daggers swing directly
                weapon.DoFloorClutterReset();
                player.beginUsingTool();
            }
            else
            {
                player.beginUsingTool();
            }
        }
        else
        {
            player.beginUsingTool();
        }
    }

    private void TryCancelToolAnimation(Farmer player, Tool tool, bool isWeapon)
    {
        int frameIndex = player.FarmerSprite.currentAnimationIndex;

        // Check if the hit/action frame has executed
        // In Stardew Valley:
        // Tools swing across frames (typically 0 -> 1 -> hit -> 2 -> recover).
        // Once player.canReleaseTool is true or animation reaches recovery index, action has triggered.
        bool hitHasLanded = player.canReleaseTool || frameIndex >= 1;

        if (isWeapon)
        {
            // For weapons, animation index >= 2 is post-damage hitbox activation
            hitHasLanded = frameIndex >= 2;
        }

        if (hitHasLanded)
        {
            player.completelyStopAnimatingOrDoingAction();
            player.UsingTool = false;
            player.forceCanMove();
        }
    }
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build AnimationCanceler/AnimationCanceler.csproj`
Expected: Build passes.

- [ ] **Step 3: Commit**

```bash
git add AnimationCanceler/Services/AnimationCancelerService.cs
git commit -m "feat: implement animation canceler service"
```

---

### Task 4: ModEntry & Event Registration

**Files:**
- Create: `AnimationCanceler/ModEntry.cs`

**Interfaces:**
- Consumes: `AnimationCancelerService`, `ModConfig`, `IGenericModConfigMenuApi`.
- Produces: SMAPI Mod entry point registering events and GMCM options.

- [ ] **Step 1: Implement `ModEntry.cs`**

```csharp
using AnimationCanceler.Integrations;
using AnimationCanceler.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace AnimationCanceler;

public sealed class ModEntry : Mod
{
    private ModConfig _config = null!;
    private AnimationCancelerService _cancelerService = null!;

    public override void Entry(IModHelper helper)
    {
        _config = helper.ReadConfig<ModConfig>();
        _cancelerService = new AnimationCancelerService(_config, helper, Monitor);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        _cancelerService.Update();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        RegisterGenericModConfigMenu();
    }

    private void RegisterGenericModConfigMenu()
    {
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        configMenu.Register(
            mod: ModManifest,
            reset: () => _config = new ModConfig(),
            save: () => Helper.WriteConfig(_config)
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => "Keybinds"
        );

        configMenu.AddKeybind(
            mod: ModManifest,
            name: () => "Hold Activation Key",
            tooltip: () => "Hold this key to automatically swing and cancel animations.",
            getValue: () => _config.HoldKey,
            setValue: value => _config.HoldKey = value
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => "Stamina Protection"
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Prevent Exhaustion",
            tooltip: () => "Stop auto-swinging when stamina drops below the minimum threshold.",
            getValue: () => _config.PreventExhaustion,
            setValue: value => _config.PreventExhaustion = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Minimum Stamina Threshold",
            tooltip: () => "The stamina value at which tool swinging halts (default: 10).",
            getValue: () => _config.MinimumStamina,
            setValue: value => _config.MinimumStamina = value,
            min: 0f,
            max: 50f,
            interval: 1f
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => "Toggles"
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Cancel Tools",
            tooltip: () => "Enable animation canceling for gathering tools (Axe, Pickaxe, Hoe, Watering Can, Scythe).",
            getValue: () => _config.CancelTools,
            setValue: value => _config.CancelTools = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Cancel Weapons",
            tooltip: () => "Enable animation canceling for melee weapons (Swords, Clubs, Daggers).",
            getValue: () => _config.CancelWeapons,
            setValue: value => _config.CancelWeapons = value
        );
    }
}
```

- [ ] **Step 2: Build the complete project in Release mode**

Run: `dotnet build AnimationCanceler/AnimationCanceler.csproj -c Release`
Expected: Build succeeds with 0 errors.

- [ ] **Step 3: Commit**

```bash
git add AnimationCanceler/ModEntry.cs
git commit -m "feat: implement ModEntry and GMCM integration"
```

---

### Task 5: Game Deployment & Verification

**Files:**
- Output directory: `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\AnimationCanceler`

**Interfaces:**
- Produces: Deployed mod in the local Stardew Valley Mods folder ready for gameplay.

- [ ] **Step 1: Deploy built mod to game directory**

Copy `AnimationCanceler/bin/Release/net6.0/AnimationCanceler/` (or debug build output) to `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\AnimationCanceler`.

- [ ] **Step 2: Verify deployed files**

Check that `manifest.json`, `AnimationCanceler.dll`, and `AnimationCanceler.pdb` are present in the mods folder.

- [ ] **Step 3: Commit any final project files and git status**

```bash
git add .
git commit -m "chore: finish mod deployment setup"
```
