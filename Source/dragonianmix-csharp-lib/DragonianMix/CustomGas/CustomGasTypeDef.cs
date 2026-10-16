using RimWorld;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    /// <summary>
    /// 自定义地图气体类型。每个 Def 在 <see cref="CustomGasGrid"/> 中拥有独立的密度层，
    /// 消散、扩散、渲染与暴露效果均按 Def 配置执行。
    /// </summary>
    public class CustomGasTypeDef : Def
    {
        // 渲染
        public Color color = Color.white;
        public string texPath = "Things/Gas/GasCloudThickA";

        // 原版：BlindSmoke/RotStink 为 4，ToxGas/DeadlifeDust 为 3
        public int dissipationAmount = 3;

        // 原版仅烟雾不扩散
        public bool diffuses = true;

        // 暴露效果：每 50 tick 按浓度累加严重度
        public HediffDef exposureHediff;
        public float severityPerExposure;
        public StatDef exposureStatFactor;
        public bool requiresBreathing = true;
        public bool affectsMechanoids;

        [Unsaved(false)]
        private Material materialInt;

        private static Texture2D emptyPlaceholderTex;

        private static Texture2D PlaceholderTex
        {
            get
            {
                if (emptyPlaceholderTex == null)
                {
                    emptyPlaceholderTex = new Texture2D(1, 1);
                    emptyPlaceholderTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
                    emptyPlaceholderTex.Apply();
                }
                return emptyPlaceholderTex;
            }
        }

        /// <summary>
        /// 每种气体一个独立 GasRotating 材质：_MainTex 放自己的贴图，
        /// 其余三个贴图槽塞 1px 透明占位。顶点色 x 通道写原始密度（0-255），
        /// 任意多种气体各占一个 subMesh，互不覆盖。
        /// </summary>
        public Material Material
        {
            get
            {
                if (materialInt == null)
                {
                    // _Color 作为 tint 乘到最终颜色上（原版 GasMat 是白色 = 无色调）
                    materialInt = MaterialPool.MatFrom(texPath, ShaderDatabase.GasRotating, color);
                    materialInt.SetTexture(ShaderPropertyIDs.ToxGasTex, PlaceholderTex);
                    materialInt.SetTexture(ShaderPropertyIDs.RotGasTex, PlaceholderTex);
                    materialInt.SetTexture(ShaderPropertyIDs.DeadlifeDustTex, PlaceholderTex);
                }
                return materialInt;
            }
        }

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            if (exposureStatFactor == null)
            {
                exposureStatFactor = StatDefOf.ToxicResistance;
            }
        }
    }
}
