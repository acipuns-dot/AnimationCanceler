using HarmonyLib;
using StardewValley;
using StardewValley.Tools;
using AnimationCanceler.Services;

namespace AnimationCanceler.Patches;

public static class ToolPatches
{
    public static void Apply(Harmony harmony)
    {
        var postfix = new HarmonyMethod(typeof(ToolPatches), nameof(Tool_DoFunction_Postfix));

        var types = new[]
        {
            typeof(Tool),
            typeof(Axe),
            typeof(Pickaxe),
            typeof(Hoe),
            typeof(WateringCan)
        };

        foreach (var type in types)
        {
            var method = type.GetMethod(
                "DoFunction",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance,
                new[]
                {
                    typeof(GameLocation),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(Farmer)
                }
            );

            if (method != null && method.DeclaringType == type)
            {
                harmony.Patch(method, postfix: postfix);
            }
        }
    }

    private static void Tool_DoFunction_Postfix(Farmer who)
    {
        if (who != null && who.IsLocalPlayer)
        {
            AnimationCancelerService.Instance?.OnToolFunctionExecuted(who);
        }
    }
}
