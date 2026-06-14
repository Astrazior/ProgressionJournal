using Terraria;
using Terraria.ID;
using Terraria.Localization;

namespace ProgressionJournal.UI.Composition;

public static class JournalStageButtonPresenter
{
    public static void RefreshEditorButton(
        JournalProfile profile,
        JournalProfileStageDocument stage,
        JournalStageButton button,
        bool selected)
    {
        ApplyContent(button, profile, stage);
        button.SetTooltip(GetStageName(profile, stage));
        button.SetInteractable(true);
        button.SetStyle(JournalUiTheme.GetStageButtonStyle(selected));
    }

    public static void Refresh(
        JournalProfile profile,
        IReadOnlyDictionary<string, JournalStageButton> buttons,
        string selectedStage,
        bool progressionModeEnabled)
    {
        foreach (var stage in profile.Stages)
        {
            var button = buttons[stage.Id];
            var unlocked = JournalProfileUnlockRegistry.IsUnlocked(stage, out var conditionResolved);
            var isAvailable = !progressionModeEnabled || unlocked || !conditionResolved;

            if (isAvailable)
            {
                ApplyContent(button, profile, stage);
                var tooltip = GetStageName(profile, stage);
                if (!conditionResolved)
                {
                    tooltip = $"{tooltip}\n{Language.GetTextValue("Mods.ProgressionJournal.UI.StageConditionUnresolved")}";
                }
                button.SetTooltip(tooltip);
            }
            else
            {
                button.SetLockedDisplay();
                button.SetTooltip(Language.GetTextValue("Mods.ProgressionJournal.UI.LockedStageTooltip"));
            }

            button.SetInteractable(isAvailable);
            button.SetStyle(JournalUiTheme.GetStageButtonStyle(
                string.Equals(stage.Id, selectedStage, StringComparison.OrdinalIgnoreCase)));
        }
    }

    private static void ApplyContent(
        JournalStageButton button,
        JournalProfile profile,
        JournalProfileStageDocument stage)
    {
        if (!string.Equals(profile.Id, JournalProfileIds.Vanilla, StringComparison.OrdinalIgnoreCase)
            || !JournalStageIds.TryToLegacy(stage.Id, out var legacyStageId))
        {
            if (JournalStageIconCatalog.TryResolve(profile, stage, out var npcType))
            {
                if (npcType == NPCID.Guide)
                {
                    button.SetNpcHeadDisplay(NPC.TypeToDefaultHeadIndex(NPCID.Guide));
                    return;
                }

                var headSlot = JournalStageIconCatalog.GetBossHeadSlot(npcType);
                if (headSlot >= 0)
                {
                    button.SetBossHeadDisplay(headSlot);
                    return;
                }
            }

            button.SetNpcHeadDisplay(NPC.TypeToDefaultHeadIndex(NPCID.Guide));
            return;
        }

        if (legacyStageId == ProgressionStageId.PreBoss)
        {
            button.SetNpcHeadDisplay(NPC.TypeToDefaultHeadIndex(NPCID.Guide));
            return;
        }

        if (legacyStageId == ProgressionStageId.PostThreeMechBosses)
        {
            button.SetBossHeadDisplay(
                GetBossHeadSlot(NPCID.TheDestroyer),
                GetBossHeadSlot(NPCID.Retinazer),
                GetBossHeadSlot(NPCID.SkeletronPrime));
            return;
        }

        if (legacyStageId == ProgressionStageId.PostCelestialPillars)
        {
            button.SetBossHeadDisplay(
                GetBossHeadSlot(NPCID.LunarTowerSolar),
                GetBossHeadSlot(NPCID.LunarTowerVortex),
                GetBossHeadSlot(NPCID.LunarTowerNebula),
                GetBossHeadSlot(NPCID.LunarTowerStardust));
            return;
        }

        var bossHeadSlot = GetStageBossHeadSlot(legacyStageId);
        if (bossHeadSlot.HasValue)
        {
            button.SetBossHeadDisplay(bossHeadSlot.Value);
            return;
        }

        button.SetNpcHeadDisplay(NPC.TypeToDefaultHeadIndex(NPCID.Guide));
    }

    private static string GetStageName(JournalProfile profile, JournalProfileStageDocument stage)
    {
        return JournalProfileText.GetStageName(profile, stage.Id);
    }

    private static int? GetStageBossHeadSlot(ProgressionStageId stageId)
    {
        var npcType = GetStageBossNpcType(stageId);
        if (!npcType.HasValue)
        {
            return null;
        }

        var headSlot = GetBossHeadSlot(npcType.Value);
        return headSlot >= 0 ? headSlot : null;
    }

    private static int? GetStageBossNpcType(ProgressionStageId stageId) => stageId switch
    {
        ProgressionStageId.PreBoss => null,
        ProgressionStageId.PostKingSlime => NPCID.KingSlime,
        ProgressionStageId.PostEyeOfCthulhu => NPCID.EyeofCthulhu,
        ProgressionStageId.PostWorldEvil => WorldGen.crimson ? NPCID.BrainofCthulhu : NPCID.EaterofWorldsHead,
        ProgressionStageId.PostQueenBee => NPCID.QueenBee,
        ProgressionStageId.PostSkeletron => NPCID.SkeletronHead,
        ProgressionStageId.PostDeerclops => NPCID.Deerclops,
        ProgressionStageId.HardmodeEntry => NPCID.WallofFlesh,
        ProgressionStageId.PostQueenSlime => NPCID.QueenSlimeBoss,
        ProgressionStageId.PostOneMechBoss => GetFirstDownedMechBossNpcType(),
        ProgressionStageId.PostThreeMechBosses => null,
        ProgressionStageId.PostPlantera => NPCID.Plantera,
        ProgressionStageId.PostDukeFishron => NPCID.DukeFishron,
        ProgressionStageId.PostEmpressOfLight => NPCID.HallowBoss,
        ProgressionStageId.PostGolem => NPCID.GolemHead,
        ProgressionStageId.PostCelestialPillars => null,
        ProgressionStageId.PostMoonLord => NPCID.MoonLordHead,
        _ => null
    };

    private static int GetBossHeadSlot(int npcType)
    {
        return npcType >= 0 && npcType < NPCID.Sets.BossHeadTextures.Length
            ? NPCID.Sets.BossHeadTextures[npcType]
            : -1;
    }

    private static int GetFirstDownedMechBossNpcType()
    {
        if (NPC.downedMechBoss1)
        {
            return NPCID.TheDestroyer;
        }

        if (NPC.downedMechBoss2)
        {
            return NPCID.Retinazer;
        }

        return NPC.downedMechBoss3 ? NPCID.SkeletronPrime : NPCID.TheDestroyer;
    }
}

