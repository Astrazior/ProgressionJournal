using System;
using System.IO;
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
    public const byte ShareBuildPacket = 1;

    private const string BuildTagName = "pjb";
    private const string JournalIconTexturePath = "ProgressionJournal/Assets/UI/JournalButtonIcon";

    public static void RegisterTags()
    {
        ChatManager.Register<BuildTagHandler>(BuildTagName);
    }

    public static void ShareBuild(string buildName, string payload)
    {
        var message = CreateChatMessage(Main.LocalPlayer.name, buildName, payload);

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Main.NewText(message, new Color(144, 213, 255));
            return;
        }

        if (Main.netMode != NetmodeID.MultiplayerClient || ProgressionJournal.Instance is not { } mod)
        {
            return;
        }

        var packet = mod.GetPacket();
        packet.Write(ShareBuildPacket);
        packet.Write(buildName);
        packet.Write(payload);
        packet.Send();
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        var packetType = reader.ReadByte();
        if (packetType != ShareBuildPacket || Main.netMode != NetmodeID.Server)
        {
            return;
        }

        var buildName = reader.ReadString();
        var payload = reader.ReadString();
        if (!JournalBuildStorage.TryReadBuildPayload(payload, out _))
        {
            return;
        }

        var playerName = whoAmI >= 0 && whoAmI < Main.maxPlayers
            ? Main.player[whoAmI].name
            : string.Empty;
        var message = CreateChatMessage(playerName, buildName, payload);
        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), new Color(144, 213, 255));
    }

    private static string CreateChatMessage(string playerName, string buildName, string payload)
    {
        var safePlayerName = SanitizeVisibleText(playerName);
        var safeBuildName = SanitizeVisibleText(buildName);
        if (string.IsNullOrWhiteSpace(safePlayerName))
        {
            safePlayerName = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSharedUnknownPlayer");
        }

        var sharedLabel = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSharedChatLabel", safeBuildName);
        var buildTag = $"[{BuildTagName}/{payload}:{sharedLabel}]";

        return Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSharedChatMessage", safePlayerName, buildTag);
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
            return new BuildSnippet(text, options ?? string.Empty);
        }
    }

    private sealed class BuildSnippet : TextSnippet
    {
        private const float IconSize = 20f;
        private const float IconGap = 5f;
        private const float ButtonHorizontalPadding = 6f;

        private static readonly Asset<Texture2D> JournalIconTexture =
            ModContent.Request<Texture2D>(JournalIconTexturePath);

        private readonly string _label;
        private readonly string _payload;

        public BuildSnippet(string text, string payload)
            : base(text, new Color(255, 231, 132), 1f)
        {
            _label = text;
            _payload = payload;
            CheckForHover = true;
            DeleteWhole = true;
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
            var iconPosition = position + new Vector2(ButtonHorizontalPadding * scale, (size.Y - iconSize) * 0.5f);
            var textPosition = position + new Vector2(ButtonHorizontalPadding * scale + iconSize + IconGap * scale, (size.Y - textSize.Y) * 0.5f);
            var drawColor = new Color(255, 231, 132);

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
            ModContent.GetInstance<JournalSystem>().ShowSharedBuildPreview(_payload);
        }

        public override void OnHover()
        {
            Main.hoverItemName = Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSharedOpenTooltip");
        }
    }
}
