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
  "Terraria/SkyMill"
];

const START_ITEMS = [
  "Terraria/Wood",
  "Terraria/BorealWood",
  "Terraria/PalmWood",
  "Terraria/RichMahogany",
  "Terraria/Ebonwood",
  "Terraria/Shadewood",
  "Terraria/AshWood",
  "Terraria/Pearlwood",
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
  "Terraria/Chain",
  "Terraria/Worm",
  "Terraria/ArmoredCavefish",
  "Terraria/ChaosFish",
  "Terraria/CrimsonTigerfish",
  "Terraria/Damselfish",
  "Terraria/DoubleCod",
  "Terraria/Ebonkoi",
  "Terraria/FlarefinKoi",
  "Terraria/FrostMinnow",
  "Terraria/Hemopiranha",
  "Terraria/Obsidifish",
  "Terraria/PrincessFish",
  "Terraria/Prismite",
  "Terraria/SpecularFish",
  "Terraria/Stinkfish",
  "Terraria/VariegatedLardfish",
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
  "Terraria/IllegalGunParts",
  "Terraria/MusketBall",
  "Terraria/WoodenArrow",
  "Terraria/FishingBobber",
  "Terraria/Frog",
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
  "Terraria/Pumpkin",
  "Terraria/PumpkinSeed",
  "Terraria/Hay",
  "Terraria/LivingWoodWand",
  "Terraria/LeafWand",
  "Terraria/NaturesGift",
  "Terraria/PanicNecklace",
  "Terraria/SweetheartNecklace",
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
  "Terraria/DarkLance",
  "Terraria/HellwingBow",
  "Terraria/ArcticDivingGear",
  "Terraria/TerrasparkBoots",
  "Terraria/SunplateBlock",
  "Terraria/ShimmerBlock",
  "Terraria/Grapes",
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
  "Terraria/ViciousPowder"
];

