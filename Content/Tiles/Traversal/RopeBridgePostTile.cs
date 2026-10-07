using AerovelenceMod.Common.Systems.Traversal;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.Traversal
{
    public sealed class RopeBridgePostTile : RopePost
    {
        public override int Height => 3;
        public override string Texture => "AerovelenceMod/Content/Tiles/Traversal/RopeBridgePostTile";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.addTile(Type);
            RegisterItemDrop(ModContent.ItemType<RopeBridgePost>());
            AddMapEntry(new Color(120, 90, 57), this.Localize("Rope Bridge Post"));
        }

        public override bool RightClick(int i, int j)
        {
            if (!Main.LocalPlayer.releaseUseTile) return false;
            if (RopeSpanSystem.IsRope(Main.LocalPlayer.HeldItem))
                RopeSpanSystem.ClickPost(Bottom(i, j), false);
            return true;
        }

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
            => DrawPost(i, j, spriteBatch, Main.tile[i, j].TileFrameY / 18 % Height, false, column: Main.tile[i, j].TileFrameX / 18);
    }

    public sealed class RopeBridgePost : TranslatableModItem
    {
        public override string Texture => "AerovelenceMod/Content/Tiles/Traversal/RopeBridgePostItem";

        public override void SetStaticDefaults()
        {
            this.AddName(Language.Default, "Rope Bridge Post")
                .AddTooltip(Language.Default, "Right-click two posts with ropes or chains to connect"
                    + "\nConsumes 2 ropes or chains per section");
        }

        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 32, Item.buyPrice(copper: 20), ModContent.TileType<RopeBridgePostTile>());
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.WoodenBeam, 6)
            .AddRecipeGroup(RecipeGroupID.IronBar)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
