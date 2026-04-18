using System.Collections.Generic;
using Terraria.ID;

namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private static void AddAccessoryEntries(List<JournalEntry> entries)
    {
        entries.AddRange(
        [
            Entry("shieldOfCthulhuPostEye", JournalItemCategory.Accessory, CombatClass.All, ItemID.EoCShield,
                            Eval(ProgressionStageId.PostEyeOfCthulhu, RecommendationTier.Recommended)),

            Entry("wormScarfOrBrainOfConfusionPostWorldEvil", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.WormScarf, ItemID.BrainOfConfusion),
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

            Entry("honeyCombPostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.HoneyComb,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Additional)),

            Entry("stingerNecklacePostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.StingerNecklace,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            Entry("honeyBalloonPostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.HoneyBalloon,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Additional)),

            Entry("sweetheartNecklacePostWorldEvil", JournalItemCategory.Accessory, CombatClass.All, ItemID.SweetheartNecklace,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            Entry("hiveBackpackPostWorldEvil", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.HiveBackpack,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Useless)),

            Entry("pygmyNecklacePostWorldEvil", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.PygmyNecklace,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            Entry("cobaltShieldPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.CobaltShield,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Additional)),

            Entry("obsidianShieldPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.ObsidianShield,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("boneGlovePostSkeletron", JournalItemCategory.Accessory, CombatClass.All, ItemID.BoneGlove,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.NotRecommended)),

            Entry("boneHelmPostSkeletron", JournalItemCategory.Accessory, CombatClass.All, ItemID.BoneHelm,
                            Eval(ProgressionStageId.PostDeerclops, RecommendationTier.Additional)),

            Entry("nazarOrArmorPolishPostSkeletron", JournalItemCategory.Accessory, CombatClass.Melee,
                            Group(ItemID.Nazar, ItemID.ArmorPolish),
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Useless)),

            Entry("celestialMagnetPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.CelestialMagnet,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("magicCuffsPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.MagicCuffs,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("celestialCuffsOrMagnetFlowerPreBoss", JournalItemCategory.Accessory, CombatClass.Magic,
                            Group(ItemID.CelestialCuffs, ItemID.MagnetFlower),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("bandOfStarpowerPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.BandofStarpower,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("manaRegenerationBandPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.ManaRegenerationBand,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("naturesGiftPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.NaturesGift,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("manaFlowerPreBoss", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.ManaFlower,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("royalGelPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.RoyalGel,
                            Eval(ProgressionStageId.PostKingSlime, RecommendationTier.NotRecommended)),

            Entry("magiluminescencePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.Magiluminescence,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            new JournalEntry("bootsProgressionPreBoss", JournalItemCategory.Accessory, CombatClass.All,
                            [
                                Group(ItemID.HermesBoots, ItemID.FlurryBoots, ItemID.SailfishBoots, ItemID.SandBoots),
                                Group(ItemID.RocketBoots),
                                Group(ItemID.SpectreBoots),
                                Group(ItemID.LightningBoots),
                                Group(ItemID.FrostsparkBoots),
                                Group(ItemID.TerrasparkBoots)
                            ],
                            [Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)]),

            Entry("amphibianBootsPreBoss", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.AmphibianBoots, ItemID.FairyBoots),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            EventEntry("sharkToothNecklacePreBoss", JournalItemCategory.Accessory, CombatClass.All, JournalEventCategory.BloodMoon, ItemID.SharkToothNecklace,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("panicNecklacePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.PanicNecklace,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("sandstormOrBlizzardBottlePreBoss", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.SandstorminaBottle, ItemID.BlizzardinaBottle, ItemID.SharkronBalloon),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("balloonBundlesPreBoss", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.BundleofBalloons, ItemID.HorseshoeBundle),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("feralClawsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FeralClaws,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("feralClawsSummonerPreBoss", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.FeralClaws,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            EventEntry("apprenticeScarfOrSquireShieldPreBoss", JournalItemCategory.Accessory, CombatClass.Summoner, JournalEventCategory.OldOnesArmy,
                            Group(ItemID.ApprenticeScarf, ItemID.SquireShield),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("magmaStonePreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.MagmaStone,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("bezoarPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.Bezoar,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("obsidianRosePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.ObsidianRose,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("lavaCharmOrMoltenCharmPreBoss", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.LavaCharm, ItemID.MoltenCharm),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("obsidianSkullLinePreBoss", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.ObsidianSkull, ItemID.ObsidianSkullRose, ItemID.MoltenSkullRose),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("bandOfRegenerationPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.BandofRegeneration,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("fledglingWingsPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.CreativeWings,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("ankletOfTheWindPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.AnkletoftheWind,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("horseshoeBalloonsPreBoss", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.WhiteHorseshoeBalloon, ItemID.BlueHorseshoeBalloon, ItemID.YellowHorseshoeBalloon, ItemID.BalloonHorseshoeHoney, ItemID.BalloonHorseshoeSharkron, ItemID.BalloonHorseshoeFart),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("cloudInBottle", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.CloudinaBottle, ItemID.TsunamiInABottle, ItemID.FartinaJar),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("stringsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee,
                            Group(ItemID.WhiteString, ItemID.BlueString, ItemID.GreenString, ItemID.BrownString, ItemID.OrangeString, ItemID.YellowString, ItemID.RedString, ItemID.PurpleString, ItemID.BlackString, ItemID.RainbowString, ItemID.PinkString, ItemID.SkyBlueString, ItemID.CyanString, ItemID.LimeString, ItemID.TealString, ItemID.VioletString),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("counterweightsPreBoss", JournalItemCategory.Accessory, CombatClass.Melee,
                            Group(ItemID.BlackCounterweight, ItemID.BlueCounterweight, ItemID.GreenCounterweight, ItemID.PurpleCounterweight, ItemID.RedCounterweight, ItemID.YellowCounterweight),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("luckyHorseshoePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.LuckyHorseshoe,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("agletPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.Aglet,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("flyingCarpetPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FlyingCarpet,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("shinyRedBalloonPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.ShinyRedBalloon,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("balloonPufferfishPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.BalloonPufferfish,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("shacklePreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.Shackle,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("frogWebbingPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FrogWebbing,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("frogGearPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FrogGear,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("frogLegPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FrogLeg,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("frogFlipperPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.FrogFlipper,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("climbingClawsPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.ClimbingClaws,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("shoeSpikesPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.ShoeSpikes,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("tigerClimbingGearPreBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.TigerClimbingGear,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("breathingReedPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.BreathingReed,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("umbrellaPreBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.Umbrella,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("postPlanteraWingsPostPlantera", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.MothronWings, ItemID.FestiveWings, ItemID.SpookyWings, ItemID.TatteredFairyWings, ItemID.LeafWings, 823, 1866),
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("fishronWingsPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.FishronWings,
                            Eval(ProgressionStageId.PostDukeFishron, RecommendationTier.Recommended)),

            Entry("empressWingsPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.EmpressFlightBooster,
                            Eval(ProgressionStageId.PostEmpressOfLight, RecommendationTier.Recommended)),

            Entry("empressActualWingsPostPlantera", JournalItemCategory.Accessory, CombatClass.All, 4823,
                            Eval(ProgressionStageId.PostEmpressOfLight, RecommendationTier.Recommended)),

            Entry("frozenShieldOrHeroShieldPostPlantera", JournalItemCategory.Accessory, CombatClass.Melee,
                            Group(ItemID.FrozenShield, ItemID.HeroShield),
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("blackBeltPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.BlackBelt,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("masterNinjaGearPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.MasterNinjaGear,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("sporeSacPostPlantera", JournalItemCategory.Accessory, CombatClass.All, ItemID.SporeSac,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("herculesBeetlePostPlantera", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.HerculesBeetle,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("papyrusScarabPostPlantera", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.PapyrusScarab,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("paladinsShieldPostPlantera", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.PaladinsShield,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("tabiPostPlantera", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.Tabi,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("moonLordWingsPostMoonLord", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.LongRainbowTrailWings, ItemID.WingsSolar, ItemID.WingsNebula, ItemID.WingsVortex, ItemID.WingsStardust),
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("destroyerEmblemPostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.DestroyerEmblem,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Entry("eyeOfTheGolemMeleePostGolem", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.EyeoftheGolem,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

            Entry("eyeOfTheGolemRangedPostGolem", JournalItemCategory.Accessory, CombatClass.Ranged, ItemID.EyeoftheGolem,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

            Entry("eyeOfTheGolemMagicPostGolem", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.EyeoftheGolem,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

            Entry("eyeOfTheGolemSummonerPostGolem", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.EyeoftheGolem,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

            Entry("scopeLinePostPlantera", JournalItemCategory.Accessory, CombatClass.Ranged,
                            Group(ItemID.RifleScope, ItemID.SniperScope, ItemID.ReconScope),
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("sunStonePostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.SunStone,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Additional)),

            Entry("celestialStonePostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.CelestialStone,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Entry("celestialShellPostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.CelestialShell,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Recommended)),

            Entry("volatileGelatinHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.VolatileGelatin,
                            Eval(ProgressionStageId.PostQueenSlime, RecommendationTier.NotRecommended)),

            Entry("ankhIngredientsHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee,
                            Group(ItemID.Blindfold, ItemID.Megaphone, ItemID.TrifoldMap, ItemID.FastClock, ItemID.Vitamins, ItemID.PocketMirror),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("ankhIntermediatesHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee,
                            Group(ItemID.CountercurseMantra, ItemID.MedicatedBandage, ItemID.ThePlan, ItemID.ArmorBracing, ItemID.ReflectiveShades),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("frozenTurtleShellHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FrozenTurtleShell,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("frozenTurtleShellRangedHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Ranged, ItemID.FrozenTurtleShell,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("crossNecklaceHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.CrossNecklace,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("moonCharmHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.MoonCharm,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("earlyHardmodeWingsHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All,
                            Group(493, 492, 761, 2494, 822, 785),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("philosophersStoneHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.PhilosophersStone,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("charmofMythsHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.CharmofMyths,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("ankhCharmHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.AnkhCharm,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("ankhShieldHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.AnkhShield,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("arcaneFlowerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.ArcaneFlower,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("manaCloakHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.ManaCloak,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("sorcererEmblemHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.SorcererEmblem,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("putridScentMagicHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.PutridScent,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("frozenTurtleShellMagicHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.FrozenTurtleShell,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("titanGloveSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.TitanGlove,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("berserkerGloveSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.BerserkerGlove,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("powerGloveSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.PowerGlove,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("mechanicalGloveSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.MechanicalGlove,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("fireGauntletSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.FireGauntlet,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("putridScentSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.PutridScent,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("frozenTurtleShellSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.FrozenTurtleShell,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("hiveBackpackSummonerHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Summoner, ItemID.HiveBackpack,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            Entry("starCloakOrBeeCloakHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.StarCloak, ItemID.BeeCloak),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("starVeilHardmodeEntry", JournalItemCategory.Accessory, CombatClass.All, ItemID.StarVeil,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("warriorEmblemHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.WarriorEmblem,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("celestialEmblemPostThreeMechBosses", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.CelestialEmblem,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("avengerEmblemPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.AvengerEmblem,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("titanGloveHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.TitanGlove,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("berserkerGloveHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.BerserkerGlove,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("powerGloveHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.PowerGlove,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("mechanicalGlovePostOneMechBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.MechanicalGlove,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("fireGauntletPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FireGauntlet,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("yoyoGloveHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.YoYoGlove,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("yoyoBagHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.YoyoBag,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("fleshKnucklesHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Melee, ItemID.FleshKnuckles,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("putridScentHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Ranged, ItemID.PutridScent,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("hivePackPostWorldEvil", JournalItemCategory.Accessory, CombatClass.Magic, ItemID.HiveBackpack,
                            Eval(ProgressionStageId.PostQueenBee, RecommendationTier.Recommended)),

            EventEntry("huntressBucklerOrMonkBeltPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.Summoner, JournalEventCategory.OldOnesArmy,
                            Group(ItemID.HuntressBuckler, ItemID.MonkBelt),
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Useless)),

            EventEntry("moonStonePostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, JournalEventCategory.SolarEclipse, ItemID.MoonStone,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            EventEntry("neptunesShellPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, JournalEventCategory.SolarEclipse, ItemID.NeptunesShell,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Entry("moonShellPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All, ItemID.MoonShell,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("shinyStonePostGolem", JournalItemCategory.Accessory, CombatClass.All, ItemID.ShinyStone,
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.Useless)),

            Entry("postOneMechBossWingsPostOneMechBoss", JournalItemCategory.Accessory, CombatClass.All,
                            Group(ItemID.Jetpack, ItemID.BatWings, ItemID.BeeWings, ItemID.ButterflyWings, ItemID.FlameWings),
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            Entry("rangerEmblemHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Ranged, ItemID.RangerEmblem,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("quiverLineHardmodeEntry", JournalItemCategory.Accessory, CombatClass.Ranged,
                            Group(ItemID.MagicQuiver, ItemID.MoltenQuiver, ItemID.StalkersQuiver),
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),
        ]);
    }
}

