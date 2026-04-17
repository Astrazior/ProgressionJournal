using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using ProgressionJournal.Data;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace ProgressionJournal.UI;

public static class JournalStageButtonPresenter
{
    public static void Refresh(IReadOnlyDictionary<ProgressionStageId, JournalStageButton> buttons, ProgressionStageId selectedStage)
    {
        foreach (var stage in ProgressionStageCatalog.All)
        {
            var button = buttons[stage.Id];
            ApplyContent(button, stage);
            button.SetCompleted(stage.IsUnlocked());
            button.SetStyle(JournalUiTheme.GetStageButtonStyle(stage.Id == selectedStage));
        }
    }

    public static void Layout(IReadOnlyDictionary<ProgressionStageId, JournalStageButton> buttons, UIElement container)
    {
        if (buttons.Count == 0 || container.Parent is null)
        {
            return;
        }

        var dimensions = container.GetInnerDimensions();
        var availableHeight = dimensions.Height;
        var availableWidth = dimensions.Width;
        if (availableHeight <= 0f)
        {
            return;
        }

        var stageOrder = JournalOrdering.StageSelection;
        var columns = GetColumnCount(availableHeight, stageOrder.Count);
        var rows = (int)MathF.Ceiling(stageOrder.Count / (float)columns);
        var buttonHeight = (availableHeight - JournalUiMetrics.StageButtonGap * (rows - 1)) / rows;
        var buttonWidth = columns == 1
            ? availableWidth
            : (availableWidth - JournalUiMetrics.StageButtonColumnGap * (columns - 1)) / columns;

        for (var index = 0; index < stageOrder.Count; index++)
        {
            var stageId = stageOrder[index];
            var button = buttons[stageId];
            var row = index / columns;
            var column = index % columns;
            var isTrailingSingleButton = columns > 1 && stageOrder.Count % columns != 0 && index == stageOrder.Count - 1;
            var top = row * (buttonHeight + JournalUiMetrics.StageButtonGap);
            var left = isTrailingSingleButton ? 0f : column * (buttonWidth + JournalUiMetrics.StageButtonColumnGap);

            button.Left.Set(left, 0f);
            button.Top.Set(top, 0f);
            button.Width.Set(isTrailingSingleButton ? availableWidth : buttonWidth, 0f);
            button.Height.Set(buttonHeight, 0f);
        }

        container.Recalculate();
    }

    private static int GetColumnCount(float availableHeight, int stageCount)
    {
        var singleColumnButtonHeight = (availableHeight - JournalUiMetrics.StageButtonGap * (stageCount - 1)) / stageCount;
        return singleColumnButtonHeight >= JournalUiMetrics.MinSingleColumnStageButtonHeight ? 1 : 2;
    }

    private static void ApplyContent(JournalStageButton button, ProgressionStage stage)
    {
        var stageName = Language.GetTextValue(stage.LocalizationKey);
        button.SetTooltip(stageName);

        switch (stage.Id)
        {
            case ProgressionStageId.PreBoss:
                button.SetNpcHeadDisplay(NPC.TypeToDefaultHeadIndex(NPCID.Guide));
                return;

            case ProgressionStageId.PostThreeMechBosses:
                button.SetBossHeadDisplay(
                    GetBossHeadSlot(NPCID.TheDestroyer),
                    GetBossHeadSlot(NPCID.Retinazer),
                    GetBossHeadSlot(NPCID.SkeletronPrime));
                return;

            case ProgressionStageId.PostCelestialPillars:
                button.SetBossHeadDisplay(
                    GetBossHeadSlot(NPCID.LunarTowerSolar),
                    GetBossHeadSlot(NPCID.LunarTowerVortex),
                    GetBossHeadSlot(NPCID.LunarTowerNebula),
                    GetBossHeadSlot(NPCID.LunarTowerStardust));
                return;
        }

        var bossHeadSlot = GetStageBossHeadSlot(stage.Id);
        if (bossHeadSlot.HasValue)
        {
            button.SetBossHeadDisplay(bossHeadSlot.Value);
            return;
        }

        ApplyText(button, stageName);
    }

    private static void ApplyText(JournalStageButton button, string text)
    {
        var availableWidth = button.GetInnerDimensions().Width - JournalUiMetrics.StageButtonTextHorizontalPadding * 2f;
        if (availableWidth <= 0f)
        {
            button.SetTextDisplay(text, JournalUiMetrics.StageButtonTextScale);
            return;
        }

        var textScale = JournalUiMetrics.StageButtonTextScale;
        while (textScale > JournalUiMetrics.MinStageButtonTextScale &&
               JournalTextUtilities.MeasureMouseTextWidth(text, textScale) > availableWidth)
        {
            textScale -= JournalUiMetrics.StageButtonTextScaleStep;
        }

        if (JournalTextUtilities.MeasureMouseTextWidth(text, textScale) <= availableWidth)
        {
            button.SetTextDisplay(text, textScale);
            return;
        }

        button.SetTextDisplay(
            JournalTextUtilities.TrimToPixelWidth(text, availableWidth, JournalUiMetrics.MinStageButtonTextScale),
            JournalUiMetrics.MinStageButtonTextScale);
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

        if (NPC.downedMechBoss3)
        {
            return NPCID.SkeletronPrime;
        }

        return NPCID.TheDestroyer;
    }
}
