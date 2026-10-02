using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Systems.Traversal;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.Traversal
{
    public sealed class RopeBridgePostTile : RopePost
    {
        public override int Height => 3;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
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
    }

    public sealed class RopeBridgePost : TranslatableModItem
    {
        public override string Texture => "Terraria/Images/Item_" + ItemID.WoodenBeam;

        public override void SetStaticDefaults()
        {
            this.AddName(Language.Default, "Rope Bridge Post")
                .AddTooltip(Language.Default, "Right-click two posts with rope to connect"
                    + "\nConsumes 2 rope per section");
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<RopeBridgePostTile>());
            Item.width = 16;
            Item.height = 32;
            Item.value = Item.buyPrice(copper: 20);
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.WoodenBeam, 6)
            .AddRecipeGroup(RecipeGroupID.IronBar, 1)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
