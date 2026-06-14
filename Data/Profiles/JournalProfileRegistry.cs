using Terraria.ModLoader;

namespace ProgressionJournal.Data.Profiles;

public static class JournalProfileRegistry
{
    private static readonly Dictionary<string, JournalProfile> Profiles =
        new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<JournalProfile> All => Profiles.Values
        .Where(IsAvailable)
        .OrderByDescending(static profile => profile.Id == JournalProfileIds.Vanilla)
        .ThenBy(static profile => profile.DisplayName, StringComparer.CurrentCultureIgnoreCase)
        .ToArray();

    public static JournalProfile Active { get; private set; } = null!;

    public static bool IsLoaded => Active is not null;

    public static void Load(IReadOnlyList<JournalEntry> vanillaEntries)
    {
        Profiles.Clear();
        Register(JournalProfileStorage.CreateVanillaProfile(vanillaEntries));
        LoadBundledProfiles();

        var activeId = JournalProfileStorage.LoadActiveProfileId();
        Active = TryGet(activeId, out var selected) && IsAvailable(selected)
            ? selected
            : Profiles[JournalProfileIds.Vanilla];
    }

    private static void Register(JournalProfile profile)
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
                             path.StartsWith("Profiles/Mods/", StringComparison.OrdinalIgnoreCase)
                             && path.EndsWith("/profile.json", StringComparison.OrdinalIgnoreCase)))
            {
                var json = System.Text.Encoding.UTF8.GetString(mod.GetFileBytes(path));
                if (JournalProfileStorage.TryParseBuiltIn(json, out var profile, out _)
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
