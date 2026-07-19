using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using System;
using UnityEngine;

namespace DragonianMix
{

    [StaticConstructorOnStartup]
    public class RoyalTitlePermitWorker_DropResources : RoyalTitlePermitWorker_Targeted
    {
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallResources(target.Cell);
        }

        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer))
            {
                yield return new FloatMenuOption("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            Action action = null;
            string label = this.def.LabelCap + ": ";
            bool free;
            if (base.FillAidOption(pawn, faction, ref label, out free))
            {
                action = delegate ()
                {
                    this.BeginCallResources(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(label, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Pawn pawn, Faction faction)
        {
            string defaultDesc;
            bool flag;
            if (!base.FillCaravanAidOption(pawn, faction, out defaultDesc, out this.free, out flag))
            {
                yield break;
            }

            Action cachedAction = null; // 声明一个合法的委托变量

            Command_Action command_Action = new Command_Action
            {
                defaultLabel = this.def.LabelCap + " (" + pawn.LabelShort + ")",
                defaultDesc = defaultDesc,
                icon = RoyalTitlePermitWorker_DropResources.CommandTex,
                action = delegate ()
                {
                    Caravan caravan = pawn.GetCaravan();
                    float num = caravan.MassUsage;
                    List<ThingDefCountClass> itemsToDrop = this.def.royalAid.itemsToDrop;
                    for (int i = 0; i < itemsToDrop.Count; i++)
                    {
                        num += itemsToDrop[i].thingDef.BaseMass * (float)itemsToDrop[i].count;
                    }
                    if (num > caravan.MassCapacity)
                    {
                        WindowStack windowStack = Find.WindowStack;
                        TaggedString text = "DropResourcesOverweightConfirm".Translate();
                        Action confirmedAct;
                        if ((confirmedAct = cachedAction) == null)
                        {
                            // 将委托实例赋值给cachedAction以避免重复创建
                            confirmedAct = cachedAction = delegate ()
                            {
                                this.CallResourcesToCaravan(pawn, faction, this.free);
                            };
                        }
                        windowStack.Add(Dialog_MessageBox.CreateConfirmation(text, confirmedAct, true, null, WindowLayer.Dialog));
                        return;
                    }
                    this.CallResourcesToCaravan(pawn, faction, this.free);
                }
            };

            if (pawn.MapHeld != null && pawn.MapHeld.generatorDef.isUnderground)
            {
                command_Action.Disable("CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")));
            }
            if (faction.HostileTo(Faction.OfPlayer))
            {
                command_Action.Disable("CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")));
            }
            if (flag)
            {
                command_Action.Disable("CommandCallRoyalAidNotEnoughFavor".Translate());
            }
            yield return command_Action;
        }


        private void BeginCallResources(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetBuildings = false;
            this.targetingParameters.canTargetPawns = false;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            float rangeActual = base.RangeClamped;
            this.targetingParameters.validator = ((TargetInfo target) => (rangeActual <= 0f || target.Cell.DistanceTo(caller.Position) <= rangeActual) && !target.Cell.Fogged(map) && DropCellFinder.CanPhysicallyDropInto(target.Cell, map, true, true));
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }

        private void CallResources(IntVec3 cell)
        {
            List<Thing> list = new List<Thing>();
            for (int i = 0; i < this.def.royalAid.itemsToDrop.Count; i++)
            {
                Thing thing = ThingMaker.MakeThing(this.def.royalAid.itemsToDrop[i].thingDef, this.def.royalAid.itemsToDrop[i].stuff);
                thing.stackCount = this.def.royalAid.itemsToDrop[i].count;
                list.Add(thing);
            }
            if (list.Any<Thing>())
            {
                ActiveTransporterInfo activeTransporterInfo = new ActiveTransporterInfo();
                activeTransporterInfo.innerContainer.TryAddRangeOrTransfer(list, true, false);
                DropPodUtility.MakeDropPodAt(cell, this.map, activeTransporterInfo, null);
                Messages.Message("MessagePermitTransportDrop".Translate(this.faction.Named("FACTION")), new LookTargets(cell, this.map), MessageTypeDefOf.NeutralEvent, true);
                this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
                if (!this.free)
                {
                    this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
                }
            }
        }
        private void CallResourcesToCaravan(Pawn caller, Faction faction, bool free)
        {
            Caravan caravan = caller.GetCaravan();
            for (int i = 0; i < this.def.royalAid.itemsToDrop.Count; i++)
            {
                Thing thing = ThingMaker.MakeThing(this.def.royalAid.itemsToDrop[i].thingDef, this.def.royalAid.itemsToDrop[i].stuff);
                thing.stackCount = this.def.royalAid.itemsToDrop[i].count;
                CaravanInventoryUtility.GiveThing(caravan, thing);
            }
            Messages.Message("MessagePermitTransportDropCaravan".Translate(faction.Named("FACTION"), caller.Named("PAWN")), caravan, MessageTypeDefOf.NeutralEvent, true);
            caller.royalty.GetPermit(this.def, faction).Notify_Used();
            if (!free)
            {
                caller.royalty.TryRemoveFavor(faction, this.def.royalAid.favorCost);
            }
        }

        private Faction faction;

        private static readonly Texture2D CommandTex = ContentFinder<Texture2D>.Get("UI/Commands/CallAid", true);
    }

}

