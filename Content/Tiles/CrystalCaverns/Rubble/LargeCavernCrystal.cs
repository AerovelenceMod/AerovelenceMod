using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ObjectData;
namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Rubble;
public abstract class LargeCavernCrystal : ModTile
{
    protected abstract int Width { get; }
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        Main.tileLighted[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style2xX);
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.Width = Width;
        TileObjectData.newTile.Origin = new Point16(0, 2);
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, Width, 0);
        TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
        TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.newTile.StyleWrapLimit = 2;
        TileObjectData.newTile.StyleMultiplier = 2;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
        TileObjectData.addAlternate(1);
        TileObjectData.addTile(Type);
        AddMapEntry(new Color(140, 171, 203));
        DustType = ModContent.DustType<Dusts.CavernCrystalDust>();
    }
    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) { r = .351f; g = .3f; b = .078f; }
    public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
    {
        if (Main.rand.NextBool(400)) Dust.NewDust(new Vector2(i * 16, j * 16), 16, 16, DustType);
    }
    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        Tile tile = Main.tile[i, j];
        if (tile.IsTileInvisible && !Main.ShouldShowInvisibleWalls()) return false;
        Vector2 offset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
        Texture2D texture = Main.instance.TilesRenderer.GetTileDrawTexture(tile, i, j);
        DrawUtils.DrawCrystalShine(spriteBatch, texture, new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16), new Vector2(i * 16, j * 16 + 2) - Main.screenPosition + offset, Lighting.GetColor(i, j), (float)Main.time * .02f);
        return false;
    }
}
