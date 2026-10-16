using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace DragonianMix
{
    public class DRMCompShieldProp : CompProperties_Shield
    {
        public DRMCompShieldProp()
        {
            compClass = typeof(DRMCompShield);
            blocksRangedWeapons = false;
            energyOnReset = 0f;
            energyLossPerDamage = 0.01f;
            startingTicksToReset = 600;
        }
    }

    public class DRMCompShield : CompShield
    {
        private bool modulesInitialized;
        private int adaptiveLevel;
        private DamageDef adaptedDamage;

        public bool HasCore { get; private set; }
        public bool IsAdaptive { get; private set; }
        public float HardenedCap { get; private set; }
        public DamageDef AdaptedDamage => adaptedDamage;
        public float EnergyMax => parent.GetStatValue(StatDefOf.EnergyShieldEnergyMax) * (HardenedCap > 0f ? 0.5f : 1f);

        public void Notify_ModulesChanged(bool initializeEnergy = false)
        {
            bool hadCore = HasCore;
            HasCore = false;
            IsAdaptive = false;
            HardenedCap = 0f;
            bool functionalModuleFound = false;
            Pawn pawn = (Pawn)parent;
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (!(hediff is HediffShield_Implant implant)) continue;
                HediffShieldCompProperties module = implant.TryGetComp<HediffCompShield>().Props;
                implant.Active = !module.FunctionalModule || !functionalModuleFound;
                if (!implant.Active) continue;
                functionalModuleFound |= module.FunctionalModule;
                HasCore |= module.CanFunction;
                IsAdaptive |= module.Adaptive;
                HardenedCap += module.Hardened / 100f;
            }
            modulesInitialized = true;
            if (initializeEnergy && !hadCore && HasCore) energy = EnergyMax;
            if (!HasCore) energy = 0f;
        }

        public override void CompTick()
        {
            if (!modulesInitialized) Notify_ModulesChanged();
            if (!HasCore)
            {
                energy = 0f;
                return;
            }
            base.CompTick();
            energy = Mathf.Min(energy, EnergyMax);
        }

        public override void PostPreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            absorbed = false;
            if (!modulesInitialized) Notify_ModulesChanged();
            if (!HasCore) return;
            Pawn pawn = (Pawn)parent;
            // 保持原玩法：服装护盾优先。不可再 GetComp<CompShield>()，那会取到自己。
            if (pawn.apparel != null)
            {
                foreach (Apparel apparel in pawn.apparel.WornApparel)
                {
                    CompShield shield = apparel.GetComp<CompShield>();
                    if (shield == null) continue;
                    shield.PostPreApplyDamage(ref dinfo, out absorbed);
                    if (absorbed) return;
                }
            }
            if (ShieldState != ShieldState.Active) return;
            if (dinfo.Def == DamageDefOf.EMP)
            {
                BreakShield();
                return;
            }
            float previousEnergy = energy;
            float cost = dinfo.Amount * Props.energyLossPerDamage;
            if (HardenedCap > 0f) cost = Mathf.Min(cost, HardenedCap);
            else if (IsAdaptive)
            {
                if (dinfo.Def == adaptedDamage)
                {
                    cost *= 1.2f - 0.2f * adaptiveLevel;
                    adaptiveLevel = Mathf.Min(adaptiveLevel + 1, 5);
                }
                else
                {
                    cost *= 1.2f;
                    adaptedDamage = dinfo.Def;
                    adaptiveLevel = 1;
                }
            }
            energy -= cost;
            if (energy < 0f)
            {
                BreakShield();
                absorbed = !(dinfo.Amount * Props.energyLossPerDamage > previousEnergy * 2f && previousEnergy < EnergyMax / 2f);
            }
            else
            {
                AbsorbFx(dinfo);
                absorbed = true;
            }
        }

        private void AbsorbFx(DamageInfo damage)
        {
            SoundDefOf.EnergyShield_AbsorbDamage.PlayOneShot(new TargetInfo(parent.Position, parent.Map));
            Vector3 direction = Vector3Utility.HorizontalVectorFromAngle(damage.Angle);
            Vector3 loc = parent.TrueCenter() + direction.RotatedBy(180f) * 0.5f;
            float scale = Mathf.Min(10f, 2f + damage.Amount / 10f);
            FleckMaker.Static(loc, parent.Map, FleckDefOf.ExplosionFlash, scale);
            for (int i = 0; i < (int)scale; i++)
            {
                FleckMaker.ThrowDustPuff(loc, parent.Map, Rand.Range(0.8f, 1.2f));
            }
            KeepDisplaying();
        }

        private void BreakShield()
        {
            if (parent.Spawned)
            {
                EffecterDefOf.Shield_Break.SpawnAttached(parent, parent.MapHeld, Mathf.Lerp(Props.minDrawSize, Props.maxDrawSize, energy));
                FleckMaker.Static(parent.TrueCenter(), parent.Map, FleckDefOf.ExplosionFlash, 12f);
                for (int i = 0; i < 6; i++)
                {
                    FleckMaker.ThrowDustPuff(parent.TrueCenter() + Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * Rand.Range(0.3f, 0.6f), parent.Map, Rand.Range(0.8f, 1.2f));
                }
            }
            energy = 0f;
            ticksToReset = Props.startingTicksToReset;
        }

        public override void PostDraw()
        {
            if (HasCore) base.PostDraw();
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (HasCore && parent.Faction == Faction.OfPlayer && Find.Selector.SingleSelectedThing == parent)
            {
                yield return new Gizmo_PlasmaShieldStatus { shield = this };
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref adaptiveLevel, "adaptiveLevel");
            Scribe_Defs.Look(ref adaptedDamage, "adaptedDamage");
            if (Scribe.mode == LoadSaveMode.PostLoadInit) modulesInitialized = false;
        }
    }
}
