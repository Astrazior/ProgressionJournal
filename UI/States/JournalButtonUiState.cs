using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;

namespace ProgressionJournal.UI.States;

public sealed class JournalButtonUiState : UIState
{
    private JournalInventoryButton _button = null!;

    public override void OnInitialize()
    {
        _button = new JournalInventoryButton();
        Append(_button);
    }

    public override void Update(GameTime gameTime)
    {
        _button.UpdatePlacement();
        base.Update(gameTime);

        if (!_button.IsMouseHovering)
        {
            return;
        }

        Main.LocalPlayer.mouseInterface = true;
        Main.blockMouse = true;
    }
}

