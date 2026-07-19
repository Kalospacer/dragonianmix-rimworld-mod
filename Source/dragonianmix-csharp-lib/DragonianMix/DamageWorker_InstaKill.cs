using RimWorld;
using Verse;

namespace DragonianMix
{
    public class InstaKillExtension : DefModExtension
    {
        public HediffDef hediff;
        // 根据Hediff严重性 (x) 决定触发所需的伤害百分比阈值 (y)
        public SimpleCurve requiredDamagePercentageCurve;
        public float destroyChance = 1.0f;
    }

    public class DamageWorker_InstaKill : DamageWorker_Frostbite
    {
        protected override void ApplySpecialEffectsToPart(Pawn pawn, float totalDamage, DamageInfo dinfo, DamageResult result)
        {
            var extension = def.GetModExtension<InstaKillExtension>();
            // 检查配置是否完整
            if (extension == null || extension.hediff == null || extension.requiredDamagePercentageCurve == null || dinfo.HitPart == null)
            {
                base.ApplySpecialEffectsToPart(pawn, totalDamage, dinfo, result);
                return;
            }

            // 获取受害者身上的Hediff
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(extension.hediff);
            if (hediff == null)
            {
                // 如果受害者没有指定的Hediff，则效果不生效
                base.ApplySpecialEffectsToPart(pawn, totalDamage, dinfo, result);
                return;
            }

            // 根据Hediff严重性从曲线计算所需的伤害百分比阈值
            float requiredPercentage = extension.requiredDamagePercentageCurve.Evaluate(hediff.Severity);
            float destroyChance = extension.destroyChance;

            // 计算本次伤害占目标部件最大生命值的实际百分比
            float actualDamagePercentage = totalDamage / dinfo.HitPart.def.GetMaxHealth(pawn);

            // 如果造成的实际伤害百分比 *大于或等于* 所需的百分比阈值，并且随机判定成功
            if (actualDamagePercentage >= requiredPercentage && Rand.Chance(destroyChance))
            {
                // 直接施加一个足以摧毁该部位的伤害
                base.ApplySpecialEffectsToPart(pawn, dinfo.HitPart.def.GetMaxHealth(pawn) + 1, dinfo, result);
            }
            else
            {
                // 否则，走标准伤害流程
                base.ApplySpecialEffectsToPart(pawn, totalDamage, dinfo, result);
            }
        }
    }
}
