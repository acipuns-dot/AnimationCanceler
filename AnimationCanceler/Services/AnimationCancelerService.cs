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

        // Cancel if currently swinging and hit has registered
        if (player.UsingTool)
        {
            TryCancelToolAnimation(player, isWeapon && !isScythe);
        }
        else if (player.CanMove)
        {
            // Initiate the swing on the next available tick
            Game1.pressUseToolButton();
        }
    }

    private void TryCancelToolAnimation(Farmer player, bool isActualWeapon)
    {
        int animIndex = player.FarmerSprite.currentAnimationIndex;

        // For tools, canReleaseTool becomes true once DoFunction/hit has executed.
        // For weapons, animation index >= 2 is post-damage hitbox activation.
        bool hitHasLanded = isActualWeapon
            ? animIndex >= 2
            : player.canReleaseTool || animIndex >= 1;

        if (hitHasLanded)
        {
            player.completelyStopAnimatingOrDoingAction();
            player.forceCanMove();
            player.UsingTool = false;
        }
    }
}
