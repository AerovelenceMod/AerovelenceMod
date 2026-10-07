using AerovelenceMod.Common.Systems.Traversal;



using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;


using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.Traversal
{
    public abstract class RopePost : ModTile
    {
        public abstract int Height { get; }

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileID.Sets.IsBeam[Type] = true;
            DustType = DustID.WoodFurniture;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1xX);
            TileObjectData.newTile.Height = Height;
            TileObjectData.newTile.CoordinateHeights = new int[Height];
            for (int i = 0; i < Height; i++) TileObjectData.newTile.CoordinateHeights[i] = 16;
            TileObjectData.newTile.Origin = new Point16(0, Height - 1);
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop, 1, 0);
        }

        public Point Bottom(int i, int j) => new(i, j - Main.tile[i, j].TileFrameY / 18 % Height + Height - 1);

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
        public override void KillMultiTile(int i, int j, int frameX, int frameY)
            => RopeSpanSystem.BreakAt(new Point(i, j + Height - 1));

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
            => DrawPost(i, j, spriteBatch, Main.tile[i, j].TileFrameY / 18 % Height, false);

        public override bool PreDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, ref Rectangle frame, ref Vector2 position, ref Color color, bool validPlacement, ref SpriteEffects spriteEffects)
            => DrawPost(i, j, spriteBatch, frame.Y / 18 % Height, false, true, validPlacement);

        protected bool DrawPost(int i, int j, SpriteBatch spriteBatch, int row, bool ceiling, bool preview = false, bool validPlacement = true, int column = 0, bool flipHorizontal = false)
        {
            Tile tile = Main.tile[i, j];
            if (tile.IsTileInvisible && !Main.ShouldShowInvisibleWalls()) return false;
            Vector2 offset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Rectangle frame = new(column * 18, row == 0 ? 0 : row == Height - 1 ? 36 : 18, 16, 16);
            Color color = new(255, validPlacement ? 255 : 127, validPlacement ? 255 : 127, preview ? 127 : 255);
            spriteBatch.Draw(preview ? ModContent.Request<Texture2D>(Texture).Value : Main.instance.TilesRenderer.GetTileDrawTexture(tile, i, j),
                new Vector2(i * 16, j * 16 + (ceiling ? -2 : 2)) - Main.screenPosition + offset, frame,
                tile.IsTileFullbright ? color : Lighting.GetColor(i, j).MultiplyRGBA(color), 0, Vector2.Zero, 1, (ceiling ? SpriteEffects.FlipVertically : SpriteEffects.None) | (flipHorizontal ? SpriteEffects.FlipHorizontally : SpriteEffects.None), 0);
            return false;
        }
    }
}
