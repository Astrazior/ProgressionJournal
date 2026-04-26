using System;
using System.Collections;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ProgressionJournal.Systems;

public static class JournalBuildChat
{
    private static bool _unloaded;
    private const string BuildTagName = "pjb";
    private const string JournalIconTexturePath = "ProgressionJournal/Assets/UI/JournalButtonIcon";

    public static void RegisterTags()
    {
        _unloaded = false;
        ChatManager.Register<BuildTagHandler>(BuildTagName);
    }

    public static void Unload()
    {
        _unloaded = true;

        TryUnregisterChatTag();
        TryClearChatHistory();

        BuildSnippet.ClearCachedAssets();
    }

    private static void TryUnregisterChatTag()
    {
        try
        {
            var handlersField = typeof(ChatManager).GetField(
                "_handlers",
                BindingFlags.Static | BindingFlags.NonPublic);

            if (handlersField?.GetValue(null) is IDictionary handlers)
            {
                handlers.Remove(BuildTagName);
            }
        }
        catch (Exception exception)
        {
            ProgressionJournal.Instance?.Logger.Debug(
                $"Failed to unregister ProgressionJournal chat tag '{BuildTagName}': {exception}");
        }
    }

    public static void ShareBuild(string buildName, string payload)
    {
        if (!JournalBuildStorage.TryReadBuildPayload(payload, out _))
        {
            Main.NewText(Language.GetTextValue("Mods.ProgressionJournal.UI.BuildExportFailed"), Color.OrangeRed);
            return;
        }

        var message = CreateChatMessage(buildName, payload);

        switch (Main.netMode)
        {
            case NetmodeID.SinglePlayer:
                Main.NewText(message, new Color(144, 213, 255));
                break;

            case NetmodeID.MultiplayerClient:
                ChatHelper.SendChatMessageFromClient(new ChatMessage(message));
                break;
        }
    }

    private static string CreateChatMessage(string buildName, string payload)
    {
        var safeBuildName = SanitizeVisibleText(buildName);

        var sharedLabel = Language.GetTextValue(
            "Mods.ProgressionJournal.UI.BuildSharedChatLabel",
            safeBuildName
        );

        var buildTag = $"[{BuildTagName}/{payload}:{sharedLabel}]";

        return Language.GetTextValue(
            "Mods.ProgressionJournal.UI.BuildSharedChatMessage",
            buildTag
        );
    }

    private static string SanitizeVisibleText(string text)
    {
        return text
            .Replace('\\', ' ')
            .Replace('[', ' ')
            .Replace(']', ' ')
            .Trim();
    }

    private sealed class BuildTagHandler : ITagHandler
    {
        public TextSnippet Parse(string text, Color baseColor = default, string? options = null)
        {
            if (_unloaded || ProgressionJournal.Instance is null)
            {
                return new TextSnippet(text, baseColor);
            }

            return new BuildSnippet(text, options ?? string.Empty);
        }
    }

    private static void TryClearChatHistory()
    {
        try
        {
            Main.chatMonitor.Clear();
        }
        catch (Exception exception)
        {
            ProgressionJournal.Instance?.Logger.Debug(
                $"Failed to clear chat history after unloading ProgressionJournal: {exception}");
        }
    }

    private sealed class BuildSnippet : TextSnippet
    {
        private const float IconSize = 20f;
        private const float IconGap = 5f;
        private const float ButtonHorizontalPadding = 6f;

        private static Asset<Texture2D>? _journalIconTexture;

        private static Asset<Texture2D> JournalIconTexture =>
            _journalIconTexture ??= ModContent.Request<Texture2D>(JournalIconTexturePath);

        private readonly string _label;
        private readonly string _payload;

        public BuildSnippet(string text, string payload)
            : base(text, new Color(255, 231, 132))
        {
            _label = text;
            _payload = payload;

            CheckForHover = true;
            DeleteWhole = true;
        }

        public static void ClearCachedAssets()
        {
            _journalIconTexture = null;
        }

        public override bool UniqueDraw(
            bool justCheckingString,
            out Vector2 size,
            SpriteBatch spriteBatch,
            Vector2 position = default,
            Color color = default,
            float scale = 1f)
        {
            var font = FontAssets.MouseText.Value;
            var drawColor = new Color(255, 231, 132);

            if (_unloaded || ProgressionJournal.Instance is null)
            {
                size = font.MeasureString(_label) * scale;

                if (!justCheckingString)
                {
                    Utils.DrawBorderStringFourWay(
                        spriteBatch,
                        font,
                        _label,
                        position.X,
                        position.Y,
                        drawColor,
                        Color.Black * 0.72f,
                        Vector2.Zero,
                        scale);
                }

                return true;
            }

            var textSize = font.MeasureString(_label) * scale;
            var iconSize = IconSize * scale;

            size = new Vector2(
                ButtonHorizontalPadding * 2f * scale + iconSize + IconGap * scale + textSize.X,
                MathF.Max(iconSize, textSize.Y));

            if (justCheckingString)
            {
                return true;
            }

            var iconTexture = JournalIconTexture.Value;
            var iconScale = MathF.Min(iconSize / iconTexture.Width, iconSize / iconTexture.Height);

            var iconPosition = position + new Vector2(
                ButtonHorizontalPadding * scale,
                (size.Y - iconSize) * 0.5f);

            var textPosition = position + new Vector2(
                ButtonHorizontalPadding * scale + iconSize + IconGap * scale,
                (size.Y - textSize.Y) * 0.5f);

            spriteBatch.Draw(
                iconTexture,
                iconPosition + new Vector2(iconSize * 0.5f),
                null,
                Color.White,
                0f,
                iconTexture.Size() * 0.5f,
                iconScale,
                SpriteEffects.None,
                0f);

            Utils.DrawBorderStringFourWay(
                spriteBatch,
                font,
                _label,
                textPosition.X,
                textPosition.Y,
                drawColor,
                Color.Black * 0.72f,
                Vector2.Zero,
                scale);

            return true;
        }

        public override void OnClick()
        {
            if (_unloaded || ProgressionJournal.Instance is null)
            {
                return;
            }

            try
            {
                ModContent.GetInstance<JournalSystem>().ShowSharedBuildPreview(_payload);
            }
            catch (Exception exception)
            {
                ProgressionJournal.Instance.Logger.Debug(
                    $"Failed to open shared build preview: {exception}");
            }
        }

        public override void OnHover()
        {
            if (_unloaded || ProgressionJournal.Instance is null)
            {
                return;
            }

            Main.hoverItemName = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSharedOpenTooltip");
        }
    }
}
