using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    public class CompPropertiesScarletSickle_Data : CompProperties
    {
        public CompPropertiesScarletSickle_Data()
        {
            this.compClass = typeof(CompScarletSickle_Data);
        }

        public float sputteringRange = 2f;
        public float sputteringNumber = 5f;
        public float sputteringDamageAmount = 10f;
        public DamageDef sputteringDamage = null;
        public HediffDef hitHediff;
        public float hitHediffSeverity = 1f;  // 新增严重性配置字段
    }

    public class CompScarletSickle_Data : ThingComp
    {
        public CompPropertiesScarletSickle_Data Props
        {
            get
            {
                return (CompPropertiesScarletSickle_Data)this.props;
            }
        }
    }
    public class Verb_MeleeAttack_ScarletSickle : Verb_MeleeAttackDamage
    {
        private CompScarletSickle_Data CompData
        {
            get
            {
                return base.EquipmentSource.GetComp<CompScarletSickle_Data>();
            }
        }

        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            IntVec3 position = target.Thing.Position;
            Vector3 drawPos = target.Thing.DrawPos;
            Vector3 origin = this.caster.DrawPos;
            List<IntVec3> list = new List<IntVec3>();
            Map map = this.caster.Map;
            int num = (int)this.CompData.Props.sputteringRange;

            List<IntVec3> list2 = GenRadial.RadialCellsAround(position, (float)num, true).ToList<IntVec3>();
            list2.RemoveAll((IntVec3 x) => x == origin.ToIntVec3());

            bool flag = position.GetFirstPawn(map) != null;
            if (flag)
            {
                // 使用XML配置的严重性值
                ((Pawn)this.caster).health.AddHediff(this.CompData.Props.hitHediff).Severity = this.CompData.Props.hitHediffSeverity;

                List<IntVec3> cells_team = new List<IntVec3>();
                foreach (IntVec3 intVec in list2)
                {
                    bool flag2 = intVec.GetFirstPawn(map) != null;
                    if (flag2)
                    {
                        bool flag3 = !this.caster.HostileTo(intVec.GetFirstPawn(map));
                        if (flag3)
                        {
                            cells_team.Add(intVec);
                        }
                    }
                }
                list2.RemoveAll((IntVec3 x) => cells_team.Exists((IntVec3 s) => s == x));

                bool flag4 = list2.Count > 0;
                if (flag4)
                {
                    foreach (IntVec3 intVec2 in list2)
                    {
                        int num2 = (int)this.CompData.Props.sputteringRange;
                        bool flag5 = intVec2.GetFirstPawn(map) != null & intVec2 != position & intVec2 != origin.ToIntVec3() & list.Count < num2;
                        if (flag5)
                        {
                            bool flag6 = target.Cell.GetFirstPawn(map) != null;
                            if (flag6)
                            {
                                bool flag7 = !intVec2.GetFirstPawn(map).HostileTo(target.Cell.GetFirstPawn(map));
                                if (flag7)
                                {
                                    list.Add(intVec2);
                                }
                            }
                            else
                            {
                                bool flag8 = drawPos.ToIntVec3().GetFirstBuilding(map) != null;
                                if (flag8)
                                {
                                    list.Add(intVec2);
                                }
                                else
                                {
                                    bool flag9 = target.Cell.GetFirstPawn(map) == null;
                                    if (flag9)
                                    {
                                        bool flag10 = intVec2.GetFirstPawn(map).HostileTo(this.caster);
                                        if (flag10)
                                        {
                                            list.Add(intVec2);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            DamageDef def = this.CompData.Props.sputteringDamage;
            bool flag11 = this.CompData.Props.sputteringDamage == null;
            if (flag11)
            {
                def = base.EquipmentSource.def.tools[0].Maneuvers.First<ManeuverDef>().verb.meleeDamageDef;
            }

            float sputteringDamageAmount = this.CompData.Props.sputteringDamageAmount;
            float meleeArmorPenetrationBase = base.EquipmentSource.def.tools[0].Maneuvers.First<ManeuverDef>().verb.meleeArmorPenetrationBase;
            DamageInfo dinfo = new DamageInfo(def, sputteringDamageAmount, meleeArmorPenetrationBase, -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, true, true, QualityCategory.Normal, true);

            foreach (IntVec3 c in list)
            {
                bool flag12 = c.GetFirstPawn(map) != null;
                if (flag12)
                {
                    Pawn firstPawn = c.GetFirstPawn(map);
                    BattleLogEntry_RangedImpact battleLogEntry_RangedImpact = new BattleLogEntry_RangedImpact(this.caster, c.GetFirstPawn(map), c.GetFirstPawn(map), base.EquipmentSource.def, null, null);
                    Find.BattleLog.Add(battleLogEntry_RangedImpact);
                    firstPawn.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_RangedImpact);
                }
                else
                {
                    bool flag13 = c.GetFirstBuilding(map) != null;
                    if (flag13)
                    {
                        c.GetFirstBuilding(map).TakeDamage(dinfo);
                    }
                }
            }

            return base.ApplyMeleeDamageToTarget(target);
        }
    }
}