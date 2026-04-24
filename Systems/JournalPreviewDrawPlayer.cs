using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ProgressionJournal.Systems;

public sealed class JournalPreviewDrawPlayer : ModPlayer
{
    public bool ForceFullBright { get; set; }

    public float ShadeOpacity { get; set; }

    public override void DrawEffects(
        PlayerDrawSet drawInfo,
        ref float r,
        ref float g,
        ref float b,
        ref float a,
        ref bool fullBright)
    {
        if (ForceFullBright)
        {
            fullBright = true;
        }
    }

    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (ForceFullBright)
        {
            drawInfo.colorMount = Color.White;
        }

        if (ShadeOpacity <= 0f)
        {
            return;
        }

        var brightness = 1f - MathHelper.Clamp(ShadeOpacity, 0f, 1f);
        drawInfo.colorHair = Darken(drawInfo.colorHair, brightness);
        drawInfo.colorEyeWhites = Darken(drawInfo.colorEyeWhites, brightness);
        drawInfo.colorEyes = Darken(drawInfo.colorEyes, brightness);
        drawInfo.colorHead = Darken(drawInfo.colorHead, brightness);
        drawInfo.colorBodySkin = Darken(drawInfo.colorBodySkin, brightness);
        drawInfo.colorLegs = Darken(drawInfo.colorLegs, brightness);
        drawInfo.colorShirt = Darken(drawInfo.colorShirt, brightness);
        drawInfo.colorUnderShirt = Darken(drawInfo.colorUnderShirt, brightness);
        drawInfo.colorPants = Darken(drawInfo.colorPants, brightness);
        drawInfo.colorShoes = Darken(drawInfo.colorShoes, brightness);
        drawInfo.colorArmorHead = Darken(drawInfo.colorArmorHead, brightness);
        drawInfo.colorArmorBody = Darken(drawInfo.colorArmorBody, brightness);
        drawInfo.colorArmorLegs = Darken(drawInfo.colorArmorLegs, brightness);
        drawInfo.colorDisplayDollSkin = Darken(drawInfo.colorDisplayDollSkin, brightness);
        drawInfo.itemColor = Darken(drawInfo.itemColor, brightness);
        drawInfo.headGlowColor = Darken(drawInfo.headGlowColor, brightness);
        drawInfo.bodyGlowColor = Darken(drawInfo.bodyGlowColor, brightness);
        drawInfo.armGlowColor = Darken(drawInfo.armGlowColor, brightness);
        drawInfo.legsGlowColor = Darken(drawInfo.legsGlowColor, brightness);
    }

    private static Color Darken(Color color, float brightness)
    {
        return new Color(
            (byte)(color.R * brightness),
            (byte)(color.G * brightness),
            (byte)(color.B * brightness),
            color.A);
    }
}
