using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora;

internal class LuminVines : ModTile
{
    private const int UpwardFrames = 54;
    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileCut[Type] = true;
        Main.tileNoFail[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        Main.tileLighted[Type] = true;
        TileID.Sets.IsVine[Type] = true;
        AddMapEntry(new Color(89, 161, 244));
        DustType = DustID.Grass;
        HitSound = SoundID.Grass;
    }

    private static bool Vine(int x, int y) => WorldGen.InWorld(x, y, 2) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == ModContent.TileType<LuminVines>();
    private static int Direction(Tile tile) => tile.TileFrameX >= UpwardFrames ? -1 : 1;
    private static bool Open(int x, int y) => WorldGen.InWorld(x, y, 2) && !Main.tile[x, y].HasTile &&
        (Main.tile[x, y].LiquidAmount == 0 || Main.tile[x, y].LiquidType != LiquidID.Lava);

    public static bool Plant(int x, int y, int length)
    {
        if (!Open(x, y)) return false;
        int direction = WorldGen.SolidTile(x, y + 1) ? -1 : WorldGen.SolidTile(x, y - 1) ? 1 : 0;
        if (direction == 0) return false;
        int count = 0;
        while (count < System.Math.Clamp(length, 1, 10) && Open(x, y + count * direction)) count++;
        for (int n = 0; n < count; n++)
        {
            Tile tile = Main.tile[x, y + n * direction];
            tile.ResetToType((ushort)ModContent.TileType<LuminVines>());
            tile.TileFrameX = (short)(direction < 0 ? UpwardFrames : 0);
        }
        for (int n = 0; n < count; n++) Frame(x, y + n * direction);
        return count > 0;
    }

    private static void Frame(int x, int y)
    {
        Tile tile = Main.tile[x, y];
        int direction = Direction(tile);
        int variant = (int)((uint)(x * 73471 ^ y * 19391) % 12);
        int column, row;
        if (!Vine(x, y - direction)) { column = 2; row = variant % 4; }
        else if (!Vine(x, y + direction))
        {
            variant %= 6;
            column = variant < 2 ? 0 : 1;
            row = variant < 2 ? variant + 2 : variant - 2;
        }
        else { column = 0; row = variant % 2; }
        tile.TileFrameX = (short)(column * 18 + (direction < 0 ? UpwardFrames : 0));
        tile.TileFrameY = (short)(row * 18);
    }

    public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
    {
        Tile tile = Main.tile[i, j];
        int direction = Direction(tile), root = j;
        if (tile.TileFrameX == 0 && WorldGen.SolidTile(i, j + 1)) direction = -1;
        tile.TileFrameX = (short)(tile.TileFrameX % UpwardFrames + (direction < 0 ? UpwardFrames : 0));
        while (Vine(i, root - direction) && Direction(Main.tile[i, root - direction]) == direction) root -= direction;
        if (!WorldGen.InWorld(i, root - direction, 1) || !WorldGen.SolidOrSlopedTile(Main.tile[i, root - direction]))
            WorldGen.KillTile(i, j);
        else Frame(i, j);
        return false;
    }

    public override void RandomUpdate(int i, int j)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        bool reset = false, noBreak = false;
        TileFrame(i, j, ref reset, ref noBreak);
        if (!Vine(i, j)) return;
        int direction = Direction(Main.tile[i, j]), root = j;
        while (Vine(i, root - direction)) root -= direction;
        int next = j + direction;
        if (System.Math.Abs(j - root) >= 9 || !Open(i, next) || !WorldGen.genRand.NextBool(10)) return;
        Tile tile = Main.tile[i, next];
        tile.ResetToType((ushort)Type);
        tile.TileFrameX = (short)(direction < 0 ? UpwardFrames : 0);
        Frame(i, j);
        Frame(i, next);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, i, System.Math.Min(j, next), 1, 2);
    }

    public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) { r = .015f; g = .19f; b = .27f; }
    public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
    {
        int direction = Direction(Main.tile[i, j]);
        if (!Vine(i, j - direction) || Direction(Main.tile[i, j - direction]) != direction)
            Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomNonSolid);
        return false;
    }

    public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
    {
        if (!Vine(i, j)) return;
        int direction = Direction(Main.tile[i, j]);
        var renderer = Main.instance.TilesRenderer;
        Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
        Vector2 position = new(i * 16 + 8, j * 16 + (direction > 0 ? 0 : 16));
        Vector2 screen = Main.Camera.UnscaledPosition;
        Vector2 origin = new(8, direction > 0 ? 0 : 16);
        SpriteEffects flip = direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        float windStrength = MathHelper.Lerp(.2f, 1, System.Math.Abs(Main.WindForVisuals) / 1.2f);
        float windBend = -.08f * windStrength;
        float wind = renderer.GetWindCycle(i, j, renderer._vineWindCounter);
        float push = 0, previousPush = 0;
        int exposed = 0;
        for (int y = j; Vine(i, y) && Direction(Main.tile[i, y]) == direction; y += direction)
        {
            Tile tile = Main.tile[i, y];
            if (exposed >= 5) windBend += .0075f * windStrength;
            if (exposed >= 2) windBend += .0025f;
            if (WallID.Sets.AllowsWind[tile.WallType] && (Main.remixWorld ? y > Main.worldSurface : y < Main.worldSurface)) exposed++;
            float contact = renderer.GetWindGridPush(i, y, direction > 0 ? 20 : 40, direction > 0 ? .01f : -.004f);
            push = contact != 0 || previousPush == 0 ? push - contact : push * -.78f;
            previousPush = contact;
            float rotation = exposed * windBend * wind * direction + push;
            Rectangle source = new(tile.TileFrameX % UpwardFrames, tile.TileFrameY, 16, 16);
            if (TileDrawing.IsVisible(tile))
            {
                Color light = tile.IsTileFullbright ? Color.White : Lighting.GetColor(i, y);
                spriteBatch.Draw(TextureAssets.Tile[Type].Value, position - screen, source, light, rotation, origin, 1, flip, 0);
                spriteBatch.Draw(glow, position - screen, source, Color.White, rotation, origin, 1, flip, 0);
            }
            position += (rotation + MathHelper.PiOver2 * direction).ToRotationVector2() * 16;
        }
    }
}
