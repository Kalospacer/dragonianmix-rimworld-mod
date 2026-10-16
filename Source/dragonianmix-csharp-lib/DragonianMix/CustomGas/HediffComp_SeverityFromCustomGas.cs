using RimWorld;
using Verse;

namespace DragonianMix
{
    /// <summary>对齐原版 HediffCompProperties_SeverityFromGas，气体类型换为 CustomGasTypeDef。</summary>
    public class HediffCompProperties_SeverityFromCustomGas : HediffCompProperties
    {
        public CustomGasTypeDef gasType;

        public float severityGasDensityFactor;

        public int intervalTicks = 60;

        public float severityNotExposed;

        public StatDef exposureStatFactor;

        public HediffCompProperties_SeverityFromCustomGas()
        {
            compClass = typeof(HediffComp_SeverityFromCustomGas);
        }
    }

    /// <summary>对齐原版 HediffComp_SeverityFromGas。</summary>
    public class HediffComp_SeverityFromCustomGas : HediffComp
    {
        public HediffCompProperties_SeverityFromCustomGas Props => (HediffCompProperties_SeverityFromCustomGas)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn pawn = parent.pawn;
            if (!pawn.Spawned || !pawn.IsHashIntervalTick(Props.intervalTicks))
            {
                return;
            }
            byte density = pawn.Position.CustomGasDensity(pawn.Map, Props.gasType);
            if (density > 0)
            {
                float num = density / 255f * Props.severityGasDensityFactor;
                if (Props.exposureStatFactor != null)
                {
                    num *= 1f - pawn.GetStatValue(Props.exposureStatFactor);
                }
                severityAdjustment += num;
            }
            else
            {
                severityAdjustment += Props.severityNotExposed;
            }
        }
    }
}
