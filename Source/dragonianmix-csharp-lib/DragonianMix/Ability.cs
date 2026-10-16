using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;

namespace DragonianMix
{
    public class CompAbilityEffect_DRM_FireBurst : CompAbilityEffect
    {
        private new CompProperties_AbilityDRM_FireBurst Props
        {
            get
            {
                return (CompProperties_AbilityDRM_FireBurst)this.props;
            }
        }

        private Pawn Pawn
        {
            get
            {
                return this.parent.pawn;
            }
        }
        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            return GenRadial.RadialCellsAround(this.Pawn.Position, Props.radius, true).ToList();
        }
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            GenExplosion.DoExplosion(
                center: this.Pawn.Position,
                map: this.Pawn.MapHeld,
                radius: this.Props.radius,
                damType: this.Props.damageDef,
                instigator: this.Pawn,
                damAmount: this.Props.damageAmount,
                postExplosionSpawnThingDef: ThingDefOf.Fire,
                overrideCells: this.AffectedCells(target) // 只需命名关键参数
            );

            base.Apply(target, dest);
        }

        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            PreCastAction preCastAction = default(PreCastAction);
            preCastAction.action = delegate (LocalTargetInfo a, LocalTargetInfo b)
            {
                this.parent.AddEffecterToMaintain(EffecterDefOf.Fire_Burst.Spawn(this.parent.pawn.Position, this.parent.pawn.Map, 1f), this.parent.pawn.Position, 17, this.parent.pawn.Map);
            };
            preCastAction.ticksAwayFromCast = 17;
            yield return preCastAction;
            yield break;
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            Pawn pawn;
            return this.Pawn.Faction != Faction.OfPlayer && target.HasThing && (pawn = (target.Thing as Pawn)) != null && pawn.TargetCurrentlyAimingAt == this.Pawn;
        }

        public override void CompTick()
        {
            bool casting = this.parent.Casting;
            if (casting)
            {
                FireBurstUtility.ThrowFuelTick(this.Pawn.Position, this.Props.radius, this.Pawn.Map);
            }
        }
    }

    public class CompProperties_AbilityDRM_FireBurst : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityDRM_FireBurst()
        {
            this.compClass = typeof(CompAbilityEffect_DRM_FireBurst);
        }

        public float radius = 6f;

        public DamageDef damageDef;

        public int damageAmount;

        public float armorPenetration = -1f;
    }

    public class CompAbilityEffect_DRM_Deaddustpop : CompAbilityEffect
    {
        public new CompProperties_AbilityDRM_Deaddustpop Props =>
            (CompProperties_AbilityDRM_Deaddustpop)props;

        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            return GenRadial.RadialCellsAround(target.Cell, Props.smokeRadius, true).ToList();
        }

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            SoundDef explosionSound = Props.explosionSound;

            GenExplosion.DoExplosion(
                center: target.Cell,
                map: parent.pawn.MapHeld,
                radius: Props.smokeRadius,
                damType: Props.damageDef,
                instigator: null,
                damAmount: Props.damageAmount,
                armorPenetration: Props.armorPenetration,
                explosionSound: explosionSound, // 使用有效的声音定义
                postExplosionSpawnThingDef: Props.postExplosionSpawnThingDef,
                postExplosionSpawnChance: Props.postExplosionSpawnChance,
                postExplosionSpawnThingCount: Props.postExplosionSpawnThingCount,
                postExplosionGasType: Props.gasType,
                overrideCells: AffectedCells(target)
            );
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(target.Cell, Props.smokeRadius);
        }
    }

    public class CompProperties_AbilityDRM_Deaddustpop : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityDRM_Deaddustpop()
        {
            compClass = typeof(CompAbilityEffect_DRM_Deaddustpop);
        }
        public SoundDef explosionSound;

        public float smokeRadius;
        public GasType gasType;
        public DamageDef damageDef;
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount;
        public ThingDef postExplosionSpawnThingDef;
        public int damageAmount;
        public float armorPenetration;
    }

    public class CompAbilityEffect_DRM_FireSpew : CompAbilityEffect
    {
        private readonly List<IntVec3> tmpCells = new List<IntVec3>();

        private new CompProperties_AbilityDRM_FireSpew Props => (CompProperties_AbilityDRM_FireSpew)props;

        private Pawn Pawn => parent.pawn;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            IntVec3 cell = target.Cell;
            Map mapHeld = this.parent.pawn.MapHeld;
            Thing pawn = this.Pawn;
            ThingDef filthDef = this.Props.filthDef;

            GenExplosion.DoExplosion(
                center: cell,
                map: mapHeld,
                radius: 0f, // 原代码中 radius = 0f
                damType: this.Props.damageDef,
                instigator: pawn,
                damAmount: this.Props.damAmount,
                postExplosionSpawnThingDef: filthDef,
                postExplosionSpawnChance: 1f,
                flammabilityChanceCurve: this.parent.verb.verbProps.flammabilityAttachFireChanceCurve,
                overrideCells: this.AffectedCells(target)
            );

            base.Apply(target, dest);
        }


        public override IEnumerable<PreCastAction> GetPreCastActions()
        {
            if (Props.effecterDef != null)
            {
                yield return new PreCastAction
                {
                    action = delegate (LocalTargetInfo a, LocalTargetInfo b)
                    {
                        parent.AddEffecterToMaintain(Props.effecterDef.Spawn(parent.pawn.Position, a.Cell, parent.pawn.Map), Pawn.Position, a.Cell, 17, Pawn.MapHeld);
                    },
                    ticksAwayFromCast = 17
                };
            }
        }

        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            GenDraw.DrawFieldEdges(AffectedCells(target));
        }

        public override bool AICanTargetNow(LocalTargetInfo target)
        {
            if (Pawn.Faction != null)
            {
                foreach (IntVec3 item in AffectedCells(target))
                {
                    List<Thing> thingList = item.GetThingList(Pawn.Map);
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        if (thingList[i].Faction == Pawn.Faction)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private List<IntVec3> AffectedCells(LocalTargetInfo target)
        {
            tmpCells.Clear();
            Vector3 vector = Pawn.Position.ToVector3Shifted().Yto0();
            IntVec3 intVec = target.Cell.ClampInsideMap(Pawn.Map);
            if (Pawn.Position == intVec)
            {
                return tmpCells;
            }
            float lengthHorizontal = (intVec - Pawn.Position).LengthHorizontal;
            float num = (float)(intVec.x - Pawn.Position.x) / lengthHorizontal;
            float num2 = (float)(intVec.z - Pawn.Position.z) / lengthHorizontal;
            intVec.x = Mathf.RoundToInt((float)Pawn.Position.x + num * Props.range);
            intVec.z = Mathf.RoundToInt((float)Pawn.Position.z + num2 * Props.range);
            float target2 = Vector3.SignedAngle(intVec.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up);
            float num3 = Props.lineWidthEnd / 2f;
            float num4 = Mathf.Sqrt(Mathf.Pow((intVec - Pawn.Position).LengthHorizontal, 2f) + Mathf.Pow(num3, 2f));
            float num5 = 57.29578f * Mathf.Asin(num3 / num4);
            int num6 = GenRadial.NumCellsInRadius(Props.range);
            for (int i = 0; i < num6; i++)
            {
                IntVec3 intVec2 = Pawn.Position + GenRadial.RadialPattern[i];
                if (CanUseCell(intVec2) && Mathf.Abs(Mathf.DeltaAngle(Vector3.SignedAngle(intVec2.ToVector3Shifted().Yto0() - vector, Vector3.right, Vector3.up), target2)) <= num5)
                {
                    tmpCells.Add(intVec2);
                }
            }
            List<IntVec3> list = GenSight.BresenhamCellsBetween(Pawn.Position, intVec);
            for (int j = 0; j < list.Count; j++)
            {
                IntVec3 intVec3 = list[j];
                if (!tmpCells.Contains(intVec3) && CanUseCell(intVec3))
                {
                    tmpCells.Add(intVec3);
                }
            }
            return tmpCells;
            bool CanUseCell(IntVec3 c)
            {
                if (!c.InBounds(Pawn.Map))
                {
                    return false;
                }
                if (c == Pawn.Position)
                {
                    return false;
                }
                if (!Props.canHitFilledCells && c.Filled(Pawn.Map))
                {
                    return false;
                }
                if (!c.InHorDistOf(Pawn.Position, Props.range))
                {
                    return false;
                }
                ShootLine resultingLine;
                return parent.verb.TryFindShootLineFromTo(parent.pawn.Position, c, out resultingLine);
            }
        }
    }

    public class CompProperties_AbilityDRM_FireSpew : CompProperties_AbilityEffect
    {
        public CompProperties_AbilityDRM_FireSpew()
        {
            this.compClass = typeof(CompAbilityEffect_DRM_FireSpew);
        }

        public float range;

        public float lineWidthEnd;

        public ThingDef filthDef;

        public int damAmount = -1;

        public DamageDef damageDef;

        public EffecterDef effecterDef;

        public bool canHitFilledCells;
    }


}
