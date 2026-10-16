using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    /// <summary>
    /// 自定义气体密度网格，平行于原版 GasGrid。
    /// 每种 <see cref="CustomGasTypeDef"/> 一个 byte[] 密度层，消散/扩散/溢出/存档逻辑复刻原版。
    /// MapComponent 由原版 Map.FillComponents 自动实例化。
    /// </summary>
    public class CustomGasGrid : MapComponent
    {
        private const int MaxDensity = 255;
        private const int MinDiffusion = 17;
        private const float CellsToDissipatePerTickFactor = 1f / 64f;
        private const float CellsToDiffusePerTickFactor = 1f / 32f;
        private const float VacuumDissipatePerTickFactor = 25f;
        private const float MaxOverflowFloodfillRadius = 40f;
        private const int AnyGasCheckIntervalTicks = 600;

        // key = CustomGasTypeDef.index
        private Dictionary<ushort, byte[]> layers = new Dictionary<ushort, byte[]>();

        private int cycleIndexDiffusion;
        private int cycleIndexDissipation;
        private bool anyGasEverAdded;

        private readonly List<IntVec3> cardinalDirections = new List<IntVec3>(GenAdj.CardinalDirections);
        private static readonly IntVec3[] beqCells = new IntVec3[4];

        public CustomGasGrid(Map map) : base(map)
        {
            cycleIndexDiffusion = Rand.Range(0, map.Area / 2);
        }

        public bool AnyGasEverAdded => anyGasEverAdded;

        private int CellCount => map.cellIndices.NumGridCells;

        private byte[] GetLayer(CustomGasTypeDef gas, bool create)
        {
            if (layers.TryGetValue(gas.index, out byte[] arr))
            {
                return arr;
            }
            if (!create)
            {
                return null;
            }
            arr = new byte[CellCount];
            layers[gas.index] = arr;
            return arr;
        }

        public byte DensityAt(IntVec3 cell, CustomGasTypeDef gas)
        {
            byte[] layer = GetLayer(gas, false);
            return layer == null ? (byte)0 : layer[map.cellIndices.CellToIndex(cell)];
        }

        public float DensityPercentAt(IntVec3 cell, CustomGasTypeDef gas)
        {
            return DensityAt(cell, gas) / 255f;
        }

        public bool AnyGasAt(IntVec3 cell)
        {
            return AnyGasAt(map.cellIndices.CellToIndex(cell));
        }

        private bool AnyGasAt(int index)
        {
            foreach (byte[] layer in layers.Values)
            {
                if (layer[index] != 0)
                {
                    return true;
                }
            }
            return false;
        }

        public bool GasCanMoveTo(IntVec3 cell)
        {
            return map.gasGrid.GasCanMoveTo(cell);
        }

        public void AddGas(IntVec3 cell, CustomGasTypeDef gas, int amount, bool canOverflow = true)
        {
            if (gas == null || amount <= 0 || !GasCanMoveTo(cell))
            {
                return;
            }
            anyGasEverAdded = true;
            byte[] layer = GetLayer(gas, true);
            int index = map.cellIndices.CellToIndex(cell);
            int newDensity = layer[index] + amount;
            int overflow = Math.Max(0, newDensity - MaxDensity);
            layer[index] = (byte)Mathf.Clamp(newDensity, 0, MaxDensity);
            map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Gas);
            if (canOverflow && overflow > 0)
            {
                Overflow(cell, gas, overflow);
            }
        }

        private void Overflow(IntVec3 cell, CustomGasTypeDef gas, int amount)
        {
            int remaining = amount;
            map.floodFiller.FloodFill(cell, GasCanMoveTo, delegate (IntVec3 c)
            {
                int add = Mathf.Min(remaining, MaxDensity - DensityAt(c, gas));
                if (add > 0)
                {
                    AddGas(c, gas, add, canOverflow: false);
                    remaining -= add;
                }
                return remaining <= 0;
            }, GenRadial.NumCellsInRadius(MaxOverflowFloodfillRadius), rememberParents: true);
        }

        public override void MapComponentTick()
        {
            if (!anyGasEverAdded)
            {
                return;
            }
            int area = map.Area;
            List<IntVec3> cells = map.cellsInRandomOrder.GetAll();
            int dissipateCount = Mathf.CeilToInt(area * CellsToDissipatePerTickFactor);
            for (int i = 0; i < dissipateCount; i++)
            {
                if (cycleIndexDissipation >= area)
                {
                    cycleIndexDissipation = 0;
                }
                TryDissipate(cells[cycleIndexDissipation]);
                cycleIndexDissipation++;
            }
            int diffuseCount = Mathf.CeilToInt(area * CellsToDiffusePerTickFactor);
            for (int j = 0; j < diffuseCount; j++)
            {
                if (cycleIndexDiffusion >= area)
                {
                    cycleIndexDiffusion = 0;
                }
                TryDiffuse(cells[cycleIndexDiffusion]);
                cycleIndexDiffusion++;
            }
            if (map.IsHashIntervalTick(AnyGasCheckIntervalTicks))
            {
                RecalculateEverHadGas();
            }
        }

        private void RecalculateEverHadGas()
        {
            anyGasEverAdded = false;
            foreach (byte[] layer in layers.Values)
            {
                for (int i = 0; i < layer.Length; i++)
                {
                    if (layer[i] != 0)
                    {
                        anyGasEverAdded = true;
                        return;
                    }
                }
            }
        }

        private void TryDissipate(IntVec3 cell)
        {
            int index = map.cellIndices.CellToIndex(cell);
            if (!AnyGasAt(index))
            {
                return;
            }
            float factor = (cell.Roofed(map) ? 0.5f : 1f) + cell.GetVacuum(map) * VacuumDissipatePerTickFactor;
            bool cleared = false;
            foreach (KeyValuePair<ushort, byte[]> pair in layers)
            {
                byte density = pair.Value[index];
                if (density == 0)
                {
                    continue;
                }
                CustomGasTypeDef gas = DefDatabase<CustomGasTypeDef>.AllDefsListForReading[pair.Key];
                int next = Math.Max(density - Mathf.RoundToInt(gas.dissipationAmount * factor), 0);
                pair.Value[index] = (byte)next;
                if (next == 0)
                {
                    cleared = true;
                }
            }
            if (cleared)
            {
                map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Gas);
            }
        }

        private void TryDiffuse(IntVec3 cell)
        {
            int index = map.cellIndices.CellToIndex(cell);
            bool changed = false;
            cardinalDirections.Shuffle();
            foreach (KeyValuePair<ushort, byte[]> pair in layers)
            {
                byte[] layer = pair.Value;
                if (layer[index] < MinDiffusion || !DefDatabase<CustomGasTypeDef>.AllDefsListForReading[pair.Key].diffuses)
                {
                    continue;
                }
                int gasA = layer[index];
                for (int i = 0; i < cardinalDirections.Count; i++)
                {
                    IntVec3 neighbor = cell + cardinalDirections[i];
                    if (!GasCanMoveTo(neighbor))
                    {
                        continue;
                    }
                    int neighborIndex = map.cellIndices.CellToIndex(neighbor);
                    int gasB = layer[neighborIndex];
                    if (TryDiffuseIndividualGas(ref gasA, ref gasB))
                    {
                        layer[neighborIndex] = (byte)gasB;
                        map.mapDrawer.MapMeshDirty(neighbor, MapMeshFlagDefOf.Gas);
                        changed = true;
                        if (gasA < MinDiffusion)
                        {
                            break;
                        }
                    }
                }
                layer[index] = (byte)gasA;
            }
            if (changed)
            {
                map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Gas);
            }
        }

        private static bool TryDiffuseIndividualGas(ref int gasA, ref int gasB)
        {
            if (gasA < MinDiffusion)
            {
                return false;
            }
            int half = Mathf.Abs(gasA - gasB) / 2;
            if (gasA > gasB && half >= MinDiffusion)
            {
                gasA -= half;
                gasB += half;
                return true;
            }
            return false;
        }

        /// <summary>对应原版 GasGrid.EqualizeGasThroughBuilding（通风口）。</summary>
        public void EqualizeGasThroughBuilding(Building b, bool twoWay)
        {
            if (!anyGasEverAdded)
            {
                return;
            }
            int count = 0;
            if (twoWay)
            {
                VisitCell(b.Position + b.Rotation.FacingCell, ref count);
                VisitCell(b.Position - b.Rotation.FacingCell, ref count);
            }
            else
            {
                for (int k = 0; k < 4; k++)
                {
                    VisitCell(b.Position + GenAdj.CardinalDirections[k], ref count);
                }
            }
            if (count <= 1)
            {
                return;
            }
            foreach (byte[] layer in layers.Values)
            {
                int total = 0;
                for (int i = 0; i < count; i++)
                {
                    total += layer[map.cellIndices.CellToIndex(beqCells[i])];
                }
                byte average = (byte)Mathf.Min(total / count, MaxDensity);
                for (int i = 0; i < count; i++)
                {
                    layer[map.cellIndices.CellToIndex(beqCells[i])] = average;
                }
            }
            for (int i = 0; i < count; i++)
            {
                map.mapDrawer.MapMeshDirty(beqCells[i], MapMeshFlagDefOf.Gas);
            }
        }

        private void VisitCell(IntVec3 cell, ref int count)
        {
            if (cell.IsValid && GasCanMoveTo(cell))
            {
                beqCells[count] = cell;
                count++;
            }
        }

        /// <summary>对应原版：满填充建筑生成时清空所占格子的气体。</summary>
        public void Notify_ThingSpawned(Thing thing)
        {
            if (!thing.Spawned || thing.def.Fillage != FillCategory.Full)
            {
                return;
            }
            foreach (IntVec3 cell in thing.OccupiedRect())
            {
                if (cell.InBounds(map) && AnyGasAt(cell))
                {
                    ClearCellUnsafe(cell);
                    map.mapDrawer.MapMeshDirty(cell, MapMeshFlagDefOf.Gas);
                }
            }
        }

        public void ClearCellUnsafe(IntVec3 cell)
        {
            int index = map.cellIndices.CellToIndex(cell);
            foreach (byte[] layer in layers.Values)
            {
                layer[index] = 0;
            }
        }

        public void ClearAll()
        {
            foreach (byte[] layer in layers.Values)
            {
                Array.Clear(layer, 0, layer.Length);
            }
            anyGasEverAdded = false;
        }

        /// <summary>每 50 tick 由 GasUtility.PawnGasEffectsTickInterval 补丁调用，与原版气体节奏一致。</summary>
        public void ApplyPawnEffects(Pawn pawn)
        {
            if (!anyGasEverAdded)
            {
                return;
            }
            int index = map.cellIndices.CellToIndex(pawn.Position);
            foreach (KeyValuePair<ushort, byte[]> pair in layers)
            {
                byte density = pair.Value[index];
                if (density == 0)
                {
                    continue;
                }
                CustomGasTypeDef gas = DefDatabase<CustomGasTypeDef>.AllDefsListForReading[pair.Key];
                if (gas.exposureHediff == null || gas.severityPerExposure <= 0f)
                {
                    continue;
                }
                if (!gas.affectsMechanoids && pawn.RaceProps.IsMechanoid)
                {
                    continue;
                }
                if (gas.requiresBreathing && !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Breathing))
                {
                    continue;
                }
                float severity = gas.severityPerExposure * (density / 255f);
                if (gas.exposureStatFactor != null)
                {
                    severity *= Mathf.Max(0f, 1f - pawn.GetStatValue(gas.exposureStatFactor));
                }
                if (severity > 0f)
                {
                    HealthUtility.AdjustSeverity(pawn, gas.exposureHediff, severity);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            foreach (CustomGasTypeDef gas in DefDatabase<CustomGasTypeDef>.AllDefsListForReading)
            {
                byte[] arr = GetLayer(gas, false);
                DataExposeUtility.LookByteArray(ref arr, "gas_" + gas.defName);
                if (Scribe.mode == LoadSaveMode.LoadingVars && arr != null)
                {
                    if (arr.Length == CellCount)
                    {
                        layers[gas.index] = arr;
                        anyGasEverAdded = true;
                    }
                    else
                    {
                        Log.Warning($"[DragonianMix] 自定义气体 {gas.defName} 的存档数据长度与地图不符，已丢弃。");
                    }
                }
            }
            Scribe_Values.Look(ref cycleIndexDiffusion, "cycleIndexDiffusion", 0);
            Scribe_Values.Look(ref cycleIndexDissipation, "cycleIndexDissipation", 0);
        }
    }
}
