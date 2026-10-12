# Animation Canceler (Stardew Valley Mod)

A fast, seamless, and customizable **Animation Canceler** mod for **Stardew Valley 1.6+** and **SMAPI 4.x**.

Hold a single key (default: `Space`) to automatically execute rapid tool swings and weapon attacks with zero post-impact animation lag.

---

## ✨ Features

- **Hold-to-Cancel:** Hold `Space` (or your configured key) to automatically attack or swing tools with instant animation canceling.
- **Upgraded Tool Support:** Seamlessly swings upgraded tools (Copper, Steel, Gold, Iridium) without getting stuck in the charging animation.
- **Smart Stamina Protection:**
  - **Exhaustion Guard:** Stops auto-swinging before you collapse (configurable threshold, default: 10 stamina).
  - **Miss Protection:** Only consumes stamina when actually hitting valid targets (trees, rocks, dirt, weeds, etc.). Swinging at empty air or missing refunds your energy.
- **Broad Tool & Weapon Support:**
  - Axes, Pickaxes, Hoes, and Watering Cans.
  - Melee Weapons (Swords, Clubs, Daggers) and Scythes.
- **Generic Mod Config Menu (GMCM) Support:** Change keybinds, thresholds, and toggles directly in-game.

---

## 📦 Requirements

- **Stardew Valley 1.6+**
- **SMAPI 4.0+**
- *(Optional)* [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) for in-game configuration.

---

## 🛠️ Installation

1. Install [SMAPI](https://smapi.io/).
2. Download the latest release from the [Releases](https://github.com/acipuns-dot/AnimationCanceler/releases) page.
3. Unzip the `AnimationCanceler` folder into your `Stardew Valley/Mods/` directory:
   - **Windows (Steam):** `C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods\`
4. Launch the game using SMAPI!

---

## ⚙️ Configuration

You can configure options in-game via **Generic Mod Config Menu** or by editing `config.json` after running the game once:

```json
{
  "HoldKey": "Space",
  "PreventExhaustion": true,
  "MinimumStamina": 10.0,
  "OnlyDrainStaminaOnHit": true,
  "CancelTools": true,
  "CancelWeapons": true
}
```

| Option | Default | Description |
|---|---|---|
| `HoldKey` | `"Space"` | Key to hold for auto-swing and animation canceling. |
| `PreventExhaustion` | `true` | Stops swinging when stamina drops to or below the minimum threshold. |
| `MinimumStamina` | `10.0` | Stamina threshold below which swinging halts. |
| `OnlyDrainStaminaOnHit` | `true` | Prevents stamina loss when swinging at empty air or missing targets. |
| `CancelTools` | `true` | Enables animation canceling for gathering tools (Axe, Pickaxe, Hoe, Watering Can). |
| `CancelWeapons` | `true` | Enables animation canceling for melee weapons (Swords, Clubs, Daggers). |

---

## 💻 Building from Source

1. Clone this repository:
   ```bash
   git clone https://github.com/acipuns-dot/AnimationCanceler.git
   ```
2. Build with .NET 6 SDK:
   ```bash
   dotnet build -c Release
   ```
   The build automatically packages the mod to your local Stardew Valley mods directory.

---

## 📄 License

MIT License. Feel free to use, modify, and distribute.
