using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ProgressionJournal.Data.Profiles;

public sealed class JournalProfileEditorSession
{
    private JournalProfileEditorSession(JournalProfileDocument document, string? sourcePath)
    {
        Document = document;
        SourcePath = sourcePath;
        SelectedClassId = document.Classes.FirstOrDefault()?.Id ?? string.Empty;
        SelectedStageId = document.Stages[0].Id;
    }

    public JournalProfileDocument Document { get; }

    public string? SourcePath { get; }

    public string SelectedClassId { get; private set; }

    public string SelectedStageId { get; private set; }

    public static JournalProfileEditorSession CreateNew(string? name = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var document = new JournalProfileDocument
        {
            Id = $"user.profile.{timestamp}",
            Name = string.IsNullOrWhiteSpace(name) ? "My progression" : name.Trim(),
            Author = Main.LocalPlayer?.name ?? string.Empty,
            ProfileVersion = "1.0.0",
            Classes = [],
            Stages =
            [
                new JournalProfileStageDocument
                {
                    Id = "start",
                    Name = "Start",
                    AccessorySlots = 5,
                    Unlock = new JournalUnlockConditionDocument { Type = "always" }
                }
            ]
        };

        return new JournalProfileEditorSession(document, sourcePath: null);
    }

    public static JournalProfileEditorSession FromProfile(JournalProfile profile)
    {
        if (profile.IsReadOnly)
        {
            throw new InvalidOperationException("Built-in profiles must be copied before editing.");
        }

        return new JournalProfileEditorSession(
            JournalProfileStorage.CloneDocument(profile.Document),
            profile.SourcePath);
    }

