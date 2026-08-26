using System.Collections.Generic;
using Verse;
using RimWorld;

namespace DragonianMix
{
    public class CompProperties_CustomUniqueWeapon : CompProperties_UniqueWeapon
    {
        // XML 可指定的强制词条列表
        public List<WeaponTraitDef> forcedTraits;

        // 词条数量范围，XML 未设置时默认 1-3（与原版一致）
        public IntRange numTraitsRange = new IntRange(1, 3);

        public CompProperties_CustomUniqueWeapon()
        {
            this.compClass = typeof(CompCustomUniqueWeapon);
        }
    }
}
