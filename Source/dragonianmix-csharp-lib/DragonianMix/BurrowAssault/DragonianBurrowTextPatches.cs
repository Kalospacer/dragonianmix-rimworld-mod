using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace DragonianMix
{
    [HarmonyPatch]
    public static class DragonianBurrowTextPatches
    {
        private static readonly FieldInfo ArrivalModeField = AccessTools.Field(typeof(IncidentParms), nameof(IncidentParms.raidArrivalMode));

        private static readonly Dictionary<FieldInfo, MethodInfo> TextReaders = new Dictionary<FieldInfo, MethodInfo>
        {
            { AccessTools.Field(typeof(PawnsArrivalModeDef), nameof(PawnsArrivalModeDef.textEnemy)),
                AccessTools.DeclaredMethod(typeof(DragonianBurrowTextPatches), nameof(GetEnemyArrivalText)) },
            { AccessTools.Field(typeof(PawnsArrivalModeDef), nameof(PawnsArrivalModeDef.textFriendly)),
                AccessTools.DeclaredMethod(typeof(DragonianBurrowTextPatches), nameof(GetFriendlyArrivalText)) },
            { AccessTools.Field(typeof(PawnsArrivalModeDef), nameof(PawnsArrivalModeDef.textWillArrive)),
                AccessTools.DeclaredMethod(typeof(DragonianBurrowTextPatches), nameof(GetFutureArrivalText)) }
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type[] letterSignature = { typeof(IncidentParms), typeof(List<Pawn>) };
            yield return AccessTools.DeclaredMethod(typeof(IncidentWorker_RaidEnemy), "GetLetterText", letterSignature);
            yield return AccessTools.DeclaredMethod(typeof(IncidentWorker_RaidFriendly), "GetLetterText", letterSignature);
            yield return AccessTools.DeclaredMethod(typeof(IncidentWorker_PsychicRitualSiege), "GetLetterText", letterSignature);
            yield return AccessTools.DeclaredMethod(typeof(QuestNode_Raid), "RunInt", Type.EmptyTypes);
            yield return AccessTools.DeclaredMethod(typeof(QuestGen_Threat), "Raid", new[]
            {
                typeof(Quest), typeof(MapParent), typeof(float), typeof(Faction),
                typeof(string), typeof(string), typeof(string), typeof(RulePack), typeof(RulePack),
                typeof(IntVec3?), typeof(string), typeof(string), typeof(string),
                typeof(PawnsArrivalModeDef), typeof(RaidStrategyDef), typeof(PawnGroupKindDef),
                typeof(bool), typeof(bool), typeof(bool), typeof(bool)
            });
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            List<CodeInstruction> codes = instructions.ToList();
            int matchIndex = -1;
            int matchCount = 0;
            for (int i = 0; i < codes.Count - 1; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && Equals(codes[i].operand, ArrivalModeField)
                    && codes[i + 1].opcode == OpCodes.Ldfld && codes[i + 1].operand is FieldInfo field
                    && TextReaders.ContainsKey(field))
                {
                    matchIndex = i;
                    matchCount++;
                }
            }
            if (matchCount != 1)
            {
                Log.Error($"[DragonianMix] 无法替换钻地入场文案：{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name} 应有 1 处入场文字读取，实际为 {matchCount}；保留原方法。");
                return codes;
            }

            MethodInfo reader = TextReaders[(FieldInfo)codes[matchIndex + 1].operand];
            // 原来的两次字段读取改为一次纯文字查询；原版事件参数和共享 Def 均不修改。
            codes[matchIndex].opcode = OpCodes.Call;
            codes[matchIndex].operand = reader;
            // 保留原指令的标签和异常块元数据。
            codes[matchIndex + 1].opcode = OpCodes.Nop;
            codes[matchIndex + 1].operand = null;
            return codes;
        }

        private static string GetEnemyArrivalText(IncidentParms parms)
        {
            return GetArrivalText(parms, "TextEnemy", parms.raidArrivalMode.textEnemy);
        }

        private static string GetFriendlyArrivalText(IncidentParms parms)
        {
            return GetArrivalText(parms, "TextFriendly", parms.raidArrivalMode.textFriendly);
        }

        private static string GetFutureArrivalText(IncidentParms parms)
        {
            return GetArrivalText(parms, "TextWillArrive", parms.raidArrivalMode.textWillArrive);
        }

        private static string GetArrivalText(IncidentParms parms, string suffix, string original)
        {
            BurrowArrivalKind kind = DragonianBurrowUtility.GetArrivalKind(parms);
            return kind == BurrowArrivalKind.None ? original : ($"DRM_Burrow{kind}_{suffix}").Translate().ToString();
        }
    }
}
