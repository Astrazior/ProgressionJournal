using System;
using Terraria.Localization;

namespace ProgressionJournal.Data.Catalogs;

public static class JournalBuildPlannerCatalog
{
    public const string PrimaryWeaponSlotKey = "weapon_primary";
    public const string SupportWeaponSlotKey = "weapon_support";
    public const string ClassSpecificSlotKey = "class_specific";
    public const string ArmorHeadSlotKey = "armor_head";
    public const string ArmorBodySlotKey = "armor_body";
    public const string ArmorLegsSlotKey = "armor_legs";

    public const int PotionSlotCount = 8;
    public const int FoodSlotCount = 8;

    public static int GetAccessorySlotCount(ProgressionStageId stageId)
    {
        var hardmodeIndex = ProgressionStageCatalog.GetStageOrderIndex(ProgressionStageId.HardmodeEntry);
        var currentIndex = ProgressionStageCatalog.GetStageOrderIndex(stageId);
        return currentIndex >= hardmodeIndex ? 6 : 5;
    }

    public static string GetAccessorySlotKey(int slotIndex) => $"accessory_{slotIndex}";

    public static string GetPotionSlotKey(int slotIndex) => $"potion_{slotIndex}";

    public static string GetFoodSlotKey(int slotIndex) => $"food_{slotIndex}";

    public static bool TryGetSlotKind(string slotKey, out JournalBuildSlotKind slotKind)
    {
        if (string.Equals(slotKey, PrimaryWeaponSlotKey, StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.PrimaryWeapon;
            return true;
        }

        if (string.Equals(slotKey, SupportWeaponSlotKey, StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.SupportWeapon;
            return true;
        }

        if (string.Equals(slotKey, ClassSpecificSlotKey, StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.ClassSpecific;
            return true;
        }

        if (string.Equals(slotKey, ArmorHeadSlotKey, StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.ArmorHead;
            return true;
        }

        if (string.Equals(slotKey, ArmorBodySlotKey, StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.ArmorBody;
            return true;
        }

        if (string.Equals(slotKey, ArmorLegsSlotKey, StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.ArmorLegs;
            return true;
        }

        if (slotKey.StartsWith("accessory_", StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.Accessory;
            return true;
        }

        if (slotKey.StartsWith("potion_", StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.Potion;
            return true;
        }

        if (slotKey.StartsWith("food_", StringComparison.OrdinalIgnoreCase))
        {
            slotKind = JournalBuildSlotKind.Food;
            return true;
        }

        slotKind = default;
        return false;
    }

    public static bool DisallowsDuplicateSelections(JournalBuildSlotKind slotKind)
    {
        return slotKind is JournalBuildSlotKind.Accessory
            or JournalBuildSlotKind.Potion
            or JournalBuildSlotKind.Food;
    }

    public static string GetSlotDisplayName(string slotKey, CombatClass combatClass)
    {
        return TryGetSlotKind(slotKey, out var slotKind)
            ? slotKind switch
            {
                JournalBuildSlotKind.PrimaryWeapon => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotPrimaryWeapon"),
                JournalBuildSlotKind.SupportWeapon => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotSupportWeapon"),
                JournalBuildSlotKind.ClassSpecific => GetClassSpecificSlotDisplayName(combatClass),
                JournalBuildSlotKind.ArmorHead => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotArmorHead"),
                JournalBuildSlotKind.ArmorBody => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotArmorBody"),
                JournalBuildSlotKind.ArmorLegs => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotArmorLegs"),
                JournalBuildSlotKind.Accessory => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotAccessory"),
                JournalBuildSlotKind.Potion => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotPotion"),
                JournalBuildSlotKind.Food => TryExtractSlotIndex(slotKey) <= 1
                    ? Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotFood")
                    : Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotFoodAlternative"),
                _ => slotKey
            }
            : slotKey;
    }

    public static string GetSlotShortLabel(string slotKey, CombatClass combatClass)
    {
        return TryGetSlotKind(slotKey, out var slotKind)
            ? slotKind switch
            {
                JournalBuildSlotKind.PrimaryWeapon => "W1",
                JournalBuildSlotKind.SupportWeapon => "W2",
                JournalBuildSlotKind.ClassSpecific => combatClass switch
                {
                    CombatClass.Ranged => "AM",
                    CombatClass.Summoner => "WH",
                    CombatClass.Magic => "MG",
                    _ => "CL"
                },
                JournalBuildSlotKind.ArmorHead => "H",
                JournalBuildSlotKind.ArmorBody => "C",
                JournalBuildSlotKind.ArmorLegs => "L",
                JournalBuildSlotKind.Accessory => $"A{TryExtractSlotIndex(slotKey)}",
                JournalBuildSlotKind.Potion => $"P{TryExtractSlotIndex(slotKey)}",
                JournalBuildSlotKind.Food => TryExtractSlotIndex(slotKey) <= 1 ? "F" : $"F{TryExtractSlotIndex(slotKey)}",
                _ => "?"
            }
            : "?";
    }

    private static string GetClassSpecificSlotDisplayName(CombatClass combatClass) => combatClass switch
    {
        CombatClass.Ranged => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotRangedAmmo"),
        CombatClass.Summoner => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotSummonerUtility"),
        CombatClass.Magic => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotMagicUtility"),
        _ => Language.GetTextValue("Mods.ProgressionJournal.UI.BuildSlotClassSpecific")
    };

    private static int TryExtractSlotIndex(string slotKey)
    {
        var separatorIndex = slotKey.LastIndexOf('_');
        if (separatorIndex < 0 || separatorIndex == slotKey.Length - 1)
        {
            return 0;
        }

        return int.TryParse(slotKey[(separatorIndex + 1)..], out var slotIndex) ? slotIndex : 0;
    }
}
