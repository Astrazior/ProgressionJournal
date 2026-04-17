using System;
using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI;

public sealed class JournalTextButton : UIPanel
{
	private UIText _label;
	private float _textScale;

	public JournalTextButton(string text, float textScale, Action onClick)
	{
		_textScale = textScale;
		SetPadding(0f);
		BackgroundColor = new Color(38, 54, 73);
		BorderColor = new Color(100, 127, 156);

		_label = CreateLabel(text);
		Append(_label);

		OnLeftClick += (_, _) => onClick();
	}

	public void SetText(string text) => _label.SetText(text);

	public void SetTextColor(Color color) => _label.TextColor = color;

	private UIText CreateLabel(string text)
	{
		var label = new UIText(text, _textScale) {
			HAlign = 0.5f,
			VAlign = 0.5f
		};
		label.TextColor = new Color(226, 233, 240);
		return label;
	}
}
