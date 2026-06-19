using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ProgressionJournal.UI.Visuals.Elements;

internal static class JournalTooltip
{
    private const float MaxWidth = 360f;
    private const float TextScale = 1f;
    private const int Padding = 10;
    private const int Offset = 16;

    private static string? _pendingText;

    public static void Request(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            _pendingText = text;
        }
    }

    public static void Clear()
    {
        _pendingText = null;
    }

    public static bool DrawPending(SpriteBatch spriteBatch)
    {
        if (string.IsNullOrWhiteSpace(_pendingText))
        {
            return true;
        }

        Draw(spriteBatch, _pendingText);
        _pendingText = null;
        return true;
    }

    private static void Draw(SpriteBatch spriteBatch, string text)
    {
        var font = FontAssets.MouseText.Value;
        var lines = JournalTextUtilities.WrapToPixelWidth(text, MaxWidth, TextScale);
        if (lines.Count == 0)
        {
            return;
        }

        var lineHeight = font.LineSpacing * TextScale;
        var textWidth = lines.Max(line => font.MeasureString(line).X * TextScale);
        var width = (int)MathF.Ceiling(textWidth) + Padding * 2;
        var height = (int)MathF.Ceiling(lineHeight * lines.Count) + Padding * 2;
        var mouse = Main.MouseScreen;
        var x = mouse.X + Offset;
        var y = mouse.Y + Offset;

        if (x + width + Offset > Main.screenWidth)
        {
            x = mouse.X - width - Offset;
        }

        if (y + height + Offset > Main.screenHeight)
        {
            y = mouse.Y - height - Offset;
        }

        x = MathHelper.Clamp(x, Offset, Math.Max(Offset, Main.screenWidth - width - Offset));
        y = MathHelper.Clamp(y, Offset, Math.Max(Offset, Main.screenHeight - height - Offset));

        JournalSourceCardRenderer.DrawTooltip(
            spriteBatch,
            new Rectangle((int)x, (int)y, width, height),
            JournalUiTheme.PresetPanelBorder);

        for (var index = 0; index < lines.Count; index++)
        {
            Utils.DrawBorderStringFourWay(
                spriteBatch,
                font,
                lines[index],
                x + Padding,
                y + Padding + index * lineHeight,
                JournalUiTheme.ContentDescriptionText,
                Color.Black * 0.75f,
                Vector2.Zero);
        }
    }
}
