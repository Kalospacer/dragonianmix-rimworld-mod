using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    public class OPToxicGas : Gas
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, true);
            this.destroyTick = Find.TickManager.TicksGame + this.def.gas.expireSeconds.RandomInRange.SecondsToTicks();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<int>(ref this.destroyTick, "destroyTick", 0, false);
        }

        protected override void Tick()
        {
            bool flag = this.destroyTick <= Find.TickManager.TicksGame;
            if (flag)
            {
                this.Destroy(DestroyMode.Vanish);
            }
            this.graphicRotation += this.graphicRotationSpeed;
            bool flag2 = !base.Destroyed && Find.TickManager.TicksGame % OPToxicDefGetValue.OPToxicGetSevUpVal(this.def) == 0;
            if (flag2)
            {
                Map map = base.Map;
                IntVec3 position = base.Position;
                List<Thing> thingList = position.GetThingList(map);
                bool flag3 = thingList.Count > 0;
                if (flag3)
                {
                    for (int i = 0; i < thingList.Count; i++)
                    {
                        bool flag4 = thingList[i] is Pawn && !(thingList[i] as Pawn).RaceProps.IsMechanoid && thingList[i].Position == position;
                        if (flag4)
                        {
                            this.DoOPToxicGas(this, thingList[i]);
                        }
                    }
                }
            }
        }

        public void DoOPToxicGas(Thing Gas, Thing targ)
        {
            Pawn pawn = targ as Pawn;
            bool flag = pawn != null && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Breathing);
            if (flag)
            {
                HediffDef namedSilentFail = DefDatabase<HediffDef>.GetNamedSilentFail(OPToxicDefGetValue.OPToxicGetHediff(Gas.def));
                bool flag2 = namedSilentFail != null;
                if (flag2)
                {
                    Pawn_HealthTracker health = pawn.health;
                    bool flag3 = health == null;
                    Hediff hediff;
                    if (flag3)
                    {
                        hediff = null;
                    }
                    else
                    {
                        HediffSet hediffSet = health.hediffSet;
                        hediff = ((hediffSet != null) ? hediffSet.GetFirstHediffOfDef(namedSilentFail, false) : null);
                    }
                    float num = 1f - pawn.GetStatValue(StatDefOf.ToxicResistance, true, -1);
                    float num2 = OPToxicDefGetValue.OPToxicGetSev(Gas.def);
                    bool flag4 = num2 < 0.01f;
                    if (flag4)
                    {
                        num2 = 0.01f;
                    }
                    float num3 = Rand.Range(0.01f * num, num2 * num);
                    bool flag5 = hediff != null && num3 > 0f;
                    if (flag5)
                    {
                        hediff.Severity += num3;
                    }
                    else
                    {
                        Hediff hediff2 = HediffMaker.MakeHediff(namedSilentFail, pawn, null);
                        hediff2.Severity = num3;
                        pawn.health.AddHediff(hediff2, null, null, null);
                    }
                }
            }
        }
    }

    public class OPToxicDefGetValue
    {
        public static string OPToxicGetHediff(ThingDef thingdef)
        {
            bool flag = thingdef.HasModExtension<OPToxicDefs>();
            string result;
            if (flag)
            {
                result = thingdef.GetModExtension<OPToxicDefs>().OPToxicHediff;
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static float OPToxicGetSev(ThingDef thingdef)
        {
            bool flag = thingdef.HasModExtension<OPToxicDefs>();
            float result;
            if (flag)
            {
                result = thingdef.GetModExtension<OPToxicDefs>().OPToxicSeverity;
            }
            else
            {
                result = 0f;
            }
            return result;
        }

        public static int OPToxicGetSevUpVal(ThingDef thingdef)
        {
            bool flag = thingdef.HasModExtension<OPToxicDefs>();
            int result;
            if (flag)
            {
                result = thingdef.GetModExtension<OPToxicDefs>().OPSevUpTickPeriod;
            }
            else
            {
                result = 120;
            }
            return result;
        }
    }

    public class OPToxicDefs : DefModExtension
    {
        public string OPToxicHediff;

        public float OPToxicSeverity;

        public int OPSevUpTickPeriod = 120;
    }

    public class DamageWorker_OPToxic : DamageWorker
    {
        public override void ExplosionStart(Explosion explosion, List<IntVec3> cellsToAffect)
        {
            bool flag = this.def.explosionHeatEnergyPerCell > float.Epsilon;
            if (flag)
            {
                GenTemperature.PushHeat(explosion.Position, explosion.Map, this.def.explosionHeatEnergyPerCell * (float)cellsToAffect.Count);
            }
            FleckMaker.Static(explosion.Position, explosion.Map, FleckDefOf.ExplosionFlash, explosion.radius * 6f);
            FleckMaker.Static(explosion.Position, explosion.Map, FleckDefOf.ExplosionFlash, explosion.radius * 6f);
            this.ExplosionVisualEffectCenter(explosion);
        }

        public override DamageWorker.DamageResult Apply(DamageInfo dinfo, Thing victim)
        {
            DamageWorker.DamageResult damageResult = new DamageWorker.DamageResult();
            bool flag = victim.def.category == ThingCategory.Pawn && victim.def.useHitPoints && dinfo.Def.harmsHealth;
            if (flag)
            {
                float amount = dinfo.Amount;
                damageResult.totalDamageDealt = (float)Mathf.Min(victim.HitPoints, GenMath.RoundRandom(amount));
                victim.HitPoints -= Mathf.RoundToInt(damageResult.totalDamageDealt);
                bool flag2 = victim.HitPoints <= 0;
                if (flag2)
                {
                    victim.HitPoints = 0;
                    victim.Kill(new DamageInfo?(dinfo), null);
                }
            }
            return damageResult;
        }
    }
}
