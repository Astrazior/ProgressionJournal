export const VanillaSupportItemIds = new Set([
  "Terraria/SpikyBall",
  "Terraria/HoundiusShootius",
  "Terraria/DD2LightningAuraT1Popper",
  "Terraria/DD2FlameburstTowerT1Popper",
  "Terraria/DD2BallistraTowerT1Popper",
  "Terraria/DD2ExplosiveTrapT1Popper",
  "Terraria/QueenSpiderStaff",
  "Terraria/DD2LightningAuraT2Popper",
  "Terraria/DD2FlameburstTowerT2Popper",
  "Terraria/DD2BallistraTowerT2Popper",
  "Terraria/DD2ExplosiveTrapT2Popper",
  "Terraria/StaffoftheFrostHydra",
  "Terraria/DD2LightningAuraT3Popper",
  "Terraria/DD2FlameburstTowerT3Popper",
  "Terraria/DD2BallistraTowerT3Popper",
  "Terraria/DD2ExplosiveTrapT3Popper",
  "Terraria/MoonlordTurretStaff",
  "Terraria/RainbowCrystalStaff",
  "Terraria/CrimsonRod",
  "Terraria/ClingerStaff",
  "Terraria/NimbusRod",
  "Terraria/RainbowGun",
  "Terraria/MagnetSphere",
  "Terraria/ShadowFlameHexDoll",
  "Terraria/ScytheWhip"
]);

export function isVanillaSupportItem(itemId) {
  return VanillaSupportItemIds.has(itemId);
}
