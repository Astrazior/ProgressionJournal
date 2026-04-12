using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Systems;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalButtonUIState : UIState
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

		if (_button.IsMouseHovering) {
			Main.LocalPlayer.mouseInterface = true;
			Main.blockMouse = true;
		}
	}

	private sealed class JournalButton : UIPanel
	{
		private const float ButtonSize = 34f;
		private const float AccessorySlotStep = 47f;
		private const float HorizontalSpacing = 8f;
		private const float VerticalSpacing = 6f;
		private static readonly Color BaseBackgroundColor = new(33, 44, 57);
		private static readonly Color HoverBackgroundColor = new(50, 73, 96);
		private static readonly Color ActiveBackgroundColor = new(67, 89, 54);
		private static readonly Color BaseBorderColor = new(104, 130, 158);
		private static readonly Color HoverBorderColor = new(170, 208, 240);
		private static readonly Color ActiveBorderColor = new(165, 214, 124);

		public JournalButton()
		{
			Width.Set(ButtonSize, 0f);
			Height.Set(ButtonSize, 0f);
			SetPadding(0f);
			OnLeftClick += (_, _) => JournalSystem.ToggleView(syncStage: true);
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
			BackgroundColor = GetBackgroundColor();
			BorderColor = GetBorderColor();
			Recalculate();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);

			var dimensions = GetInnerDimensions();
			var icon = TextureAssets.Item[ItemID.Book].Value;
			var scale = MathF.Min((dimensions.Width - 8f) / icon.Width, (dimensions.Height - 8f) / icon.Height);
			var drawPosition = dimensions.Center() - icon.Size() * scale * 0.5f;

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

		private static JournalSystem JournalSystem => ModContent.GetInstance<JournalSystem>();
	}
}
