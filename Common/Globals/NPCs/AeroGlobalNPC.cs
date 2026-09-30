using AerovelenceMod.Common.Globals.Worlds;
using AerovelenceMod.Content.Biomes;
using AerovelenceMod.Content.Items.Accessories.SmallAccessories;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace AerovelenceMod.Common.Globals.NPCs
{
	public class AeroGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;
		
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.InModBiome(ModContent.GetInstance<CrystalCavernsSurfaceBiome>()) || spawnInfo.Player.InModBiome(ModContent.GetInstance<CrystalCavernsBiome>()))
			{
				pool[0] = 0f;
			}
        }
    }
}