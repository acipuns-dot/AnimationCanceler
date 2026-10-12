using StardewModdingAPI;

namespace AnimationCanceler;

public sealed class ModConfig
{
    public SButton HoldKey { get; set; } = SButton.Space;
    public bool PreventExhaustion { get; set; } = true;
    public float MinimumStamina { get; set; } = 10f;
    public bool OnlyDrainStaminaOnHit { get; set; } = true;
    public bool CancelTools { get; set; } = true;
    public bool CancelWeapons { get; set; } = true;
}
