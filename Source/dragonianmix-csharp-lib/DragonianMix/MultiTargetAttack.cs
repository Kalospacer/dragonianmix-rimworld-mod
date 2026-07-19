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
    // 自定义组件类
    public class CompMultiTargetAttack : ThingComp
    {
        public CompProperties_MultiTargetAttack Props => (CompProperties_MultiTargetAttack)props;

        // 监听攻击完成事件
        public void Notify_AttackSucceeded(Pawn attacker, Verb verb, LocalTargetInfo hitTarget)
        {
            //base.Notify_AttackSucceeded(attacker, verb, hitTarget);

            // 仅处理装备此武器时的攻击
            if (verb is Verb_MeleeAttack && parent == verb.EquipmentSource)
            {
                ApplyAOEAttack(attacker, hitTarget);
            }
        }

        private void ApplyAOEAttack(Pawn attacker, LocalTargetInfo primaryTarget)
        {
            // 检测扇形区域内的敌人
            IEnumerable<Pawn> enemies = GenRadial
                .RadialDistinctThingsAround(attacker.Position, attacker.Map, Props.radius, true)
                .OfType<Pawn>()
                .Where(p => p != primaryTarget.Thing &&
                           p.HostileTo(attacker.Faction) &&
                           IsInSector(p.Position, attacker.Position, attacker.Rotation.AsAngle, Props.angle));

            // 对每个额外目标施加伤害
            foreach (Pawn target in enemies)
            {
                ApplySecondaryDamage(attacker, target);
            }
        }

        private bool IsInSector(IntVec3 targetPos, IntVec3 originPos, float centerAngle, float sectorWidth)
        {
            float angleToTarget = (targetPos - originPos).AngleFlat;
            // GenMath.AbsAngleDifference在1.6中不存在，使用Mathf.DeltaAngle代替
            return Mathf.Abs(Mathf.DeltaAngle(centerAngle, angleToTarget)) <= sectorWidth / 2f;
        }

        private void ApplySecondaryDamage(Pawn attacker, Pawn target)
        {
            DamageInfo dinfo = new DamageInfo(
                def: Props.damageDef ?? DamageDefOf.Blunt,
                amount: Props.extraDamage,
                armorPenetration: 0.5f,
                instigator: attacker,
                hitPart: null
            );
            target.TakeDamage(dinfo);
        }
    }

    // 组件属性定义
    public class CompProperties_MultiTargetAttack : CompProperties
    {
        public float radius = 2.5f;
        public float angle = 120f;
        public int extraDamage = 15;
        public DamageDef damageDef;

        public CompProperties_MultiTargetAttack() =>
            compClass = typeof(CompMultiTargetAttack);
    }
}
