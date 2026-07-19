using System.Collections.Generic;
using Verse;
using RimWorld; // 引用RimWorld命名空间以使用其基类

namespace DragonianMix
{
    public class CompProperties_CustomUniqueWeapon : CompProperties_UniqueWeapon
    {
        // 一个用于无条件强制添加的特性列表
        public List<WeaponTraitDef> forcedTraits;

        // 一个可选的特性总数范围。如果XML中未设置，代码将使用默认值 1-3
        public IntRange? numTraitsRange;

        public CompProperties_CustomUniqueWeapon()
        {
            // 将这个属性类指向我们自定义的逻辑实现组件
            this.compClass = typeof(CompCustomUniqueWeapon);
        }
    }
}