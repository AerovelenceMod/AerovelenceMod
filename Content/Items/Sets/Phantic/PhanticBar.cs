/*
using Terraria.Localization;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Items.Sets.Phantic
{
    public class PhanticBar : ModItem
    {
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 32, 24, 10000, ModContent.TileType<PhanticBarTile>(), ItemRarities.MidPHM);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SpectreBar, 3)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public class PhanticBarTile : ModTile
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
            AddMapEntry(new Color(110, 074, 056),Terraria.Localization.Language.GetText("Phantic Bar"));
        }
    }
}*/
