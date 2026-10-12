using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using AnimationCanceler.Services;

namespace AnimationCanceler.Patches;

public struct ToolHitState
{
    public float PreStamina;
    public bool IsEffective;
}

public static class ToolPatches
{
    private static readonly Func<Tool, Vector2, int, Farmer, List<Vector2>>? TilesAffectedFunc =
        AccessTools.MethodDelegate<Func<Tool, Vector2, int, Farmer, List<Vector2>>>(
            AccessTools.Method(typeof(Tool), "tilesAffected", new[] { typeof(Vector2), typeof(int), typeof(Farmer) })
        );

    public static void Apply(Harmony harmony)
    {
        var prefix = new HarmonyMethod(typeof(ToolPatches), nameof(Tool_DoFunction_Prefix));
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
                harmony.Patch(method, prefix: prefix, postfix: postfix);
            }
        }
    }

    private static void Tool_DoFunction_Prefix(
        Tool __instance,
        GameLocation location,
        int x,
        int y,
        int power,
        Farmer who,
        out ToolHitState __state)
    {
        __state = new ToolHitState
        {
            PreStamina = who?.Stamina ?? 0f,
            IsEffective = who != null && IsEffectiveHit(__instance, location, x, y, power, who)
        };
    }

    private static void Tool_DoFunction_Postfix(
        Tool __instance,
        GameLocation location,
        int x,
        int y,
        int power,
        Farmer who,
        ToolHitState __state)
    {
        if (who != null && who.IsLocalPlayer)
        {
            if (AnimationCancelerService.Config?.OnlyDrainStaminaOnHit == true)
            {
                if (!__state.IsEffective)
                {
                    who.Stamina = __state.PreStamina;
                }
            }

            AnimationCancelerService.Instance?.OnToolFunctionExecuted(who);
        }
    }

    private static List<Vector2> GetTilesAffected(Tool tool, Vector2 tileLocation, int power, Farmer who)
    {
        if (TilesAffectedFunc != null)
        {
            try
            {
                return TilesAffectedFunc(tool, tileLocation, power, who);
            }
            catch { }
        }

        return new List<Vector2> { tileLocation };
    }

    private static bool IsEffectiveHit(Tool tool, GameLocation? location, int x, int y, int power, Farmer who)
    {
        if (location == null)
            return true;

        int tileX = x / 64;
        int tileY = y / 64;
        Vector2 tile = new Vector2(tileX, tileY);
        Rectangle tileRect = new Rectangle(tileX * 64, tileY * 64, 64, 64);

        if (tool is Axe)
        {
            if (location.terrainFeatures.ContainsKey(tile))
                return true;

            if (location.largeTerrainFeatures != null)
            {
                foreach (var ltf in location.largeTerrainFeatures)
                {
                    if (ltf.getBoundingBox().Intersects(tileRect))
                        return true;
                }
            }

            if (location.resourceClumps != null)
            {
                foreach (var clump in location.resourceClumps)
                {
                    if (clump.getBoundingBox().Intersects(tileRect))
                        return true;
                }
            }

            if (location.Objects.ContainsKey(tile))
                return true;

            var buildingsLayer = location.Map?.GetLayer("Buildings");
            if (buildingsLayer != null && tileX >= 0 && tileX < buildingsLayer.LayerWidth && tileY >= 0 && tileY < buildingsLayer.LayerHeight)
            {
                var tileObj = buildingsLayer.Tiles[tileX, tileY];
                if (tileObj != null && tileObj.TileIndexProperties.ContainsKey("TreeStump"))
                    return true;
            }

            return false;
        }

        if (tool is Pickaxe)
        {
            Vector2[] candidateTiles =
            {
                tile,
                new Vector2((x - 8) / 64, tileY),
                new Vector2((x + 8) / 64, tileY),
                new Vector2(tileX, (y - 8) / 64),
                new Vector2(tileX, (y + 8) / 64)
            };

            foreach (var candidate in candidateTiles)
            {
                if (location.Objects.ContainsKey(candidate))
                    return true;
            }

            if (location.resourceClumps != null)
            {
                foreach (var clump in location.resourceClumps)
                {
                    if (clump.getBoundingBox().Intersects(tileRect))
                        return true;
                }
            }

            if (location.terrainFeatures.TryGetValue(tile, out var tf))
            {
                if (tf is HoeDirt dirt && dirt.crop == null)
                    return true;
                if (tf is Flooring)
                    return true;
            }

            return false;
        }

        if (tool is Hoe hoe)
        {
            var affected = GetTilesAffected(hoe, tile, power, who);
            foreach (var t in affected)
            {
                int tx = (int)t.X;
                int ty = (int)t.Y;

                if (location.Objects.ContainsKey(t))
                    return true;

                if (location.terrainFeatures.TryGetValue(t, out var tf))
                {
                    if (tf is HoeDirt dirt && dirt.crop != null)
                        return true;
                    continue;
                }

                if (location.doesTileHaveProperty(tx, ty, "Diggable", "Back") != null)
                    return true;
            }

            return false;
        }

        if (tool is WateringCan can)
        {
            if (location.CanRefillWateringCanOnTile(tileX, tileY))
                return true;

            if (can.WaterLeft <= 0 && !who.hasWateringCanEnchantment)
                return false;

            var affected = GetTilesAffected(can, tile, power, who);
            foreach (var t in affected)
            {
                if (location.terrainFeatures.TryGetValue(t, out var tf) && tf is HoeDirt dirt)
                {
                    if (dirt.state.Value == 0)
                        return true;
                }

                if (location.buildings != null)
                {
                    foreach (var building in location.buildings)
                    {
                        if (building.occupiesTile(t))
                            return true;
                    }
                }
            }

            return false;
        }

        return true;
    }
}
