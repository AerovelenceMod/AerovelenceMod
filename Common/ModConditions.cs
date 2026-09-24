using AerovelenceMod.Content.Biomes;
using Terraria;

namespace AerovelenceMod.Common
{
	public static class ModConditions
	{
		public static Condition InCrystalCaverns = new Condition("Mods.AerovelenceMod.Conditions.InCrystalCaverns", () => Main.LocalPlayer.InModBiome<CrystalCavernsSurfaceBiome>() || Main.LocalPlayer.InModBiome<CrystalCavernsBiome>());
	}
}
