using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DragonianMix
{
    /// <summary>挂在轰炸 ThingDef 上，替换原版 Bombardment 写死的 Bomb 伤害。</summary>
    public class BombardmentDamageExtension : DefModExtension
    {
        public DamageDef damageDef;

        public ThingDef postExplosionSpawnThingDef;

        public float postExplosionSpawnChance = 1f;
    }

    /// <summary>挂在皇家许可 Def 上，指定生成哪个轰炸 ThingDef。</summary>
    public class PermitBombardmentExtension : DefModExtension
    {
        public ThingDef bombardmentDef;
    }

    /// <summary>
    /// 原版 Bombardment.TryDoExplosion 为 private 且写死 DamageDefOf.Bomb。
    /// 带 <see cref="BombardmentDamageExtension"/> 的轰炸改用扩展中的伤害，其余原版表现（下落、点火、拦截、存档）全部保留。
    /// </summary>
    [HarmonyPatch(typeof(Bombardment), "TryDoExplosion", new[] { typeof(Bombardment.BombardmentProjectile) })]
    public static class Patch_Bombardment_TryDoExplosion
    {
        public static bool Prefix(Bombardment __instance, Bombardment.BombardmentProjectile proj)
        {
            BombardmentDamageExtension ext = __instance.def.GetModExtension<BombardmentDamageExtension>();
            if (ext?.damageDef == null)
            {
                return true;
            }
            List<Thing> interceptors = __instance.Map.listerThings.ThingsInGroup(ThingRequestGroup.ProjectileInterceptor);
            for (int i = 0; i < interceptors.Count; i++)
            {
                if (interceptors[i].TryGetComp<CompProjectileInterceptor>().CheckBombardmentIntercept(__instance, proj))
                {
                    return false;
                }
            }
            GenExplosion.DoExplosion(proj.targetCell, __instance.Map, __instance.explosionRadiusRange.RandomInRange, ext.damageDef, __instance.instigator,
                weapon: __instance.weaponDef,
                projectile: __instance.def,
                postExplosionSpawnThingDef: ext.postExplosionSpawnThingDef,
                postExplosionSpawnChance: ext.postExplosionSpawnChance);
            return false;
        }
    }
}
