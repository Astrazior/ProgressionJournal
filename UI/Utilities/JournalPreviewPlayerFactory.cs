using Microsoft.Xna.Framework;
using ProgressionJournal.Systems;
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
        ConfigurePreviewDraw(preview);
        return preview;
    }

    public static Player CreateSavedBuildPreview(JournalSavedBuild build, ProgressionStageId stageId)
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

        ResetItems(preview.inventory);
        ResetItems(preview.armor);
        ResetItems(preview.miscEquips);
        ResetItems(preview.miscDyes);
        ResetItems(preview.dye);
        preview.hideMisc[0] = true;

        SetArmor(preview, 0, build.GetSelectedItemId(JournalBuildPlannerCatalog.ArmorHeadSlotKey));
        SetArmor(preview, 1, build.GetSelectedItemId(JournalBuildPlannerCatalog.ArmorBodySlotKey));
        SetArmor(preview, 2, build.GetSelectedItemId(JournalBuildPlannerCatalog.ArmorLegsSlotKey));

        var accessorySlot = 3;
        for (var slotIndex = 1; slotIndex <= JournalBuildPlannerCatalog.GetAccessorySlotCount(stageId); slotIndex++)
        {
            var itemId = build.GetSelectedItemId(JournalBuildPlannerCatalog.GetAccessorySlotKey(slotIndex));
            if (itemId <= ItemID.None)
            {
                continue;
            }

            SetArmor(preview, accessorySlot, itemId);
            accessorySlot++;
        }

        var heldItemId = GetPreviewHeldItemId(build);
        if (JournalItemUtilities.TryCreateItem(heldItemId, out var heldItem))
        {
            preview.inventory[0] = heldItem;
        }

        preview.PlayerFrame();
        ConfigurePreviewDraw(preview);
        return preview;
    }

    private static void ResetItems(Item[] items)
    {
        for (var index = 0; index < items.Length; index++)
        {
            items[index] = new Item();
        }
    }

    private static void SetArmor(Player preview, int armorIndex, int itemId)
    {
        if (armorIndex < 0
            || armorIndex >= preview.armor.Length
            || !JournalItemUtilities.TryCreateItem(itemId, out var item))
        {
            return;
        }

        preview.armor[armorIndex] = item;
    }

    private static int GetPreviewHeldItemId(JournalSavedBuild build)
    {
        var primaryWeapon = build.GetSelectedItemId(JournalBuildPlannerCatalog.PrimaryWeaponSlotKey);
        if (primaryWeapon > ItemID.None)
        {
            return primaryWeapon;
        }

        var supportWeapon = build.GetSelectedItemId(JournalBuildPlannerCatalog.SupportWeaponSlotKey);
        return supportWeapon > ItemID.None ? supportWeapon : build.GetSelectedItemId(JournalBuildPlannerCatalog.ClassSpecificSlotKey);
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

    private static void ConfigurePreviewDraw(Player preview)
    {
        var drawPlayer = preview.GetModPlayer<JournalPreviewDrawPlayer>();
        drawPlayer.ForceFullBright = true;
        drawPlayer.ShadeOpacity = 0f;
    }
}
