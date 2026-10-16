using HarmonyLib;
using Verse;

namespace DragonianMix
{
    public class DragonianMix : Mod
    {
        public DragonianMix(ModContentPack content) : base(content)
        {
            new Harmony("com.kalospacer.DragonianMix").PatchAll();
        }
    }
}
