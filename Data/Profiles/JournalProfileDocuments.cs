using System.Text.Json.Serialization;

namespace ProgressionJournal.Data.Profiles;

public sealed class JournalProfileDocument
{
    public string Format { get; set; } = JournalProfileStorage.ProfileFormat;

    public int Version { get; set; } = JournalProfileStorage.CurrentVersion;

    public string Id { get; set; } = string.Empty;

    public JournalLocalizedText Name { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public string ProfileVersion { get; set; } = "1.0.0";

    public bool ReadOnly { get; set; }

    public string SourceUrl { get; set; } = string.Empty;

    public string SourceRevision { get; set; } = string.Empty;

    public string GeneratedAtUtc { get; set; } = string.Empty;

    public List<JournalRequiredModDocument> RequiredMods { get; set; } = [];

    public List<JournalProfileClassDocument> Classes { get; set; } = [];

    public List<JournalProfileStageDocument> Stages { get; set; } = [];

    public List<JournalProfileEntryDocument> Entries { get; set; } = [];

    public List<JournalProfileCombatBuffDocument> CombatBuffs { get; set; } = [];
}

public sealed class JournalRequiredModDocument
{
    public string Name { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;
}

public sealed class JournalProfileClassDocument
{
    public string Id { get; set; } = string.Empty;

    public JournalLocalizedText Name { get; set; } = string.Empty;

    public string IconMod { get; set; } = string.Empty;

    public string IconItem { get; set; } = string.Empty;

    public List<string> DamageClassNames { get; set; } = [];
}

public sealed class JournalProfileStageDocument
{
    public string Id { get; set; } = string.Empty;

    public JournalLocalizedText Name { get; set; } = string.Empty;

    public string IconMod { get; set; } = string.Empty;

    public string IconNpc { get; set; } = string.Empty;

    public int AccessorySlots { get; set; } = 5;

    public JournalUnlockConditionDocument Unlock { get; set; } = new();
}

public sealed class JournalUnlockConditionDocument
{
    public string Type { get; set; } = "always";

    public string Key { get; set; } = string.Empty;

    public string Mod { get; set; } = string.Empty;

    public string Npc { get; set; } = string.Empty;

    public string Mode { get; set; } = "all";

    public List<JournalUnlockConditionDocument> Conditions { get; set; } = [];
}

public sealed class JournalProfileEntryDocument
{
    public string Key { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JournalItemCategory Category { get; set; }

    public List<string> Classes { get; set; } = [];

    public List<List<JournalItemReferenceDocument>> ItemGroups { get; set; } = [];

    public List<JournalProfileEvaluationDocument> Evaluations { get; set; } = [];

    public List<JournalWikiRecommendationDocument> Wiki { get; set; } = [];

    public bool IsSupportWeapon { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JournalEventCategory? EventCategory { get; set; }

    public string CustomEventName { get; set; } = string.Empty;
}

public sealed class JournalProfileEvaluationDocument
{
    public string StageId { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RecommendationTier Tier { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JournalEvaluationScope Scope { get; set; } = JournalEvaluationScope.UntilNext;
}

public enum JournalEvaluationScope
{
    UntilNext,
    StageOnly
}

public sealed class JournalWikiRecommendationDocument
{
    public string StageId { get; set; } = string.Empty;

    public List<string> Classes { get; set; } = [];

    public string SourceName { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public JournalLocalizedText Target { get; set; } = string.Empty;
}

public sealed class JournalItemReferenceDocument
{
    public string Mod { get; set; } = "Terraria";

    public string Item { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}

public sealed class JournalProfileCombatBuffDocument
{
    public string Key { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JournalBuffCategory Category { get; set; }

    public string ClassId { get; set; } = string.Empty;

    public List<string> Classes { get; set; } = [];

    public string StageId { get; set; } = string.Empty;

    public List<List<JournalItemReferenceDocument>> ItemGroups { get; set; } = [];
}
