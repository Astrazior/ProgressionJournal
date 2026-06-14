using Microsoft.Xna.Framework;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalConditionPanel : JournalVolumetricPanel
{
    public JournalConditionPanel()
    {
        SetPadding(0f);
        BackgroundColor = JournalUiTheme.RootBackground * 0.72f;
        BorderColor = new Color(218, 181, 74) * 0.78f;
    }
}