const MILESTONE_FACTS = [
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
    items: ["Terraria/BeeWax"],
    stations: ["Terraria/ImbuingStation"],
    shops: ["Terraria/WitchDoctor"]
  },
  {
    findStage: manifest => findEventStage(manifest, "GoblinArmy"),
    items: ["Terraria/TinkerersWorkshop"],
    stations: ["Terraria/TinkerersWorkbench"],
    shops: ["Terraria/GoblinTinkerer"],
    enemies: [
      "Terraria/ZombieMerman",
      "Terraria/EyeballFlyingFish"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedBoss3", "skeletron"),
    stations: ["Terraria/AlchemyTable"],
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
      "Terraria/BewitchingTable",
      "Terraria/QuadBarrelShotgun",
      "Terraria/CowboyHat",
      "Terraria/WaterCandle"
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
      "Terraria/SoulofLight",
      "Terraria/SoulofNight",
      "Terraria/SoulofFlight",
      "Terraria/PixieDust",
      "Terraria/UnicornHorn",
      "Terraria/SpiderFang",
      "Terraria/FrostCore",
      "Terraria/ForbiddenFragment",
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
      "Terraria/Katana",
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
      "Terraria/SporeSkeleton"
    ],
    shops: [
      "Terraria/Wizard",
      "Terraria/Truffle"
    ],
    containers: [
      "Terraria/WoodenCrateHard",
      "Terraria/GoldenCrateHard",
      "Terraria/OasisCrateHard"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedMechBoss1", "destroyer"),
    items: [
      "Terraria/SoulofMight",
      "Terraria/Megashark",
      "Terraria/DeathSickle",
      "Terraria/LivingFireBlock",
      "Terraria/FireGauntlet",
      "Terraria/SuperStarCannon"
    ],
    shops: ["Terraria/Steampunker"],
    enemies: ["Terraria/RedDevil"]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedMechBoss2", "twins"),
    items: ["Terraria/SoulofSight"]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedMechBoss3", "skeletron-prime"),
    items: [
      "Terraria/SoulofFright",
      "Terraria/HallowedBar",
      "Terraria/Cog",
      "Terraria/ChlorophyteOre",
      "Terraria/ChlorophyteBar"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedPlantBoss", "plantera"),
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
      "Terraria/VampireKnives",
      "Terraria/StaffoftheFrostHydra",
      "Terraria/RainbowGun",
      "Terraria/PiranhaGun",
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
      "Terraria/PulseBow",
      "Terraria/RocketII"
    ],
    enemies: [
      "Terraria/Vampire",
      "Terraria/Reaper",
      "Terraria/Psycho",
      "Terraria/GiantCursedSkull"
    ],
    shops: ["Terraria/Cyborg", "Terraria/Princess"]
  },
  {
    findStage: manifest => findEventStage(manifest, "PumpkinMoon"),
    items: [
      "Terraria/TheHorsemansBlade",
      "Terraria/RavenStaff"
    ]
  },
  {
    findStage: manifest => findEventStage(manifest, "FrostMoon"),
    items: [
      "Terraria/ChainGun",
      "Terraria/BlizzardStaff",
      "Terraria/ElfMelter",
      "Terraria/Razorpine",
      "Terraria/SnowmanCannon"
    ],
    enemies: [
      "Terraria/SnowmanGangsta",
      "Terraria/MisterStabby",
      "Terraria/SnowBalla",
      "Terraria/PresentMimic",
      "Terraria/Yeti",
      "Terraria/ElfCopter",
      "Terraria/Nutcracker",
      "Terraria/NutcrackerSpinning",
      "Terraria/Krampus",
      "Terraria/Flocko"
    ]
  },
  {
    findStage: manifest => findVanillaFlagStage(manifest, "downedGolemBoss", "golem"),
    enemies: [
      "Terraria/FlyingSnake",
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
  "Terraria/BlueJellyfish",
  "Terraria/GreenJellyfish",
  "Terraria/BoneSerpentHead",
  "Terraria/Harpy",
  "Terraria/Crawdad",
  "Terraria/Crawdad2",
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
  "Terraria/Skeleton",
  "Terraria/SkeletonMerchant",
  "Terraria/TombCrawlerHead",
  "Terraria/VoodooDemon"
];

const START_CONTAINERS = [
  "Terraria/WoodenCrate",
  "Terraria/GoldenCrate",
  "Terraria/OasisCrate"
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
  "Terraria/SkeletonMerchant"
];

export function applyVanillaSourceCatalog(sourceManifest) {
  const manifest = structuredClone(sourceManifest);
  manifest.initialItems = unique([
    ...(manifest.initialItems ?? []),
    ...START_ITEMS
  ]);
  manifest.initialStations = unique([
    ...(manifest.initialStations ?? []),
    ...START_STATIONS
  ]);

  const stages = new Map(manifest.stages.map(stage => [stage.id, stage]));
  const start = stages.get("start") ?? manifest.stages[0];
  if (start) {
    start.enemies = unique([...(start.enemies ?? []), ...START_SOURCES]);
    start.containers = unique([...(start.containers ?? []), ...START_CONTAINERS]);
    start.shops = unique([...(start.shops ?? []), ...START_SHOPS]);
  }
  for (const fact of MILESTONE_FACTS) {
    const stageId = fact.findStage(manifest);
    const stage = stages.get(stageId);
    if (!stage) continue;
    manifest.itemStageFloors ??= {};
    manifest.stationStageFloors ??= {};
    for (const item of fact.items ?? []) {
      manifest.itemStageFloors[item] ??= stageId;
    }
    for (const station of fact.stations ?? []) {
      manifest.stationStageFloors[station] ??= stageId;
    }
    stage.stations = unique([...(stage.stations ?? []), ...(fact.stations ?? [])]);
    stage.include = unique([...(stage.include ?? []), ...(fact.items ?? [])]);
    stage.enemies = unique([...(stage.enemies ?? []), ...(fact.enemies ?? [])]);
    stage.shops = unique([...(stage.shops ?? []), ...(fact.shops ?? [])]);
    stage.containers = unique([...(stage.containers ?? []), ...(fact.containers ?? [])]);
  }
  addVanillaShopConditionStages(manifest);
  return manifest;
}

function addVanillaShopConditionStages(manifest) {
  const mappings = [
    {
      stageId: findVanillaFlagStage(manifest, "hardMode", "wall-of-flesh"),
      descriptions: ["In Hardmode", "В хардмоде"]
    },
    {
      stageId: findEarliestVanillaFlagStage(
        manifest,
        ["downedMechBoss1", "downedMechBoss2", "downedMechBoss3"],
        ["destroyer", "twins", "skeletron-prime"]),
      descriptions: [
        "After defeating any mechanical boss",
        "После победы над любым механическим боссом"
      ]
    },
    {
      stageId: findVanillaFlagStage(manifest, "downedGolemBoss", "golem"),
      descriptions: ["After defeating Golem", "После победы над Големом"]
    }
  ];

  manifest.conditionUnlocks = [...(manifest.conditionUnlocks ?? [])];
  for (const mapping of mappings) {
    if (!mapping.stageId) continue;
    manifest.conditionUnlocks.push({
      stageId: mapping.stageId,
      sources: ["shop"],
      sourceIds: ["Terraria/DD2Bartender"],
      conditionTypes: [],
      conditionDescriptions: mapping.descriptions
    });
  }
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

function findEarliestVanillaFlagStage(manifest, keys, fallbackIds) {
  return manifest.stages.find(stage =>
    keys.some(key => unlockContainsVanillaFlag(stage.unlock, key)))?.id
    ?? fallbackIds.map(id => findStageById(manifest, id)).find(Boolean)
    ?? null;
}

function unlockContainsVanillaFlag(unlock, key) {
  if (!unlock) return false;
  if (unlock.type === "vanilla-flag" && unlock.key === key) return true;
  return (unlock.conditions ?? []).some(condition =>
    unlockContainsVanillaFlag(condition, key));
}

function unique(values) {
  return [...new Set(values)];
}
