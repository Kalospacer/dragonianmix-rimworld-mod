using System.Collections.Generic;
using Verse;

namespace DragonianMix
{
    public class HediffComp_TimedExplosion : HediffComp_Disappears
    {
        public new HediffCompProperties_TimedExplosion Props => (HediffCompProperties_TimedExplosion)props;

        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (!Pawn.Spawned || Props.explosionRadius <= 0.01f || Props.damageDef == null) return;
            GenExplosion.DoExplosion(Pawn.Position, Pawn.Map, Props.explosionRadius, Props.damageDef, Pawn,
                Props.damageAmount, Props.armorPenetration, Props.soundDef,
                postExplosionSpawnThingDef: Props.postExplosionSpawnThingDef,
                postExplosionSpawnChance: Props.postExplosionSpawnChance,
                postExplosionSpawnThingCount: Props.postExplosionSpawnThingCount,
                chanceToStartFire: Props.chanceToStartFire,
                damageFalloff: Props.damageFalloff,
                ignoredThings: new List<Thing> { Pawn });
            if (Props.destroyGear) PawnGearUtility.DestroyGear(Pawn);
        }

        public override string CompDebugString() => base.CompDebugString() + "\nexplosionRadius: " + Props.explosionRadius;
    }

    public class HediffCompProperties_TimedExplosion : HediffCompProperties_Disappears
    {
        public float explosionRadius = 3f;
        public DamageDef damageDef;
        public int damageAmount = 20;
        public float armorPenetration;
        public SoundDef soundDef;
        public float chanceToStartFire;
        public bool damageFalloff = true;
        public bool destroyGear;
        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount = 1;

        public HediffCompProperties_TimedExplosion()
        {
            compClass = typeof(HediffComp_TimedExplosion);
            disappearsAfterTicks = new IntRange(600, 1200);
            showRemainingTime = true;
            leaveFreshWounds = true;
        }
    }
}
