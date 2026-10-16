using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    /// <summary>
    /// 自定义气体渲染层。继承原版 SectionLayer_Gas 的绘制/可见性/quad 生成，
    /// 每种气体一个子网格并用同一个 GasRotating shader——
    /// 密度写进顶点色的对应通道（与原版 4 种气体的通道位一致），
    /// 因此旋转动画、贴图混合、消散淡出全部复用原版实现。
    /// </summary>
    public class SectionLayer_CustomGas : SectionLayer_Gas
    {
        private CustomGasGrid grid;
        private CustomGasTypeDef currentGas;

        public SectionLayer_CustomGas(Section section) : base(section)
        {
        }

        /// <summary>
        /// 顶点色 x 通道写原始密度值（0-255），对应 _MainTex 贴图槽；
        /// 其余通道为 0，配合材质中其余贴图槽的透明占位，shader 只采样本气体贴图。
        /// </summary>
        public override Color ColorAt(IntVec3 cell)
        {
            return new Color(grid.DensityAt(cell, currentGas), 0f, 0f, 0f);
        }

        public override void Regenerate()
        {
            ClearSubMeshes(MeshParts.All);
            grid = base.Map.CustomGasGrid();
            if (grid == null || !grid.AnyGasEverAdded)
            {
                return;
            }
            float altitude = AltitudeLayer.Gas.AltitudeFor();
            List<CustomGasTypeDef> defs = DefDatabase<CustomGasTypeDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                currentGas = defs[i];
                LayerSubMesh subMesh = null;
                int index = section.botLeft.x;
                foreach (IntVec3 cell in section.CellRect)
                {
                    if (grid.DensityAt(cell, currentGas) > 0)
                    {
                        if (subMesh == null)
                        {
                            subMesh = GetSubMesh(currentGas.Material);
                        }
                        AddCell(cell, index, subMesh.verts.Count, subMesh, altitude);
                    }
                    index++;
                }
                if (subMesh != null && subMesh.verts.Count > 0)
                {
                    subMesh.FinalizeMesh(MeshParts.All);
                }
            }
            currentGas = null;
        }
    }
}
