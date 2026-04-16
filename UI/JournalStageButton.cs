using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI;

public sealed class JournalStageButton : UIPanel
{
	private const float DefaultTextScale = 0.9f;
	private const float IconPadding = 6f;
	private const float IconOverlap = 10f;
	private enum HeadTextureKind
	{
		Boss,
		Town
	}

	private UIText _label;
	private readonly Action _onClick;
	private float _textScale;
	private readonly List<(HeadTextureKind Kind, int Slot)> _headSlots = [];

	public JournalStageButton(Action onClick)
	{
		_onClick = onClick;
		_textScale = DefaultTextScale;
		SetPadding(0f);
		BackgroundColor = new Color(29, 42, 58);
		BorderColor = new Color(88, 115, 142);

		_label = CreateLabel(string.Empty);
		Append(_label);

		OnLeftClick += (_, _) => _onClick();
	}

	public void SetTextDisplay(string text, float textScale)
	{
		_headSlots.Clear();

		if (Math.Abs(_textScale - textScale) >= 0.001f) {
			_textScale = textScale;
			RemoveChild(_label);
			_label = CreateLabel(text);
			Append(_label);
			return;
		}

		_label.SetText(text);
	}

	public void SetBossHeadDisplay(params int[] bossHeadSlots)
	{
		_headSlots.Clear();
		foreach (int bossHeadSlot in bossHeadSlots) {
			_headSlots.Add((HeadTextureKind.Boss, bossHeadSlot));
		}

		_label.SetText(string.Empty);
	}

	public void SetNpcHeadDisplay(int npcHeadSlot)
	{
		_headSlots.Clear();
		_headSlots.Add((HeadTextureKind.Town, npcHeadSlot));

		_label.SetText(string.Empty);
	}

	public void SetTextColor(Color color)
	{
		_label.TextColor = color;
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		base.DrawSelf(spriteBatch);

		if (_headSlots.Count == 0) {
			return;
		}

		var dimensions = GetInnerDimensions();
		float maxWidth = Math.Max(0f, dimensions.Width - IconPadding * 2f);
		float maxHeight = Math.Max(0f, dimensions.Height - IconPadding * 2f);
		if (maxWidth <= 0f || maxHeight <= 0f) {
			return;
		}

		float slotWidth = (maxWidth + IconOverlap * (_headSlots.Count - 1)) / _headSlots.Count;
		if (slotWidth <= 0f) {
			return;
		}

		float totalWidth = slotWidth * _headSlots.Count - IconOverlap * (_headSlots.Count - 1);
		float startX = dimensions.Center().X - totalWidth * 0.5f + slotWidth * 0.5f;
		var shadowColor = new Color(10, 12, 20) * 0.55f;
		var iconColor = IsMouseHovering ? Color.White : new Color(235, 240, 245);

		for (int index = 0; index < _headSlots.Count; index++) {
			if (!TryGetHeadTexture(_headSlots[index], out Texture2D texture)) {
				continue;
			}

			float iconWidth = texture.Width;
			float iconHeight = texture.Height;
			float scale = MathF.Min(slotWidth / iconWidth, maxHeight / iconHeight);
			scale = MathF.Min(scale, 1.35f);
			Vector2 drawPosition = new(startX + index * (slotWidth - IconOverlap), dimensions.Center().Y);
			Vector2 origin = new(iconWidth * 0.5f, iconHeight * 0.5f);

			spriteBatch.Draw(texture, drawPosition + new Vector2(1f, 2f), null, shadowColor, 0f, origin, scale, SpriteEffects.None, 0f);
			spriteBatch.Draw(texture, drawPosition, null, iconColor, 0f, origin, scale, SpriteEffects.None, 0f);
		}
	}

	private UIText CreateLabel(string text)
	{
		var label = new UIText(text, _textScale, false) {
			HAlign = 0.5f,
			VAlign = 0.5f
		};
		label.TextColor = new Color(226, 233, 240);
		return label;
	}

	private static bool TryGetHeadTexture((HeadTextureKind Kind, int Slot) head, out Texture2D texture)
	{
		switch (head.Kind) {
			case HeadTextureKind.Boss:
				if (head.Slot >= 0 && head.Slot < TextureAssets.NpcHeadBoss.Length) {
					texture = TextureAssets.NpcHeadBoss[head.Slot].Value;
					return true;
				}
				break;

			case HeadTextureKind.Town:
				if (head.Slot >= 0 && head.Slot < TextureAssets.NpcHead.Length) {
					texture = TextureAssets.NpcHead[head.Slot].Value;
					return true;
				}
				break;
		}

		texture = null!;
		return false;
	}
}
