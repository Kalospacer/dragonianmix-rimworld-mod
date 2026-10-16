using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;

namespace DragonianMix
{
    [HarmonyPatch]
    public static class DragonianBurrowArrivalPatches
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_EdgeDrop), "TryResolveRaidSpawnCenter");
            yield return AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_EdgeDropGroups), "TryResolveRaidSpawnCenter");
            yield return AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_CenterDrop), "TryResolveRaidSpawnCenter");
            yield return AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_RandomDrop), "TryResolveRaidSpawnCenter");
        }

        public static bool Prefix(IncidentParms parms, ref bool __result)
        {
            if (!DragonianBurrowUtility.IsDragonianFaction(parms.faction))
            {
                return true;
            }
            PawnsArrivalModeDef replacement = DragonianBurrowUtility.ReplacementFor(parms.raidArrivalMode);
            if (replacement == null)
            {
                return true;
            }
            // 指定入场、紧急援助和 CenterDrop 的边缘回退都会绕过模式黑名单。
            parms.raidArrivalMode = replacement;
            __result = replacement.Worker.TryResolveRaidSpawnCenter(parms);
            return false;
        }
    }
}
