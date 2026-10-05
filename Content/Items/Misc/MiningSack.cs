using AerovelenceMod.Common;
using Terraria.GameContent.ItemDropRules;

namespace AerovelenceMod.Content.Items.Misc
{
	public class MiningSack : TranslatableModItem
    {
		public override void SetStaticDefaults()
		{
			this.ModifyLocalization("Mining Sack", "Right click to open");
			Item.ResearchUnlockCount = 5;
		}

		public override void SetDefaults()
		{
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.width = 36;
			Item.height = 32;
			Item.rare = ItemRarities.MidPHM;
		}

		public override bool CanRightClick() => true;

        public override void ModifyItemLoot(ItemLoot itemLoot)
        {
			itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 1, 3, 3));
			itemLoot.Add(new DropRules.LootPoolDrop(1, 1, 1, 
			[
				new(ItemID.IronOre, 20..30),
				new(ItemID.LeadOre, 20..30),
				new(ItemID.SilverOre, 20..30),
				new(ItemID.TungstenOre, 20..30),
				new(ItemID.GoldOre, 20..30),
				new(ItemID.PlatinumOre, 20..30),
			]
			));
			itemLoot.Add(new DropRules.LootPoolDrop(1, 1, 1, 
			[
				new(ItemID.Topaz, 2..8),
				new(ItemID.Sapphire, 2..8),
				new(ItemID.Ruby, 2..8),
				new(ItemID.Emerald, 2..8),
				new(ItemID.Diamond, 2..8),
			]
			));
		}
	}
}