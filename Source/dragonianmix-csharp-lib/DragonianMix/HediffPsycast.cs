using RimWorld;
using Verse;

namespace DragonianMix
{
    public class HediffPsycast : Psycast
    {
        public HediffPsycast()
            : base(null)
        {
        }

        public HediffPsycast(Pawn pawn)
            : base(pawn)
        {
        }

        public HediffPsycast(Pawn pawn, AbilityDef def)
            : base(pawn, def)
        {
        }
    }
}
