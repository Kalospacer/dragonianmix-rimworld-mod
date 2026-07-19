using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
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

        // 新添加：触发效果所需的仿生体 Hediff
        public HediffDef requiredBionicHediff;
    }

    [HarmonyPatch(typeof(Pawn), "PostApplyDamage")]
    public static class DRM_ProphecyGear_Patch
    {
        private static readonly FieldInfo damageInfoWeaponField =
            typeof(DamageInfo).GetField("weapon", BindingFlags.Instance | BindingFlags.NonPublic);

        private static bool isApplyingExtraDamage = false;

        public static void Postfix(Pawn __instance, DamageInfo dinfo, float totalDamageDealt)
        {
            if (isApplyingExtraDamage) return;

            try
            {
                if (__instance == null || __instance.Dead || __instance.health == null) return;
                if (!(dinfo.Instigator is Pawn attacker) || attacker.apparel == null) return;
                if (totalDamageDealt <= 0) return;

                // 收集所有可能的效果来源
                var effectsToApply = new List<ProphecyGearEffect>();

                // 1. 从装备收集效果
                foreach (var apparel in attacker.apparel.WornApparel)
                {
                    var effect = apparel.def.GetModExtension<ProphecyGearEffect>();
                    if (effect != null && IsEffectValid(effect, attacker))
                    {
                        effectsToApply.Add(effect);
                    }
                }

                // 2. 从仿生体 Hediff 收集效果
                foreach (var hediff in attacker.health.hediffSet.hediffs)
                {
                    var effect = hediff.def.GetModExtension<ProphecyGearEffect>();
                    if (effect != null && IsEffectValid(effect, attacker))
                    {
                        effectsToApply.Add(effect);
                    }
                }

                // 应用所有效果
                foreach (var effect in effectsToApply)
                {
                    if (effect.enableHediffEffect && effect.hediffToApply != null)
                    {
                        ApplyHediffEffect(__instance, effect, totalDamageDealt);
                    }

                    if (effect.enableExtraDamage &&
                        effect.extraDamageFactor > 0 &&
                        effect.extraDamageType != null)
                    {
                        isApplyingExtraDamage = true;
                        ApplyExtraDamage(__instance, effect, totalDamageDealt, dinfo);
                        isApplyingExtraDamage = false;
                    }
                }
            }
            catch (Exception ex)
            {
                isApplyingExtraDamage = false;
                Log.Error($"[DragonianMix] Error in PostApplyDamage patch: {ex}");
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

                Thing weapon = null;
                if (damageInfoWeaponField != null)
                {
                    weapon = damageInfoWeaponField.GetValue(originalDinfo) as Thing;
                }

                BodyPartRecord hitPart = null;
                if (effect.targetBodyPart != null)
                {
                    var bodyPartDef = DefDatabase<BodyPartDef>.GetNamedSilentFail(effect.targetBodyPart.defName);
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