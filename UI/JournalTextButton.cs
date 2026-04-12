using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI;

public sealed class JournalTextButton : UIPanel
{
	private readonly UIText _label;
	private readonly Action _onClick;

	public JournalTextButton(string text, float textScale, Action onClick)
	{
		_onClick = onClick;
		SetPadding(0f);
		BackgroundColor = new Color(38, 54, 73);
		BorderColor = new Color(100, 127, 156);

		_label = new UIText(text, textScale, false) {
			HAlign = 0.5f,
			VAlign = 0.5f
		};
		_label.TextColor = new Color(226, 233, 240);
		Append(_label);

		OnLeftClick += (_, _) => _onClick();
	}

	public void SetText(string text) => _label.SetText(text);

	public void SetTextColor(Color color) => _label.TextColor = color;
}
