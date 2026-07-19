using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace DragonianMix
{
    public class DragonianMix : Mod
    {
        public static Harmony harmony;

        public DragonianMix(ModContentPack content) : base(content)
        {
            // 初始化Harmony实例
            harmony = new Harmony("com.kalospacer.DragonianMix");
            Log.Message("[DragonianMix] Harmony instance created");

            // 应用所有补丁
            harmony.PatchAll();
            Log.Message("[DragonianMix] All Harmony patches applied");
        }
    }

}
