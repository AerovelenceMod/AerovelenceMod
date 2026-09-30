using AerovelenceMod.Content.Biomes;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Globals.Players
{
	public class FishingPlayer : ModPlayer
	{
		public override void CatchFish(FishingAttempt attempt, ref int itemDrop, ref int npcSpawn, ref AdvancedPopupRequest sonar, ref Vector2 sonarPosition)
		{
			bool inWater = !attempt.inLava && !attempt.inHoney;
			bool inCrystalBiome = Player.InModBiome<CrystalCavernsSurfaceBiome>() && Player.InModBiome<CrystalCavernsBiome>();
			if (inWater && inCrystalBiome && attempt.crate)
			{
				itemDrop = Main.hardMode ? ModContent.ItemType<Content.Tiles.Crates.ThunderCrateItem>() : ModContent.ItemType<Content.Tiles.Crates.CrystalCrateItem>();
			}
		}
	}
}