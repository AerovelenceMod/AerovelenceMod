using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
namespace AerovelenceMod.Common.Utilities
{
    public static class SlimeSurface
    {
        public static bool IsSolidAt(Vector2 position)
        {
            Point point = position.ToTileCoordinates();
            if (!WorldGen.InWorld(point.X, point.Y, 1)) return true;
            Tile tile = Main.tile[point.X, point.Y];
            if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) return false;
            float x = position.X - point.X * 16;
            float y = position.Y - point.Y * 16;
            if (tile.IsHalfBlock) return y >= 8;
            return tile.Slope switch
            {
                SlopeType.SlopeDownLeft => x <= y,
                SlopeType.SlopeDownRight => x + y >= 16,
                SlopeType.SlopeUpLeft => x + y <= 16,
                SlopeType.SlopeUpRight => x >= y,
                _ => true
            };
        }
        public static bool TryContact(Vector2 start, Vector2 direction, float distance, out Vector2 point, out Vector2 normal)
        {
            point = start;
            normal = -direction;
            if (direction.LengthSquared() < .001f || IsSolidAt(start)) return false;
            direction.Normalize();
            Vector2 previous = start;
            for (float traveled = 1; traveled <= distance; traveled++)
            {
                Vector2 sample = start + direction * traveled;
                if (!IsSolidAt(sample)) { previous = sample; continue; }
                point = (previous + sample) * .5f;
                normal = new Vector2((IsSolidAt(sample - Vector2.UnitX * 2) ? 1 : 0) - (IsSolidAt(sample + Vector2.UnitX * 2) ? 1 : 0), (IsSolidAt(sample - Vector2.UnitY * 2) ? 1 : 0) - (IsSolidAt(sample + Vector2.UnitY * 2) ? 1 : 0));
                if (normal.LengthSquared() > .01f) normal.Normalize();
                else normal = -direction;
                return true;
            }
            return false;
        }
    }
}