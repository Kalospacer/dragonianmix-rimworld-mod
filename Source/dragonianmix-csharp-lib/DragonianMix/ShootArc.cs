using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DragonianMix
{
    public class VerbProperties_Arc : VerbProperties
    {
        public DamageDef damageDef;
        public float EMPDamageAmount = -1f;
        public int damageAmount = -1;
        public float armorPenetration = -1f;
        public float affectedAngle;
        public bool isConductible;
        public int conductNum;
        public bool conductFriendly;
    }

    public class Verb_ShootArc : Verb
    {
        private VerbProperties_Arc Props => (VerbProperties_Arc)verbProps;
        public override void WarmupComplete() => TryCastShot();

        protected override bool TryCastShot()
        {
            MakeExplosion();
            verbProps.soundCast?.PlayOneShot(new TargetInfo(caster.Position, caster.MapHeld));
            verbProps.soundCastTail?.PlayOneShotOnCamera(caster.Map);
            return true;
        }

        public override bool CanHitTarget(LocalTargetInfo target)
        {
            return caster != null && caster.Spawned && (target == caster || CanHitTargetFrom(caster.Position, target));
        }

        protected void MakeExplosion()
        {
            Pawn pawn = CasterPawn;
            if (!pawn.Spawned) return;
            if (currentTarget.Thing is Pawn target && !target.Downed && target.GetPosture() == PawnPosture.Standing && pawn.skills != null)
            {
                pawn.skills.Learn(SkillDefOf.Shooting, 250f * verbProps.AdjustedFullCycleTime(this, pawn));
            }
            if (Props == null) return;

            FloatRange angle = TargetAngle(pawn.Position, currentTarget.Cell, Props.affectedAngle);
            HashSet<IntVec3> cells = new HashSet<IntVec3>(GetSectorCells(pawn.Position, pawn.Map, Props.range, angle));
            List<Pawn> conductPawns = new List<Pawn> { pawn };
            foreach (IntVec3 cell in cells)
            {
                List<Thing> things = pawn.Map.thingGrid.ThingsListAt(cell);
                for (int i = things.Count - 1; i >= 0; i--)
                {
                    if (!(things[i] is Pawn candidate)) continue;
                    bool friendly = candidate.Faction != null && pawn.Faction != null && !candidate.Faction.HostileTo(pawn.Faction);
                    if (!Props.conductFriendly && friendly) continue;
                    if (candidate.GetPosture() == PawnPosture.Standing || currentTarget.Thing == candidate)
                    {
                        conductPawns.Add(candidate);
                    }
                }
            }
            float multiplier = EquipmentSource.GetStatValue(StatDefOf.RangedWeapon_DamageMultiplier);
            int damage = GetDamageAmount(multiplier);
            float penetration = GetArmorPenetration(multiplier);
            if (Props.isConductible)
            {
                for (int i = 0; i < Props.conductNum && i + 1 < conductPawns.Count; i++)
                {
                    if (Props.EMPDamageAmount > 0f)
                    {
                        TargetTakeDamage(pawn, conductPawns[i + 1], DamageDefOf.EMP, Props.EMPDamageAmount);
                    }
                    TargetTakeDamage(pawn, conductPawns[i + 1], Props.damageDef, damage, penetration);
                    if (verbProps.beamMoteDef != null)
                    {
                        MoteMaker.MakeInteractionOverlay(verbProps.beamMoteDef,
                            new TargetInfo(conductPawns[i].Position, caster.Map),
                            new TargetInfo(conductPawns[i + 1].Position, caster.Map));
                    }
                }
                return;
            }

            foreach (FloatRange sector in SplitAngle(angle)) DoArcExplosion(sector, damage, penetration);
            if (Props.EMPDamageAmount > 0f)
            {
                for (int i = 1; i < conductPawns.Count; i++)
                {
                    TargetTakeDamage(pawn, conductPawns[i], DamageDefOf.EMP, Props.EMPDamageAmount);
                }
            }
        }

        private void DoArcExplosion(FloatRange angle, int damage, float penetration)
        {
            GenExplosion.DoExplosion(CasterPawn.Position, caster.MapHeld, verbProps.range, Props.damageDef, null,
                damage, penetration, weapon: CasterPawn.equipment.Primary.def,
                postExplosionSpawnThingDef: ThingDefOf.Filth_FlammableBile,
                affectedAngle: angle, propagationSpeed: 0.6f, doSoundEffects: false);
        }

        public override void DrawHighlight(LocalTargetInfo target)
        {
            base.DrawHighlight(target);
            if (target.IsValid)
            {
                GenDraw.DrawFieldEdges(GetSectorCells(caster.Position, caster.Map, Props.range,
                    TargetAngle(caster.Position, target.Cell, Props.affectedAngle)), verbProps.explosionRadiusRingColor);
            }
        }

        public static void RenderPredictedAreaOfEffect(IntVec3 loc, float radius, Color color, FloatRange affectedAngle)
        {
            GenDraw.DrawFieldEdges(GetSectorCells(loc, Find.CurrentMap, radius, affectedAngle), color);
        }

        // 爆炸扇形角度与 IntVec3.AngleFlat 的朝向约定不同，不可直接替换。
        private static FloatRange TargetAngle(IntVec3 center, IntVec3 target, float halfAngle)
        {
            float angle = Mathf.Atan2(-(float)(target.z - center.z), (float)(target.x - center.x)) * 57.29578f;
            return new FloatRange(angle - halfAngle, angle + halfAngle);
        }

        private static IEnumerable<FloatRange> SplitAngle(FloatRange angle)
        {
            if (angle.min < -180f || angle.max > 180f)
            {
                yield return new FloatRange(AngleWrapped(angle.min), 180f);
                yield return new FloatRange(-180f, AngleWrapped(angle.max));
            }
            else yield return angle;
        }

        private static List<IntVec3> GetSectorCells(IntVec3 center, Map map, float radius, FloatRange angle)
        {
            List<IntVec3> cells = new List<IntVec3>();
            DamageWorker worker = DamageDefOf.Bomb.Worker;
            foreach (FloatRange sector in SplitAngle(angle))
            {
                cells.AddRange(worker.ExplosionCellsToHit(center, map, radius, null, null, sector));
            }
            return cells;
        }

        public static float AngleWrapped(float angle)
        {
            while (angle > 180f) angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle == 180f ? -180f : angle;
        }

        private void TargetTakeDamage(Pawn source, Pawn target, DamageDef damageDef, float amount, float penetration = -1f)
        {
            if (source == null || target == null)
            {
                Log.Error("TargetTakeDamage has null caster or target");
                return;
            }
            BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(source, target, currentTarget.Thing, EquipmentSource.def, null, null);
            DamageInfo damage = new DamageInfo(damageDef, amount, penetration, (currentTarget.Cell - source.Position).AngleFlat,
                source, weapon: EquipmentSource.def, intendedTarget: currentTarget.Thing);
            target.TakeDamage(damage).AssociateWithLog(log);
        }

        public int GetDamageAmount(float multiplier)
        {
            int damage = Props.damageAmount > 0 ? Props.damageAmount : verbProps.beamDamageDef.defaultDamage;
            return Mathf.RoundToInt(damage * multiplier);
        }

        public float GetArmorPenetration(float multiplier)
        {
            float penetration = Props.armorPenetration > 0f ? Props.armorPenetration : verbProps.beamDamageDef.defaultArmorPenetration;
            if (penetration < 0f)
            {
                int damage = Props.damageAmount > 0 ? Props.damageAmount : verbProps.beamDamageDef.defaultDamage;
                penetration = damage * 0.015f;
            }
            return penetration * multiplier;
        }
    }
}
