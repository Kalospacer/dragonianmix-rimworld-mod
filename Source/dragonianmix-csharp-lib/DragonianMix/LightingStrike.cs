using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DragonianMix
{
    [StaticConstructorOnStartup]
    public class CompAbilityEffect_DRM_LightingStrike : CompAbilityEffect
    {
        private static readonly Material LightningMat = MatLoader.LoadMat("Weather/LightningBolt", -1);

        public new CompProperties_AbilityDRM_LightingStrike Props
        {
            get
            {
                return (CompProperties_AbilityDRM_LightingStrike)this.props;
            }
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Map map = this.parent.pawn.MapHeld;
            IntVec3 targetCell = target.Cell;

            // 生成闪电特效和音效
            SpawnLightningEffects(targetCell, map);

            // 执行电击爆炸（参数已通过命名方式显式对齐）
            GenExplosion.DoExplosion(
                center: targetCell,
                map: map,
                radius: this.Props.explosionRadius,
                damType: this.Props.damageDef,
                instigator: this.parent.pawn,
                damAmount: this.Props.damageAmount,
                armorPenetration: this.Props.armorPenetration,
                explosionSound: null,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: this.Props.postExplosionSpawnThingDef,
                postExplosionSpawnChance: this.Props.postExplosionSpawnChance,
                postExplosionSpawnThingCount: this.Props.postExplosionSpawnThingCount,
                postExplosionGasType: null,                   // GasType? 类型
                postExplosionGasRadiusOverride: null,         // float? 类型（原错误参数位置16）
                postExplosionGasAmount: 0,                  // int 类型（原错误参数位置17）
                applyDamageToExplosionCellsNeighbors: false,  // bool 类型（原错误参数位置18）
                preExplosionSpawnThingDef: null,
                preExplosionSpawnChance: 0f,
                preExplosionSpawnThingCount: 1,
                chanceToStartFire: 0f,
                damageFalloff: false,
                direction: null,
                ignoredThings: null,
                affectedAngle: null,
                doVisualEffects: true,
                propagationSpeed: 1f,
                excludeRadius: 0f,
                doSoundEffects: true,
                postExplosionSpawnThingDefWater: null,
                screenShakeFactor: 1f,
                flammabilityChanceCurve: null,
                overrideCells: null,
                postExplosionSpawnSingleThingDef: null,
                preExplosionSpawnSingleThingDef: null
            );
        }


        private void SpawnLightningEffects(IntVec3 strikeLoc, Map map)
        {
            if (!strikeLoc.IsValid || map == null) return;

            // 播放全局雷声
            SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(map);

            if (strikeLoc.Fogged(map)) return;

            Vector3 position = strikeLoc.ToVector3Shifted();

            // 生成粒子效果
            for (int i = 0; i < 4; i++)
            {
                FleckMaker.ThrowSmoke(position, map, 1.5f);
                FleckMaker.ThrowMicroSparks(position, map);
                FleckMaker.ThrowLightningGlow(position, map, 1.5f);
            }

            // 绘制闪电图形
            Mesh boltMesh = LightningBoltMeshPool.RandomBoltMesh;
            Graphics.DrawMesh(
                boltMesh,
                strikeLoc.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather),
                Quaternion.identity,
                FadedMaterialPool.FadedVersionOf(LightningMat, 1f),
                0
            );

            // 播放局部雷声
            SoundInfo soundInfo = SoundInfo.InMap(new TargetInfo(strikeLoc, map));
            SoundDefOf.Thunder_OnMap.PlayOneShot(soundInfo);
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            // 显示电击作用范围
            GenDraw.DrawRadiusRing(target.Cell, this.Props.explosionRadius, Color.cyan);
        }
    }

    public class CompProperties_AbilityDRM_LightingStrike : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityDRM_LightingStrike()
        {
            this.compClass = typeof(CompAbilityEffect_DRM_LightingStrike);
        }

        public float explosionRadius = 3.5f;

        public DamageDef damageDef;

        public int damageAmount = 30;

        public float armorPenetration = 0.8f;

        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount;
    }

}