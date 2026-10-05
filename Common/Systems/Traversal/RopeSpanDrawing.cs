using System;
using System.Collections.Generic;



using Terraria.GameContent;

using AerovelenceMod.Content.Tiles.Traversal;

namespace AerovelenceMod.Common.Systems.Traversal
{
    public sealed class RopeSpanDrawing : ModSystem
    {
        private static Point? blocker;
        private static ulong blockerUntil;
        private static RopeSpan preview;
        private static RopeSpanSystem.ConnectionError? previewError;
        private static ulong nextPreviewCheck;
        private static bool BlockerVisible => blocker.HasValue && Main.GameUpdateCount < blockerUntil;

        internal static void ShowBlocker(Point? point)
        {
            blocker = point;
            blockerUntil = Main.GameUpdateCount + 300;
        }

        public override void ClearWorld()
        {
            ShowBlocker(null);
            preview = null;
            previewError = null;
        }
        public override void Load()
        {
            if (!Main.dedServ) On_Main.DoDraw_Tiles_Solid += DrawBeforeTiles;
        }

        public override void Unload()
        {
            ClearWorld();
            if (!Main.dedServ) On_Main.DoDraw_Tiles_Solid -= DrawBeforeTiles;
        }

        private static void DrawBeforeTiles(On_Main.orig_DoDraw_Tiles_Solid orig, Main self)
        {
            if (!Main.gameMenu && RopeSpanSystem.Spans.Count > 0)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                Draw();
                Main.spriteBatch.End();
            }
            orig(self);
        }

        public override void PostDrawTiles()
        {
            if (Main.gameMenu) return;
            bool showPreview = PreparePreview(Main.myPlayer, Main.MouseWorld);
            if (!showPreview && !BlockerVisible && !Main.SmartCursorIsUsed) return;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (showPreview) DrawSpan(preview, 0.5f, true);
            DrawOverlay(showPreview);
            Main.spriteBatch.End();
        }

        internal static bool PreparePreview(int playerId, Vector2 cursor)
        {
            if (!RopeSpanSystem.TryGetPreviewTarget(playerId, cursor, out Point a, out Point b, out bool zipline, out int rope))
            {
                preview = null;
                previewError = null;
                return false;
            }
            if (a.X > b.X || a.X == b.X && a.Y > b.Y) (a, b) = (b, a);
            if (a == b || !zipline && b.X - a.X < 1)
            {
                preview = null;
                previewError = null;
                return false;
            }
            bool changed = preview == null || preview.Left != a || preview.Right != b || preview.Zipline != zipline || preview.RopeType != rope;
            if (changed || Main.GameUpdateCount >= nextPreviewCheck)
            {
                previewError = RopeSpanSystem.CheckConnection(a, b, zipline, out RopeSpan candidate, rope);
                preview = candidate ?? new RopeSpan(0, a, b, zipline, rope);
                nextPreviewCheck = Main.GameUpdateCount + 8;
            }
            return true;
        }

        private static bool Visible(Vector2 a, Vector2 b)
            => Math.Max(a.X, b.X) >= Main.screenPosition.X - 64 && Math.Min(a.X, b.X) <= Main.screenPosition.X + Main.screenWidth + 64
                && Math.Max(a.Y, b.Y) >= Main.screenPosition.Y - 64 && Math.Min(a.Y, b.Y) <= Main.screenPosition.Y + Main.screenHeight + 64;

        private static Vector2 Screen(Vector2 point) => point - Main.screenPosition;

        private static void Draw()
        {
            Rectangle view = new((int)Main.screenPosition.X - 64, (int)Main.screenPosition.Y - 64, Main.screenWidth + 128, Main.screenHeight + 128);
            foreach (RopeSpan span in RopeSpanSystem.Spans.Values)
            {
                if (!span.Bounds.Intersects(view)) continue;
                DrawSpan(span, 1, false);
            }
        }

