using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DragonianMix
{
    public class ProphecyGearEffect : DefModExtension
    {
        public HediffDef hediffToApply;
        public float severityPerDamage = 0f;
        public float minSeverity = 0f;
        public float maxSeverity = 1f;
        public bool enableHediffEffect = true;

        public float extraDamageFactor = 0f;
        public DamageDef extraDamageType;
        public float armorPenetration = 0f;
        public BodyPartDef targetBodyPart;
        public bool enableExtraDamage = true;

        public HediffDef requiredBionicHediff;
    }

    [HarmonyPatch(typeof(Pawn), nameof(Pawn.PostApplyDamage), new[] { typeof(DamageInfo), typeof(float) })]
    public static class DRM_ProphecyGear_Patch
    {
        private static bool isApplyingExtraDamage = false;

        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (isApplyingExtraDamage) return;

            try
            {
                if (__instance == null || __instance.Dead || __instance.health == null) return;
                if (!(dinfo.Instigator is Pawn attacker) || attacker.apparel == null) return;
                if (totalDamageDealt <= 0) return;

                foreach (var apparel in attacker.apparel.WornApparel)
                {
                    ApplyEffect(__instance, attacker, apparel.def.GetModExtension<ProphecyGearEffect>(), totalDamageDealt, dinfo);
                }
                foreach (var hediff in attacker.health.hediffSet.hediffs)
                {
                    ApplyEffect(__instance, attacker, hediff.def.GetModExtension<ProphecyGearEffect>(), totalDamageDealt, dinfo);
                }
            }
            catch (Exception ex)
            {
                isApplyingExtraDamage = false;
                Log.Error($"[DragonianMix] Error in PostApplyDamage patch: {ex}");
            }
        }

        private static void ApplyEffect(Pawn target, Pawn attacker, ProphecyGearEffect effect, float damage, DamageInfo info)
        {
            if (effect == null || !IsEffectValid(effect, attacker)) return;
            if (effect.enableHediffEffect && effect.hediffToApply != null)
            {
                ApplyHediffEffect(target, effect, damage);
            }
            if (effect.enableExtraDamage && effect.extraDamageFactor > 0f && effect.extraDamageType != null)
            {
                isApplyingExtraDamage = true;
                try
                {
                    ApplyExtraDamage(target, effect, damage, info);
                }
                finally
                {
                    isApplyingExtraDamage = false;
                }
            }
        }

        // 检查效果是否有效（满足仿生体要求）
        private static bool IsEffectValid(ProphecyGearEffect effect, Pawn attacker)
        {
            // 如果没有要求仿生体，则始终有效
            if (effect.requiredBionicHediff == null)
                return true;

            // 检查攻击者是否拥有所需的仿生体
            return attacker.health.hediffSet.HasHediff(effect.requiredBionicHediff);
        }

        private static void ApplyHediffEffect(Pawn target, ProphecyGearEffect effect, float damageDealt)
        {
            try
            {
                double severityToAdd = (double)damageDealt * (double)effect.severityPerDamage;
                float finalSeverity = (float)severityToAdd;

                if (finalSeverity < 0.001f) return;

                var existingHediff = target.health.hediffSet.GetFirstHediffOfDef(effect.hediffToApply);

                if (existingHediff == null)
                {
                    Hediff newHediff = HediffMaker.MakeHediff(effect.hediffToApply, target);
                    if (newHediff != null)
                    {
                        newHediff.Severity = finalSeverity;

                        if (effect.minSeverity > 0)
                            newHediff.Severity = Math.Max(newHediff.Severity, effect.minSeverity);
                        if (effect.maxSeverity > 0)
                            newHediff.Severity = Math.Min(newHediff.Severity, effect.maxSeverity);

                        target.health.AddHediff(newHediff);
                    }
                }
                else
                {
                    float newSeverity = existingHediff.Severity + finalSeverity;

                    if (effect.minSeverity > 0)
                        newSeverity = Math.Max(newSeverity, effect.minSeverity);
                    if (effect.maxSeverity > 0)
                        newSeverity = Math.Min(newSeverity, effect.maxSeverity);

                    existingHediff.Severity = newSeverity;

                    if (existingHediff is HediffWithComps hwc)
                    {
                        var comp = hwc.TryGetComp<HediffComp_Disappears>();
                        if (comp != null)
                        {
                            comp.ticksToDisappear = comp.Props.disappearsAfterTicks.max;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[DragonianMix] Error in ApplyHediffEffect: {ex}");
            }
        }

        private static void ApplyExtraDamage(
            Pawn target,
            ProphecyGearEffect effect,
            float baseDamage,
            DamageInfo originalDinfo)
        {
            try
            {
                float extraDamageAmount = baseDamage * effect.extraDamageFactor;
                if (extraDamageAmount < 0.5f) extraDamageAmount = 0.5f;
                if (extraDamageAmount <= 0) return;

                BodyPartRecord hitPart = null;
                if (effect.targetBodyPart != null)
                {
                    var bodyPartDef = effect.targetBodyPart;
                    if (bodyPartDef != null)
                    {
                        hitPart = target.RaceProps.body.GetPartsWithDef(bodyPartDef).FirstOrFallback();
                    }
                }
                else
                {
                    hitPart = originalDinfo.HitPart;
                }

                DamageInfo extraDinfo = new DamageInfo(
                    def: effect.extraDamageType,
                    amount: extraDamageAmount,
                    armorPenetration: effect.armorPenetration,
                    angle: -1f,
                    instigator: originalDinfo.Instigator,
                    hitPart: hitPart,
                    weapon: originalDinfo.Weapon,
                    category: DamageInfo.SourceCategory.ThingOrUnknown,
                    intendedTarget: target
                );

                target.TakeDamage(extraDinfo);
            }
            catch (Exception ex)
            {
                Log.Error($"[DragonianMix] Error in ApplyExtraDamage: {ex}");
            }
        }
    }
}