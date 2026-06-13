using Terraria.ModLoader;

namespace ProgressionJournal.Data.Profiles;

public static class JournalProfileRegistry
{
    private static readonly Dictionary<string, JournalProfile> Profiles =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, JournalProfile> AddonProfiles =
        new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<JournalProfile> All => Profiles.Values
        .Where(IsAvailable)
        .OrderByDescending(static profile => profile.Id == JournalProfileIds.Vanilla)
        .ThenBy(static profile => profile.Name, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public static JournalProfile Active { get; private set; } = null!;

    public static bool IsLoaded => Active is not null;

    public static void Load(IReadOnlyList<JournalEntry> vanillaEntries)
    {
        Profiles.Clear();
        Register(JournalProfileStorage.CreateVanillaProfile(vanillaEntries));
        LoadBundledProfiles();

        foreach (var profile in AddonProfiles.Values)
        {
            Register(profile);
        }

        foreach (var profile in JournalProfileStorage.LoadUserProfiles())
        {
            Register(profile);
        }

        var activeId = JournalProfileStorage.LoadActiveProfileId();
        Active = TryGet(activeId, out var selected) && IsAvailable(selected)
            ? selected
            : Profiles[JournalProfileIds.Vanilla];
    }

    public static void ReloadUserProfiles()
    {
        var activeId = Active?.Id ?? JournalProfileIds.Vanilla;

        foreach (var id in Profiles.Values.Where(static profile => !profile.IsBuiltIn).Select(static profile => profile.Id).ToArray())
        {
            Profiles.Remove(id);
        }

        foreach (var profile in JournalProfileStorage.LoadUserProfiles())
        {
            Register(profile);
        }

        Active = TryGet(activeId, out var selected) && IsAvailable(selected)
            ? selected
            : Profiles[JournalProfileIds.Vanilla];
    }

    public static void Register(JournalProfile profile)
    {
        Profiles[profile.Id] = profile;
    }

    public static void RefreshVanillaProfile(IReadOnlyList<JournalEntry> entries)
    {
        var wasActive = Active is not null
            && string.Equals(Active.Id, JournalProfileIds.Vanilla, StringComparison.OrdinalIgnoreCase);
        var profile = JournalProfileStorage.CreateVanillaProfile(entries);
        Profiles[profile.Id] = profile;

        if (wasActive)
        {
            Active = profile;
        }
    }

    public static bool RegisterJson(string json, string sourceName, out string error)
    {
        if (!JournalProfileStorage.TryParse(json, sourceName, isBuiltIn: true, out var profile, out error)
            || profile is null)
        {
            return false;
        }

        AddonProfiles[profile.Id] = profile;
        Register(profile);
        return true;
    }

    public static bool TryGet(string profileId, out JournalProfile profile)
    {
        return Profiles.TryGetValue(profileId, out profile!);
    }

    public static bool Select(string profileId)
    {
        if (!TryGet(profileId, out var profile) || !IsAvailable(profile))
        {
            return false;
        }

        Active = profile;
        JournalProfileStorage.SaveActiveProfileId(profile.Id);
        return true;
    }

    public static void Unload()
    {
        Profiles.Clear();
        AddonProfiles.Clear();
        Active = null!;
        JournalProfileUnlockRegistry.Clear();
        JournalNpcUnlockTracker.Clear();
    }

    public static bool IsAvailable(JournalProfile profile)
    {
        return profile.Document.RequiredMods.All(requirement => ModLoader.HasMod(requirement.Name));
    }

    private static void LoadBundledProfiles()
    {
        try
        {
            var mod = ProgressionJournal.Instance;
            if (mod is null)
            {
                return;
            }

            foreach (var path in mod.GetFileNames()
                         .Where(static path =>
                             path.StartsWith("Profiles/Builtin/", StringComparison.OrdinalIgnoreCase)
                             && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                             && !path.EndsWith("-report.json", StringComparison.OrdinalIgnoreCase)))
            {
                var json = System.Text.Encoding.UTF8.GetString(mod.GetFileBytes(path));
                if (JournalProfileStorage.TryParse(json, $"builtin:{path}", isBuiltIn: true, out var profile, out _)
                    && profile is not null)
                {
                    Register(profile);
                }
            }
        }
        catch
        {
            // A missing optional bundled profile must not prevent the journal from loading.
        }
    }
}
