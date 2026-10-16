using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DragonianMix
{
    public class PawnsArrivalModeWorker_DragonianBurrowEdge : PawnsArrivalModeWorker_EdgeDrop
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return DragonianBurrowUtility.IsDragonianFaction(parms.faction) && base.CanUseWith(parms);
        }

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            DragonianBurrowUtility.SpawnBurrow(pawns, parms.spawnCenter, parms);
        }
    }

    public class PawnsArrivalModeWorker_DragonianBurrowCenter : PawnsArrivalModeWorker_CenterDrop
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return DragonianBurrowUtility.IsDragonianFaction(parms.faction) && base.CanUseWith(parms);
        }

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            DragonianBurrowUtility.SpawnBurrow(pawns, parms.spawnCenter, parms);
        }
    }

    public class PawnsArrivalModeWorker_DragonianBurrowEdgeGroups : PawnsArrivalModeWorker_EdgeDropGroups
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return DragonianBurrowUtility.IsDragonianFaction(parms.faction) && base.CanUseWith(parms);
        }

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            List<Pair<List<Pawn>, IntVec3>> groups = PawnsArrivalModeWorkerUtility.SplitIntoRandomGroupsNearMapEdge(
                pawns, (Map)parms.target, arriveInPods: true);
            PawnsArrivalModeWorkerUtility.SetPawnGroupsInfo(parms, groups);
            foreach (Pair<List<Pawn>, IntVec3> group in groups)
            {
                DragonianBurrowUtility.SpawnBurrow(group.First, group.Second, parms);
            }
        }
    }

    public class PawnsArrivalModeWorker_DragonianBurrowRandom : PawnsArrivalModeWorker_RandomDrop
    {
        public override bool CanUseWith(IncidentParms parms)
        {
            return DragonianBurrowUtility.IsDragonianFaction(parms.faction) && base.CanUseWith(parms);
        }

        public override void Arrive(List<Pawn> pawns, IncidentParms parms)
        {
            foreach (Pawn pawn in pawns)
            {
                DragonianBurrowUtility.SpawnBurrow(Gen.YieldSingle(pawn), DropCellFinder.RandomDropSpot((Map)parms.target), parms);
            }
        }
    }
}
