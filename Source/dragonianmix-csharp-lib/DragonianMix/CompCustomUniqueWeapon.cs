using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace DragonianMix
{
    /// <summary>
    /// 继承原版 CompUniqueWeapon，额外支持 XML 强制指定词条与可配置词条数量。
    /// 词条数据直接写入基类 traits 列表，复用原版全部消费链路（弹道/连发/属性/UI/序列化）。
    /// 刻意跳过基类的随机命名与染色（见 TransformLabel/ForceColor override）。
    /// </summary>
    public class CompCustomUniqueWeapon : CompUniqueWeapon
    {
        public CompProperties_CustomUniqueWeapon CustomProps => (CompProperties_CustomUniqueWeapon)props;

        public override void PostPostMake()
        {
            InitializeCustomTraits();
            if (parent.TryGetComp<CompQuality>(out var comp))
            {
                comp.SetQuality(QualityUtility.GenerateQuality(QualityGenerator.Super), ArtGenerationContext.Outsider);
            }
        }

        private void InitializeCustomTraits()
        {
            // 强制词条优先写入基类列表，跳过与已加词条互斥的
            if (CustomProps.forcedTraits != null)
            {
                foreach (WeaponTraitDef forced in CustomProps.forcedTraits)
                {
                    bool overlapsExisting = false;
                    foreach (WeaponTraitDef existing in TraitsListForReading)
                    {
                        if (existing.Overlaps(forced))
                        {
                            overlapsExisting = true;
                            break;
                        }
                    }
                    if (!overlapsExisting)
                    {
                        AddTrait(forced);
                    }
                }
            }

            // 随机词条补足到目标数量
            IntRange range = CustomProps.numTraitsRange;
            int target = Mathf.Max(TraitsListForReading.Count, range.RandomInRange);
            while (TraitsListForReading.Count < target)
            {
                List<WeaponTraitDef> candidates = DefDatabase<WeaponTraitDef>.AllDefs.Where(CanAddTrait).ToList();
                if (candidates.Count == 0) break;
                AddTrait(candidates.RandomElementByWeight(t => t.commonality));
            }

            Setup(fromSave: false);
        }

        public override string TransformLabel(string label) => label;

        public override Color? ForceColor() => null;
    }
}
