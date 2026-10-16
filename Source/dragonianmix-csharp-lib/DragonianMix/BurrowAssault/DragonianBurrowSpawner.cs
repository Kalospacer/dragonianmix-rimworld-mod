using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DragonianMix
{
    /// <summary>
    /// 龙娘破土突袭 spawner：复用原版 GroundSpawner 钻地特效与延迟，
    /// 用 ThingOwner 容器持有待出兵（同原版 PawnGroundSpawner 机制，但支持多兵）。
    /// </summary>
    public class DragonianBurrowSpawner : GroundSpawner, IThingHolder
    {
        private ThingOwner<Pawn> pawns;

        public DragonianBurrowSpawner()
        {
            pawns = new ThingOwner<Pawn>(this);
        }

        public void AddPawn(Pawn pawn)
        {
            pawns.TryAddOrTransfer(pawn);
        }

        public ThingOwner GetDirectlyHeldThings() => pawns;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, pawns);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref pawns, "pawns", this);
        }

        protected override void Spawn(Map map, IntVec3 loc)
        {
            // 基类先 Destroy 再回调；TryDropAll 会从容器移走已出土的兵，保留原版 Lord。
            pawns.TryDropAll(loc, map, ThingPlaceMode.Near, null, null, playDropSound: false);
        }
    }
}
