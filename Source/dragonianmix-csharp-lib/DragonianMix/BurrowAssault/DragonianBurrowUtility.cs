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

        static DragonianBurrowDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DragonianBurrowDefOf));
        }
    }

    internal enum BurrowArrivalKind
    {
        None,
        Edge,
        Center,
        EdgeGroups,
        Random
    }

    internal static class DragonianBurrowUtility
    {
        internal static BurrowArrivalKind GetArrivalKind(IncidentParms parms)
        {
            Faction faction = parms.faction;
            if (faction == null || (faction.def != DragonianBurrowDefOf.Dragonian_NPC
                && faction.def != DragonianBurrowDefOf.Unfrindly_Dragonian))
            {
                return BurrowArrivalKind.None;
            }

            PawnsArrivalModeDef mode = parms.raidArrivalMode;
            if (mode == PawnsArrivalModeDefOf.EdgeDrop)
                return BurrowArrivalKind.Edge;
            if (mode == PawnsArrivalModeDefOf.CenterDrop)
                return BurrowArrivalKind.Center;
            if (mode == DragonianBurrowDefOf.EdgeDropGroups)
                return BurrowArrivalKind.EdgeGroups;
            if (mode == PawnsArrivalModeDefOf.RandomDrop)
                return BurrowArrivalKind.Random;
            return BurrowArrivalKind.None;
        }

        internal static bool TryArrive(List<Pawn> pawns, IncidentParms parms)
        {
            BurrowArrivalKind kind = GetArrivalKind(parms);
            if (kind == BurrowArrivalKind.None)
            {
                return false;
            }
            if (pawns.Count == 0)
            {
                return true;
            }

            Map map = (Map)parms.target;
            switch (kind)
            {
                case BurrowArrivalKind.Edge:
                case BurrowArrivalKind.Center:
                    SpawnBurrow(pawns, parms.spawnCenter, parms);
                    break;
                case BurrowArrivalKind.EdgeGroups:
                    List<Pair<List<Pawn>, IntVec3>> groups = PawnsArrivalModeWorkerUtility.SplitIntoRandomGroupsNearMapEdge(
                        pawns, map, arriveInPods: true);
                    PawnsArrivalModeWorkerUtility.SetPawnGroupsInfo(parms, groups);
                    foreach (Pair<List<Pawn>, IntVec3> group in groups)
                    {
                        SpawnBurrow(group.First, group.Second, parms);
                    }
                    break;
                case BurrowArrivalKind.Random:
                    foreach (Pawn pawn in pawns)
                    {
                        SpawnBurrow(Gen.YieldSingle(pawn), DropCellFinder.RandomDropSpot(map), parms);
                    }
                    break;
            }
            return true;
        }

        private static void SpawnBurrow(IEnumerable<Pawn> pawns, IntVec3 center, IncidentParms parms)
        {
            Map map = (Map)parms.target;
            // 只沿用洞附近的站位搜索，不新增屋顶或通往殖民地的可达性限制。
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
