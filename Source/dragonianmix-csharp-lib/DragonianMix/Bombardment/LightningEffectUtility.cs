using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DragonianMix
{
    /// <summary>闪电落雷视听效果，供落雷技能与雷电轰炸共用。</summary>
    [StaticConstructorOnStartup]
    public static class LightningEffectUtility
    {
        private static readonly Material LightningMat = MatLoader.LoadMat("Weather/LightningBolt");

        public static void Spawn(IntVec3 strikeLoc, Map map)
        {
            if (!strikeLoc.IsValid || map == null)
            {
                return;
            }
            SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera(map);
            if (strikeLoc.Fogged(map))
            {
                return;
            }
            Vector3 position = strikeLoc.ToVector3Shifted();
            for (int i = 0; i < 4; i++)
            {
                FleckMaker.ThrowSmoke(position, map, 1.5f);
                FleckMaker.ThrowMicroSparks(position, map);
                FleckMaker.ThrowLightningGlow(position, map, 1.5f);
            }
            Graphics.DrawMesh(LightningBoltMeshPool.RandomBoltMesh, strikeLoc.ToVector3ShiftedWithAltitude(AltitudeLayer.Weather), Quaternion.identity, FadedMaterialPool.FadedVersionOf(LightningMat, 1f), 0);
            SoundDefOf.Thunder_OnMap.PlayOneShot(SoundInfo.InMap(new TargetInfo(strikeLoc, map)));
        }
    }
}
