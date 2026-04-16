using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalRecommendationHeader : UIElement
{
	private readonly string _title;
	private readonly Color _accentColor;

	public JournalRecommendationHeader(string title, Color accentColor)
	{
		_title = title;
		_accentColor = accentColor;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		var dimensions = GetInnerDimensions();
		var pixel = TextureAssets.MagicPixel.Value;
		var font = FontAssets.MouseText.Value;
		const float textScale = 1.06f;
		const float sideInset = 10f;
		const float titleGap = 18f;
		const int mainLineThickness = 2;
		const int accentWidth = 10;
		const int accentHeight = 4;

		Vector2 titleSize = font.MeasureString(_title) * textScale;
		float centerX = dimensions.X + dimensions.Width * 0.5f;
		float centerY = dimensions.Y + dimensions.Height * 0.5f;
		float rightEdge = dimensions.X + dimensions.Width;
		float textX = centerX - titleSize.X * 0.5f;
		float textY = dimensions.Y + (dimensions.Height - titleSize.Y) * 0.5f - 1f;
		float leftEnd = centerX - titleSize.X * 0.5f - titleGap;
		float rightStart = centerX + titleSize.X * 0.5f + titleGap;
		int lineY = (int)(centerY + 1f);
		int secondaryLineY = lineY + 5;
		int accentY = lineY - 1;

		DrawSegment(spriteBatch, pixel, dimensions.X + sideInset, leftEnd, lineY, mainLineThickness, _accentColor * 0.9f);
		DrawSegment(spriteBatch, pixel, rightStart, rightEdge - sideInset, lineY, mainLineThickness, _accentColor * 0.9f);
		DrawSegment(spriteBatch, pixel, dimensions.X + sideInset + 12f, leftEnd - 8f, secondaryLineY, 1, _accentColor * 0.34f);
		DrawSegment(spriteBatch, pixel, rightStart + 8f, rightEdge - sideInset - 12f, secondaryLineY, 1, _accentColor * 0.34f);

		DrawAccent(spriteBatch, pixel, leftEnd - accentWidth, accentY, accentWidth, accentHeight, _accentColor);
		DrawAccent(spriteBatch, pixel, rightStart, accentY, accentWidth, accentHeight, _accentColor);

		Utils.DrawBorderStringFourWay(
			spriteBatch,
			font,
			_title,
			textX,
			textY,
			new Color(241, 244, 247),
			Color.Black * 0.7f,
			Vector2.Zero,
			textScale);
	}

	private static void DrawSegment(SpriteBatch spriteBatch, Texture2D pixel, float startX, float endX, int y, int thickness, Color color)
	{
		int width = (int)(endX - startX);
		if (width <= 0) {
			return;
		}

		spriteBatch.Draw(pixel, new Rectangle((int)startX, y, width, thickness), color);
	}

	private static void DrawAccent(SpriteBatch spriteBatch, Texture2D pixel, float x, float y, int width, int height, Color color)
	{
		spriteBatch.Draw(pixel, new Rectangle((int)x, (int)y, width, height), color);
	}
}
