using Microsoft.Xna.Framework;
namespace ProgressionJournal.UI.Styling;

public static class JournalUiTheme
{
    // Switch between InlineRule, AccentTag, and SideRail to compare category treatments in-game.
    public static readonly JournalCategoryHeaderStyle CategoryHeaderStyle = JournalCategoryHeaderStyle.InlineRule;

    public static readonly Color RootBackground = new(12, 20, 30);
    public static readonly Color RootBorder = new(78, 101, 124);
    public static readonly Color PanelBackground = new(21, 33, 45);
    public static readonly Color PanelBorder = new(88, 115, 142);
    public static readonly Color RootTitleText = new(236, 240, 245);
    public static readonly Color ContentDescriptionText = new(198, 214, 229);
    public static readonly Color SectionHeaderText = new(248, 204, 15);
    public static readonly Color PresetPanelBackground = new(24, 43, 58);
    public static readonly Color PresetPanelBorder = new(104, 138, 168);
    public static readonly Color PresetPanelText = new(220, 228, 236);
    public static readonly Color InventoryButtonHoverGlow = new(170, 208, 240);
    public static readonly Color InventoryButtonActiveGlow = new(165, 214, 124);
    public static readonly Color InventoryButtonShadow = new(10, 12, 20);
    public static readonly Color EntryAlternativeMarker = new(162, 214, 255);
    public static readonly Color EventEntryOutline = new(228, 196, 84);
    public static readonly Color EventEntryOutlineBright = new(255, 236, 154);
    public static readonly Color EventEntryOutlineShadow = new(96, 72, 24);
    public static readonly Color EventBadgeBackground = new(27, 33, 43, 230);
    public static readonly Color EventBadgeBorder = new(234, 214, 124);

    public static float RootBackgroundOpacity => 0.98f;

    public static JournalButtonStyle GetDefaultTextButtonStyle()
    {
        return new JournalButtonStyle(new Color(38, 54, 73), new Color(100, 127, 156), new Color(226, 233, 240));
    }

    public static JournalButtonStyle GetHeaderButtonStyle(bool danger)
    {
        return danger ? new JournalButtonStyle(new Color(52, 39, 44), new Color(98, 76, 84), new Color(234, 224, 228)) : new JournalButtonStyle(new Color(31, 44, 58), new Color(79, 100, 122), new Color(224, 230, 236));
    }

    public static JournalButtonStyle GetTabButtonStyle(bool active)
    {
        return active
            ? new JournalButtonStyle(new Color(58, 100, 71), new Color(130, 194, 149), new Color(226, 233, 240))
            : GetDefaultTextButtonStyle();
    }

    public static JournalButtonStyle GetStageButtonStyle(bool active)
    {
        return active
            ? new JournalButtonStyle(new Color(60, 88, 114), new Color(156, 196, 230), new Color(226, 233, 240))
            : new JournalButtonStyle(new Color(29, 42, 58), PanelBorder, new Color(226, 233, 240));
    }

    public static JournalPanelStyle GetRecommendationBlockStyle(RecommendationTier tier) => tier switch
    {
        RecommendationTier.Recommended => new JournalPanelStyle(new Color(22, 56, 33), new Color(90, 196, 116)),
        RecommendationTier.Additional => new JournalPanelStyle(new Color(44, 54, 26), new Color(190, 178, 94)),
        RecommendationTier.NotRecommended => new JournalPanelStyle(new Color(64, 34, 48), new Color(205, 116, 160)),
        RecommendationTier.Useless => new JournalPanelStyle(new Color(76, 22, 22), new Color(228, 72, 72)),
        _ => new JournalPanelStyle(PanelBackground, PanelBorder)
    };

    public static JournalCategoryStyle GetCategoryStyle(JournalItemCategory category) => category switch
    {
        JournalItemCategory.Weapon => new JournalCategoryStyle(new Color(196, 162, 88), RootTitleText),
        JournalItemCategory.ClassSpecific => new JournalCategoryStyle(new Color(104, 194, 196), RootTitleText),
        JournalItemCategory.Armor => new JournalCategoryStyle(new Color(134, 166, 214), RootTitleText),
        JournalItemCategory.Accessory => new JournalCategoryStyle(new Color(182, 136, 204), RootTitleText),
        _ => new JournalCategoryStyle(new Color(120, 136, 152), RootTitleText)
    };

    public static JournalClassPalette GetClassPalette(CombatClass combatClass) => combatClass switch
    {
        CombatClass.Melee => new JournalClassPalette(
            new Color(44, 29, 24),
            new Color(124, 92, 72),
            new Color(231, 121, 62),
            new Color(247, 226, 207)),
        CombatClass.Ranged => new JournalClassPalette(
            new Color(25, 43, 37),
            new Color(80, 132, 116),
            new Color(115, 216, 171),
            new Color(222, 245, 236)),
        CombatClass.Magic => new JournalClassPalette(
            new Color(31, 31, 58),
            new Color(92, 96, 168),
            new Color(156, 139, 255),
            new Color(231, 227, 255)),
        CombatClass.Summoner => new JournalClassPalette(
            new Color(24, 39, 62),
            new Color(84, 121, 172),
            new Color(128, 204, 255),
            new Color(224, 241, 255)),
        _ => new JournalClassPalette(
            new Color(26, 38, 52),
            PanelBorder,
            new Color(156, 196, 230),
            new Color(226, 233, 240))
    };

}

public readonly record struct JournalButtonStyle(Color Background, Color Border, Color Text);

public readonly record struct JournalPanelStyle(Color Background, Color Border);

public readonly record struct JournalCategoryStyle(Color Border, Color Text);

public readonly record struct JournalClassPalette(Color Background, Color Border, Color Accent, Color Text);

