using RimWorld;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    /// <summary>落雷技能：在目标处落雷并造成一次爆炸。</summary>
    public class CompAbilityEffect_DRM_LightingStrike : CompAbilityEffect
    {
        public new CompProperties_AbilityDRM_LightingStrike Props => (CompProperties_AbilityDRM_LightingStrike)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.MapHeld;
            LightningEffectUtility.Spawn(target.Cell, map);
            GenExplosion.DoExplosion(target.Cell, map, Props.explosionRadius, Props.damageDef, parent.pawn, Props.damageAmount, Props.armorPenetration,
                postExplosionSpawnThingDef: Props.postExplosionSpawnThingDef,
                postExplosionSpawnChance: Props.postExplosionSpawnChance,
                postExplosionSpawnThingCount: Props.postExplosionSpawnThingCount);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(target.Cell, Props.explosionRadius, Color.cyan);
        }
    }

    public class CompProperties_AbilityDRM_LightingStrike : CompProperties_AbilityEffect
    {
        public float explosionRadius = 3.5f;
        public DamageDef damageDef;
        public int damageAmount = 30;
        public float armorPenetration = 0.8f;
        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount;

        public CompProperties_AbilityDRM_LightingStrike()
        {
            compClass = typeof(CompAbilityEffect_DRM_LightingStrike);
        }
    }
}
