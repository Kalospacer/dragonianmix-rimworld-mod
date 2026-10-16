using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    public class HediffComp_ContinuousDamage : HediffComp
    {
        private int elapsedTicks;
        private int ticksToDamage;
        private bool partMissing;

        public HediffCompProperties_ContinuousDamage Props => (HediffCompProperties_ContinuousDamage)props;
        public override bool CompShouldRemove => partMissing || base.CompShouldRemove;

        public override void CompPostMake()
        {
            base.CompPostMake();
            ticksToDamage = Props.ticksPerDamage;
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            if (!Pawn.Spawned || Pawn.Dead || (Props.endTicks >= 0f && elapsedTicks >= Props.endTicks))
            {
                return;
            }
            // 全身周期伤害沿用原 CurseFlame 的哈希节奏和等概率部位选择。
            if (Props.randomHittablePart)
            {
                elapsedTicks++;
                if (!Pawn.IsHashIntervalTick(Props.ticksPerDamage)) return;
            }
            else
            {
                if (parent.IsTended() || parent.IsPermanent()) return;
                elapsedTicks++;
                if (--ticksToDamage > 0) return;
                ticksToDamage = Props.ticksPerDamage;
            }

            BodyPartRecord part = parent.Part;
            if (Props.randomHittablePart)
            {
                List<BodyPartRecord> parts = Pawn.health.hediffSet.GetNotMissingParts()
                    .Where(p => p.coverageAbs > 0f && (p.depth == BodyPartDepth.Outside ||
                        (p.depth == BodyPartDepth.Inside && p.def.IsSolid(p, Pawn.health.hediffSet.hediffs))))
                    .ToList();
                if (parts.Count == 0) return;
                part = parts.RandomElement();
            }
            float amount = Props.damageRange?.RandomInRange ?? Props.damageAmount;
            DamageInfo damage = new DamageInfo(Props.damageDef, amount, Props.randomHittablePart ? 999f : 0f,
                hitPart: part, intendedTarget: Pawn);
            if (!Props.randomHittablePart) damage.SetIgnoreArmor(true);
            Pawn.TakeDamage(damage);
            if (parent.Part != null && Pawn.health.hediffSet.PartIsMissing(parent.Part)) partMissing = true;
        }

        public override void CompPostMerged(Hediff other)
        {
            base.CompPostMerged(other);
            HediffComp_ContinuousDamage comp = other.TryGetComp<HediffComp_ContinuousDamage>();
            if (comp != null) elapsedTicks = Mathf.Min(comp.elapsedTicks, elapsedTicks);
        }

        public override void CompExposeData()
        {
            Scribe_Values.Look(ref elapsedTicks, "elapsedTicks");
            Scribe_Values.Look(ref ticksToDamage, "ticksToDamage");
            Scribe_Values.Look(ref partMissing, "partMissing");
        }

        public override string CompDebugString() => "ticksToDamage: " + ticksToDamage;
    }

    public class HediffCompProperties_ContinuousDamage : HediffCompProperties
    {
        public DamageDef damageDef;
        public int ticksPerDamage = 100;
        public float endTicks = -1f;
        public float damageAmount = 1f;
        public FloatRange? damageRange;
        public bool randomHittablePart;

        public HediffCompProperties_ContinuousDamage()
        {
            compClass = typeof(HediffComp_ContinuousDamage);
        }
    }

    public class HediffComp_DRMExplodeOnDeath : HediffComp_ExplodeOnDeath
    {
        public new HediffCompProperties_DRMExplodeOnDeath Props => (HediffCompProperties_DRMExplodeOnDeath)props;

        public override void Notify_PawnKilled()
        {
            if (!Pawn.Spawned) return;
            GenExplosion.DoExplosion(Pawn.Position, Pawn.Map, Props.explosionRadius, Props.damageDef, Pawn,
                Props.damageAmount, Props.armorPenetration, Props.soundDef,
                postExplosionSpawnThingDef: Props.postExplosionSpawnThingDef,
                postExplosionSpawnChance: Props.postExplosionSpawnChance,
                postExplosionSpawnThingCount: Props.postExplosionSpawnThingCount,
                postExplosionGasType: Props.gasType);
            if (Props.destroyGear) PawnGearUtility.DestroyGear(Pawn);
        }
    }

    public class HediffCompProperties_DRMExplodeOnDeath : HediffCompProperties_ExplodeOnDeath
    {
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount;
        public ThingDef postExplosionSpawnThingDef;
        public GasType? gasType;
        public float armorPenetration;
        public SoundDef soundDef;

        public HediffCompProperties_DRMExplodeOnDeath()
        {
            damageAmount = 0;
            compClass = typeof(HediffComp_DRMExplodeOnDeath);
        }
    }

    internal static class PawnGearUtility
    {
        public static void DestroyGear(Pawn pawn)
        {
            pawn.equipment?.DestroyAllEquipment(DestroyMode.Vanish);
            pawn.apparel?.DestroyAll(DestroyMode.Vanish);
        }
    }
}
