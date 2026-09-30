using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using AerovelenceMod.Common.Utilities;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Building
{
    public class TumblerStatue : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            CommonTileHelper.SetupMultiTile(this, 2, 3, [16, 16, 16]);
            AddMapEntry(new Color(120, 160, 200));
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            Texture2D glowmask = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
            Vector2 drawPosition = new Vector2(
                i * 16 - (int)Main.screenPosition.X,
                j * 16 - (int)Main.screenPosition.Y
            );
            if (!Main.drawToScreen)
                drawPosition += new Vector2(Main.offScreenRange);
            spriteBatch.Draw(glowmask, drawPosition, new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16), Color.White);
        }
    }

    public class TumblerStatueItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 9999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<TumblerStatue>();
            Item.placeStyle = 0;
        }
    }
}
