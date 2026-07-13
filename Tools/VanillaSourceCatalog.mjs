import { resolveSnapshotStageIndex } from "./SnapshotStageResolver.mjs";

const START_STATIONS = [
  "Terraria/WorkBenches",
  "Terraria/Furnaces",
  "Terraria/Anvils",
  "Terraria/Bottles",
  "Terraria/Kegs",
  "Terraria/Bookcases",
  "Terraria/DemonAltar",
  "Terraria/Hellforge",
  "Terraria/CookingPots",
  "Terraria/Loom",
  "Terraria/Sawmill",
  "Terraria/SkyMill",
  "Terraria/Tombstones"
];

const START_ITEMS = [
  "Terraria/Wood",
  "Terraria/BorealWood",
  "Terraria/PalmWood",
  "Terraria/RichMahogany",
  "Terraria/Ebonwood",
  "Terraria/Shadewood",
  "Terraria/AshWood",
  "Terraria/StoneBlock",
  "Terraria/DirtBlock",
  "Terraria/MudBlock",
  "Terraria/ClayBlock",
  "Terraria/SandBlock",
  "Terraria/EbonsandBlock",
  "Terraria/CrimsandBlock",
  "Terraria/PearlsandBlock",
  "Terraria/SiltBlock",
  "Terraria/SlushBlock",
  "Terraria/AshBlock",
  "Terraria/SnowBlock",
  "Terraria/IceBlock",
  "Terraria/Glass",
  "Terraria/Bottle",
  "Terraria/BottledWater",
  "Terraria/Gel",
  "Terraria/PinkGel",
  "Terraria/Cobweb",
  "Terraria/Silk",
  "Terraria/Mushroom",
  "Terraria/GlowingMushroom",
  "Terraria/Obsidian",
  "Terraria/Cactus",
  "Terraria/Coral",
  "Terraria/Seashell",
  "Terraria/WhitePearl",
  "Terraria/BlackPearl",
  "Terraria/PinkPearl",
  "Terraria/JungleSpores",
  "Terraria/JungleRose",
  "Terraria/Vine",
  "Terraria/Stinger",
  "Terraria/SharkFin",
  "Terraria/AntlionMandible",
  "Terraria/FlinxFur",
  "Terraria/Feather",
  "Terraria/FallenStar",
  "Terraria/MagmaStone",
  "Terraria/Gel",
  "Terraria/WoodenArrow",
  "Terraria/Musket",
  "Terraria/TheUndertaker",
  "Terraria/Vilethorn",
  "Terraria/CrimsonRod",
  "Terraria/Boomstick",
  "Terraria/SnowballCannon",
  "Terraria/Shroomerang",
  "Terraria/Gladius",
  "Terraria/PanicNecklace",
  "Terraria/Chain",
  "Terraria/Worm",
  "Terraria/Robe",
  "Terraria/DynastyWood",
  "Terraria/GoldButterfly",
  "Terraria/PinkPaint",
  "Terraria/CyanPaint",
  "Terraria/RedPaint",
  "Terraria/Lens",
  "Terraria/BlackLens",
  "Terraria/RottenChunk",
  "Terraria/Vertebrae",
  "Terraria/TatteredCloth",
  "Terraria/Spike",
  "Terraria/Amethyst",
  "Terraria/Topaz",
  "Terraria/Sapphire",
  "Terraria/Emerald",
  "Terraria/Ruby",
  "Terraria/Diamond",
  "Terraria/Amber",
  "Terraria/CopperOre",
  "Terraria/TinOre",
  "Terraria/IronOre",
  "Terraria/LeadOre",
  "Terraria/SilverOre",
  "Terraria/TungstenOre",
  "Terraria/GoldOre",
  "Terraria/PlatinumOre",
  "Terraria/CopperBar",
  "Terraria/TinBar",
  "Terraria/IronBar",
  "Terraria/LeadBar",
  "Terraria/SilverBar",
  "Terraria/TungstenBar",
  "Terraria/GoldBar",
  "Terraria/PlatinumBar",
  "Terraria/Hive",
  "Terraria/HoneyBlock",
  "Terraria/BottledHoney",
  "Terraria/Sandstone",
  "Terraria/IllegalGunParts",
  "Terraria/MusketBall",
  "Terraria/WoodenArrow",
  "Terraria/FishingBobber",
  "Terraria/Frog",
  "Terraria/Bird",
  "Terraria/Stinkbug",
  "Terraria/LavaBucket",
  "Terraria/Granite",
  "Terraria/Marble",
  "Terraria/Daybloom",
  "Terraria/Blinkroot",
  "Terraria/Moonglow",
  "Terraria/Waterleaf",
  "Terraria/Deathweed",
  "Terraria/Shiverthorn",
  "Terraria/Fireblossom",
  "Terraria/LadyBug",
  "Terraria/Pumpkin",
  "Terraria/PumpkinSeed",
  "Terraria/Hay",
  "Terraria/LivingWoodWand",
  "Terraria/LeafWand",
  "Terraria/NaturesGift",
  "Terraria/PanicNecklace",
  "Terraria/Boomstick",
  "Terraria/Gladius",
  "Terraria/Musket",
  "Terraria/TheUndertaker",
  "Terraria/Revolver",
  "Terraria/ThrowingKnife",
  "Terraria/SnowballCannon",
  "Terraria/NightVisionHelmet",
  "Terraria/IceMirror",
  "Terraria/MagicMirror",
  "Terraria/Vilethorn",
  "Terraria/CrimsonRod",
  "Terraria/ArcticDivingGear",
  "Terraria/TerrasparkBoots",
  "Terraria/SunplateBlock",
  "Terraria/ShimmerBlock",
  "Terraria/Lemon",
  "Terraria/Grapefruit",
  "Terraria/Plum",
  "Terraria/Starfruit",
  "Terraria/SpicyPepper",
  "Terraria/GiantHarpyFeather",
  "Terraria/BouncyGrenade",
  "Terraria/Grenade",
  "Terraria/Bomb",
  "Terraria/Sickle",
  "Terraria/TigerSkin",
  "Terraria/AntlionClaw",
  "Terraria/ConfettiCannon",
  "Terraria/Confetti",
  "Terraria/PainterPaintballGun",
  "Terraria/LavaproofTackleBag",
  "Terraria/HorseshoeBundle",
  "Terraria/FrogFlipper",
  "Terraria/FrogGear",
  "Terraria/Cloud",
  "Terraria/TopHat",
  "Terraria/BowlofSoup",
  "Terraria/VilePowder",
  "Terraria/ViciousPowder",
  "Terraria/AbigailsFlower",
  "Terraria/Seed"
];

