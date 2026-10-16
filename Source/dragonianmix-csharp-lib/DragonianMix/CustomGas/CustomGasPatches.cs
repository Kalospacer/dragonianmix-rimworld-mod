using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    /// <summary>
    /// 把自定义气体的悬停提示插进原版 MouseoverReadout 的气体显示块里。
    /// 原版 DrawGas 是 private 且 num 是局部变量，借 Postfix 在 DeadlifeDust 行之后追加自定义气体行。
    /// </summary>
    /// <summary>让原版 AnyGasAt 也把自定义气体算进去，保证悬停块显示。</summary>
    [HarmonyPatch(typeof(GasGrid), nameof(GasGrid.AnyGasAt), new[] { typeof(IntVec3) })]
    public static class Patch_GasGrid_AnyGasAt
    {
        public static void Postfix(GasGrid __instance, IntVec3 cell, ref bool __result)
        {
            if (__result) return;
            // 原版 4 种都没有才查自定义气体；自定义气体 grid 也要按 cell 判断
            // 注意：GasGrid.map 是 private，用反射拿
            Map map = mapRef(__instance);
            CustomGasGrid grid = map?.CustomGasGrid();
            if (grid != null && grid.AnyGasAt(cell))
            {
                __result = true;
            }
        }

        private static readonly AccessTools.FieldRef<GasGrid, Map> mapRef = AccessTools.FieldRefAccess<GasGrid, Map>("map");
    }

    [HarmonyPatch(typeof(MouseoverReadout), "DrawGas")]
    public static class Patch_MouseoverReadout_DrawGas
    {
        public static void Postfix(GasType gasType, byte density, ref float curYOffset)
        {
            if (gasType != GasType.DeadlifeDust) return;
            CustomGasGrid grid = Find.CurrentMap?.CustomGasGrid();
            if (grid == null) return;
            IntVec3 cell = UI.MouseCell();
            foreach (CustomGasTypeDef gas in DefDatabase<CustomGasTypeDef>.AllDefsListForReading)
            {
                byte d = grid.DensityAt(cell, gas);
                if (d <= 0) continue;
                // 与原版 BotLeft = (15, 65) 一致
                Rect rect = new Rect(15f, (float)UI.screenHeight - 65f - curYOffset, 999f, 999f);
                Widgets.Label(rect, gas.LabelCap + " " + ((float)d / 255f).ToStringPercent("F0"));
                curYOffset += 19f;
            }
        }
    }
}

namespace DragonianMix
{
    /// <summary>
    /// 把原版气体网格的生命周期钩子转发给 CustomGasGrid，
    /// 使自定义气体与原版气体共享暴露节奏、建筑清空、通风口平衡与调试清空。
    /// </summary>
    [HarmonyPatch(typeof(GasUtility), nameof(GasUtility.PawnGasEffectsTickInterval), new[] { typeof(Pawn), typeof(int) })]
    public static class Patch_GasUtility_PawnGasEffectsTickInterval
    {
        public static void Postfix(Pawn pawn, int delta)
        {
            if (pawn.Spawned && pawn.IsHashIntervalTick(50, delta))
            {
                pawn.Map.CustomGasGrid()?.ApplyPawnEffects(pawn);
            }
        }
    }

    [HarmonyPatch(typeof(GasGrid), nameof(GasGrid.Notify_ThingSpawned), new[] { typeof(Thing) })]
    public static class Patch_GasGrid_Notify_ThingSpawned
    {
        public static void Postfix(Thing thing)
        {
            thing.Map?.CustomGasGrid()?.Notify_ThingSpawned(thing);
        }
    }

    [HarmonyPatch(typeof(GasGrid), nameof(GasGrid.EqualizeGasThroughBuilding), new[] { typeof(Building), typeof(bool) })]
    public static class Patch_GasGrid_EqualizeGasThroughBuilding
    {
        public static void Postfix(Building b, bool twoWay)
        {
            b.Map?.CustomGasGrid()?.EqualizeGasThroughBuilding(b, twoWay);
        }
    }

    [HarmonyPatch(typeof(GasGrid), nameof(GasGrid.ClearCellUnsafe), new[] { typeof(IntVec3) })]
    public static class Patch_GasGrid_ClearCellUnsafe
    {
        private static readonly AccessTools.FieldRef<GasGrid, Map> mapRef = AccessTools.FieldRefAccess<GasGrid, Map>("map");

        public static void Postfix(GasGrid __instance, IntVec3 cell)
        {
            mapRef(__instance)?.CustomGasGrid()?.ClearCellUnsafe(cell);
        }
    }

    [HarmonyPatch(typeof(GasGrid), nameof(GasGrid.Debug_ClearAll))]
    public static class Patch_GasGrid_Debug_ClearAll
    {
        private static readonly AccessTools.FieldRef<GasGrid, Map> mapRef = AccessTools.FieldRefAccess<GasGrid, Map>("map");

        public static void Postfix(GasGrid __instance)
        {
            Map map = mapRef(__instance);
            CustomGasGrid grid = map?.CustomGasGrid();
            if (grid != null)
            {
                grid.ClearAll();
                map.mapDrawer.WholeMapChanged(RimWorld.MapMeshFlagDefOf.Gas);
            }
        }
    }
}
