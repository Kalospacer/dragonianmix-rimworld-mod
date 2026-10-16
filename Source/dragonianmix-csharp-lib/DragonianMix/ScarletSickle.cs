using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    public class CompPropertiesScarletSickle_Data : CompProperties
    {
        public float sputteringRange = 2f;
        public float sputteringNumber = 5f;
        public float sputteringDamageAmount = 10f;
        public DamageDef sputteringDamage;
        public HediffDef hitHediff;
        public float hitHediffSeverity = 1f;

        public CompPropertiesScarletSickle_Data()
        {
            compClass = typeof(CompScarletSickle_Data);
        }
    }

    public class CompScarletSickle_Data : ThingComp
    {
        public CompPropertiesScarletSickle_Data Props => (CompPropertiesScarletSickle_Data)props;
    }

    public class Verb_MeleeAttack_ScarletSickle : Verb_MeleeAttackDamage
    {
        protected override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            CompPropertiesScarletSickle_Data settings = EquipmentSource.GetComp<CompScarletSickle_Data>().Props;
            IntVec3 primaryCell = target.Thing.Position;
            IntVec3 sourceCell = caster.DrawPos.ToIntVec3();
            Map map = caster.Map;
            int range = (int)settings.sputteringRange;
            List<IntVec3> splashCells = new List<IntVec3>();
            Pawn primaryPawn = primaryCell.GetFirstPawn(map);
            if (primaryPawn != null)
            {
                ((Pawn)caster).health.AddHediff(settings.hitHediff).Severity = settings.hitHediffSeverity;
                // 沿用旧玩法：目标数量上限也是 sputteringRange，不在清理中改成 sputteringNumber。
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(primaryCell, range, true))
                {
                    if (cell == sourceCell || cell == primaryCell || splashCells.Count >= range) continue;
                    Pawn candidate = cell.GetFirstPawn(map);
                    if (candidate != null && caster.HostileTo(candidate) && !candidate.HostileTo(primaryPawn))
                    {
                        splashCells.Add(cell);
                    }
                }
            }
            VerbProperties melee = EquipmentSource.def.tools[0].Maneuvers.First().verb;
            DamageInfo damage = new DamageInfo(settings.sputteringDamage ?? melee.meleeDamageDef,
                settings.sputteringDamageAmount, melee.meleeArmorPenetrationBase);
            foreach (IntVec3 cell in splashCells)
            {
                Pawn pawn = cell.GetFirstPawn(map);
                if (pawn != null)
                {
                    BattleLogEntry_RangedImpact log = new BattleLogEntry_RangedImpact(caster, pawn, pawn, EquipmentSource.def, null, null);
                    Find.BattleLog.Add(log);
                    pawn.TakeDamage(damage).AssociateWithLog(log);
                }
                else
                {
                    cell.GetFirstBuilding(map)?.TakeDamage(damage);
                }
            }
            return base.ApplyMeleeDamageToTarget(target);
        }
    }
}
