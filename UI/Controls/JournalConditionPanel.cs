using Microsoft.Xna.Framework;
using Terraria.GameContent.UI.Elements;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalConditionPanel : UIPanel
{
    public JournalConditionPanel()
    {
        SetPadding(0f);
        BackgroundColor = JournalUiTheme.RootBackground * 0.72f;
        BorderColor = new Color(218, 181, 74) * 0.78f;
    }
}
