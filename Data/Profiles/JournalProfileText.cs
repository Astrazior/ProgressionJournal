using Terraria.Localization;

namespace ProgressionJournal.Data.Profiles;

public static class JournalProfileText
{
    public static string GetClassName(JournalProfile profile, string classId)
    {
        var definition = profile.GetClass(classId);
        if (string.Equals(profile.Id, JournalProfileIds.Vanilla, StringComparison.OrdinalIgnoreCase))
        {
            var legacyClass = JournalClassIds.ToLegacy(classId);
            return Language.GetTextValue($"Mods.ProgressionJournal.Classes.{legacyClass}");
        }

        return LocalizeIfKey(definition.Name.Resolve());
    }

    public static string GetStageName(JournalProfile profile, string stageId)
    {
        return LocalizeIfKey(profile.GetStage(stageId).Name.Resolve());
    }

    private static string LocalizeIfKey(string value)
    {
        return value.StartsWith("Mods.", StringComparison.Ordinal)
            ? Language.GetTextValue(value)
            : value;
    }
}
