using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.UI.Utilities;

public static class JournalProfileIconResolver
{
    private const string AchievementsTexturePath = "Images/UI/Achievements";
    private const string VanillaAchievementId = "PURIFY_ENTIRE_WORLD";
    private const int AchievementIconSize = 64;

    public static JournalProfileIcon GetIcon(JournalProfile profile)
    {
        if (string.Equals(profile.Id, JournalProfileIds.Vanilla, StringComparison.OrdinalIgnoreCase))
        {
            var texture = Main.Assets.Request<Texture2D>(AchievementsTexturePath);
            var iconIndex = Main.Achievements.GetIconIndex(VanillaAchievementId);
            if (iconIndex >= 0)
            {
                var columns = texture.Value.Width / AchievementIconSize;
                if (columns > 0)
                {
                    return new JournalProfileIcon(
                        texture,
                        new Rectangle(
                            iconIndex % columns * AchievementIconSize,
                            iconIndex / columns * AchievementIconSize,
                            AchievementIconSize,
                            AchievementIconSize));
                }
            }
        }

        foreach (var requirement in profile.Document.RequiredMods)
        {
            if (!ModLoader.TryGetMod(requirement.Name, out var mod))
            {
                continue;
            }

            if (mod.RequestAssetIfExists<Texture2D>("icon", out var iconAsset)
                || mod.RequestAssetIfExists("Icon", out iconAsset))
            {
                return new JournalProfileIcon(iconAsset);
            }
        }

        return new JournalProfileIcon(TextureAssets.Item[ItemID.Book]);
    }
}

public readonly record struct JournalProfileIcon(
    Asset<Texture2D> Texture,
    Rectangle? SourceRectangle = null);
