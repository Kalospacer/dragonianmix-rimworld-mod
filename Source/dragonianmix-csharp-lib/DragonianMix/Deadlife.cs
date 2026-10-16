using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DragonianMix
{
    public class Hediff_NecroticVirus : HediffWithComps
    {
        private const int EffectInterval = 600;

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            // 立即触发转换逻辑
            TransformToShambler();
        }

        public override void PostTick()
        {
            base.PostTick();
            // 定期显示病毒效果
            if (pawn.IsHashIntervalTick(EffectInterval) && pawn.Spawned)
            {
                ShowVirusEffects();
            }
        }

        private void TransformToShambler()
        {
            try
            {
                // 确保Pawn有尸体
                if (pawn.Corpse == null)
                {
                    Log.Warning($"[NecroticVirus] No corpse found for {pawn.LabelShort}");
                    return;
                }

                // 检查尸体状态
                if (pawn.Corpse.Destroyed)
                {
                    Log.Warning($"[NecroticVirus] Corpse already destroyed for {pawn.LabelShort}");
                    return;
                }

                Map map = pawn.Corpse.Map;
                IntVec3 position = pawn.Corpse.Position;

                // 使用原版DeadLife逻辑转换尸体
                if (!MutantUtility.CanResurrectAsShambler(pawn.Corpse))
                {
                    return;
                }

                // 获取派系
                Faction faction = GetHostileFaction();

                // 使用原版转换方法
                MutantUtility.ResurrectAsShambler(pawn, faction: faction);

                // 添加转化特效
                FleckMaker.ThrowSmoke(position.ToVector3Shifted(), map, 1.5f);
                for (int i = 0; i < 3; i++)
                {
                    FleckMaker.ThrowDustPuff(position.ToVector3Shifted(), map, 1.2f);
                }

                // 发送转化消息
                if (PawnUtility.ShouldSendNotificationAbout(pawn))
                {
                    Messages.Message(
                        "NecroticVirus_TransformationMessage".Translate(pawn.LabelShortCap, pawn.LabelShortCap),
                        new LookTargets(position, map),
                        MessageTypeDefOf.NegativeEvent
                    );
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[NecroticVirus] Error during transformation: {ex}");
            }
        }

        private Faction GetHostileFaction()
        {
            // 优先使用原尸体的敌对派系
            if (pawn.Faction != null && pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                return pawn.Faction;
            }

            Faction entitiesFaction = Faction.OfEntities;
            if (entitiesFaction != null && !entitiesFaction.defeated)
            {
                return entitiesFaction;
            }

            // 否则使用随机敌对派系
            return Find.FactionManager.RandomEnemyFaction(
                allowNonHumanlike: true,
                allowHidden: false,
                allowDefeated: false
            );
        }

        private void ShowVirusEffects()
        {
            // 仅当pawn存在且在地图上时显示效果
            if (pawn == null || !pawn.Spawned) return;

            Vector3 pos = pawn.DrawPos;
            Map map = pawn.Map;

            // 根据阶段调整效果强度
            float sizeMultiplier = 1f;
            if (CurStageIndex == 1) sizeMultiplier = 1.2f;
            if (CurStageIndex == 2) sizeMultiplier = 1.5f;

            // 创建视觉特效
            FleckMaker.ThrowMicroSparks(pos, map);
            FleckMaker.ThrowDustPuff(pos, map, 0.8f * sizeMultiplier);
        }

        public override string TipStringExtra
        {
            get
            {
                return base.TipStringExtra + "\n" + "NecroticVirus_EffectTip".Translate();
            }
        }
    }
}