    public void SetName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            Document.Name = name.Trim();
        }
    }

    public void SelectClass(string classId)
    {
        if (Document.Classes.Any(value => string.Equals(value.Id, classId, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedClassId = classId;
        }
    }

    public void SelectStage(string stageId)
    {
        if (Document.Stages.Any(value => string.Equals(value.Id, stageId, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedStageId = stageId;
        }
    }

    public JournalProfileClassDocument AddClass(string name)
    {
        var displayName = string.IsNullOrWhiteSpace(name) ? "New class" : name.Trim();
        var classId = CreateUniqueId(displayName, Document.Classes.Select(static value => value.Id), "class");
        var definition = new JournalProfileClassDocument
        {
            Id = classId,
            Name = displayName,
            DamageClassNames = []
        };
        Document.Classes.Add(definition);
        SelectedClassId = definition.Id;
        return definition;
    }

    public JournalProfileClassDocument AddClass(
        string id,
        string name,
        IEnumerable<string> damageClassNames)
    {
        var existing = Document.Classes.FirstOrDefault(value =>
            string.Equals(value.Id, id, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            SelectedClassId = existing.Id;
            return existing;
        }

        var definition = new JournalProfileClassDocument
        {
            Id = CreateUniqueId(id, Document.Classes.Select(static value => value.Id), "class"),
            Name = string.IsNullOrWhiteSpace(name) ? id : name.Trim(),
            DamageClassNames = damageClassNames
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
        Document.Classes.Add(definition);
        SelectedClassId = definition.Id;
        return definition;
    }

    public void RenameSelectedClass(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var definition = Document.Classes.FirstOrDefault(value =>
            string.Equals(value.Id, SelectedClassId, StringComparison.OrdinalIgnoreCase));
        if (definition is not null)
        {
            definition.Name = name.Trim();
        }
    }

    public bool RemoveSelectedClass()
    {
        if (Document.Classes.Count == 0)
        {
            return false;
        }

        var removed = Document.Classes.RemoveAll(
            value => string.Equals(value.Id, SelectedClassId, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed)
        {
            return false;
        }

        foreach (var entry in Document.Entries.ToArray())
        {
            entry.Classes.RemoveAll(value => string.Equals(value, SelectedClassId, StringComparison.OrdinalIgnoreCase));
            if (entry.Classes.Count == 0)
            {
                Document.Entries.Remove(entry);
            }
        }

        Document.CombatBuffs.RemoveAll(value =>
            string.Equals(value.ClassId, SelectedClassId, StringComparison.OrdinalIgnoreCase));
        SelectedClassId = Document.Classes.FirstOrDefault()?.Id ?? string.Empty;
        return true;
    }

    public JournalProfileStageDocument AddStage(string name)
    {
        var displayName = string.IsNullOrWhiteSpace(name) ? "New stage" : name.Trim();
        var stageId = CreateUniqueId(displayName, Document.Stages.Select(static value => value.Id), "stage");
        var definition = new JournalProfileStageDocument
        {
            Id = stageId,
            Name = displayName,
            AccessorySlots = Document.Stages.LastOrDefault()?.AccessorySlots ?? 5,
            Unlock = new JournalUnlockConditionDocument { Type = "always" }
        };
        Document.Stages.Add(definition);
        SelectedStageId = definition.Id;
        return definition;
    }

    public bool RemoveSelectedStage()
    {
        if (Document.Stages.Count <= 1)
        {
            return false;
        }

        var removed = Document.Stages.RemoveAll(
            value => string.Equals(value.Id, SelectedStageId, StringComparison.OrdinalIgnoreCase)) > 0;
        if (!removed)
        {
            return false;
        }

        foreach (var entry in Document.Entries.ToArray())
        {
            entry.Evaluations.RemoveAll(
                value => string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase));
            if (entry.Evaluations.Count == 0)
            {
                Document.Entries.Remove(entry);
            }
        }

        Document.CombatBuffs.RemoveAll(value =>
            string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase));
        SelectedStageId = Document.Stages[0].Id;
        return true;
    }

    public void RenameSelectedStage(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            GetSelectedStage().Name = name.Trim();
        }
    }

    public bool MoveSelectedStage(int direction)
    {
        var index = Document.Stages.FindIndex(
            value => string.Equals(value.Id, SelectedStageId, StringComparison.OrdinalIgnoreCase));
        var targetIndex = index + Math.Sign(direction);
        if (index < 0 || targetIndex < 0 || targetIndex >= Document.Stages.Count)
        {
            return false;
        }

        (Document.Stages[index], Document.Stages[targetIndex]) =
            (Document.Stages[targetIndex], Document.Stages[index]);
        return true;
    }

    public void SetSelectedStageUnlockBoss(string modName, string npcName)
    {
        var stage = GetSelectedStage();
        stage.Unlock = new JournalUnlockConditionDocument
        {
            Type = "npc",
            Mod = modName,
            Npc = npcName
        };
    }

    public void SetSelectedStageIcon(string modName, string npcName)
    {
        var stage = GetSelectedStage();
        stage.IconMod = modName;
        stage.IconNpc = npcName;
    }

    public void ClearSelectedStageIcon()
    {
        var stage = GetSelectedStage();
        stage.IconMod = string.Empty;
        stage.IconNpc = string.Empty;
    }

    public void SetSelectedStageAlwaysAvailable()
    {
        GetSelectedStage().Unlock = new JournalUnlockConditionDocument { Type = "always" };
    }

    public void SetSelectedStageAccessorySlots(int count)
    {
        GetSelectedStage().AccessorySlots = Math.Clamp(count, 0, 7);
    }

    public bool AddItem(int itemId, JournalItemCategory category, RecommendationTier tier)
    {
        if (string.IsNullOrWhiteSpace(SelectedClassId)
            || !ContentSamples.ItemsByType.TryGetValue(itemId, out var item)
            || item is null
            || item.IsAir)
        {
            return false;
        }

        var reference = CreateItemReference(item);
        var existing = Document.Entries.FirstOrDefault(entry =>
            entry.ItemGroups.SelectMany(static group => group).Any(value =>
                string.Equals(value.Mod, reference.Mod, StringComparison.OrdinalIgnoreCase)
                && string.Equals(value.Item, reference.Item, StringComparison.OrdinalIgnoreCase))
            && entry.Classes.Contains(SelectedClassId, StringComparer.OrdinalIgnoreCase));

        if (existing is null)
        {
            existing = new JournalProfileEntryDocument
            {
                Key = CreateUniqueId(
                    $"{reference.Mod}.{reference.Item}",
                    Document.Entries.Select(static value => value.Key),
                    "entry"),
                Category = category,
                Classes = [SelectedClassId],
                ItemGroups = [[reference]],
                Evaluations =
                [
                    new JournalProfileEvaluationDocument
                    {
                        StageId = SelectedStageId,
                        Tier = tier
                    }
                ]
            };
            Document.Entries.Add(existing);
            return true;
        }

        existing.Category = category;
        var evaluation = existing.Evaluations.FirstOrDefault(
            value => string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase));
        if (evaluation is null)
        {
            existing.Evaluations.Add(new JournalProfileEvaluationDocument
            {
                StageId = SelectedStageId,
                Tier = tier
            });
        }
        else
        {
            evaluation.Tier = tier;
        }

        return true;
    }

    public bool RemoveItem(int itemId)
    {
        if (!ContentSamples.ItemsByType.TryGetValue(itemId, out var item) || item is null)
        {
            return false;
        }

        var reference = CreateItemReference(item);
        var changed = false;

        foreach (var entry in Document.Entries.ToArray())
        {
            var containsItem = entry.ItemGroups.SelectMany(static group => group).Any(value =>
                string.Equals(value.Mod, reference.Mod, StringComparison.OrdinalIgnoreCase)
                && string.Equals(value.Item, reference.Item, StringComparison.OrdinalIgnoreCase));
            if (!containsItem || !entry.Classes.Contains(SelectedClassId, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            changed |= entry.Evaluations.RemoveAll(
                value => string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase)) > 0;
            if (entry.Evaluations.Count == 0)
            {
                Document.Entries.Remove(entry);
            }
        }

        return changed;
    }

    public bool ContainsItem(int itemId)
    {
        if (!ContentSamples.ItemsByType.TryGetValue(itemId, out var item) || item is null)
        {
            return false;
        }

        var reference = CreateItemReference(item);
        return Document.Entries.Any(entry =>
            entry.Classes.Contains(SelectedClassId, StringComparer.OrdinalIgnoreCase)
            && entry.Evaluations.Any(value =>
                string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase))
            && entry.ItemGroups.SelectMany(static group => group).Any(value =>
                string.Equals(value.Mod, reference.Mod, StringComparison.OrdinalIgnoreCase)
                && string.Equals(value.Item, reference.Item, StringComparison.OrdinalIgnoreCase)));
    }

    public JournalProfileEntryDocument? FindSelectedItemEntry(int itemId)
    {
        if (!TryGetItemReference(itemId, out var reference))
        {
            return null;
        }

        return Document.Entries.FirstOrDefault(entry =>
            entry.Classes.Contains(SelectedClassId, StringComparer.OrdinalIgnoreCase)
            && entry.Evaluations.Any(value =>
                string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase))
            && ContainsReference(entry.ItemGroups, reference));
    }

    public void SetItemEvent(int itemId, JournalEventCategory? eventCategory, string customEventName)
    {
        var entry = FindSelectedItemEntry(itemId);
        if (entry is null)
        {
            return;
        }

        entry.EventCategory = eventCategory;
        entry.CustomEventName = eventCategory is null ? customEventName.Trim() : string.Empty;
    }

    public void SetItemSupportWeapon(int itemId, bool isSupportWeapon)
    {
        var entry = FindSelectedItemEntry(itemId);
        if (entry is not null)
        {
            entry.IsSupportWeapon = isSupportWeapon;
        }
    }

    public void SetItemPlacement(int itemId, JournalItemCategory category, RecommendationTier tier)
    {
        var entry = FindSelectedItemEntry(itemId);
        if (entry is null)
        {
            return;
        }

        entry.Category = category;
        var evaluation = entry.Evaluations.First(value =>
            string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase));
        evaluation.Tier = tier;
    }

    public IReadOnlyList<JournalStageEntry> GetSelectedEntries()
    {
        if (string.IsNullOrWhiteSpace(SelectedClassId))
        {
            return [];
        }

        List<JournalStageEntry> result = [];
        foreach (var document in Document.Entries.Where(value =>
            value.Classes.Contains(SelectedClassId, StringComparer.OrdinalIgnoreCase)))
        {
            var evaluation = document.Evaluations.FirstOrDefault(value =>
                string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase));
            if (evaluation is null)
            {
                continue;
            }

            var groups = ResolveItemGroups(document.ItemGroups);
            if (groups.Count == 0)
            {
                continue;
            }

            var entry = new JournalEntry(
                document.Key,
                document.Category,
                document.Classes,
                groups,
                document.Evaluations.Select(static value => new StageEvaluation(value.StageId, value.Tier)),
                document.EventCategory,
                document.IsSupportWeapon,
                document.CustomEventName);
            result.Add(new JournalStageEntry(entry, new StageEvaluation(SelectedStageId, evaluation.Tier)));
        }

        return result;
    }

    public IReadOnlyList<JournalCombatBuffEntry> GetSelectedCombatBuffEntries()
    {
        if (string.IsNullOrWhiteSpace(SelectedClassId))
        {
            return [];
        }

        List<JournalCombatBuffEntry> result = [];
        foreach (var document in Document.CombatBuffs.Where(value =>
            string.Equals(value.ClassId, SelectedClassId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase)))
        {
            var groups = ResolveItemGroups(document.ItemGroups);
            if (groups.Count == 0)
            {
                continue;
            }

            result.Add(new JournalCombatBuffEntry(
                document.Key,
                document.Category,
                [document.ClassId],
                groups,
                document.StageId));
        }

        return result;
    }

    public bool AddCombatBuff(int itemId, JournalBuffCategory category)
    {
        if (string.IsNullOrWhiteSpace(SelectedClassId)
            || !TryGetItemReference(itemId, out var reference))
        {
            return false;
        }

        var existing = Document.CombatBuffs.FirstOrDefault(value =>
            string.Equals(value.ClassId, SelectedClassId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase)
            && ContainsReference(value.ItemGroups, reference));
        if (existing is not null)
        {
            existing.Category = category;
            return true;
        }

        Document.CombatBuffs.Add(new JournalProfileCombatBuffDocument
        {
            Key = CreateUniqueId(
                $"buff-{reference.Mod}-{reference.Item}-{SelectedClassId}-{SelectedStageId}",
                Document.CombatBuffs.Select(static value => value.Key),
                "buff"),
            Category = category,
            ClassId = SelectedClassId,
            StageId = SelectedStageId,
            ItemGroups = [[reference]]
        });
        return true;
    }

    public bool RemoveCombatBuff(int itemId)
    {
        if (!TryGetItemReference(itemId, out var reference))
        {
            return false;
        }

        return Document.CombatBuffs.RemoveAll(value =>
            string.Equals(value.ClassId, SelectedClassId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase)
            && ContainsReference(value.ItemGroups, reference)) > 0;
    }

    public bool ContainsCombatBuff(int itemId)
    {
        if (!TryGetItemReference(itemId, out var reference))
        {
            return false;
        }

        return Document.CombatBuffs.Any(value =>
            string.Equals(value.ClassId, SelectedClassId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase)
            && ContainsReference(value.ItemGroups, reference));
    }

    public bool Save(out JournalProfile? profile, out string error)
    {
        profile = null;
        if (!JournalProfileStorage.Save(Document, out var path, out error))
        {
            return false;
        }

        return JournalProfileStorage.TryLoad(path, isBuiltIn: false, out profile, out error);
    }

    private JournalProfileStageDocument GetSelectedStage()
    {
        return Document.Stages.First(value =>
            string.Equals(value.Id, SelectedStageId, StringComparison.OrdinalIgnoreCase));
    }

    private static JournalItemReferenceDocument CreateItemReference(Item item)
    {
        return item.ModItem is { } modItem
            ? new JournalItemReferenceDocument
            {
                Mod = modItem.Mod.Name,
                Item = modItem.Name,
                DisplayName = item.HoverName
            }
            : new JournalItemReferenceDocument
            {
                Mod = "Terraria",
                Item = ItemID.Search.GetName(item.type) ?? item.type.ToString(),
                DisplayName = item.HoverName
            };
    }

    private static bool TryGetItemReference(int itemId, out JournalItemReferenceDocument reference)
    {
        reference = null!;
        if (!ContentSamples.ItemsByType.TryGetValue(itemId, out var item) || item is null || item.IsAir)
        {
            return false;
        }

        reference = CreateItemReference(item);
        return true;
    }

    private static bool ContainsReference(
        IEnumerable<List<JournalItemReferenceDocument>> groups,
        JournalItemReferenceDocument reference)
    {
        return groups.SelectMany(static group => group).Any(value =>
            string.Equals(value.Mod, reference.Mod, StringComparison.OrdinalIgnoreCase)
            && string.Equals(value.Item, reference.Item, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<JournalItemGroup> ResolveItemGroups(
        IEnumerable<List<JournalItemReferenceDocument>> groups)
    {
        return groups
            .Select(group => group
                .Select(ResolveItemReference)
                .Where(static itemId => itemId > ItemID.None)
                .ToArray())
            .Where(static group => group.Length > 0)
            .Select(static group => new JournalItemGroup(group))
            .ToArray();
    }

    private static int ResolveItemReference(JournalItemReferenceDocument reference)
    {
        if (string.Equals(reference.Mod, "Terraria", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(reference.Item, out var numericId)
                && ContentSamples.ItemsByType.ContainsKey(numericId))
            {
                return numericId;
            }

            return ItemID.Search.TryGetId(reference.Item, out var vanillaId)
                ? vanillaId
                : ItemID.None;
        }

        return ModContent.TryFind(reference.Mod, reference.Item, out ModItem modItem)
            ? modItem.Type
            : ItemID.None;
    }

    private static string CreateUniqueId(string value, IEnumerable<string> existingIds, string fallback)
    {
        var normalized = new string(value
            .Trim()
            .ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = fallback;
        }

        var existing = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidate = normalized;
        var suffix = 2;
        while (existing.Contains(candidate))
        {
            candidate = $"{normalized}-{suffix++}";
        }

        return candidate;
    }
}
