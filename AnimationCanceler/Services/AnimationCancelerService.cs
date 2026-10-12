using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;

namespace AnimationCanceler.Services;

public sealed class AnimationCancelerService
{
    public static AnimationCancelerService? Instance { get; private set; }

    private readonly ModConfig _config;
    private readonly IModHelper _helper;
    private readonly IMonitor _monitor;

    public AnimationCancelerService(ModConfig config, IModHelper helper, IMonitor monitor)
    {
        _config = config;
        _helper = helper;
        _monitor = monitor;
        Instance = this;
    }

    public void OnToolFunctionExecuted(Farmer player)
    {
        if (!_config.CancelTools)
            return;

        if (!_helper.Input.IsDown(_config.HoldKey))
            return;

        // Tool impact has just landed and DoFunction has completed.
        // Immediately cut the post-impact recovery frames.
        player.completelyStopAnimatingOrDoingAction();
        player.forceCanMove();
        player.UsingTool = false;
    }

    public void Update()
    {
        if (!Context.IsWorldReady || !Context.IsPlayerFree)
            return;

        if (Game1.paused || Game1.activeClickableMenu != null || Game1.eventUp)
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
        bool isScythe = isWeapon && ((MeleeWeapon)tool).isScythe();

        if (isWeapon && !isScythe && !_config.CancelWeapons)
            return;
        if ((!isWeapon || isScythe) && !_config.CancelTools)
            return;

        // Stamina protection: weapons and scythes consume 0 stamina
        if (_config.PreventExhaustion && !isWeapon && !isScythe)
        {
            if (player.Stamina <= _config.MinimumStamina)
                return;
        }

        if (player.UsingTool)
        {
            int animIndex = player.FarmerSprite.currentAnimationIndex;

            if (isWeapon && !isScythe)
            {
                // Weapons deal damage on frame index 2; cancel once swipe damage phase completes
                if (animIndex >= 2)
                {
                    player.completelyStopAnimatingOrDoingAction();
                    player.forceCanMove();
                    player.UsingTool = false;
                }
            }
            else
            {
                // Tool hit is primarily canceled via Tool.DoFunction Harmony postfix.
                // Fallback: If animation reaches recovery frame (index >= 3), cancel.
                if (animIndex >= 3)
                {
                    player.completelyStopAnimatingOrDoingAction();
                    player.forceCanMove();
                    player.UsingTool = false;
                }
            }
        }
        else if (player.CanMove)
        {
            // Initiate the swing on the next available tick
            Game1.pressUseToolButton();
        }
    }
}
