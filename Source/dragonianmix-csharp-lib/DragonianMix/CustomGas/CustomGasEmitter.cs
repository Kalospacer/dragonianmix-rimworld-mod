using Verse;

namespace DragonianMix
{
    public class CustomGasEmitterExtension : DefModExtension
    {
        public CustomGasTypeDef gas;

        public int amount = 255;
    }

    /// <summary>
    /// 一次性放气器：被生成（例如爆炸的 postExplosionSpawnThingDef）时向所在格写入自定义气体，
    /// 下一 tick 自毁。效果等价于原版爆炸的 postExplosionGasType。
    /// </summary>
    public class CustomGasEmitter : Thing
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (respawningAfterLoad)
            {
                return;
            }
            CustomGasEmitterExtension ext = def.GetModExtension<CustomGasEmitterExtension>();
            if (ext?.gas != null)
            {
                CustomGasUtility.AddGas(Position, map, ext.gas, ext.amount);
            }
        }

        protected override void Tick()
        {
            // 不能在 SpawnSetup 中销毁：GenSpawn 返回后调用方还会访问该物体
            Destroy();
        }
    }
}
