using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Systems;
using ReLogic.Content;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalButtonUiState : UIState
{
	private JournalButton _button = null!;

	public override void OnInitialize()
	{
		_button = new JournalButton();
		Append(_button);
	}

	public override void Update(GameTime gameTime)
	{
		_button.UpdatePlacement();
		base.Update(gameTime);

		if (!_button.IsMouseHovering) return;
		Main.LocalPlayer.mouseInterface = true;
		Main.blockMouse = true;
	}

	private sealed class JournalButton : UIElement
	{
		private const float ButtonSize = 34f;
		private const float AccessorySlotStep = 47f;
		private const float HorizontalSpacing = 8f;
		private const float VerticalSpacing = 6f;
		private static readonly Asset<Texture2D> IconTexture =
			ModContent.Request<Texture2D>("ProgressionJournal/Assets/UI/JournalButtonIcon");
		private static readonly Color HoverGlowColor = new(170, 208, 240);
		private static readonly Color ActiveGlowColor = new(165, 214, 124);
		private static readonly Color ShadowColor = new(10, 12, 20);

		public JournalButton()
		{
			Width.Set(ButtonSize, 0f);
			Height.Set(ButtonSize, 0f);
			OnLeftClick += (_, _) => JournalSystem.ToggleView();
		}

		public void UpdatePlacement()
		{
			var defensePosition = AccessorySlotLoader.DefenseIconPosition;

			if (defensePosition == Vector2.Zero) {
				return;
			}

			// AccessorySlotLoader.DefenseIconPosition uses the functional accessory slot column as its X anchor.
			// To avoid overlapping the accessory row above, place the button in the free area left of the dye/vanity/function trio.
			var x = defensePosition.X - AccessorySlotStep * 2f - Width.Pixels - HorizontalSpacing;
			var y = defensePosition.Y - Height.Pixels - VerticalSpacing;

			Left.Set(MathF.Round(x), 0f);
			Top.Set(MathF.Round(y), 0f);
			Recalculate();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);

			var dimensions = GetDimensions();
			var icon = IconTexture.Value;
			float scale = MathF.Min((dimensions.Width - 2f) / icon.Width, (dimensions.Height - 2f) / icon.Height);
			float pulseScale = JournalSystem.Visible ? 1.06f : IsMouseHovering ? 1.02f : 1f;
			Vector2 origin = icon.Size() * 0.5f;
			Vector2 center = dimensions.Center();
			Color glowColor = JournalSystem.Visible
				? ActiveGlowColor
				: IsMouseHovering
					? HoverGlowColor
					: Color.Transparent;

			if (glowColor != Color.Transparent) {
				float glowScale = scale * (pulseScale + 0.08f);
				spriteBatch.Draw(icon, center + new Vector2(-1f, 0f), null, glowColor * 0.22f, 0f, origin, glowScale, SpriteEffects.None, 0f);
				spriteBatch.Draw(icon, center + new Vector2(1f, 0f), null, glowColor * 0.22f, 0f, origin, glowScale, SpriteEffects.None, 0f);
				spriteBatch.Draw(icon, center + new Vector2(0f, -1f), null, glowColor * 0.22f, 0f, origin, glowScale, SpriteEffects.None, 0f);
				spriteBatch.Draw(icon, center + new Vector2(0f, 1f), null, glowColor * 0.22f, 0f, origin, glowScale, SpriteEffects.None, 0f);
			}

			spriteBatch.Draw(icon, center + new Vector2(1f, 2f), null, ShadowColor * 0.55f, 0f, origin, scale * pulseScale, SpriteEffects.None, 0f);
			spriteBatch.Draw(icon, center, null, Color.White, 0f, origin, scale * pulseScale, SpriteEffects.None, 0f);

			if (!IsMouseHovering) {
				return;
			}

			Main.hoverItemName = Language.GetTextValue("Mods.ProgressionJournal.UI.InventoryButtonTooltip");
		}

		private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
	}
}
