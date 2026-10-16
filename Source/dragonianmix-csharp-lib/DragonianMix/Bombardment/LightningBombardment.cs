using RimWorld;
using UnityEngine;
using Verse;

namespace DragonianMix
{
    /// <summary>
    /// 雷电轰炸实体：按间隔在范围内随机落雷并爆炸，次数耗尽后自毁。
    /// 结构对齐原版 Bombardment（地图上的持久 Thing，完整存档），但不依赖轨道光束组件。
    /// </summary>
    public class LightningBombardment : ThingWithComps
    {
        public Thing instigator;
        public float impactAreaRadius = 10f;
        public FloatRange explosionRadiusRange = new FloatRange(3f, 4f);
        public int bombIntervalTicks = 30;
        public int explosionsRemaining = 3;
        public DamageDef damageDef;
        public int damageAmount = 30;
        public float armorPenetration = 0.8f;
        public ThingDef postExplosionSpawnThingDef;
        public float postExplosionSpawnChance;
        public int postExplosionSpawnThingCount = 1;

        private int ticksToNextBomb = -1;

        protected override void Tick()
        {
            base.Tick();
            if (ticksToNextBomb < 0)
            {
                ticksToNextBomb = bombIntervalTicks;
            }
            if (--ticksToNextBomb > 0)
            {
                return;
            }
            ExecuteBomb();
            explosionsRemaining--;
            if (explosionsRemaining <= 0)
            {
                Destroy();
                return;
            }
            ticksToNextBomb = bombIntervalTicks;
        }

        private void ExecuteBomb()
        {
            IntVec3 cell = Position;
            if (impactAreaRadius > 0f)
            {
                float radius = impactAreaRadius;
                IntVec3 center = Position;
                Map map = Map;
                if (CellFinder.TryFindRandomCellNear(center, map, Mathf.CeilToInt(radius), c => c.DistanceTo(center) <= radius && c.Standable(map), out IntVec3 found))
                {
                    cell = found;
                }
            }
            LightningEffectUtility.Spawn(cell, Map);
            GenExplosion.DoExplosion(cell, Map, explosionRadiusRange.RandomInRange, damageDef, instigator, damageAmount, armorPenetration,
                postExplosionSpawnThingDef: postExplosionSpawnThingDef,
                postExplosionSpawnChance: postExplosionSpawnChance,
                postExplosionSpawnThingCount: postExplosionSpawnThingCount,
                applyDamageToExplosionCellsNeighbors: true,
                chanceToStartFire: 0.4f,
                damageFalloff: true);
        }

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref instigator, "instigator");
            Scribe_Values.Look(ref impactAreaRadius, "impactAreaRadius", 10f);
            Scribe_Values.Look(ref explosionRadiusRange, "explosionRadiusRange", new FloatRange(3f, 4f));
            Scribe_Values.Look(ref bombIntervalTicks, "bombIntervalTicks", 30);
            Scribe_Values.Look(ref explosionsRemaining, "explosionsRemaining", 0);
            Scribe_Defs.Look(ref damageDef, "damageDef");
            Scribe_Values.Look(ref damageAmount, "damageAmount", 30);
            Scribe_Values.Look(ref armorPenetration, "armorPenetration", 0.8f);
            Scribe_Defs.Look(ref postExplosionSpawnThingDef, "postExplosionSpawnThingDef");
            Scribe_Values.Look(ref postExplosionSpawnChance, "postExplosionSpawnChance", 0f);
            Scribe_Values.Look(ref postExplosionSpawnThingCount, "postExplosionSpawnThingCount", 1);
            Scribe_Values.Look(ref ticksToNextBomb, "ticksToNextBomb", -1);
        }
    }
}
