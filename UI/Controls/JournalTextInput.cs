using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.UI;

namespace ProgressionJournal.UI.Controls;

public sealed class JournalTextInput : UIElement
{
    private bool _focused;
    private int _textBlinkerCount;
    private int _textBlinkerState;

    public JournalTextInput(string hintText)
    {
        HintText = hintText;
        Width.Set(0f, 1f);
        Height.Set(20f, 0f);
    }

    public string HintText { get; set; }

    public int MaxLength { get; } = 64;

    public string CurrentString { get; private set; } = string.Empty;

    public bool Focused
    {
        get => _focused;
        set
        {
            if (_focused == value)
            {
                return;
            }

            _focused = value;
            if (_focused)
            {
                Main.clrInput();
            }

            _textBlinkerCount = 0;
            _textBlinkerState = 0;
        }
    }

    public void SetText(string? text)
    {
        var normalizedText = text ?? string.Empty;
        if (normalizedText.Length > MaxLength)
        {
            normalizedText = normalizedText[..MaxLength];
        }

        CurrentString = normalizedText;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        Main.clrInput();
        Focused = true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var mousePosition = new Vector2(Main.mouseX, Main.mouseY);
        if (!ContainsPoint(mousePosition) && Main.mouseLeft)
        {
            Focused = false;
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (_focused)
        {
            PlayerInput.WritingText = true;
            Main.instance.HandleIME();

            var newText = Main.GetInputText(CurrentString);
            if (newText.Length > MaxLength)
            {
                newText = newText[..MaxLength];
            }

            if (!newText.Equals(CurrentString))
            {
                CurrentString = newText;
            }

            if (JustPressed(Keys.Tab))
            {
                Focused = false;
            }

            if (++_textBlinkerCount >= 20)
            {
                _textBlinkerState = (_textBlinkerState + 1) % 2;
                _textBlinkerCount = 0;
            }
        }

        var displayText = CurrentString;
        if (_textBlinkerState == 1 && _focused)
        {
            displayText += "|";
        }

        var dimensions = GetDimensions();
        if (CurrentString.Length == 0 && !_focused)
        {
            Utils.DrawBorderString(spriteBatch, HintText, new Vector2(dimensions.X, dimensions.Y), Color.Gray);
            return;
        }

        Main.LocalPlayer.mouseInterface = true;
        Utils.DrawBorderString(spriteBatch, displayText, new Vector2(dimensions.X, dimensions.Y), Color.White);
    }

    private static bool JustPressed(Keys key)
    {
        return Main.inputText.IsKeyDown(key) && !Main.oldInputText.IsKeyDown(key);
    }
}
