using System;
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
            if (direction.LengthSquared() < .001f || distance <= 0f) return false;
            direction.Normalize();
            if (IsSolidAt(start))
            {
                bool escaped = false;
                for (float back = 1; back <= distance; back++)
                {
                    Vector2 sample = start - direction * back;
                    if (IsSolidAt(sample)) continue;
                    start = sample;
                    escaped = true;
                    break;
                }
                if (!escaped) return false;
            }
            Vector2 previous = start;
            for (float traveled = 1; traveled <= distance; traveled++)
            {
                Vector2 sample = start + direction * traveled;
                if (!IsSolidAt(sample)) { previous = sample; continue; }
                Vector2 inside = sample;
                for (int refinement = 0; refinement < 6; refinement++)
                {
                    Vector2 midpoint = (previous + inside) * .5f;
                    if (IsSolidAt(midpoint)) inside = midpoint;
                    else previous = midpoint;
                }
                point = (previous + inside) * .5f;
                normal = new Vector2((IsSolidAt(sample - Vector2.UnitX * 2) ? 1 : 0) - (IsSolidAt(sample + Vector2.UnitX * 2) ? 1 : 0), (IsSolidAt(sample - Vector2.UnitY * 2) ? 1 : 0) - (IsSolidAt(sample + Vector2.UnitY * 2) ? 1 : 0));
                if (normal.LengthSquared() > .01f) normal.Normalize();
                else normal = -direction;
                Point tilePoint = inside.ToTileCoordinates();
                if (WorldGen.InWorld(tilePoint.X, tilePoint.Y, 1))
                {
                    Tile tile = Main.tile[tilePoint.X, tilePoint.Y];
                    float x = inside.X - tilePoint.X * 16;
                    float y = inside.Y - tilePoint.Y * 16;
                    bool diagonal = tile.Slope is SlopeType.SlopeDownLeft or SlopeType.SlopeUpRight
                        ? Math.Abs(y - x) < .1f : Math.Abs(x + y - 16) < .1f;
                    if (diagonal && tile.Slope != SlopeType.Solid)
                        normal = Vector2.Normalize(tile.Slope switch
                        {
                            SlopeType.SlopeDownLeft => new Vector2(1, -1),
                            SlopeType.SlopeDownRight => new Vector2(-1, -1),
                            SlopeType.SlopeUpLeft => new Vector2(1, 1),
                            _ => new Vector2(-1, 1)
                        });
                }
                return true;
            }
            return false;
        }

        public static bool TryGroundContact(NPC npc, out Vector2 point, out Vector2 normal, float distance = 4f)
        {
            point = npc.Bottom;
            normal = -Vector2.UnitY;
            if (npc.noTileCollide || npc.velocity.Y < -.5f) return false;
            bool found = false;
            for (int i = 0; i < 5; i++)
            {
                float x = MathHelper.Lerp(npc.position.X + 2f, npc.position.X + npc.width - 2f, i / 4f);
                Vector2 start = new(x, npc.Bottom.Y - distance);
                if (TryContact(start, Vector2.UnitY, distance * 2f, out Vector2 contact, out Vector2 surfaceNormal)
                    && surfaceNormal.Y < -.25f && Math.Abs(contact.Y - npc.Bottom.Y) <= distance + .05f)
                {
                    if (!found || contact.Y < point.Y) { point = contact; normal = surfaceNormal; }
                    found = true;
                }
                Point tilePoint = new Vector2(x, npc.Bottom.Y + distance).ToTileCoordinates();
                for (int y = tilePoint.Y - 1; y <= tilePoint.Y; y++)
                {
                    if (!WorldGen.InWorld(tilePoint.X, y, 1)) continue;
                    Tile tile = Main.tile[tilePoint.X, y];
                    if (!tile.HasUnactuatedTile || !Main.tileSolidTop[tile.TileType] || tile.TileFrameY != 0
                        || npc.Bottom.Y > y * 16f + 1f || y * 16f - npc.Bottom.Y > distance) continue;
                    Vector2 platformPoint = new(x, y * 16f);
                    if (!found || platformPoint.Y < point.Y) { point = platformPoint; normal = -Vector2.UnitY; }
                    found = true;
                }
            }
            return found;
        }

        public static bool IsGrounded(NPC npc) => TryGroundContact(npc, out _, out _);
    }
}