const MILESTONE_FACTS = [
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedSlimeKing", "king-slime"),
    enemies: ["Terraria/KingSlime"]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedBoss1", "eye-of-cthulhu"),
    items: [
      "Terraria/CrimtaneOre",
      "Terraria/DemoniteOre",
      "Terraria/CrimtaneBar",
      "Terraria/DemoniteBar"
    ],
    shops: ["Terraria/Dryad"]
  },
  {
    findStage: manifest => findStageById(manifest, "world-evil"),
    items: [
      "Terraria/ShadowScale",
      "Terraria/TissueSample",
      "Terraria/Hellstone",
      "Terraria/HellstoneBar",
      "Terraria/Meteorite",
      "Terraria/MeteoriteBar"
    ],
    shops: ["Terraria/DD2Bartender"]
  },
  {
    findStage: manifest => findStageById(manifest, "queen-bee"),
    items: [
      "Terraria/BeeWax",
      "Terraria/SweetheartNecklace"
    ],
    stations: ["Terraria/ImbuingStation"],
    shops: ["Terraria/WitchDoctor"]
  },
  {
    eventCategory: "GoblinArmy",
    findStage: manifest => findEventStage(manifest, "GoblinArmy"),
    items: ["Terraria/TinkerersWorkshop"],
    stations: ["Terraria/TinkerersWorkbench"],
    shops: ["Terraria/GoblinTinkerer"]
  },
  {
    eventCategory: "BloodMoon",
    findStage: manifest => findEventStage(manifest, "BloodMoon"),
    enemies: [
      "Terraria/ZombieMerman",
      "Terraria/EyeballFlyingFish"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedBoss3", "skeletron"),
    stations: ["Terraria/AlchemyTable"],
    containers: ["Terraria/DungeonGoldChest", "Terraria/ShadowChest", "Terraria/ObsidianLockbox"],
    items: [
      "Terraria/AlchemyTable",
      "Terraria/BlueMoon",
      "Terraria/AquaScepter",
      "Terraria/MagicMissile",
      "Terraria/ShadowKey",
      "Terraria/Bone",
      "Terraria/Wire",
      "Terraria/WaterBolt",
      "Terraria/Sunfury",
      "Terraria/Valor",
      "Terraria/NightsEdge",
      "Terraria/BewitchingTable",
      "Terraria/QuadBarrelShotgun",
      "Terraria/CowboyHat",
      "Terraria/WaterCandle",
      "Terraria/DarkLance",
      "Terraria/HellwingBow"
    ],
    enemies: [
      "Terraria/Clothier",
      "Terraria/DungeonGuardian",
      "Terraria/DungeonSlime"
    ],
    shops: ["Terraria/Clothier", "Terraria/Mechanic"]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "hardMode", "wall-of-flesh"),
    stations: ["Terraria/MythrilAnvil", "Terraria/AdamantiteForge"],
    items: [
      "Terraria/AdamantiteForge",
      "Terraria/CobaltOre",
      "Terraria/PalladiumOre",
      "Terraria/MythrilOre",
      "Terraria/OrichalcumOre",
      "Terraria/AdamantiteOre",
      "Terraria/TitaniumOre",
      "Terraria/CobaltBar",
      "Terraria/PalladiumBar",
      "Terraria/MythrilBar",
      "Terraria/OrichalcumBar",
      "Terraria/AdamantiteBar",
      "Terraria/TitaniumBar",
      "Terraria/CrystalShard",
      "Terraria/Pearlwood",
      "Terraria/CursedFlame",
      "Terraria/Ichor",
      "Terraria/RainbowBrick",
      "Terraria/SoulofLight",
      "Terraria/SoulofNight",
      "Terraria/SoulofFlight",
      "Terraria/PixieDust",
      "Terraria/UnicornHorn",
      "Terraria/SpiderFang",
      "Terraria/FrostCore",
      "Terraria/LightShard",
      "Terraria/DarkShard",
      "Terraria/TurtleShell",
      "Terraria/AncientCloth",
      "Terraria/CrossNecklace",
      "Terraria/NimbusRod",
      "Terraria/FastClock",
      "Terraria/IceSickle",
      "Terraria/MagicDagger",
      "Terraria/BeamSword",
      "Terraria/SpiritFlame",
      "Terraria/IceFeather",
      "Terraria/IceRod",
      "Terraria/MagicQuiver",
      "Terraria/SpellTome",
      "Terraria/CharmofMyths",
      "Terraria/PhilosophersStone",
      "Terraria/FetidBaghnakhs",
      "Terraria/DaedalusStormbow",
      "Terraria/StarVeil",
      "Terraria/BeeCloak",
      "Terraria/StarCloak",
      "Terraria/BrokenBatWing",
      "Terraria/CelestialEmblem",
      "Terraria/CrystalBall"
      ,"Terraria/GreaterManaPotion"
      ,"Terraria/AncientBattleArmorMaterial"
      ,"Terraria/EmptyBullet"
      ,"Terraria/ExplosivePowder"
      ,"Terraria/GoldDust"
      ,"Terraria/CursedArrow"
      ,"Terraria/CursedBullet"
      ,"Terraria/CursedDart"
      ,"Terraria/IchorArrow"
      ,"Terraria/IchorBullet"
      ,"Terraria/IchorDart"
      ,"Terraria/ExplodingBullet"
      ,"Terraria/GoldenBullet"
      ,"Terraria/FinWings"
    ],
    enemies: [
      "Terraria/MossHornet",
      "Terraria/IceGolem",
      "Terraria/SandElemental",
      "Terraria/WyvernHead",
      "Terraria/RuneWizard",
      "Terraria/RainbowSlime",
      "Terraria/BigMimicJungle",
      "Terraria/BigMimicCorruption",
      "Terraria/BigMimicCrimson",
      "Terraria/BigMimicHallow",
      "Terraria/GoblinSummoner",
      "Terraria/SkeletonArcher",
      "Terraria/VampireBat",
      "Terraria/Wizard",
      "Terraria/Truffle",
      "Terraria/ZombieMushroom",
      "Terraria/ZombieMushroomHat",
      "Terraria/FungoFish",
      "Terraria/AnomuraFungus",
      "Terraria/MushiLadybug",
      "Terraria/FungiBulb",
      "Terraria/GiantFungiBulb",
      "Terraria/SporeBat",
      "Terraria/SporeSkeleton",
      "Terraria/AngryTrapper",
      "Terraria/IceMimic",
      "Terraria/BlackRecluse",
      "Terraria/BlackRecluseWall",
      "Terraria/IceElemental",
      "Terraria/IcyMerman",
      "Terraria/PigronCorruption",
      "Terraria/PigronHallow",
      "Terraria/PigronCrimson",
      "Terraria/Medusa",
      "Terraria/Clown",
      "Terraria/BloodEelHead",
      "Terraria/GoblinShark",
      "Terraria/AngryNimbus",
      "Terraria/SnowBalla",
      "Terraria/SnowmanGangsta",
      "Terraria/MisterStabby"
    ],
    shops: [
      "Terraria/Wizard",
      "Terraria/Truffle"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedMechBoss1", "destroyer"),
    items: [
      "Terraria/SoulofMight",
      "Terraria/Megashark",
            "Terraria/LivingFireBlock",
      "Terraria/HallowedBar",
      "Terraria/SuperStarCannon",
      "Terraria/DD2BallistraTowerT2Popper",
      "Terraria/DD2ExplosiveTrapT2Popper",
      "Terraria/DD2FlameburstTowerT2Popper",
      "Terraria/DD2LightningAuraT2Popper"
    ],
    shops: ["Terraria/Steampunker"],
    enemies: ["Terraria/RedDevil", "Terraria/Vampire"]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedMechBoss2", "twins"),
    items: ["Terraria/SoulofSight"]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedMechBoss3", "skeletron-prime"),
    items: [
      "Terraria/SoulofFright",
      "Terraria/MechanicalGlove",
      "Terraria/Cog",
      "Terraria/ChlorophyteOre",
      "Terraria/ChlorophyteBar"
    ],
    enemies: ["Terraria/Reaper"]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedPlantBoss", "plantera"),
    containers: [
      "Terraria/JungleBiomeChest",
      "Terraria/CorruptionBiomeChest",
      "Terraria/CrimsonBiomeChest",
      "Terraria/HallowedBiomeChest",
      "Terraria/FrozenBiomeChest",
      "Terraria/DesertBiomeChest"
    ],
    items: [
      "Terraria/Ectoplasm",
      "Terraria/BrokenHeroSword",
      "Terraria/TempleKey",
      "Terraria/SniperRifle",
      "Terraria/ToxicFlask",
      "Terraria/TacticalShotgun",
      "Terraria/RocketLauncher",
      "Terraria/SpectreStaff",
      "Terraria/MagnetSphere",
      "Terraria/PaladinsShield",
      "Terraria/PaladinsHammer",
      "Terraria/StaffoftheFrostHydra",
      "Terraria/ScourgeoftheCorruptor",
      "Terraria/RainbowGun",
      "Terraria/VampireKnives",
      "Terraria/PiranhaGun",
      "Terraria/StormTigerStaff",
      "Terraria/LifeFruit",
      "Terraria/FrozenShield",
      "Terraria/ShadowbeamStaff",
      "Terraria/PapyrusScarab",
      "Terraria/PygmyNecklace",
      "Terraria/MasterNinjaGear",
      "Terraria/Tabi",
      "Terraria/BlackBelt",
      "Terraria/MoonStone",
      "Terraria/InfernoFork",
      "Terraria/PhoenixBlaster",
      "Terraria/ButterflyDust",
      "Terraria/VialofVenom",
      "Terraria/Keybrand",
      "Terraria/RocketII"
    ],
    enemies: [
      "Terraria/Psycho",
      "Terraria/GiantCursedSkull",
      "Terraria/Mothron",
      "Terraria/Nailhead",
      "Terraria/RustyArmoredBonesAxe",
      "Terraria/RustyArmoredBonesFlail",
      "Terraria/RustyArmoredBonesSword",
      "Terraria/BlueArmoredBones",
      "Terraria/DeadlySphere",
      "Terraria/DrManFly",
      "Terraria/Lihzahrd",
      "Terraria/LihzahrdCrawler",
      "Terraria/FlyingSnake"
    ],
    shops: ["Terraria/Cyborg", "Terraria/Princess"]
  },
  {
    eventCategory: "PumpkinMoon",
    findStage: manifest => findEventStage(manifest, "PumpkinMoon"),
    items: [
      "Terraria/TheHorsemansBlade",
      "Terraria/RavenStaff"
    ],
    enemies: [
      "Terraria/MourningWood",
      "Terraria/Pumpking"
    ]
  },
  {
    eventCategory: "FrostMoon",
    findStage: manifest => findEventStage(manifest, "FrostMoon"),
    items: [
      "Terraria/ChainGun",
      "Terraria/BlizzardStaff",
      "Terraria/ElfMelter",
      "Terraria/Razorpine",
      "Terraria/SnowmanCannon"
    ],
    enemies: [
      "Terraria/PresentMimic",
      "Terraria/Yeti",
      "Terraria/ElfCopter",
      "Terraria/Nutcracker",
      "Terraria/NutcrackerSpinning",
      "Terraria/Krampus",
      "Terraria/Flocko",
      "Terraria/Everscream",
      "Terraria/SantaNK1",
      "Terraria/IceQueen"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedGolemBoss", "golem"),
    items: [
      "Terraria/DD2BallistraTowerT3Popper",
      "Terraria/DD2ExplosiveTrapT3Popper",
      "Terraria/DD2FlameburstTowerT3Popper",
      "Terraria/DD2LightningAuraT3Popper",
      "Terraria/FireworksLauncher"
    ],
    enemies: [
      "Terraria/BrainScrambler",
      "Terraria/GigaZapper",
      "Terraria/GrayGrunt",
      "Terraria/MartianSaucer",
      "Terraria/MartianOfficer",
      "Terraria/PirateShipCannon",
      "Terraria/RayGunner",
      "Terraria/ScutlixRider"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedAncientCultist", "lunatic-cultist"),
    stations: ["Terraria/LunarCraftingStation"],
    items: [
      "Terraria/FragmentSolar",
      "Terraria/FragmentVortex",
      "Terraria/FragmentNebula",
      "Terraria/FragmentStardust"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedMoonlord", "moon-lord"),
    items: ["Terraria/LunarOre", "Terraria/LunarBar"]
  }
];

const START_SOURCES = [
  "Terraria/GreenSlime",
  "Terraria/BlueSlime",
  "Terraria/PurpleSlime",
  "Terraria/RedSlime",
  "Terraria/YellowSlime",
  "Terraria/Zombie",
  "Terraria/ZombieEskimo",
  "Terraria/DemonEye",
  "Terraria/BlueJellyfish",
  "Terraria/CaveBat",
  "Terraria/JungleBat",
  "Terraria/SporeBat",
  "Terraria/EaterofSouls",
  "Terraria/DevourerHead",
  "Terraria/DevourerBody",
  "Terraria/DevourerTail",
  "Terraria/Crimera",
  "Terraria/FaceMonster",
  "Terraria/BloodCrawler",
  "Terraria/BloodCrawlerWall",
  "Terraria/GreenJellyfish",
  "Terraria/BoneSerpentHead",
  "Terraria/Harpy",
  "Terraria/Crawdad",
  "Terraria/Crawdad2",
  "Terraria/GiantWormHead",
  "Terraria/GiantWormBody",
  "Terraria/GiantWormTail",
  "Terraria/GiantShelly",
  "Terraria/GiantShelly2",
  "Terraria/GreekSkeleton",
  "Terraria/Demon",
  "Terraria/FireImp",
  "Terraria/Ghost",
  "Terraria/MossHornet",
  "Terraria/Tim",
  "Terraria/Nymph",
  "Terraria/LostGirl",
  "Terraria/DoctorBones",
  "Terraria/CorruptBunny",
  "Terraria/CrimsonBunny",
  "Terraria/CorruptGoldfish",
  "Terraria/CrimsonGoldfish",
  "Terraria/CorruptPenguin",
  "Terraria/CrimsonPenguin",
  "Terraria/PinkJellyfish",
  "Terraria/Shark",
  "Terraria/Squid",
  "Terraria/SeaSnail",
  "Terraria/Skeleton",
  "Terraria/SkeletonMerchant",
  "Terraria/TombCrawlerHead",
  "Terraria/VoodooDemon"
];

const START_SHOPS = [
  "Terraria/ArmsDealer",
  "Terraria/Merchant",
  "Terraria/Demolitionist",
  "Terraria/DyeTrader",
  "Terraria/PartyGirl",
  "Terraria/Painter",
  "Terraria/Stylist",
  "Terraria/Golfer",
  "Terraria/BestiaryGirl",
  "Terraria/SkeletonMerchant",
  "Terraria/TravellingMerchant",
  "Terraria/TaxCollector"
];

const START_CONTAINERS = [
  "Terraria/SurfaceWoodenChest",
  "Terraria/UndergroundWoodenChest",
  "Terraria/UndergroundGoldChest",
  "Terraria/IceChest",
  "Terraria/IvyChest",
  "Terraria/WaterChest",
  "Terraria/SkywareChest",
  "Terraria/SandstoneChest",
  "Terraria/LivingWoodChest",
  "Terraria/WebCoveredChest",
  "Terraria/PyramidChest",
  "Terraria/EnchantedSwordShrine",
  "Terraria/Present",
  "Terraria/GoodieBag"
];

const WORLDGEN_CONTAINER_ITEMS = new Set([
  "Terraria/Spear",
  "Terraria/Blowpipe",
  "Terraria/WoodenBoomerang",
  "Terraria/WandofSparking",
  "Terraria/Aglet",
  "Terraria/ClimbingClaws",
  "Terraria/Radar",
  "Terraria/GuideToPlantFiberCordage",
  "Terraria/BandofRegeneration",
  "Terraria/MagicMirror",
  "Terraria/CloudinaBottle",
  "Terraria/HermesBoots",
  "Terraria/ShoeSpikes",
  "Terraria/FlareGun",
  "Terraria/Mace",
  "Terraria/LavaCharm",
  "Terraria/IceBlade",
  "Terraria/IceBoomerang",
  "Terraria/IceSkates",
  "Terraria/FlurryBoots",
  "Terraria/BlizzardinaBottle",
  "Terraria/SnowballCannon",
  "Terraria/IceMirror",
  "Terraria/Boomstick",
  "Terraria/FeralClaws",
  "Terraria/AnkletoftheWind",
  "Terraria/StaffofRegrowth",
  "Terraria/FiberglassFishingPole",
  "Terraria/FlowerBoots",
  "Terraria/Trident",
  "Terraria/Flipper",
  "Terraria/BreathingReed",
  "Terraria/WaterWalkingBoots",
  "Terraria/Starfury",
  "Terraria/ShinyRedBalloon",
  "Terraria/LuckyHorseshoe",
  "Terraria/CelestialMagnet",
  "Terraria/CreativeWings",
  "Terraria/ThunderSpear",
  "Terraria/ThunderStaff",
  "Terraria/MagicConch",
  "Terraria/MysticCoilSnake",
  "Terraria/AncientChisel",
  "Terraria/DuneriderBoots",
  "Terraria/CatBast",
  "Terraria/FinchStaff",
  "Terraria/LivingWoodWand",
  "Terraria/LeafWand",
  "Terraria/LivingLoom",
  "Terraria/WebSlinger",
  "Terraria/FlyingCarpet",
  "Terraria/SandstorminaBottle",
  "Terraria/EnchantedSword",
  "Terraria/Terragrim",
  "Terraria/Muramasa",
  "Terraria/CobaltShield",
  "Terraria/AquaScepter",
  "Terraria/BlueMoon",
  "Terraria/MagicMissile",
  "Terraria/Valor",
  "Terraria/Handgun",
  "Terraria/DarkLance",
  "Terraria/Sunfury",
  "Terraria/FlowerofFire",
  "Terraria/Flamelash",
  "Terraria/HellwingBow",
  "Terraria/PiranhaGun",
  "Terraria/ScourgeoftheCorruptor",
  "Terraria/VampireKnives",
  "Terraria/RainbowGun",
  "Terraria/StaffoftheFrostHydra",
  "Terraria/StormTigerStaff"
]);

function excludeWorldgenContainerItems(items) {
  return (items ?? []).filter(item => !WORLDGEN_CONTAINER_ITEMS.has(item));
}

const START_VISIBLE_ITEMS = [
  "Terraria/Gel",
  "Terraria/WoodenArrow",
  "Terraria/Shroomerang",
  "Terraria/FallenStar",
  "Terraria/Musket",
  "Terraria/TheUndertaker",
  "Terraria/Vilethorn",
  "Terraria/CrimsonRod",
  "Terraria/Boomstick",
  "Terraria/SnowballCannon",
  "Terraria/Gladius",
  "Terraria/PanicNecklace",
  "Terraria/NaturesGift",
  "Terraria/BouncyGrenade",
  "Terraria/FrogGear",
  "Terraria/FrogFlipper",
  "Terraria/HorseshoeBundle",
  "Terraria/PainterPaintballGun",
  "Terraria/AbigailsFlower",
  "Terraria/Seed"
];

export function applyVanillaSourceCatalog(sourceManifest, snapshot = null) {
  const manifest = structuredClone(sourceManifest);
  const conditionBackedGlobalDrops = new Set((snapshot?.drops ?? [])
    .filter(drop => drop.sourceType === "global" && (drop.conditions ?? []).length > 0)
    .map(drop => drop.item)
    .filter(Boolean));
  manifest.itemOverrides ??= {};
  manifest.itemOverrides["Terraria/TitaniumHelmet"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumHelmet"] ?? {}),
    category: "Armor",
    classes: ["ranged"]
  };
  manifest.itemOverrides["Terraria/TitaniumMask"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumMask"] ?? {}),
    category: "Armor",
    classes: ["melee"]
  };
  manifest.itemOverrides["Terraria/TitaniumHeadgear"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumHeadgear"] ?? {}),
    category: "Armor",
    classes: ["magic"]
  };
  manifest.itemOverrides ??= {};
  manifest.itemOverrides["Terraria/TitaniumHelmet"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumHelmet"] ?? {}),
    category: "Armor",
    classes: ["ranged"]
  };
  manifest.itemOverrides["Terraria/TitaniumMask"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumMask"] ?? {}),
    category: "Armor",
    classes: ["melee"]
  };
  manifest.itemOverrides["Terraria/TitaniumHeadgear"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumHeadgear"] ?? {}),
    category: "Armor",
    classes: ["magic"]
  };
  manifest.itemOverrides ??= {};
  manifest.itemOverrides["Terraria/TitaniumHelmet"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumHelmet"] ?? {}),
    category: "Armor",
    classes: ["ranged"]
  };
  manifest.itemOverrides["Terraria/TitaniumMask"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumMask"] ?? {}),
    category: "Armor",
    classes: ["melee"]
  };
  manifest.itemOverrides["Terraria/TitaniumHeadgear"] = {
    ...(manifest.itemOverrides["Terraria/TitaniumHeadgear"] ?? {}),
    category: "Armor",
    classes: ["magic"]
  };
  manifest.initialItems = unique([
    ...(manifest.initialItems ?? []),
    ...excludeWorldgenContainerItems(START_ITEMS)
  ]);
  manifest.initialVisibleItems = unique([
    ...(manifest.initialVisibleItems ?? []),
    ...excludeWorldgenContainerItems(START_VISIBLE_ITEMS)
  ]);
  manifest.initialStations = unique([
    ...(manifest.initialStations ?? []),
    ...START_STATIONS
  ]);

  const stages = new Map(manifest.stages.map(stage => [stage.id, stage]));
  const start = stages.get("start") ?? manifest.stages[0];
  if (start) {
    start.enemies = unique([
      ...(start.enemies ?? []),
      ...filterLegacyNpcSources(START_SOURCES, start.id, "spawn", manifest, snapshot)
    ]);
    start.shops = unique([
      ...(start.shops ?? []),
      ...filterLegacyNpcSources(START_SHOPS, start.id, "town", manifest, snapshot)
    ]);
    start.containers = unique([
      ...(start.containers ?? []),
      ...START_CONTAINERS
    ]);
  }
  for (const fact of MILESTONE_FACTS) {
    const stageId = fact.findStage(manifest);
    const stage = stages.get(stageId);
    if (!stage) continue;
    const items = excludeWorldgenContainerItems(fact.items ?? []);
    const legacyOnlyItems = items.filter(item => !conditionBackedGlobalDrops.has(item));
    manifest.itemStageFloors ??= {};
    manifest.stationStageFloors ??= {};
    for (const item of legacyOnlyItems) {
      manifest.itemStageFloors[item] ??= stageId;
    }
    for (const station of fact.stations ?? []) {
      manifest.stationStageFloors[station] ??= stageId;
    }
    stage.stations = unique([...(stage.stations ?? []), ...(fact.stations ?? [])]);
    stage.include = unique([...(stage.include ?? []), ...items]);
    stage.enemies = unique([
      ...(stage.enemies ?? []),
      ...filterLegacyNpcSources(fact.enemies ?? [], stageId, "spawn", manifest, snapshot)
    ]);
    if (fact.eventCategory) {
      const event = (manifest.events ?? []).find(value =>
        value.eventCategory === fact.eventCategory);
      if (event) {
        event.enemies = unique([
          ...(event.enemies ?? []),
          ...filterLegacyNpcSources(fact.enemies ?? [], stageId, "spawn", manifest, snapshot)
        ]);
      }
    }
    stage.containers = unique([
      ...(stage.containers ?? []),
      ...(fact.containers ?? [])
    ]);
    stage.shops = unique([
      ...(stage.shops ?? []),
      ...filterLegacyNpcSources(fact.shops ?? [], stageId, "town", manifest, snapshot)
    ]);
  }

  const hardmodeStageId = findVanillaFlagStage(manifest, "hardMode", "wall-of-flesh");
  if (hardmodeStageId) {
    manifest.itemStageFloors["Terraria/Megaphone"] ??= hardmodeStageId;
    // Fishing-spawn probes cannot observe Dreadnautilus, so keep this source-level fact shared.
    manifest.sourceStageFloors ??= {};
    const existingBloodNautilusStageId =
      manifest.sourceStageFloors["Terraria/BloodNautilus"];
    const existingBloodNautilusStageIndex = manifest.stages.findIndex(stage =>
      stage.id === existingBloodNautilusStageId);
    const hardmodeStageIndex = manifest.stages.findIndex(stage =>
      stage.id === hardmodeStageId);
    if (existingBloodNautilusStageIndex < hardmodeStageIndex) {
      manifest.sourceStageFloors["Terraria/BloodNautilus"] = hardmodeStageId;
    }
    const hardmodeStage = stages.get(hardmodeStageId);
    hardmodeStage.enemies = unique([
      ...(hardmodeStage.enemies ?? []),
      "Terraria/BloodNautilus"
    ]);
  }

  const fireGauntletHasRecipe = (snapshot?.recipes ?? []).some(recipe =>
    recipe.result === "Terraria/FireGauntlet");
  if (!fireGauntletHasRecipe) {
    const allMechsStageId = findVanillaFlagStage(
      manifest,
      "downedMechBoss3",
      "skeletron-prime");
    const allMechsStage = stages.get(allMechsStageId);
    if (allMechsStage) {
      manifest.itemStageFloors["Terraria/FireGauntlet"] ??= allMechsStageId;
      allMechsStage.include = unique([
        ...(allMechsStage.include ?? []),
        "Terraria/FireGauntlet"
      ]);
    }
  }

  const empressStage = stages.get("empress-of-light");
  if (empressStage) {
    empressStage.enemies = unique([
      ...(empressStage.enemies ?? []),
      "Terraria/HallowBoss"
    ]);
  }

  return manifest;
}

function filterLegacyNpcSources(sources, expectedStageId, kind, manifest, snapshot) {
  if (!snapshot) return sources;
  const expectedStageIndex = manifest.stages.findIndex(stage => stage.id === expectedStageId);
  const availability = new Map(
    (snapshot.npcAvailability ?? []).map(record => [record.npc, record]));
  return sources.filter(source => {
    const observed = availability.get(source);
    if (!observed?.observed
        || observed.kind !== kind
        || resolveSnapshotStageIndex(
          observed,
          manifest.stages,
          `vanilla source ${source}`) !== expectedStageIndex) {
      return true;
    }
    return kind === "town"
      && (snapshot.shops ?? []).some(shop => shop.npc === source && !shop.observed);
  });
}

function findEventStage(manifest, eventCategory) {
  return (manifest.events ?? []).find(event =>
    event.eventCategory === eventCategory)?.stageId ?? null;
}

function findStageById(manifest, id) {
  return manifest.stages.find(stage => stage.id === id)?.id ?? null;
}

function findVanillaFlagStage(manifest, key, fallbackId) {
  return manifest.stages.find(stage =>
    stage.unlock?.type === "vanilla-flag" && stage.unlock.key === key)?.id
    ?? manifest.stages.find(stage => stage.id === fallbackId)?.id
    ?? null;
}

function unique(values) {
  return [...new Set(values)];
}
