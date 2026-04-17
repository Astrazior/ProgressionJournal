using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ProgressionJournal.Data;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI;

public sealed class JournalClassButton : UIPanel
{
	private const float TitleTop = 10f;
	private const float AccentInset = 14f;
	private const int AccentThickness = 3;
	private const int BaseLineThickness = 2;

	private readonly CombatClass _combatClass;
	private readonly UIText _title;
	private readonly UICharacter _characterPreview;
	private bool _selected;

	public JournalClassButton(CombatClass combatClass, bool selected, float height)
	{
		_combatClass = combatClass;
		_selected = selected;

		Height.Set(height, 0f);
		SetPadding(0f);

		_title = new UIText(Language.GetTextValue($"Mods.ProgressionJournal.Classes.{combatClass}"), 0.5f, true) {
			HAlign = 0.5f
		};
		_title.Top.Set(TitleTop, 0f);
		Append(_title);

		_characterPreview = new UICharacter(CreatePreviewPlayer(combatClass), false, false, 1.42f);
		_characterPreview.Width.Set(102f, 0f);
		_characterPreview.Height.Set(112f, 0f);
		_characterPreview.HAlign = 0.5f;
		_characterPreview.Top.Set(38f, 0f);
		_characterPreview.IgnoresMouseInteraction = true;
		Append(_characterPreview);

		ApplyVisualState();
	}

	public void SetSelected(bool selected)
	{
		_selected = selected;
		ApplyVisualState();
	}

	protected override void DrawSelf(SpriteBatch spriteBatch)
	{
		ApplyVisualState();
		base.DrawSelf(spriteBatch);

		var dimensions = GetInnerDimensions();
		DrawAccent(spriteBatch, dimensions);
		DrawBaseLine(spriteBatch, dimensions);

		if (IsMouseHovering) {
			Main.hoverItemName = Language.GetTextValue($"Mods.ProgressionJournal.Classes.{_combatClass}");
		}
	}

	private void ApplyVisualState()
	{
		var (background, border, accent, text) = GetPalette(_combatClass);
		float emphasis = _selected ? 1f : IsMouseHovering ? 0.52f : 0f;

		BackgroundColor = Color.Lerp(background, accent * 0.2f, emphasis);
		BorderColor = Color.Lerp(border, accent, _selected ? 0.9f : IsMouseHovering ? 0.55f : 0.18f);
		_title.TextColor = Color.Lerp(text * 0.88f, Color.White, _selected ? 0.82f : IsMouseHovering ? 0.35f : 0f);
	}

	private void DrawAccent(SpriteBatch spriteBatch, CalculatedStyle dimensions)
	{
		Color accent = GetPalette(_combatClass).Accent;
		float accentAlpha = _selected ? 1f : IsMouseHovering ? 0.72f : 0.4f;
		var pixel = TextureAssets.MagicPixel.Value;
		int width = (int)(dimensions.Width - AccentInset * 2f);
		if (width <= 0) {
			return;
		}

		int x = (int)(dimensions.X + AccentInset);
		int y = (int)(dimensions.Y + 32f);
		spriteBatch.Draw(pixel, new Rectangle(x, y, width, AccentThickness), accent * accentAlpha);
	}

	private void DrawBaseLine(SpriteBatch spriteBatch, CalculatedStyle dimensions)
	{
		Color accent = GetPalette(_combatClass).Accent;
		var pixel = TextureAssets.MagicPixel.Value;
		int width = (int)(dimensions.Width - 42f);
		if (width <= 0) {
			return;
		}

		int x = (int)(dimensions.X + (dimensions.Width - width) * 0.5f);
		int y = (int)(dimensions.Y + dimensions.Height - 18f);
		spriteBatch.Draw(pixel, new Rectangle(x, y, width, BaseLineThickness), accent * 0.45f);
	}

	private static Player CreatePreviewPlayer(CombatClass combatClass)
	{
		Player preview = Main.LocalPlayer.SerializedClone();
		preview.dead = false;
		preview.isDisplayDollOrInanimate = true;
		preview.direction = 1;
		preview.gravDir = 1f;
		preview.velocity = Vector2.Zero;
		preview.itemAnimation = 0;
		preview.itemTime = 0;
		preview.selectedItem = 0;

		for (int index = 0; index < preview.armor.Length; index++) {
			preview.armor[index] = new Item();
		}

		for (int index = 0; index < preview.miscEquips.Length; index++) {
			preview.miscEquips[index] = new Item();
		}

		for (int index = 0; index < preview.miscDyes.Length; index++) {
			preview.miscDyes[index] = new Item();
		}

		for (int index = 0; index < preview.dye.Length; index++) {
			preview.dye[index] = new Item();
		}

		preview.hideMisc[0] = true;
		preview.mount.SetMount(0, preview);

		int[] armor = GetArmorItemIds(combatClass);
		for (int index = 0; index < armor.Length; index++) {
			preview.armor[index] = CreateItem(armor[index]);
		}

		preview.PlayerFrame();
		return preview;
	}

	private static Item CreateItem(int itemId)
	{
		var item = new Item();
		item.SetDefaults(itemId);
		return item;
	}

	private static int[] GetArmorItemIds(CombatClass combatClass) => combatClass switch
	{
		CombatClass.Melee => [ItemID.SolarFlareHelmet, ItemID.SolarFlareBreastplate, ItemID.SolarFlareLeggings],
		CombatClass.Ranged => [ItemID.VortexHelmet, ItemID.VortexBreastplate, ItemID.VortexLeggings],
		CombatClass.Magic => [ItemID.NebulaHelmet, ItemID.NebulaBreastplate, ItemID.NebulaLeggings],
		CombatClass.Summoner => [ItemID.StardustHelmet, ItemID.StardustBreastplate, ItemID.StardustLeggings],
		_ => [ItemID.SolarFlareHelmet, ItemID.SolarFlareBreastplate, ItemID.SolarFlareLeggings]
	};

	private static (Color Background, Color Border, Color Accent, Color Text) GetPalette(CombatClass combatClass) => combatClass switch
	{
		CombatClass.Melee => (
			new Color(44, 29, 24),
			new Color(124, 92, 72),
			new Color(231, 121, 62),
			new Color(247, 226, 207)),
		CombatClass.Ranged => (
			new Color(25, 43, 37),
			new Color(80, 132, 116),
			new Color(115, 216, 171),
			new Color(222, 245, 236)),
		CombatClass.Magic => (
			new Color(31, 31, 58),
			new Color(92, 96, 168),
			new Color(156, 139, 255),
			new Color(231, 227, 255)),
		CombatClass.Summoner => (
			new Color(24, 39, 62),
			new Color(84, 121, 172),
			new Color(128, 204, 255),
			new Color(224, 241, 255)),
		_ => (
			new Color(26, 38, 52),
			new Color(88, 115, 142),
			new Color(156, 196, 230),
			new Color(226, 233, 240))
	};
}
