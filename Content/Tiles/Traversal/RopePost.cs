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
    public abstract class RopePost : ModTile
    {
        public abstract int Height { get; }
        public override string Texture => "Terraria/Images/Tiles_" + TileID.WoodenBeam;

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
            => DrawPost(i, j, spriteBatch, Main.tile[i, j].TileFrameY / 18 % Height, SpriteEffects.None);

        protected bool DrawPost(int i, int j, SpriteBatch spriteBatch, int row, SpriteEffects effects)
        {
            Tile tile = Main.tile[i, j];
            if (tile.IsTileInvisible && !Main.ShouldShowInvisibleWalls()) return false;
            Vector2 offset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
            Rectangle frame = new(0, row == 0 ? 0 : row == Height - 1 ? 36 : 18, 16, 16);
            spriteBatch.Draw(Main.instance.TilesRenderer.GetTileDrawTexture(tile, i, j),
                new Vector2(i * 16, j * 16) - Main.screenPosition + offset, frame,
                tile.IsTileFullbright ? Color.White : Lighting.GetColor(i, j), 0, Vector2.Zero, 1, effects, 0);
            return false;
        }
    }
}
