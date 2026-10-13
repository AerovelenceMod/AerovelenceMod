using System;
using AerovelenceMod.Common.Systems;
using AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler;
namespace AerovelenceMod.Common.Utilities;

internal static class RailDrawUtils
{
    internal static void Draw(Vector2[] upper, Vector2[] lower, Color color, Color highlight, float strength, bool sparks = true, RenderLayer? layer = null)
    {
        int middle = upper.Length / 2;
        TumblerLightningSystem.DrawPath([upper[0], lower[middle], upper[^1]], color, strength * .35f, 1f, false, layer);
        TumblerLightningSystem.DrawPath(upper, highlight, strength * .9f, 3f, sparks, layer);
        TumblerLightningSystem.DrawPath(lower, color, strength * .45f, 1.5f, sparks, layer);
    }
    internal static Vector2 TrackPoint(Point tile, int offset)
    {
        int sample = Math.Clamp(offset, 0, 14);
        Vector2 point = new(tile.X * 16 + sample, tile.Y * 16 - 26);
        if (!Minecart.GetOnTrack(tile.X, tile.Y, ref point, 0, 0)) return new(tile.X * 16 + sample, tile.Y * 16 + 6);
        return point + new Vector2(0, 26);
    }
    internal static void TrackRails(Point tile, out Vector2[] upper, out Vector2[] lower)
    {
        upper = new Vector2[5];
        lower = new Vector2[5];
        for (int i = 0; i < 5; i++)
        {
            Vector2 point = TrackPoint(tile, Math.Min(i * 4, 14));
            if (i == 4) point += TrackPoint(tile, 14) - TrackPoint(tile, 12);
            upper[i] = point;
            lower[i] = point + new Vector2(0, 7);
        }
    }
}
