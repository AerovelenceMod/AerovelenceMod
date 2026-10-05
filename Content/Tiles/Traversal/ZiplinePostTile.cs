using AerovelenceMod.Common.Systems;
using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Systems.Traversal;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.Traversal
{
    public sealed class ZiplinePostTile : RopePost
    {
        public override int Height => 4;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(0, 0);
            TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 0);
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);
            RegisterItemDrop(ModContent.ItemType<ZiplinePost>());
            AddMapEntry(new Color(120, 90, 57), this.Localize("Zipline Post"));
        }

        public override bool RightClick(int i, int j)
        {
            if (!Main.LocalPlayer.releaseUseTile) return false;
            if (RopeSpanSystem.IsRope(Main.LocalPlayer.HeldItem))
                RopeSpanSystem.ClickPost(Bottom(i, j), true);
            else
                Main.LocalPlayer.GetModPlayer<RopeSpanPlayer>().TryRideAt(Main.SmartCursorIsUsed ? Main.MouseWorld : new Vector2(i * 16 + 8, j * 16 + 8));
            return true;
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
            => Main.SmartCursorIsUsed || RopeSpanSystem.IsRope(Main.LocalPlayer.HeldItem);

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            int row = tile.TileFrameY / 18 % Height;
            bool ceiling = tile.TileFrameX == 18;
            if (ceiling) row = Height - 1 - row;
            return DrawPost(i, j, spriteBatch, row, ceiling);
        }

        public override bool PreDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, ref Rectangle frame, ref Vector2 position, ref Color color, bool validPlacement, ref SpriteEffects spriteEffects)
        {
            int row = frame.Y / 18 % Height;
            bool ceiling = frame.X == 18;
            if (ceiling) row = Height - 1 - row;
            return DrawPost(i, j, spriteBatch, row, ceiling, true, validPlacement);
        }
    }

    public sealed class ZiplinePost : TranslatableModItem
    {
        public override string Texture => "AerovelenceMod/Content/Tiles/Traversal/ZiplinePostItem";

        public override void SetStaticDefaults()
        {
            this.AddName(Language.Default, "Zipline Post")
                .AddTooltip(Language.Default, "Right-click two posts with rope to connect"
                    + "\nConsumes 2 rope per section");
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<ZiplinePostTile>());
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
