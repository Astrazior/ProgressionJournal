using System.Reflection;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Utilities;

namespace ProgressionJournal.Data.Resolvers;

internal sealed class JournalWorldStateIsolation : IDisposable
{
    private static readonly BooleanMember[] IdentityMembers = CreateIdentityMembers();

    private readonly (BooleanMember Member, bool Value)[] _identityState =
        IdentityMembers.Select(static member => (member, member.Get())).ToArray();
    private readonly int _gameMode = Main.GameMode;
    private readonly bool _hardMode = Main.hardMode;
    private readonly bool _dayTime = Main.dayTime;
    private readonly double _time = Main.time;
    private readonly bool _bloodMoon = Main.bloodMoon;
    private readonly int _moonPhase = Main.moonPhase;
    private readonly bool _crimson = WorldGen.crimson;
    private readonly bool _eclipse = Main.eclipse;
    private readonly bool _raining = Main.raining;
    private readonly bool _slimeRain = Main.slimeRain;
    private readonly bool _pumpkinMoon = Main.pumpkinMoon;
    private readonly bool _snowMoon = Main.snowMoon;
    private readonly bool _xmas = Main.xMas;
    private readonly bool _halloween = Main.halloween;
    private readonly int _invasionType = Main.invasionType;
    private readonly double _invasionX = Main.invasionX;
    private readonly int _invasionSize = Main.invasionSize;
    private readonly int _invasionDelay = Main.invasionDelay;
    private readonly int _spawnTileX = Main.spawnTileX;
    private readonly int _spawnTileY = Main.spawnTileY;
    private readonly int _waterStyle = Main.waterStyle;
    private readonly int _netMode = Main.netMode;
    private readonly UnifiedRandom _random = Main.rand;
    private readonly bool _sandstorm = Sandstorm.Happening;
    private readonly float _sandstormSeverity = Sandstorm.Severity;
    private readonly bool _dd2Ongoing = DD2Event.Ongoing;
    private readonly int _dd2Difficulty = DD2Event.OngoingDifficulty;
    private readonly bool _genuineParty = BirthdayParty.GenuineParty;

    public static JournalWorldProbeScenario[] ComprehensiveScenarios { get; } =
    [
        new("Normal Corruption", GameModeID.Normal),
        new("Normal Crimson", GameModeID.Normal, Crimson: true),
        new("Expert", GameModeID.Expert),
        new("Master", GameModeID.Master),
        new("Hardmode", GameModeID.Normal, Hardmode: true),
        new("Afternoon", GameModeID.Normal, Time: 36000d),
        new("Full moon", GameModeID.Normal, DayTime: false, MoonPhase: 0),
        new("Blood moon", GameModeID.Normal, DayTime: false, BloodMoon: true),
        new("Drunk world", GameModeID.Normal, EnabledIdentityFlags: ["drunkWorld"]),
        new("For the Worthy", GameModeID.Normal, EnabledIdentityFlags: ["getGoodWorld"]),
        new("Celebration", GameModeID.Normal, EnabledIdentityFlags: ["tenthAnniversaryWorld"]),
        new("The Constant", GameModeID.Normal, EnabledIdentityFlags: ["dontStarveWorld"]),
        new("Not the Bees", GameModeID.Normal, EnabledIdentityFlags: ["notTheBeesWorld"]),
        new("Remix", GameModeID.Normal, EnabledIdentityFlags: ["remixWorld"]),
        new("No Traps", GameModeID.Normal, EnabledIdentityFlags: ["noTrapsWorld"]),
        new(
            "Zenith",
            GameModeID.Normal,
            EnabledIdentityFlags: ["getGoodWorld", "remixWorld", "zenithWorld"])
    ];

    public static JournalWorldProbeScenario[] IdentityScenarios { get; } =
    [
        new("Standard Corruption", GameModeID.Normal),
        new("Standard Crimson", GameModeID.Normal, Crimson: true),
        new("Drunk world", GameModeID.Normal, EnabledIdentityFlags: ["drunkWorld"]),
        new("For the Worthy", GameModeID.Normal, EnabledIdentityFlags: ["getGoodWorld"]),
        new("Celebration", GameModeID.Normal, EnabledIdentityFlags: ["tenthAnniversaryWorld"]),
        new("The Constant", GameModeID.Normal, EnabledIdentityFlags: ["dontStarveWorld"]),
        new("Not the Bees", GameModeID.Normal, EnabledIdentityFlags: ["notTheBeesWorld"]),
        new("Remix", GameModeID.Normal, EnabledIdentityFlags: ["remixWorld"]),
        new("No Traps", GameModeID.Normal, EnabledIdentityFlags: ["noTrapsWorld"]),
        new(
            "Zenith",
            GameModeID.Normal,
            EnabledIdentityFlags: ["getGoodWorld", "remixWorld", "zenithWorld"])
    ];

