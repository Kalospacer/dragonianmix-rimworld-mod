using UnityEngine;
using Verse;

namespace DragonianMix
{
    [StaticConstructorOnStartup]
    public class Gizmo_PlasmaShieldStatus : Gizmo
    {
        public DRMCompShield shield;
        private static readonly Texture2D FullBar = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        private static readonly Texture2D EmptyBar = SolidColorMaterials.NewSolidColorTexture(Color.clear);

        public Gizmo_PlasmaShieldStatus()
        {
            Order = -100f;
        }

        public override float GetWidth(float maxWidth) => 140f;

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Rect content = rect.ContractedBy(6f);
            Widgets.DrawWindowBackground(rect);
            Rect label = content;
            label.height = rect.height / 2f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(label, "DRMShieldLabel".Translate());
            Rect bar = content;
            bar.yMin = content.y + content.height / 2f;
            Widgets.FillableBar(bar, shield.EnergyMax > 0f ? shield.Energy / shield.EnergyMax : 0f, FullBar, EmptyBar, false);
            Text.Font = GameFont.Small;
            if (shield.AdaptedDamage != null)
            {
                Text.Anchor = TextAnchor.LowerCenter;
                Widgets.Label(bar, shield.AdaptedDamage.LabelCap);
                Text.Anchor = TextAnchor.UpperCenter;
            }
            else Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(bar, (shield.Energy * 100f).ToString("F0") + " / " + (shield.EnergyMax * 100f).ToString("F0"));
            Text.Anchor = TextAnchor.UpperLeft;
            string tip = shield.HardenedCap > 0f ? "DRMShieldHardenedTip" : shield.IsAdaptive ? "DRMShieldAdaptiveTip" : "DRMShieldTip";
            TooltipHandler.TipRegion(content, tip.Translate());
            return new GizmoResult(GizmoState.Clear);
        }
    }
}