        private static void DrawSpan(RopeSpan span, float opacity, bool ghost)
        {
            for (int i = 1; i < span.Nodes.Length - 1; i++)
            {
                if (!Visible(span.Nodes[i - 1], span.Nodes[i + 1])) continue;
                int section = i - 1;
                Point ropeTile = span.Zipline ? span.Rest[i].ToTileCoordinates() : span.RopeTiles[section];
                Tile rope = Main.tile[ropeTile.X, ropeTile.Y];
                Vector2 center = span.Nodes[i];
                Vector2 a = (span.Nodes[i - 1] + center) * 0.5f;
                Vector2 b = (center + span.Nodes[i + 1]) * 0.5f;
                if (ghost || !rope.IsTileInvisible || Main.ShouldShowInvisibleWalls())
                {
                    int type = span.Zipline ? ModContent.TileType<ZiplineRopeTile>() : ModContent.TileType<RopeBridgeRopeTile>();
                    Texture2D texture = ghost ? TextureAssets.Tile[type].Value : PaintedTexture(rope, type);
                    Color light = (ghost || rope.IsTileFullbright ? Color.White : Lighting.GetColor(ropeTile.X, ropeTile.Y)) * opacity;
                    Vector2 start = i == 1 ? span.Nodes[0] : a;
                    Vector2 end = i == span.Nodes.Length - 2 ? span.Nodes[^1] : b;
                    if (span.Zipline)
                    {
                        DrawRope(texture, start, center, light);
                        DrawRope(texture, center, end, light);
                    }
                    else
                    {
                        Vector2 rail = new(0, -32);
                        Vector2 stringOffset = new(0, 4);
                        DrawRope(texture, start + stringOffset, center + stringOffset, light);
                        DrawRope(texture, center + stringOffset, end + stringOffset, light);
                        DrawRope(texture, center + stringOffset, center + rail, light);
                        DrawRope(texture, start + rail, center + rail, light);
                        DrawRope(texture, center + rail, end + rail, light);
                    }
                }
                if (!span.Zipline)
                {
                    Point deckTile = span.DeckTiles[section];
                    Tile deck = Main.tile[deckTile.X, deckTile.Y];
                    if (ghost || !deck.IsTileInvisible || Main.ShouldShowInvisibleWalls())
                    {
                        int type = ModContent.TileType<RopeBridgeDeckTile>();
                        Texture2D texture = ghost ? TextureAssets.Tile[type].Value : PaintedTexture(deck, type);
                        Color light = (ghost || deck.IsTileFullbright ? Color.White : Lighting.GetColor(deckTile.X, deckTile.Y)) * opacity;
                        Vector2 edge = Vector2.Normalize(b - a);
                        Vector2 inset = new Vector2(-edge.Y, edge.X) * 2;
                        DrawStrip(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, 1, 1),
                            center - edge * 6, center + edge * 6, light.MultiplyRGB(new Color(55, 39, 26)), 0, 8);
                        Main.spriteBatch.Draw(texture, Screen(center + inset), new Rectangle(18, 18, 8, 4),
                            light, edge.ToRotation(), new Vector2(4, 0), 1, SpriteEffects.None, 0);
                    }
                }
            }
        }

        private static void DrawOverlay(bool showPreview)
        {
            Player player = Main.LocalPlayer;
            RopeSpan highlighted = null;
            float selectedParameter = 0;
            if (Main.SmartCursorIsUsed && !player.dead && !player.mouseInterface && !Main.playerInventory)
                RopeSpanPlayer.TrySelectZipline(player, Main.MouseWorld, true, out highlighted, out selectedParameter);
            Point? marked = showPreview ? previewError?.Blocker : BlockerVisible ? blocker : null;
            if (marked.HasValue)
            {
                Vector2 corner = new(marked.Value.X * 16, marked.Value.Y * 16);
                Color red = new Color(255, 80, 65) * (0.8f + MathF.Sin((float)Main.GlobalTimeWrappedHourly * 6) * 0.2f);
                PixelLine(corner, corner + new Vector2(16, 0), red);
                PixelLine(corner + new Vector2(16, 0), corner + new Vector2(16, 16), red);
                PixelLine(corner + new Vector2(16, 16), corner + new Vector2(0, 16), red);
                PixelLine(corner + new Vector2(0, 16), corner, red);
            }
            if (highlighted != null)
            {
                float pulse = 0.7f + MathF.Sin((float)Main.GlobalTimeWrappedHourly * 5) * 0.2f;
                Color yellow = new Color(255, 225, 55) * pulse;
                for (int i = 1; i < highlighted.Nodes.Length - 1; i++)
                {
                    Vector2 node = highlighted.Nodes[i];
                    Vector2 start = i == 1 ? highlighted.Nodes[0] : (highlighted.Nodes[i - 1] + node) * 0.5f;
                    Vector2 end = i == highlighted.Nodes.Length - 2 ? highlighted.Nodes[^1] : (node + highlighted.Nodes[i + 1]) * 0.5f;
                    DrawRope(TextureAssets.MagicPixel.Value, start, node, yellow, true);
                    DrawRope(TextureAssets.MagicPixel.Value, node, end, yellow, true);
                }
                Vector2 center = highlighted.At(selectedParameter);
                int radius = (int)MathF.Round((12 + pulse * 4) / 2);
                Vector2 origin = new(MathF.Round(center.X / 2) * 2, MathF.Round(center.Y / 2) * 2);
                foreach (Point point in CirclePixels(radius)) DrawPixel(origin + point.ToVector2() * 2, yellow);
            }
        }

        private static Texture2D PaintedTexture(Tile tile, int type)
            => Main.instance.TilePaintSystem.TryGetTileAndRequestIfNotReady(type, 0, tile.TileColor) ?? TextureAssets.Tile[type].Value;

        private static void DrawRope(Texture2D texture, Vector2 a, Vector2 b, Color color, bool outline = false)
        {
            Vector2 edge = b - a;
            float length = edge.Length();
            if (length < 0.001f) return;
            Vector2 direction = edge / length;
            for (int i = 0; i * 16 < length; i++)
            {
                int pixels = Math.Min(16, (int)MathF.Ceiling(length - i * 16));
                Rectangle source = new(94, (i + (int)(a.X / 16)) % 3 * 18, 8, pixels);
                Vector2 position = Screen(a + direction * (i * 16));
                float rotation = edge.ToRotation() - MathHelper.PiOver2;
                if (outline)
                {
                    Rectangle pixel = new(0, 0, 1, 1);
                    Vector2 scale = new(2, pixels);
                    Main.spriteBatch.Draw(texture, position, pixel, color, rotation, new Vector2(2, 0), scale, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(texture, position, pixel, color, rotation, new Vector2(-1, 0), scale, SpriteEffects.None, 0);
                }
                else Main.spriteBatch.Draw(texture, position, source, color, rotation, new Vector2(4, 0), 1, SpriteEffects.None, 0);
            }
        }

        private static void DrawStrip(Texture2D texture, Rectangle source, Vector2 a, Vector2 b, Color color, float spriteRotation, float thickness = 8)
        {
            Vector2 start = Screen(a), edge = b - a;
            Vector2 scale = spriteRotation == 0 ? new Vector2(edge.Length() / source.Width, thickness / source.Height)
                : new Vector2(thickness / source.Width, edge.Length() / source.Height);
            Main.spriteBatch.Draw(texture, start, source, color, edge.ToRotation() - spriteRotation,
                spriteRotation == 0 ? Vector2.Zero : new Vector2(source.Width / 2f, 0), scale, SpriteEffects.None, 0);
        }

        internal static IEnumerable<Point> LinePixels(Point a, Point b)
        {
            int dx = Math.Abs(b.X - a.X), dy = -Math.Abs(b.Y - a.Y);
            int sx = a.X < b.X ? 1 : -1, sy = a.Y < b.Y ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                yield return a;
                if (a == b) yield break;
                int next = error * 2;
                if (next >= dy) { error += dy; a.X += sx; }
                if (next <= dx) { error += dx; a.Y += sy; }
            }
        }

        internal static IEnumerable<Point> CirclePixels(int radius)
        {
            HashSet<Point> pixels = new();
            int x = radius, y = 0, error = 1 - radius;
            while (x >= y)
            {
                pixels.Add(new Point(x, y));
                pixels.Add(new Point(y, x));
                pixels.Add(new Point(-y, x));
                pixels.Add(new Point(-x, y));
                pixels.Add(new Point(-x, -y));
                pixels.Add(new Point(-y, -x));
                pixels.Add(new Point(y, -x));
                pixels.Add(new Point(x, -y));
                y++;
                if (error < 0) error += 2 * y + 1;
                else { x--; error += 2 * (y - x) + 1; }
            }
            return pixels;
        }

        private static void DrawPixel(Vector2 point, Color color)
            => Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Screen(point), new Rectangle(0, 0, 1, 1),
                color, 0, Vector2.Zero, 2, SpriteEffects.None, 0);

        private static void PixelLine(Vector2 a, Vector2 b, Color color)
        {
            Point start = new((int)MathF.Round(a.X / 2), (int)MathF.Round(a.Y / 2));
            Point end = new((int)MathF.Round(b.X / 2), (int)MathF.Round(b.Y / 2));
            foreach (Point point in LinePixels(start, end)) DrawPixel(point.ToVector2() * 2, color);
        }
    }
}
