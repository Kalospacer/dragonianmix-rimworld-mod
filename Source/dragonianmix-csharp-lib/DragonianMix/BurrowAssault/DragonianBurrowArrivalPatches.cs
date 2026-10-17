using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DragonianMix
{
    [HarmonyPatch]
    public static class DragonianBurrowArrivalPatches
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] signature = { typeof(List<Pawn>), typeof(IncidentParms) };
            yield return AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_EdgeDrop), "Arrive", signature);
            yield return AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_EdgeDropGroups), "Arrive", signature);
            yield return AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_CenterDrop), "Arrive", signature);
            yield return AccessTools.DeclaredMethod(typeof(PawnsArrivalModeWorker_RandomDrop), "Arrive", signature);
        }

        public static bool Prefix(List<Pawn> pawns, IncidentParms parms)
        {
            // 选法、点数、选点和回退都已由原版完成；这里只替换真正的入场动作。
            return !DragonianBurrowUtility.TryArrive(pawns, parms);
        }
    }
}
