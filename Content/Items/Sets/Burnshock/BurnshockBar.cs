using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Items.Sets.Burnshock
{
    public class BurnshockBar : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Burnshock Bar", "'Formed from the raging crystal thunderstorms'");
			Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 6));
		}
		public override void SetDefaults()
		{
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTurn = true;
			Item.useAnimation = 15;
			Item.useTime = 10;
			Item.width = 66;
			Item.height = 24;
			Item.autoReuse = true;
			Item.consumable = true;
			Item.noUseGraphic = true;
			Item.placeStyle = 0;
			Item.consumable = true;
			Item.createTile = ModContent.TileType<BurnshockBarTile>();
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(0, 0, 20, 0);
		}
		public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<BurnshockOre>(3)
				.AddIngredient<ChargedStoneItem>()
				.AddTile(TileID.Furnaces)
				.Register();
		}
	}

    public class BurnshockBarTile : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileShine[Type] = 1100;
			Main.tileSolid[Type] = true;
			Main.tileSolidTop[Type] = true;
			Main.tileFrameImportant[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.addTile(Type);
			
			
			AddMapEntry(new Color(110, 074, 056),Terraria.Localization.Language.GetText("Burnshock Bar"));
		}

		public override void AnimateTile(ref int frame, ref int frameCounter)
		{
			frame = frameCounter / 54;
			frameCounter += 6;
			if (frame == 2)
			{
				frame = 0;
				frameCounter = 0;
			}
		}
	}
}