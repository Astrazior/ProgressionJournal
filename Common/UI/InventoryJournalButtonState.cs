using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Common.Systems;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.Common.UI;

public sealed class InventoryJournalButtonState : UIState
{
	private InventoryJournalButton _button = null!;

	public override void OnInitialize()
	{
		_button = new InventoryJournalButton();
		Append(_button);
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		_button.UpdatePlacement();

		if (_button.IsMouseHovering) {
			Main.LocalPlayer.mouseInterface = true;
			Main.blockMouse = true;
		}
	}

	private sealed class InventoryJournalButton : UIPanel
	{
		private static readonly Color BaseBackgroundColor = new(33, 44, 57);
		private static readonly Color HoverBackgroundColor = new(50, 73, 96);
		private static readonly Color ActiveBackgroundColor = new(67, 89, 54);
		private static readonly Color BaseBorderColor = new(104, 130, 158);
		private static readonly Color HoverBorderColor = new(170, 208, 240);
		private static readonly Color ActiveBorderColor = new(165, 214, 124);

		public InventoryJournalButton()
		{
			Width.Set(40f, 0f);
			Height.Set(40f, 0f);
			SetPadding(0f);
			OnLeftClick += (_, _) => JournalSystem.ToggleView(syncStage: true);
		}

		public void UpdatePlacement()
		{
			float x = MathF.Min(540f, Main.screenWidth * 0.44f);
			float y = 248f;

			Left.Set(x, 0f);
			Top.Set(y, 0f);
			BackgroundColor = GetBackgroundColor();
			BorderColor = GetBorderColor();
			Recalculate();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);

			CalculatedStyle dimensions = GetInnerDimensions();
			Texture2D icon = TextureAssets.Item[ItemID.Book].Value;
			float scale = MathF.Min((dimensions.Width - 10f) / icon.Width, (dimensions.Height - 10f) / icon.Height);
			Vector2 drawPosition = dimensions.Center() - icon.Size() * scale * 0.5f;

			spriteBatch.Draw(icon, drawPosition, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

			if (!IsMouseHovering) {
				return;
			}

			Main.hoverItemName = Language.GetTextValue("Mods.ProgressionJournal.UI.InventoryButtonTooltip");
		}

		private Color GetBackgroundColor()
		{
			if (JournalSystem.Visible) {
				return ActiveBackgroundColor;
			}

			return IsMouseHovering ? HoverBackgroundColor : BaseBackgroundColor;
		}

		private Color GetBorderColor()
		{
			if (JournalSystem.Visible) {
				return ActiveBorderColor;
			}

			return IsMouseHovering ? HoverBorderColor : BaseBorderColor;
		}

		private static ProgressionJournalUISystem JournalSystem => ModContent.GetInstance<ProgressionJournalUISystem>();
	}
}
