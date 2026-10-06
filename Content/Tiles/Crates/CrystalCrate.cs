
using AerovelenceMod.Content.Items.Accessories.SmallAccessories;
using AerovelenceMod.Content.Items.Weapons.CrystalCaverns;
using AerovelenceMod.Content.Items.Weapons.CrystalCaverns.CrystalCrescent;


using Terraria.GameContent.ItemDropRules;

using Terraria.Localization;

using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.Crates
{
	public class CrystalCrateTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileSolidTop[Type] = true;
			Main.tileTable[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			TileObjectData.newTile.CoordinateHeights = [16, 18];
			TileObjectData.addTile(Type);

			LocalizedText name = CreateMapEntryName();
			AddMapEntry(new Color(200, 200, 200), name);
		}

		public override bool CreateDust(int i, int j, ref int type)
		{
			return false;
		}
	}

	public class CrystalCrateItem : TranslatableModItem
    {
		public override void SetStaticDefaults()
		{
			this.ModifyLocalization("Crystal Crate", "Right click to open");
			ItemID.Sets.IsFishingCrate[Type] = true;
			Item.ResearchUnlockCount = 5;
		}

		public override void SetDefaults()
		{
			Item.DefaultToPlaceableTile(ModContent.TileType<CrystalCrateTile>());
			Item.width = 12;
			Item.height = 12;
			Item.rare = ItemRarities.MidPHM;
			Item.value = Item.sellPrice(0, 2);
		}

		public override void ModifyResearchSorting(ref ContentSamples.CreativeHelper.ItemGroup itemGroup)
		{
			itemGroup = ContentSamples.CreativeHelper.ItemGroup.Crates;
		}

		public override bool CanRightClick()
		{
			return true;
		}

		public override void ModifyItemLoot(ItemLoot itemLoot)
		{
			int[] themedDrops = [
				ModContent.ItemType<CrystalCrescent>(),
            	ModContent.ItemType<BandOfCrystallization>(),
            	ModContent.ItemType<SpikesInABottle>(),
            	ModContent.ItemType<SilkenScarf>(),
            	ModContent.ItemType<Items.Weapons.CrystalCaverns.TheSling.TheSling>(),
            	ModContent.ItemType<RockRumbler>(),
            	ModContent.ItemType<CavernousRampart>(),
            	ModContent.ItemType<Items.Weapons.CrystalCaverns.TumblerCommander.TumblerCommander>(),
            	ModContent.ItemType<SaplingCane>(),
            	ModContent.ItemType<CrystalStompers>()
			];
			itemLoot.Add(ItemDropRule.OneFromOptionsNotScalingWithLuck(1, themedDrops));

			itemLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 4, 5, 12));

			IItemDropRule[] oreTypes = [
				ItemDropRule.Common(ItemID.CopperOre, 1, 20, 35),
				ItemDropRule.Common(ItemID.TinOre, 1, 20, 35),
				ItemDropRule.Common(ItemID.IronOre, 1, 20, 35),
				ItemDropRule.Common(ItemID.LeadOre, 1, 20, 35),
				ItemDropRule.Common(ItemID.SilverOre, 1, 20, 35),
				ItemDropRule.Common(ItemID.TungstenOre, 1, 20, 35),
				ItemDropRule.Common(ItemID.GoldOre, 1, 20, 35),
				ItemDropRule.Common(ItemID.PlatinumOre, 1, 20, 35),
			];
			itemLoot.Add(new OneFromRulesRule(7, oreTypes));

			IItemDropRule[] oreBars = [
				ItemDropRule.Common(ItemID.IronBar, 1, 6, 16),
				ItemDropRule.Common(ItemID.LeadBar, 1, 6, 16),
				ItemDropRule.Common(ItemID.SilverBar, 1, 6, 16),
				ItemDropRule.Common(ItemID.TungstenBar, 1, 6, 16),
				ItemDropRule.Common(ItemID.GoldBar, 1, 6, 16),
				ItemDropRule.Common(ItemID.PlatinumBar, 1, 6, 16),
			];
			itemLoot.Add(new OneFromRulesRule(4, oreBars));

			IItemDropRule[] explorationPotions = [
				ItemDropRule.Common(ItemID.ObsidianSkinPotion, 1, 2, 4),
				ItemDropRule.Common(ItemID.SpelunkerPotion, 1, 2, 4),
				ItemDropRule.Common(ItemID.HunterPotion, 1, 2, 4),
				ItemDropRule.Common(ItemID.GravitationPotion, 1, 2, 4),
				ItemDropRule.Common(ItemID.MiningPotion, 1, 2, 4),
				ItemDropRule.Common(ItemID.HeartreachPotion, 1, 2, 4),
			];
			itemLoot.Add(new OneFromRulesRule(4, explorationPotions));

			IItemDropRule[] resourcePotions = [
				ItemDropRule.Common(ItemID.HealingPotion, 1, 5, 17),
				ItemDropRule.Common(ItemID.ManaPotion, 1, 5, 17),
			];
			itemLoot.Add(new OneFromRulesRule(2, resourcePotions));

			IItemDropRule[] highendBait = [
				ItemDropRule.Common(ItemID.JourneymanBait, 1, 2, 6),
				ItemDropRule.Common(ItemID.MasterBait, 1, 2, 6),
			];
			itemLoot.Add(new OneFromRulesRule(2, highendBait));
		}
	}
}