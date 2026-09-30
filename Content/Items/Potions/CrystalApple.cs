using AerovelenceMod.Common.Systems.Language;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Potions
{
	public class CrystalApple : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Crystal Apple", "Minor improvements to all stats");
			Item.ResearchUnlockCount = 5;
			Main.RegisterItemAnimation(Type, new DrawAnimationVertical(-1, 3) {
				NotActuallyAnimating = true
			});

			ItemID.Sets.FoodParticleColors[Type] = [
				new Color(81, 142, 238)
			];

			ItemID.Sets.IsFood[Type] = true;

            base.SetStaticDefaults();
		}

		public override void SetDefaults()
		{
			Item.DefaultToFood(24, 26, BuffID.WellFed, 5 * 60 * 60);
			Item.value = Item.buyPrice(0, 1);
			Item.rare = ItemRarityID.Blue;
		}
	}
}
