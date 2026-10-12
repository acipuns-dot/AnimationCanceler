# Animation Canceler Mod Design Specification

## Overview
A Stardew Valley 1.6 / SMAPI 4.x mod that enables continuous, automated animation canceling while holding a single designated key (default: `Space`). The mod triggers tool and weapon swings, detects the precise moment the action/hit registers, interrupts the recovery frames, and repeats seamlessly as long as the key remains held.

## Requirements & Scope

### Functional Requirements
1. **Single-Key Hold:** While the configured key (`Space` by default) is held down, the mod automatically executes the tool swing or weapon attack and performs animation canceling.
2. **Supported Items:**
   - Gathering Tools: Axe, Pickaxe, Hoe, Watering Can, Scythe (including Golden / Iridium Scythes).
   - Melee Weapons: Swords, Clubs, Daggers.
3. **Stamina Protection:**
   - Configurable minimum stamina threshold (default: `10`).
   - If using a tool that expends stamina and the farmer's stamina is less than or equal to this threshold, auto-swinging halts immediately to prevent exhaustion.
   - Items with zero stamina cost (e.g., Weapons, Scythes) ignore this threshold and continue swinging.
4. **Instant Action Cancellation:**
   - Detects the frame where tool impact or weapon damage occurs.
   - Immediately cancels the remaining animation via `farmer.completelyStopAnimatingOrDoingAction()`, `farmer.forceCanMove()`, and state resets.
   - Loops instantly on the next frame while the key remains held.
5. **Configuration & GMCM:**
   - `config.json` stores user preferences.
   - Integrates with Generic Mod Config Menu (GMCM) if installed, allowing live in-game customization.

### Non-Functional Requirements
- Compatible with Stardew Valley 1.6.x and SMAPI 4.0+.
- Target framework: `.NET 6.0` (`net6.0`).
- No bytecode patching or fragile raw keyboard emulation; driven entirely by SMAPI event loops and game state APIs.
- Multiplayer compatible without desyncing game states.

---

## System Architecture

### Project Layout
```
StardewValley/
├── AnimationCanceler/
│   ├── AnimationCanceler.csproj
│   ├── manifest.json
│   ├── ModConfig.cs
│   ├── ModEntry.cs
│   ├── Services/
│   │   └── AnimationCancelerService.cs
│   └── Integrations/
│       └── IGenericModConfigMenuApi.cs
```

### Components

#### 1. `ModConfig`
Stores persisted user settings:
- `HoldKey`: `SButton` (Default: `SButton.Space`)
- `PreventExhaustion`: `bool` (Default: `true`)
- `MinimumStamina`: `float` (Default: `10f`)
- `CancelTools`: `bool` (Default: `true`)
- `CancelWeapons`: `bool` (Default: `true`)

#### 2. `ModEntry`
- Entry point inheriting from `StardewModdingAPI.Mod`.
- Loads and exposes `ModConfig`.
- Subscribes to `GameLoop.GameLaunched` to register GMCM options.
- Subscribes to `GameLoop.UpdateTicked` to invoke `AnimationCancelerService.Update()`.

#### 3. `AnimationCancelerService`
Coordinates the state machine for tool use and cancellation:
- **Pre-Conditions Check:**
  - `Context.IsWorldReady` and `Context.IsPlayerFree`.
  - Game is not paused (`!Game1.paused`).
  - Farmer is not riding a horse (`!Game1.player.isRidingHorse()`).
  - Activation key is held down (`helper.Input.IsDown(config.HoldKey)`).
- **Item Suitability Check:**
  - Validates `Game1.player.CurrentItem` as a supported `Tool` or `MeleeWeapon`.
  - Enforces stamina check for stamina-draining tools: `player.Stamina > config.MinimumStamina`.
- **Swing Initiation:**
  - If `player.CanMove` and not `player.UsingTool`, triggers tool use using `player.BeginUsingTool()`, `tool.beginUsing(...)`, or weapon swing invocation.
- **Cancel Timing Detection:**
  - Tracks the farmer's sprite frame (`FarmerSprite.currentAnimationIndex`).
  - When the tool impact occurs (typically frame index reaches swing strike, or `player.canReleaseTool` becomes true):
    - Invokes `player.completelyStopAnimatingOrDoingAction()`.
    - Resets `player.UsingTool = false`.
    - Invokes `player.forceCanMove()`.
  - Next tick, `CanMove` is true, repeating the cycle seamlessly.

#### 4. `IGenericModConfigMenuApi`
Provides loose coupling to GMCM:
- Title, descriptions, keybind picker, toggle switches, and a slider (0 to 50) for `MinimumStamina`.

---

## Edge Cases & Handling
1. **Charging Tools (Hoe / Can):** Only standard swings are canceled; charging logic is preserved if the player holds the standard tool use key instead of the cancel key.
2. **Mounts:** Auto-canceling is disabled while riding a horse to prevent state glitches.
3. **Menus & Cutscenes:** `Context.IsPlayerFree` ensures no accidental swings happen during cutscenes, dialogue, or inventory operations.
4. **Weapons vs Tools:** Weapons utilize `MeleeWeapon.setFarmerAnimating` and swipe timing; canceling occurs right after weapon collision/damage registration.

---

## Verification & Testing
1. **Build Verification:** Project compiles under `dotnet build` with zero warnings and produces a valid SMAPI mod package.
2. **SMAPI Load Test:** Mod loads properly in SMAPI without manifest or dependency errors.
3. **In-Game Functionality:**
   - Holding Space with Axe cuts trees with rapid animation cancellation.
   - Holding Space with Pickaxe breaks rocks smoothly.
   - Stamina stops consumption when dropping to 10.
   - Sword swings auto-cancel cleanly without weapon lockup.
   - Releasing Space halts swinging immediately.
