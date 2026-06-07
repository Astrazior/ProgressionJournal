using Terraria;
using Terraria.ID;

namespace ProgressionJournal.Data.Profiles;

public sealed class JournalProfileEditorSession
{
    private JournalProfileEditorSession(JournalProfileDocument document, string? sourcePath)
    {
        Document = document;
        SourcePath = sourcePath;
        SelectedClassId = document.Classes[0].Id;
        SelectedStageId = document.Stages[0].Id;
    }

    public JournalProfileDocument Document { get; }

    public string? SourcePath { get; }

    public string SelectedClassId { get; private set; }

    public string SelectedStageId { get; private set; }

    public JournalItemCategory SelectedCategory { get; private set; } = JournalItemCategory.Weapon;

    public RecommendationTier SelectedTier { get; private set; } = RecommendationTier.FromGuide;

    public static JournalProfileEditorSession CreateNew(string? name = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        var document = new JournalProfileDocument
        {
            Id = $"user.profile.{timestamp}",
            Name = string.IsNullOrWhiteSpace(name) ? "My progression" : name.Trim(),
            Author = Main.LocalPlayer?.name ?? string.Empty,
            ProfileVersion = "1.0.0",
            Classes =
            [
                new JournalProfileClassDocument
                {
                    Id = JournalClassIds.Melee,
                    Name = "Melee",
                    DamageClassNames = ["MeleeDamageClass"]
                }
            ],
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

    public void CycleClass(int direction)
    {
        SelectedClassId = CycleId(Document.Classes.Select(static value => value.Id).ToArray(), SelectedClassId, direction);
    }

    public void SelectStage(string stageId)
    {
        if (Document.Stages.Any(value => string.Equals(value.Id, stageId, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedStageId = stageId;
        }
    }

    public void CycleStage(int direction)
    {
        SelectedStageId = CycleId(Document.Stages.Select(static value => value.Id).ToArray(), SelectedStageId, direction);
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

    public bool RemoveSelectedClass()
    {
        if (Document.Classes.Count <= 1)
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

        SelectedClassId = Document.Classes[0].Id;
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

        SelectedStageId = Document.Stages[0].Id;
        return true;
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

    public void SetSelectedStageBoss(string modName, string npcName)
    {
        var stage = GetSelectedStage();
        stage.IconMod = modName;
        stage.IconNpc = npcName;
        stage.Unlock = new JournalUnlockConditionDocument
        {
            Type = "npc",
            Mod = modName,
            Npc = npcName
        };
    }

    public void SetSelectedStageAlwaysAvailable()
    {
        GetSelectedStage().Unlock = new JournalUnlockConditionDocument { Type = "always" };
    }

    public void SetSelectedStageAccessorySlots(int count)
    {
        GetSelectedStage().AccessorySlots = Math.Clamp(count, 0, 7);
    }

    public void CycleCategory(int direction)
    {
        SelectedCategory = CycleEnum(SelectedCategory, direction);
    }

    public void CycleTier(int direction)
    {
        SelectedTier = CycleEnum(SelectedTier, direction);
    }

    public bool AddItem(int itemId)
    {
        if (!ContentSamples.ItemsByType.TryGetValue(itemId, out var item) || item is null || item.IsAir)
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
                Category = SelectedCategory,
                Classes = [SelectedClassId],
                ItemGroups = [[reference]],
                Evaluations =
                [
                    new JournalProfileEvaluationDocument
                    {
                        StageId = SelectedStageId,
                        Tier = SelectedTier
                    }
                ]
            };
            Document.Entries.Add(existing);
            return true;
        }

        existing.Category = SelectedCategory;
        var evaluation = existing.Evaluations.FirstOrDefault(
            value => string.Equals(value.StageId, SelectedStageId, StringComparison.OrdinalIgnoreCase));
        if (evaluation is null)
        {
            existing.Evaluations.Add(new JournalProfileEvaluationDocument
            {
                StageId = SelectedStageId,
                Tier = SelectedTier
            });
        }
        else
        {
            evaluation.Tier = SelectedTier;
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

    private static TEnum CycleEnum<TEnum>(TEnum current, int direction) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        var index = Array.IndexOf(values, current);
        return values[(index + Math.Sign(direction) + values.Length) % values.Length];
    }

    private static string CycleId(IReadOnlyList<string> values, string current, int direction)
    {
        var index = values
            .Select((value, valueIndex) => new { value, valueIndex })
            .FirstOrDefault(pair => string.Equals(pair.value, current, StringComparison.OrdinalIgnoreCase))
            ?.valueIndex ?? 0;
        return values[(index + Math.Sign(direction) + values.Count) % values.Count];
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
