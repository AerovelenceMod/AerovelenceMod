using AerovelenceMod.Common.Systems;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Glimmerwood;
namespace AerovelenceMod.Common.Globals;
public sealed class CavernTrackTile : GlobalTile
{
    public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
    {
        if (!MinecartTrackUtils.IsTrack(type)) return true;
        Tile above = Main.tile[i, j - 1];
        bool hologram = HolotrackSystem.Contains(i, j);
        if (above.HasTile && (!above.IsTileInvisible || Main.ShouldShowInvisibleWalls()))
        {
            bool rope = Main.tileRope[above.TileType], beam = above.TileType == ModContent.TileType<GlimmerwoodBeamTile>();
            if (beam || rope && (hologram || !WorldGen.IsRope(i, j)))
            {
                Vector2 offset = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);
                Rectangle source = rope ? new(90, (i + j) % 3 * 18, 16, 16) : CommonTileHelper.VerticalBeamFrame(above.TileFrameNumber);
                spriteBatch.Draw(Main.instance.TilesRenderer.GetTileDrawTexture(above, i, j - 1), new Vector2(i * 16, j * 16) - Main.screenPosition + offset, source, Lighting.GetColor(i, j));
            }
        }
        return true;
    }
}
