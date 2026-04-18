using System.Collections.Generic;
using Terraria.ID;

namespace ProgressionJournal.Data.Repositories;

public static partial class JournalRepository
{
    private static void AddClassSpecificEntries(List<JournalEntry> entries)
    {
        entries.AddRange(
        [
            Entry("flareAmmoPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
                            Group(ItemID.Flare, ItemID.BlueFlare, ItemID.SpelunkerFlare, ItemID.ShimmerFlare),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("seedAmmoPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.Seed,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Useless)),

            Entry("musketBallPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.MusketBall,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("silverOrTungstenBulletPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
                            Group(ItemID.SilverBullet, ItemID.TungstenBullet),
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("poisonDartPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.PoisonDart,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("woodenArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.WoodenArrow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.NotRecommended)),

            Entry("flamingArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.FlamingArrow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("unholyArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.UnholyArrow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("jestersArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.JestersArrow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("hellfireArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.HellfireArrow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("boneArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.BoneArrow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("frostburnArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.FrostburnArrow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("shimmerArrowPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.ShimmerArrow,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("meteorShotPostWorldEvil", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.MeteorShot,
                            Eval(ProgressionStageId.PostWorldEvil, RecommendationTier.Recommended)),

            Entry("cursedArrowHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.CursedArrow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("holyArrowHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.HolyArrow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("ichorArrowHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.IchorArrow,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("endlessQuiverHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.EndlessQuiver,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("crystalBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.CrystalBullet,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("endlessMusketPouchHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.EndlessMusketPouch,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("cursedBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.CursedBullet,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("ichorBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.IchorBullet,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("partyBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.PartyBullet,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Useless)),

            Entry("explodingBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.ExplodingBullet,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Additional)),

            Entry("goldenBulletHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.GoldenBullet,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.NotRecommended)),

            Entry("highVelocityBulletPostOneMechBoss", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.HighVelocityBullet,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Additional)),

            Entry("chlorophyteArrowPostThreeMechBosses", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.ChlorophyteArrow,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Additional)),

            Entry("chlorophyteBulletPostThreeMechBosses", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.ChlorophyteBullet,
                            Eval(ProgressionStageId.PostThreeMechBosses, RecommendationTier.Recommended)),

            Entry("venomArrowPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.VenomArrow,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("venomBulletPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.VenomBullet,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("nanoBulletPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.NanoBullet,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("basicRocketSetPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
                            Group(ItemID.RocketI, ItemID.RocketII, ItemID.RocketIII, ItemID.RocketIV, ItemID.DryRocket, ItemID.WetRocket, ItemID.LavaRocket, ItemID.HoneyRocket),
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("miniNukeSetPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
                            Group(ItemID.MiniNukeI, ItemID.MiniNukeII),
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.NotRecommended)),

            Entry("clusterRocketSetPostGolem", JournalItemCategory.ClassSpecific, CombatClass.Ranged,
                            Group(ItemID.ClusterRocketI, ItemID.ClusterRocketII),
                            Eval(ProgressionStageId.PostGolem, RecommendationTier.NotRecommended)),

            Entry("moonlordBulletPostMoonLord", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.MoonlordBullet,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("moonlordArrowPostMoonLord", JournalItemCategory.ClassSpecific, CombatClass.Ranged, ItemID.MoonlordArrow,
                            Eval(ProgressionStageId.PostMoonLord, RecommendationTier.Recommended)),

            Entry("leatherWhipPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.BlandWhip,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Additional)),

            Entry("snapthornPreBoss", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.ThornWhip,
                            Eval(ProgressionStageId.PreBoss, RecommendationTier.Recommended)),

            Entry("spinalTapPostSkeletron", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.BoneWhip,
                            Eval(ProgressionStageId.PostSkeletron, RecommendationTier.Recommended)),

            Entry("firecrackerHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.FireWhip,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("coolWhipHardmodeEntry", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.CoolWhip,
                            Eval(ProgressionStageId.HardmodeEntry, RecommendationTier.Recommended)),

            Entry("durendalPostOneMechBoss", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.SwordWhip,
                            Eval(ProgressionStageId.PostOneMechBoss, RecommendationTier.Recommended)),

            SupportEntry("darkHarvestPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.ScytheWhip,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Additional)),

            Entry("morningStarPostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.MaceWhip,
                            Eval(ProgressionStageId.PostPlantera, RecommendationTier.Recommended)),

            Entry("kaleidoscopePostPlantera", JournalItemCategory.ClassSpecific, CombatClass.Summoner, ItemID.RainbowWhip,
                            Eval(ProgressionStageId.PostEmpressOfLight, RecommendationTier.Recommended)),
        ]);
    }
}

