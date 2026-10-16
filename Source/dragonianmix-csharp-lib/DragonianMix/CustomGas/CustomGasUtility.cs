using Verse;

namespace DragonianMix
{
    /// <summary>自定义气体对外 API，签名对齐原版 GasUtility。</summary>
    public static class CustomGasUtility
    {
        public static CustomGasGrid CustomGasGrid(this Map map)
        {
            return map?.GetComponent<CustomGasGrid>();
        }

        public static void AddGas(IntVec3 cell, Map map, CustomGasTypeDef gas, int amount)
        {
            map.CustomGasGrid()?.AddGas(cell, gas, amount);
        }

        public static void AddGas(IntVec3 cell, Map map, CustomGasTypeDef gas, float radius)
        {
            map.CustomGasGrid()?.AddGas(cell, gas, 255 * GenRadial.NumCellsInRadius(radius));
        }

        public static byte CustomGasDensity(this IntVec3 cell, Map map, CustomGasTypeDef gas)
        {
            CustomGasGrid grid = map.CustomGasGrid();
            return grid == null ? (byte)0 : grid.DensityAt(cell, gas);
        }

        public static float CustomGasDensityPercent(this IntVec3 cell, Map map, CustomGasTypeDef gas)
        {
            return cell.CustomGasDensity(map, gas) / 255f;
        }

        public static bool AnyCustomGas(this IntVec3 cell, Map map, CustomGasTypeDef gas)
        {
            return cell.CustomGasDensity(map, gas) > 0;
        }
    }
}
