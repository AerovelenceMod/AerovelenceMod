using ReLogic.Content;
using System;
using Terraria.GameContent;
using Terraria.GameContent.RGB;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural
{
    [LegacyName("CrystalDirt")]
    public class CrystalDirtTile : ModTile
    {
        private Asset<Texture2D> glowTexture;

        public override void SetStaticDefaults()
        {
            MineResist = 2.5f;
            Main.tileSolid[Type] = true;
            Main.tileMerge[Type][ModContent.TileType<CrystalGrassTile>()] = true;
            Main.tileMerge[Type][ModContent.TileType<CavernCrystalTile>()] = true;
            Main.tileMerge[Type][ModContent.TileType<CavernStoneTile>()] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlendAll[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            AddMapEntry(new Color(80, 80, 110));
            DustType = 116;
            HitSound = SoundID.Dig;
            TileID.Sets.GeneralPlacementTiles[Type] = false;

            glowTexture = (Main.dedServ ? null : ModContent.Request<Texture2D>(Texture + "_Glowmask"));
        }
        public override bool CanExplode(int i, int j)
        {
            return true;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];

            Vector2 position = new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y);

            // Pulsating color for glowmask
            Color maskColor = Color.White
                * MathHelper.Lerp(0.0f, 2f, ((float)Math.Pow(Math.Sin(NoiseHelper.GetDynamicNoise(new Vector2(i * 0.05f, j * 0.05f), Main.GlobalTimeWrappedHourly * 0.1f)), 6)));

            DrawUtils.DrawSlopedTile(glowTexture.Value, position, tile, maskColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
    }

    public class CrystalDirtItem : ModItem
    {
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 0, ModContent.TileType<CrystalDirtTile>());
        }
    }
}
