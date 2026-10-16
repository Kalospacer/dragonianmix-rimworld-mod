using HarmonyLib;
using Verse;

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
