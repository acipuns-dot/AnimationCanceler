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

        var harmony = new HarmonyLib.Harmony(ModManifest.UniqueID);
        Patches.ToolPatches.Apply(harmony);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

        Monitor.Log("Animation Canceler initialized with Harmony patches.", LogLevel.Info);
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

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Only Drain Stamina On Hit",
            tooltip: () => "Refunds stamina when tool swings hit empty air or miss valid targets.",
            getValue: () => _config.OnlyDrainStaminaOnHit,
            setValue: value => _config.OnlyDrainStaminaOnHit = value
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

        Monitor.Log("Generic Mod Config Menu integrated successfully.", LogLevel.Debug);
    }
}
