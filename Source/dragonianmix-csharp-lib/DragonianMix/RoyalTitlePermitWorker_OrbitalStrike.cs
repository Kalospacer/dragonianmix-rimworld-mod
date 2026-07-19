using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DragonianMix
{
    // Token: 0x020012DB RID: 4827
    public class RoyalTitlePermitWorker_DRM_OrbitalStrike : RoyalTitlePermitWorker_Targeted
    {
        // Token: 0x06007236 RID: 29238 RVA: 0x0026FAD4 File Offset: 0x0026DCD4
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.CanHitTarget(target))
            {
                if (target.IsValid && showMessages)
                {
                    Messages.Message(this.def.LabelCap + ": " + "AbilityCannotHitTarget".Translate(), MessageTypeDefOf.RejectInput, true);
                }
                return false;
            }
            return true;
        }

        // Token: 0x06007237 RID: 29239 RVA: 0x0026FB2C File Offset: 0x0026DD2C
        public override void DrawHighlight(LocalTargetInfo target)
        {
            GenDraw.DrawRadiusRing(this.caller.Position, this.def.royalAid.targetingRange, Color.white, null);
            GenDraw.DrawRadiusRing(target.Cell, this.def.royalAid.radius + this.def.royalAid.explosionRadiusRange.max, Color.white, null);
            if (target.IsValid)
            {
                GenDraw.DrawTargetHighlight(target);
            }
        }

        // Token: 0x06007238 RID: 29240 RVA: 0x0026FBA6 File Offset: 0x0026DDA6
        public override void OrderForceTarget(LocalTargetInfo target)
        {
            this.CallBombardment(target.Cell);
        }

        // Token: 0x06007239 RID: 29241 RVA: 0x0026FBB5 File Offset: 0x0026DDB5
        public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
        {
            if (map.generatorDef.isUnderground)
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidMapUnreachable".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            if (faction.HostileTo(Faction.OfPlayer))
            {
                yield return new FloatMenuOption(this.def.LabelCap + ": " + "CommandCallRoyalAidFactionHostile".Translate(faction.Named("FACTION")), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
                yield break;
            }
            string label = this.def.LabelCap + ": ";
            Action action = null;
            bool free;
            if (base.FillAidOption(pawn, faction, ref label, out free))
            {
                action = delegate ()
                {
                    this.BeginCallBombardment(pawn, faction, map, free);
                };
            }
            yield return new FloatMenuOption(label, action, faction.def.FactionIcon, faction.Color, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0, HorizontalJustification.Left, false);
            yield break;
        }

        // Token: 0x0600723A RID: 29242 RVA: 0x0026FBDC File Offset: 0x0026DDDC
        private void BeginCallBombardment(Pawn caller, Faction faction, Map map, bool free)
        {
            this.targetingParameters = new TargetingParameters();
            this.targetingParameters.canTargetLocations = true;
            this.targetingParameters.canTargetSelf = true;
            this.targetingParameters.canTargetFires = true;
            this.targetingParameters.canTargetItems = true;
            this.caller = caller;
            this.map = map;
            this.faction = faction;
            this.free = free;
            this.targetingParameters.validator = delegate (TargetInfo target)
            {
                if (this.def.royalAid.targetingRange > 0f && target.Cell.DistanceTo(caller.Position) > this.def.royalAid.targetingRange)
                {
                    return false;
                }
                if (target.Cell.Fogged(map))
                {
                    return false;
                }
                RoofDef roof = target.Cell.GetRoof(map);
                return roof == null || !roof.isThickRoof;
            };
            Find.Targeter.BeginTargeting(this, null, false, null, null, true);
        }

        // Token: 0x0600723B RID: 29243 RVA: 0x0026FC90 File Offset: 0x0026DE90
        private void CallBombardment(IntVec3 targetCell)
        {
            Bombardment bombardment = (Bombardment)GenSpawn.Spawn(ThingDefOf.Bombardment, targetCell, this.map, WipeMode.Vanish);
            bombardment.impactAreaRadius = this.def.royalAid.radius;
            bombardment.explosionRadiusRange = this.def.royalAid.explosionRadiusRange;
            bombardment.bombIntervalTicks = this.def.royalAid.intervalTicks;
            bombardment.randomFireRadius = 1;
            bombardment.explosionCount = this.def.royalAid.explosionCount;
            bombardment.warmupTicks = this.def.royalAid.warmupTicks;
            bombardment.instigator = this.caller;
            SoundDefOf.OrbitalStrike_Ordered.PlayOneShotOnCamera(null);
            this.caller.royalty.GetPermit(this.def, this.faction).Notify_Used();
            if (!this.free)
            {
                this.caller.royalty.TryRemoveFavor(this.faction, this.def.royalAid.favorCost);
            }
        }

        // Token: 0x040043F6 RID: 17398
        private Faction faction;
    }

    [StaticConstructorOnStartup]
    public class Bombardment : OrbitalStrike
    {
        // Token: 0x06008BF4 RID: 35828 RVA: 0x002FE4AD File Offset: 0x002FC6AD
        public override void SpawnSetup(Map map, bool respawningAfterReload)
        {
            base.SpawnSetup(map, respawningAfterReload);
            if (!respawningAfterReload)
            {
                this.GetNextExplosionCell();
            }
        }

        // Token: 0x06008BF5 RID: 35829 RVA: 0x002FE4C0 File Offset: 0x002FC6C0
        public override void StartStrike()
        {
            this.duration = this.bombIntervalTicks * this.explosionCount;
            base.StartStrike();
        }

        // Token: 0x06008BF6 RID: 35830 RVA: 0x002FE4DC File Offset: 0x002FC6DC
        public new void Tick()
        {
            if (base.Destroyed)
            {
                return;
            }
            if (this.warmupTicks > 0)
            {
                this.warmupTicks--;
                if (this.warmupTicks == 0)
                {
                    this.StartStrike();
                }
            }
            else
            {
                base.Tick();
                if (Find.TickManager.TicksGame % 20 == 0 && base.TicksLeft > 0)
                {
                    this.StartRandomFire();
                }
            }
            this.EffectTick();
        }

        // Token: 0x06008BF7 RID: 35831 RVA: 0x002FE544 File Offset: 0x002FC744
        private void EffectTick()
        {
            if (!this.nextExplosionCell.IsValid)
            {
                this.ticksToNextEffect = this.warmupTicks - this.bombIntervalTicks;
                this.GetNextExplosionCell();
            }
            this.ticksToNextEffect--;
            if (this.ticksToNextEffect <= 0 && base.TicksLeft >= this.bombIntervalTicks)
            {
                SoundDefOf.Bombardment_PreImpact.PlayOneShot(new TargetInfo(this.nextExplosionCell, base.Map, false));
                this.projectiles.Add(new Bombardment.BombardmentProjectile(60, this.nextExplosionCell));
                this.ticksToNextEffect = this.bombIntervalTicks;
                this.GetNextExplosionCell();
            }
            for (int i = this.projectiles.Count - 1; i >= 0; i--)
            {
                this.projectiles[i].Tick();
                if (this.projectiles[i].LifeTime <= 0)
                {
                    this.TryDoExplosion(this.projectiles[i]);
                    this.projectiles.RemoveAt(i);
                }
            }
        }

        // Token: 0x06008BF8 RID: 35832 RVA: 0x002FE644 File Offset: 0x002FC844
        private void TryDoExplosion(Bombardment.BombardmentProjectile proj)
        {
            List<Thing> list = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
            for (int i = 0; i < list.Count; i++)
            {
            }
            IntVec3 targetCell = proj.targetCell;
            Map map = base.Map;
            float randomInRange = this.explosionRadiusRange.RandomInRange;
            DamageDef bomb = DamageDefOf.DRM_Acidburn;
            Thing instigator = this.instigator;
            int damAmount = -1;
            float armorPenetration = -1f;
            SoundDef explosionSound = null;
            ThingDef postExplosionSpawnThingDef = DRM_AcidGasCloud;
            float postExplosionSpawnChance = 0f;
            int postExplosionSpawnThingCount = 1;
            // RimWorld 1.6 简化版API调用
            GenExplosion.DoExplosion(
                center: targetCell,
                map: map,
                radius: randomInRange,
                damType: bomb,
                instigator: instigator,
                damAmount: damAmount,
                armorPenetration: armorPenetration,
                explosionSound: explosionSound,
                weapon: this.weaponDef,
                projectile: this.def,
                intendedTarget: null,
                postExplosionSpawnThingDef: postExplosionSpawnThingDef,
                postExplosionSpawnChance: postExplosionSpawnChance,
                postExplosionSpawnThingCount: postExplosionSpawnThingCount
            );
        }

        public static class DamageDefOf
        {
            public static DamageDef DRM_Acidburn;
        }
        public static ThingDef DRM_AcidGasCloud;
        

        // Token: 0x06008BF9 RID: 35833 RVA: 0x002FE710 File Offset: 0x002FC910
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            if (this.projectiles.NullOrEmpty<Bombardment.BombardmentProjectile>())
            {
                return;
            }
            for (int i = 0; i < this.projectiles.Count; i++)
            {
                this.projectiles[i].Draw(Bombardment.ProjectileMaterial);
            }
        }

        // Token: 0x06008BFA RID: 35834 RVA: 0x002FE760 File Offset: 0x002FC960
        private void StartRandomFire()
        {
            IntVec3 intVec = (from x in GenRadial.RadialCellsAround(base.Position, (float)this.randomFireRadius, true)
                              where x.InBounds(base.Map)
                              select x).RandomElementByWeight((IntVec3 x) => Bombardment.DistanceChanceFactor.Evaluate(x.DistanceTo(base.Position)));
            List<Thing> list = base.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
            for (int i = 0; i < list.Count; i++)
            {
            }
            RoofDef roof = intVec.GetRoof(base.Map);
            if (roof != null && roof.isThickRoof)
            {
                return;
            }
            FireUtility.TryStartFireIn(intVec, base.Map, Rand.Range(0.1f, 0.925f), this.instigator, null);
        }

        // Token: 0x06008BFB RID: 35835 RVA: 0x002FE819 File Offset: 0x002FCA19
        private void GetNextExplosionCell()
        {
            this.nextExplosionCell = (from x in GenRadial.RadialCellsAround(base.Position, this.impactAreaRadius, true)
                                      where x.InBounds(base.Map)
                                      select x).RandomElementByWeight((IntVec3 x) => Bombardment.DistanceChanceFactor.Evaluate(x.DistanceTo(base.Position) / this.impactAreaRadius));
        }

        // Token: 0x06008BFC RID: 35836 RVA: 0x002FE858 File Offset: 0x002FCA58
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.impactAreaRadius, "impactAreaRadius", 15f, false);
            Scribe_Values.Look<FloatRange>(ref this.explosionRadiusRange, "explosionRadiusRange", new FloatRange(6f, 8f), false);
            Scribe_Values.Look<int>(ref this.randomFireRadius, "randomFireRadius", 25, false);
            Scribe_Values.Look<int>(ref this.bombIntervalTicks, "bombIntervalTicks", 18, false);
            Scribe_Values.Look<int>(ref this.warmupTicks, "warmupTicks", 0, false);
            Scribe_Values.Look<int>(ref this.ticksToNextEffect, "ticksToNextEffect", 0, false);
            Scribe_Values.Look<IntVec3>(ref this.nextExplosionCell, "nextExplosionCell", default(IntVec3), false);
            Scribe_Collections.Look<Bombardment.BombardmentProjectile>(ref this.projectiles, "projectiles", LookMode.Deep, Array.Empty<object>());
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (!this.nextExplosionCell.IsValid)
                {
                    this.GetNextExplosionCell();
                }
                if (this.projectiles == null)
                {
                    this.projectiles = new List<Bombardment.BombardmentProjectile>();
                }
            }
        }

        // Token: 0x04004EDA RID: 20186
        public float impactAreaRadius = 15f;

        // Token: 0x04004EDB RID: 20187
        public FloatRange explosionRadiusRange = new FloatRange(6f, 8f);

        // Token: 0x04004EDC RID: 20188
        public int randomFireRadius = 25;

        // Token: 0x04004EDD RID: 20189
        public int bombIntervalTicks = 18;

        // Token: 0x04004EDE RID: 20190
        public int warmupTicks = 60;

        // Token: 0x04004EDF RID: 20191
        public int explosionCount = 30;

        // Token: 0x04004EE0 RID: 20192
        private int ticksToNextEffect;

        // Token: 0x04004EE1 RID: 20193
        private IntVec3 nextExplosionCell = IntVec3.Invalid;

        // Token: 0x04004EE2 RID: 20194
        private List<Bombardment.BombardmentProjectile> projectiles = new List<Bombardment.BombardmentProjectile>();

        // Token: 0x04004EE3 RID: 20195
        public const int EffectiveAreaRadius = 23;

        // Token: 0x04004EE4 RID: 20196
        private const int StartRandomFireEveryTicks = 20;

        // Token: 0x04004EE5 RID: 20197
        private const int EffectDuration = 60;

        // Token: 0x04004EE6 RID: 20198
        private static readonly Material ProjectileMaterial = MaterialPool.MatFrom("Things/Projectile/Bullet_Big", ShaderDatabase.Transparent, Color.white);

        // Token: 0x04004EE7 RID: 20199
        public static readonly SimpleCurve DistanceChanceFactor = new SimpleCurve
        {
            {
                new CurvePoint(0f, 1f),
                true
            },
            {
                new CurvePoint(1f, 0.1f),
                true
            }
        };

        // Token: 0x020030E3 RID: 12515
        public class BombardmentProjectile : IExposable
        {
            // Token: 0x17002C44 RID: 11332
            // (get) Token: 0x060119E0 RID: 72160 RVA: 0x00582BA9 File Offset: 0x00580DA9
            public int LifeTime
            {
                get
                {
                    return this.lifeTime;
                }
            }

            // Token: 0x060119E1 RID: 72161 RVA: 0x0000254B File Offset: 0x0000074B
            public BombardmentProjectile()
            {
            }

            // Token: 0x060119E2 RID: 72162 RVA: 0x00582BB1 File Offset: 0x00580DB1
            public BombardmentProjectile(int lifeTime, IntVec3 targetCell)
            {
                this.lifeTime = lifeTime;
                this.maxLifeTime = lifeTime;
                this.targetCell = targetCell;
            }

            // Token: 0x060119E3 RID: 72163 RVA: 0x00582BCE File Offset: 0x00580DCE
            public void Tick()
            {
                this.lifeTime--;
            }

            // Token: 0x060119E4 RID: 72164 RVA: 0x00582BE0 File Offset: 0x00580DE0
            public void Draw(Material material)
            {
                if (this.lifeTime > 0)
                {
                    Vector3 pos = this.targetCell.ToVector3() + Vector3.forward * Mathf.Lerp(60f, 0f, 1f - (float)this.lifeTime / (float)this.maxLifeTime);
                    pos.z += 1.25f;
                    pos.y = AltitudeLayer.MoteOverhead.AltitudeFor();
                    Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.Euler(0f, 180f, 0f), new Vector3(2.5f, 1f, 2.5f));
                    Graphics.DrawMesh(MeshPool.plane10, matrix, material, 0);
                }
            }

            // Token: 0x060119E5 RID: 72165 RVA: 0x00582C94 File Offset: 0x00580E94
            public void ExposeData()
            {
                Scribe_Values.Look<int>(ref this.lifeTime, "lifeTime", 0, false);
                Scribe_Values.Look<int>(ref this.maxLifeTime, "maxLifeTime", 0, false);
                Scribe_Values.Look<IntVec3>(ref this.targetCell, "targetCell", default(IntVec3), false);
            }

            // Token: 0x0400BE46 RID: 48710
            private int lifeTime;

            // Token: 0x0400BE47 RID: 48711
            private int maxLifeTime;

            // Token: 0x0400BE48 RID: 48712
            public IntVec3 targetCell;

            // Token: 0x0400BE49 RID: 48713
            private const float StartZ = 60f;

            // Token: 0x0400BE4A RID: 48714
            private const float Scale = 2.5f;

            // Token: 0x0400BE4B RID: 48715
            private const float Angle = 180f;
        }
    }
}
