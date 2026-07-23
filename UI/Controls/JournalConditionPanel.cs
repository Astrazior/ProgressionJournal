using Microsoft.Xna.Framework;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalConditionPanel : JournalVolumetricPanel
{
    private const string FrameTexturePath =
        "ProgressionJournal/Assets/UI/Panels/ConditionPanelFrame";

    public JournalConditionPanel()
    {
        SetPadding(0f);
        BackgroundColor = new Color(39, 45, 49);
        BorderColor = Color.White;
        PreservePalette = true;
        FrameTextureOverridePath = FrameTexturePath;
    }
}