    public static void ApplyNeutralBaseline()
    {
        Main.netMode = NetmodeID.SinglePlayer;
        Main.rand = new UnifiedRandom(0);
        Main.GameMode = GameModeID.Normal;
        Main.hardMode = false;
        Main.dayTime = true;
        Main.time = 0d;
        Main.bloodMoon = false;
        Main.moonPhase = 1;
        Main.eclipse = false;
        Main.raining = false;
        Main.slimeRain = false;
        Main.pumpkinMoon = false;
        Main.snowMoon = false;
        Main.xMas = false;
        Main.halloween = false;
        Main.invasionType = InvasionID.None;
        Main.invasionX = Main.maxTilesX / 2d;
        Main.invasionSize = 0;
        Main.invasionDelay = 0;
        Main.spawnTileX = Math.Clamp(Main.maxTilesX / 2, 10, Main.maxTilesX - 10);
        Main.spawnTileY = Math.Clamp((int)Main.worldSurface, 10, Main.maxTilesY - 10);
        Main.waterStyle = 0;
        Sandstorm.Happening = false;
        Sandstorm.Severity = 0f;
        DD2Event.Ongoing = false;
        DD2Event.OngoingDifficulty = 0;
        BirthdayParty.GenuineParty = false;
        ApplyNeutralWorldIdentity();
    }

    public static void ApplyNeutralWorldIdentity()
    {
        WorldGen.crimson = false;
        foreach (var member in IdentityMembers)
        {
            member.Set(false);
        }
    }

    public static void ApplyScenario(JournalWorldProbeScenario scenario)
    {
        ApplyNeutralBaseline();
        Main.GameMode = scenario.GameMode;
        Main.hardMode = scenario.Hardmode;
        Main.dayTime = scenario.DayTime;
        Main.time = scenario.Time;
        Main.bloodMoon = scenario.BloodMoon;
        Main.moonPhase = scenario.MoonPhase;
        WorldGen.crimson = scenario.Crimson;
        foreach (var name in scenario.EnabledIdentityFlags ?? [])
        {
            SetIdentityFlag(name, true);
        }
    }

    public static void ApplyWorldIdentityScenario(JournalWorldProbeScenario scenario)
    {
        ApplyNeutralWorldIdentity();
        WorldGen.crimson = scenario.Crimson;
        foreach (var name in scenario.EnabledIdentityFlags ?? [])
        {
            SetIdentityFlag(name, true);
        }
    }

    public static void SetIdentityFlag(string name, bool value)
    {
        var member = IdentityMembers.FirstOrDefault(member =>
            member.Name.Equals(name, StringComparison.Ordinal));
        member?.Set(value);
    }

    public void Dispose()
    {
        Main.GameMode = _gameMode;
        Main.hardMode = _hardMode;
        Main.dayTime = _dayTime;
        Main.time = _time;
        Main.bloodMoon = _bloodMoon;
        Main.moonPhase = _moonPhase;
        WorldGen.crimson = _crimson;
        Main.eclipse = _eclipse;
        Main.raining = _raining;
        Main.slimeRain = _slimeRain;
        Main.pumpkinMoon = _pumpkinMoon;
        Main.snowMoon = _snowMoon;
        Main.xMas = _xmas;
        Main.halloween = _halloween;
        Main.invasionType = _invasionType;
        Main.invasionX = _invasionX;
        Main.invasionSize = _invasionSize;
        Main.invasionDelay = _invasionDelay;
        Main.spawnTileX = _spawnTileX;
        Main.spawnTileY = _spawnTileY;
        Main.waterStyle = _waterStyle;
        Main.netMode = _netMode;
        Main.rand = _random;
        Sandstorm.Happening = _sandstorm;
        Sandstorm.Severity = _sandstormSeverity;
        DD2Event.Ongoing = _dd2Ongoing;
        DD2Event.OngoingDifficulty = _dd2Difficulty;
        BirthdayParty.GenuineParty = _genuineParty;
        foreach (var (member, value) in _identityState)
        {
            member.Set(value);
        }
    }

    private static BooleanMember[] CreateIdentityMembers()
    {
        const BindingFlags flags =
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        string[] names =
        [
            "drunkWorld",
            "getGoodWorld",
            "tenthAnniversaryWorld",
            "dontStarveWorld",
            "notTheBeesWorld",
            "remixWorld",
            "noTrapsWorld",
            "zenithWorld"
        ];
        return names
            .Select(name => BooleanMember.TryCreate(typeof(Main), name, flags))
            .OfType<BooleanMember>()
            .ToArray();
    }

    private sealed record BooleanMember(
        string Name,
        Func<bool> Get,
        Action<bool> Set)
    {
        public static BooleanMember? TryCreate(Type type, string name, BindingFlags flags)
        {
            var field = type.GetField(name, flags);
            if (field is { FieldType: not null, IsInitOnly: false }
                && field.FieldType == typeof(bool))
            {
                return new BooleanMember(
                    name,
                    () => (bool)(field.GetValue(null) ?? false),
                    value => field.SetValue(null, value));
            }

            var property = type.GetProperty(name, flags);
            if (property is { PropertyType: not null, CanRead: true, CanWrite: true }
                && property.PropertyType == typeof(bool))
            {
                return new BooleanMember(
                    name,
                    () => (bool)(property.GetValue(null) ?? false),
                    value => property.SetValue(null, value));
            }

            return null;
        }
    }
}

internal readonly record struct JournalWorldProbeScenario(
    string Name,
    int GameMode,
    bool Crimson = false,
    bool Hardmode = false,
    bool DayTime = true,
    double Time = 0d,
    bool BloodMoon = false,
    int MoonPhase = 1,
    string[]? EnabledIdentityFlags = null);
