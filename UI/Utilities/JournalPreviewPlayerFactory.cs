using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ProgressionJournal.UI.Utilities;

public static class JournalPreviewPlayerFactory
{
    public static Player Create(CombatClass combatClass)
    {
        var preview = Main.LocalPlayer.SerializedClone();
        preview.dead = false;
        preview.isDisplayDollOrInanimate = true;
        preview.direction = 1;
        preview.gravDir = 1f;
        preview.velocity = Vector2.Zero;
        preview.itemAnimation = 0;
        preview.itemTime = 0;
        preview.selectedItem = 0;

        ResetItems(preview.armor);
        ResetItems(preview.miscEquips);
        ResetItems(preview.miscDyes);
        ResetItems(preview.dye);
        preview.hideMisc[0] = true;

        var armor = GetArmorItemIds(combatClass);
        for (var index = 0; index < armor.Length; index++)
        {
            preview.armor[index] = JournalItemUtilities.CreateItem(armor[index]);
        }

        preview.mount.SetMount(GetMountId(combatClass), preview);
        preview.mount.UpdateFrame(preview, GetMountPreviewState(preview), preview.velocity);
        preview.PlayerFrame();
        ApplyMountBodyPose(preview);
        return preview;
    }

    private static void ResetItems(Item[] items)
    {
        for (var index = 0; index < items.Length; index++)
        {
            items[index] = new Item();
        }
    }

    private static int[] GetArmorItemIds(CombatClass combatClass) => combatClass switch
    {
        CombatClass.Melee => [ItemID.SolarFlareHelmet, ItemID.SolarFlareBreastplate, ItemID.SolarFlareLeggings],
        CombatClass.Ranged => [ItemID.VortexHelmet, ItemID.VortexBreastplate, ItemID.VortexLeggings],
        CombatClass.Magic => [ItemID.NebulaHelmet, ItemID.NebulaBreastplate, ItemID.NebulaLeggings],
        CombatClass.Summoner => [ItemID.StardustHelmet, ItemID.StardustBreastplate, ItemID.StardustLeggings],
        _ => [ItemID.SolarFlareHelmet, ItemID.SolarFlareBreastplate, ItemID.SolarFlareLeggings]
    };

    private static int GetMountId(CombatClass combatClass) => combatClass switch
    {
        CombatClass.Melee => MountID.DarkHorse,
        CombatClass.Ranged => MountID.Unicorn,
        CombatClass.Magic => MountID.WitchBroom,
        CombatClass.Summoner => MountID.DarkMageBook,
        _ => MountID.DarkHorse
    };

    private static int GetMountPreviewState(Player preview)
    {
        return preview.mount.CanFly() ? Mount.FrameFlying : Mount.FrameRunning;
    }

    private static void ApplyMountBodyPose(Player preview)
    {
        preview.bodyFrame.X = 0;
        preview.bodyFrame.Y = preview.mount.BodyFrame * preview.bodyFrame.Height;
    }
}

