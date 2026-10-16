using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DragonianMix
{
    /// <summary>雷电轰炸技能：在目标处生成持久的 <see cref="LightningBombardment"/> 实体。</summary>
    public class CompAbilityEffect_DRM_LightningBombardment : CompAbilityEffect
    {
        public new CompProperties_AbilityDRM_LightningBombardment Props => (CompProperties_AbilityDRM_LightningBombardment)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = parent.pawn.MapHeld;
            LightningBombardment bombardment = (LightningBombardment)GenSpawn.Spawn(Props.bombardmentDef, target.Cell, map);
            bombardment.instigator = parent.pawn;
            bombardment.impactAreaRadius = Props.impactAreaRadius;
            bombardment.explosionRadiusRange = Props.explosionRadiusRange;
            bombardment.bombIntervalTicks = Props.bombIntervalTicks;
            bombardment.explosionsRemaining = Props.explosionCount;
            bombardment.damageDef = Props.damageDef;
            bombardment.damageAmount = Props.damageAmount;
            bombardment.armorPenetration = Props.armorPenetration;
            bombardment.postExplosionSpawnThingDef = Props.postExplosionSpawnThingDef;
            bombardment.postExplosionSpawnChance = Props.postExplosionSpawnChance;
            bombardment.postExplosionSpawnThingCount = Props.postExplosionSpawnThingCount;
            SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(map);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(parent.pawn.Position, parent.verb.verbProps.range, Color.white);
            GenDraw.DrawRadiusRing(target.Cell, Props.impactAreaRadius, Color.white);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }
    }

    public class CompProperties_AbilityDRM_LightningBombardment : CompProperties_AbilityEffect
    {
        public ThingDef bombardmentDef;
        public float impactAreaRadius = 10f;
        public FloatRange explosionRadiusRange = new FloatRange(3f, 4f);
        public int bombIntervalTicks = 30;
        public int explosionCount = 3;

        public DamageDef damageDef;
        public int damageAmount = 30;
        public float armorPenetration = 0.8f;

        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount;

        public CompProperties_AbilityDRM_LightningBombardment()
        {
            compClass = typeof(CompAbilityEffect_DRM_LightningBombardment);
        }

        public override IEnumerable<string> ConfigErrors(AbilityDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }
            if (bombardmentDef == null || !typeof(LightningBombardment).IsAssignableFrom(bombardmentDef.thingClass))
            {
                yield return "bombardmentDef must be a ThingDef whose thingClass is DragonianMix.LightningBombardment";
            }
        }
    }
}
