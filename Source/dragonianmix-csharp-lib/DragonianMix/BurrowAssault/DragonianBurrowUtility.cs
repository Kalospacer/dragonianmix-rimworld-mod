using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DragonianMix
{
    [DefOf]
    public static class DragonianBurrowDefOf
    {
        public static FactionDef Dragonian_NPC;
        public static FactionDef Unfrindly_Dragonian;
        public static PawnsArrivalModeDef EdgeDropGroups;
        public static ThingDef DRM_DragonianBurrowSpawner;
        public static PawnsArrivalModeDef DRM_BurrowEdge;
        public static PawnsArrivalModeDef DRM_BurrowEdgeGroups;
        public static PawnsArrivalModeDef DRM_BurrowCenter;
        public static PawnsArrivalModeDef DRM_BurrowRandom;

        static DragonianBurrowDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DragonianBurrowDefOf));
        }
    }

    public static class DragonianBurrowUtility
    {
        public static bool IsDragonianFaction(Faction faction)
        {
            return faction != null && (faction.def == DragonianBurrowDefOf.Dragonian_NPC
                || faction.def == DragonianBurrowDefOf.Unfrindly_Dragonian);
        }

        public static PawnsArrivalModeDef ReplacementFor(PawnsArrivalModeDef mode)
        {
            if (mode == PawnsArrivalModeDefOf.EdgeDrop)
                return DragonianBurrowDefOf.DRM_BurrowEdge;
            if (mode == DragonianBurrowDefOf.EdgeDropGroups)
                return DragonianBurrowDefOf.DRM_BurrowEdgeGroups;
            if (mode == PawnsArrivalModeDefOf.CenterDrop)
                return DragonianBurrowDefOf.DRM_BurrowCenter;
            if (mode == PawnsArrivalModeDefOf.RandomDrop)
                return DragonianBurrowDefOf.DRM_BurrowRandom;
            return null;
        }

        public static void SpawnBurrow(IEnumerable<Pawn> pawns, IntVec3 center, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            // 只找洞附近能站立的格子，不附加屋顶或通往殖民地的可达性限制。
            IntVec3 cell = CellFinder.RandomClosewalkCellNear(center, map, 2);
            DragonianBurrowSpawner burrow = (DragonianBurrowSpawner)ThingMaker.MakeThing(
                DragonianBurrowDefOf.DRM_DragonianBurrowSpawner);
            foreach (Pawn pawn in pawns)
            {
                pawn.Rotation = parms.spawnRotation;
                burrow.AddPawn(pawn);
            }
            GenSpawn.Spawn(burrow, cell, map);
        }
    }
}
