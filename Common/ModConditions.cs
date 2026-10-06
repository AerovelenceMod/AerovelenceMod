using AerovelenceMod.Content.Biomes;


namespace AerovelenceMod.Common
{
    public static class ModConditions
    {
        public static Condition InCrystalCaverns = new Condition("Mods.AerovelenceMod.Conditions.InCrystalCaverns", () => Main.LocalPlayer.InModBiome<CrystalCavernsSurfaceBiome>() || Main.LocalPlayer.InModBiome<CrystalCavernsBiome>());
    }
}
