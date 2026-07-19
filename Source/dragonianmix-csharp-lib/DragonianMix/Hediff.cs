using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    public class CurseFlameModExt : DefModExtension
    {
        // XML 配置字段
        public string damageDefName = "AcidBurn"; // 默认 AcidBurn
        public FloatRange damageRange = new FloatRange(1f, 3f);
        public int damageIntervalTicks = 40;

        // 延迟加载的 DamageDef
        private DamageDef _resolvedDamageDef;
        public DamageDef ResolvedDamageDef
        {
            get
            {
                if (_resolvedDamageDef == null)
                {
                    // 从 DefDatabase 解析 DamageDef
                    _resolvedDamageDef = DefDatabase<DamageDef>.GetNamedSilentFail(damageDefName);

                    // 回退逻辑：如果配置的 DefName 无效，使用 AcidBurn
                    if (_resolvedDamageDef == null)
                    {
                        Log.Error($"[DragonianMix] 未找到 DamageDef: {damageDefName}, 已回退到 AcidBurn");
                        _resolvedDamageDef = DamageDefOf.AcidBurn;
                    }
                }
                return _resolvedDamageDef;
            }
        }
    }
    public class HediffCurseFlame : HediffWithComps
    {
        public override void Tick()
        {
            base.Tick();

            // 1. 获取 ModExtension 配置
            var modExt = def.GetModExtension<CurseFlameModExt>();
            if (modExt == null || pawn == null) return;

            // 2. 解析 DamageDef（此时 DefOf 已初始化）
            DamageDef damageDef = modExt.ResolvedDamageDef;
            if (damageDef == null) return;

            // 3. 触发伤害逻辑
            if (pawn.Spawned && !pawn.Dead && pawn.IsHashIntervalTick(modExt.damageIntervalTicks))
            {
                var hittableParts = HittablePartsViolence(pawn.health.hediffSet).ToList();
                if (hittableParts.Any())
                {
                    DamageInfo damage = new DamageInfo(
                        def: damageDef,
                        amount: modExt.damageRange.RandomInRange,
                        armorPenetration: 999f,
                        hitPart: hittableParts.RandomElement(),
                        instigator: null
                    );
                    pawn.TakeDamage(damage);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref parent, "parent", false);
        }

        private static IEnumerable<BodyPartRecord> HittablePartsViolence(HediffSet bodyModel)
        {
            return bodyModel.GetNotMissingParts()
                .Where(part =>
                    part.coverageAbs > 0 && // 关键过滤：只选可被击中的部位
                    (part.depth == BodyPartDepth.Outside ||
                     (part.depth == BodyPartDepth.Inside && part.def.IsSolid(part, bodyModel.hediffs)))
                );
        }

        public Pawn parent;
    }
    public class HediffComp_ContinuousDamage : HediffComp
    {
        // Token: 0x1700000E RID: 14
        // (get) Token: 0x0600004F RID: 79 RVA: 0x00003885 File Offset: 0x00001A85
        public HediffCompProperties_ContinuousDamage Props
        {
            get
            {
                return (HediffCompProperties_ContinuousDamage)this.props;
            }
        }

        // Token: 0x1700000F RID: 15
        // (get) Token: 0x06000050 RID: 80 RVA: 0x00003892 File Offset: 0x00001A92
        public override bool CompShouldRemove
        {
            get
            {
                return this.partMissing || base.CompShouldRemove;
            }
        }

        // Token: 0x06000051 RID: 81 RVA: 0x000038A4 File Offset: 0x00001AA4
        public override void CompPostMake()
        {
            base.CompPostMake();
            this.ticksToDamage = this.Props.ticksPerDamage;
        }

        // Token: 0x06000052 RID: 82 RVA: 0x000038C0 File Offset: 0x00001AC0
        public override void CompPostTick(ref float severityAdjustment)
        {
            if (this.parent.IsTended() || this.parent.IsPermanent() || this.Props.endTicks <= (float)this.ticksNullify)
            {
                return;
            }
            this.ticksToDamage--;
            this.ticksNullify++;
            if (this.ticksToDamage <= 0)
            {
                this.ticksToDamage = this.Props.ticksPerDamage;
                DamageDef damageDef = this.Props.damageDef;
                DamageInfo dinfo = new DamageInfo(damageDef, this.Props.damageAmount, 0f, 0f, null, this.parent.Part, null, DamageInfo.SourceCategory.ThingOrUnknown, base.Pawn, true, true, QualityCategory.Normal, true);
                dinfo.SetIgnoreArmor(true);
                base.Pawn.TakeDamage(dinfo);
                if (base.Pawn.health.hediffSet.PartIsMissing(this.parent.Part))
                {
                    this.partMissing = true;
                }
            }
        }

        // Token: 0x06000053 RID: 83 RVA: 0x000039B4 File Offset: 0x00001BB4
        public override void CompPostMerged(Hediff other)
        {
            base.CompPostMerged(other);
            HediffComp_ContinuousDamage hediffComp_ContinuousDamage = other.TryGetComp<HediffComp_ContinuousDamage>();
            if (hediffComp_ContinuousDamage != null)
            {
                this.ticksNullify = Mathf.Min(hediffComp_ContinuousDamage.ticksNullify, this.ticksNullify);
            }
        }

        // Token: 0x06000054 RID: 84 RVA: 0x000039E9 File Offset: 0x00001BE9
        public override void CompExposeData()
        {
            Scribe_Values.Look<int>(ref this.ticksNullify, "ticksNullify", 0, false);
            Scribe_Values.Look<int>(ref this.ticksToDamage, "ticksToDamage", 0, false);
            Scribe_Values.Look<bool>(ref this.partMissing, "HCPD_partMissing", false, false);
        }

        // Token: 0x06000055 RID: 85 RVA: 0x00003A21 File Offset: 0x00001C21
        public override string CompDebugString()
        {
            return "ticksToDamage: " + this.ticksToDamage.ToString();
        }

        // Token: 0x0400002C RID: 44
        public int ticksNullify;

        // Token: 0x0400002D RID: 45
        public int ticksToDamage;

        // Token: 0x0400002E RID: 46
        public bool partMissing;
    }
    public class HediffCompProperties_ContinuousDamage : HediffCompProperties
    {
        // Token: 0x0600004E RID: 78 RVA: 0x0000384F File Offset: 0x00001A4F
        public HediffCompProperties_ContinuousDamage()
        {
            this.compClass = typeof(HediffComp_ContinuousDamage);
        }

        // Token: 0x04000028 RID: 40
        public DamageDef damageDef;

        // Token: 0x04000029 RID: 41
        public int ticksPerDamage = 100;

        // Token: 0x0400002A RID: 42
        public float endTicks = -1f;

        // Token: 0x0400002B RID: 43
        public float damageAmount = 1f;
    }

    public class HediffComp_DRMExplodeOnDeath : HediffComp
    {
        // Token: 0x17000006 RID: 6
        // (get) Token: 0x0600005C RID: 92 RVA: 0x0000425E File Offset: 0x0000245E
        public HediffCompProperties_DRMExplodeOnDeath Props
        {
            get
            {
                return (HediffCompProperties_DRMExplodeOnDeath)this.props;
            }
        }

        // Token: 0x0600005D RID: 93 RVA: 0x0000426C File Offset: 0x0000246C
        public override void Notify_PawnKilled()
        {
            if (!base.Pawn.Spawned)
            {
                return;
            }
            GenExplosion.DoExplosion(
                center: base.Pawn.Position,
                map: base.Pawn.Map,
                radius: this.Props.explosionRadius,
                damType: this.Props.damageDef,
                instigator: base.Pawn,
                damAmount: this.Props.damageAmount,
                armorPenetration: this.Props.armorPenetration,
                explosionSound: this.Props.soundDef,
                weapon: null,
                projectile: null,
                intendedTarget: null,
                postExplosionSpawnThingDef: this.Props.postExplosionSpawnThingDef,
                postExplosionSpawnChance: this.Props.postExplosionSpawnChance,
                postExplosionSpawnThingCount: this.Props.postExplosionSpawnThingCount,
                postExplosionGasType: this.Props.gasType,
                postExplosionGasRadiusOverride: null, // 原第16个参数改为null
                postExplosionGasAmount: 0,          // 使用默认值或适当数值
                applyDamageToExplosionCellsNeighbors: false,
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
            if (this.Props.destroyGear)
            {
                base.Pawn.equipment.DestroyAllEquipment(DestroyMode.Vanish);
                base.Pawn.apparel.DestroyAll(DestroyMode.Vanish);
            }
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            if (this.Props.destroyBody)
            {
                base.Pawn.Corpse.Destroy(DestroyMode.Vanish);
            }
        }

    }

    // Token: 0x02000011 RID: 17
    public class HediffCompProperties_DRMExplodeOnDeath : HediffCompProperties
    {
        // Token: 0x0600005B RID: 91 RVA: 0x0000423F File Offset: 0x0000243F
        public HediffCompProperties_DRMExplodeOnDeath()
        {
            this.compClass = typeof(HediffComp_DRMExplodeOnDeath);
        }

        // Token: 0x0400005B RID: 91
        public float explosionRadius;

        // Token: 0x0400005C RID: 92
        public DamageDef damageDef;

        // Token: 0x0400005D RID: 93
        public int damageAmount;

        // Token: 0x0400005E RID: 94
        public bool destroyGear;

        // Token: 0x0400005F RID: 95
        public bool destroyBody;

        // Token: 0x04000060 RID: 96
        public float postExplosionSpawnChance;

        // Token: 0x04000061 RID: 97
        public int postExplosionSpawnThingCount;

        // Token: 0x04000062 RID: 98
        public ThingDef postExplosionSpawnThingDef;

        // Token: 0x04000062 RID: 98
        public GasType gasType;

        // Token: 0x04000063 RID: 99
        public float armorPenetration;

        // Token: 0x04000064 RID: 100
        public SoundDef soundDef;

        // Token: 0x04000065 RID: 101
        public bool destroyImplantAfterDetonation = true;
    }

}
