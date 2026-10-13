using System;
namespace AerovelenceMod.Common.Utilities
{
    internal static class DrawUtils
    {
        private const int CrystalShineSampleSize = 2;
        private const float CrystalShineSpeed = 2;
        private const float CrystalShineVerticalWeight = .2f;
        private const float CrystalShineFrequency = 15;
        private const float CrystalShineMinimum = .75f;
        // Dont restart the tile batch here, it trips up the rest of the tile drawing
        internal static void DrawCrystalShine(SpriteBatch batch, Texture2D texture, Rectangle source, Vector2 position, Color light, float time, float opacity = .8f)
        {
            for (int y = 0; y < source.Height; y += CrystalShineSampleSize)
                for (int x = 0; x < source.Width; x += CrystalShineSampleSize)
                {
                    float wave = Math.Clamp(MathF.Sin(CrystalShineSpeed * time - ((source.X + x) / (float)texture.Width + (source.Y + y) / (float)texture.Height * CrystalShineVerticalWeight) * CrystalShineFrequency), CrystalShineMinimum, 1);
                    Color color = light * (wave * opacity);
                    color.A = 0;
                    batch.Draw(texture, position + new Vector2(x, y), new Rectangle(source.X + x, source.Y + y, Math.Min(CrystalShineSampleSize, source.Width - x), Math.Min(CrystalShineSampleSize, source.Height - y)), color);
                }
        }
        internal static void EnsureRenderTarget(ref RenderTarget2D target, int width, int height)
        {
            GraphicsDevice device = Main.instance.GraphicsDevice;
            if (target != null && !target.IsDisposed && target.GraphicsDevice == device && target.Width == width && target.Height == height)
                return;
            target?.Dispose();
            target = new RenderTarget2D(device, width, height, false, SurfaceFormat.Color, DepthFormat.None);
        }

        //Me when steal from infernum
        internal static void SwapToRenderTarget(this RenderTarget2D renderTarget, Color? flushColor = null)
        {
            // Local variables for convinience.
            GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
            SpriteBatch spriteBatch = Main.spriteBatch;

            // If we are in the menu, a server, or any of these are null, return.
            if (Main.gameMenu || Main.dedServ || renderTarget is null || graphicsDevice is null || spriteBatch is null)
                return;

            // Otherwise set the render target.
            graphicsDevice.SetRenderTarget(renderTarget);

            // "Flush" the screen, removing any previous things drawn to it.
            flushColor ??= Color.Transparent;
            graphicsDevice.Clear(flushColor.Value);
        }

        internal static void DrawSlopedTile(
            Texture2D texture,
            Vector2 position,
            Tile tile,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth)
        {
            Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);

            if (tile.Slope == SlopeType.Solid)
            {
                Main.spriteBatch.Draw(
                    texture,
                    position + new Vector2(0, tile.IsHalfBlock.ToInt() * 8) + zero,
                    new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16),
                    color, rotation, origin, scale, effects, layerDepth);
                return;
            }

            for (int x = 0; x < 8; x++)
            {
                int sliceWidth;
                int xOffset;

                int sliceHeight = 2;
                int yOffset = x * 2;

                switch (tile.Slope)
                {
                    default:
                        sliceWidth = 16;
                        xOffset = 0;
                        break;
                    case SlopeType.SlopeDownLeft:
                        sliceWidth = 2 + x * 2;
                        xOffset = 0;
                        break;
                    case SlopeType.SlopeDownRight:
                        sliceWidth = 2 + x * 2;
                        xOffset = 16 - sliceWidth;
                        break;
                    case SlopeType.SlopeUpLeft:
                        sliceWidth = 16 - x * 2;
                        xOffset = 0;
                        break;
                    case SlopeType.SlopeUpRight:
                        sliceWidth = 16 - x * 2;
                        xOffset = 16 - sliceWidth;
                        break;
                }

                Main.spriteBatch.Draw(
                    texture,
                    position + new Vector2(xOffset, yOffset) + zero,
                    new Rectangle(tile.TileFrameX + xOffset, tile.TileFrameY + yOffset, sliceWidth, sliceHeight),
                    color, rotation, origin, scale, effects, layerDepth);
            }
        }
    }
}
