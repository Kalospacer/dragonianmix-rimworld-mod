using Verse;

namespace DragonianMix
{
    public class HediffShieldCompProperties : HediffCompProperties
    {
        public bool CanFunction;
        public bool FunctionalModule;
        public bool Adaptive;
        public int Hardened;

        public HediffShieldCompProperties()
        {
            compClass = typeof(HediffCompShield);
        }
    }

    public class HediffCompShield : HediffComp
    {
        public HediffShieldCompProperties Props => (HediffShieldCompProperties)props;
    }

    public class HediffShield_Implant : Hediff_Implant
    {
        // 有效状态由组件按已安装模块的顺序推算，不是第二份运行时状态。
        public bool Active { get; internal set; } = true;
        public override string Label => Active ? base.Label : base.Label + " (" + "DRMShieldDisabled".Translate() + ")";

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            pawn.GetComp<DRMCompShield>().Notify_ModulesChanged(initializeEnergy: true);
        }

        public override void PostRemoved()
        {
            base.PostRemoved();
            pawn.GetComp<DRMCompShield>().Notify_ModulesChanged();
        }
    }
}
