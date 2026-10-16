using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DragonianMix
{
    public class RoyalTitlePermitWorker_DRM_OrbitalStrike : RoyalTitlePermitWorker_Targeted
    {
        private Faction faction;

        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(caller.Position, RangeClamped, Color.white);
            GenDraw.DrawRadiusRing(target.Cell, def.royalAid.radius + def.royalAid.explosionRadiusRange.max, Color.white);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }

        public override void OrderForceTarget(LocalTargetInfo target)
        {
            ThingDef bombardmentDef = def.GetModExtension<PermitBombardmentExtension>().bombardmentDef;
            Bombardment bombardment = (Bombardment)GenSpawn.Spawn(bombardmentDef, target.Cell, map);
            bombardment.impactAreaRadius = def.royalAid.radius;
            bombardment.explosionRadiusRange = def.royalAid.explosionRadiusRange;
            bombardment.bombIntervalTicks = def.royalAid.intervalTicks;
            bombardment.randomFireRadius = 1;
            bombardment.explosionCount = def.royalAid.explosionCount;
            bombardment.warmupTicks = def.royalAid.warmupTicks;
            bombardment.instigator = caller;
            SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera();
            caller.royalty.GetPermit(def, faction).Notify_Used();
            if (!free)
            {
                caller.royalty.TryRemoveFavor(faction, def.royalAid.favorCost);
            }
        }

        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (AidDisabled_NewTemp(map, pawn, faction, out string reason, temperatureMatters: false))
            {
                yield return new FloatMenuOption(def.LabelCap + ": " + reason, null);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer))
            {
                yield return new FloatMenuOption(def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null);
                yield break;
            }
            string description = def.LabelCap + ": ";
            Action action = null;
            if (FillAidOption(pawn, faction, ref description, out bool free))
            {
                action = () => BeginCallBombardment(pawn, faction, map, free);
            }
            yield return new FloatMenuOption(description, action, faction.def.FactionIcon, faction.Color);
        }

        private void BeginCallBombardment(Pawn caller, Faction faction, Map map, bool free)
        {
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            float range = RangeClamped;
            targetingParameters = new TargetingParameters
            {
                canTargetLocations = true,
                canTargetSelf = true,
                canTargetFires = true,
                canTargetItems = true,
                validator = target =>
                {
                    if ((range > 0f && target.Cell.DistanceTo(caller.Position) > range) || target.Cell.Fogged(map))
                    {
                        return false;
                    }
                    RoofDef roof = target.Cell.GetRoof(map);
                    return roof == null || !roof.isThickRoof;
                }
            };
            Find.Targeter.BeginTargeting(this);
        }
    }
}
