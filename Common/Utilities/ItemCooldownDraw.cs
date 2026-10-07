using System;
using Terraria.GameContent;

namespace AerovelenceMod.Common.Utilities
{
    internal static class ItemCooldownDraw
    {
        internal static float Progress(int cooldown, int duration) => MathHelper.Clamp(1f - cooldown / (float)duration, 0f, 1f);
        internal static void DrawBase(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame, Color drawColor, Vector2 origin, float scale, int cooldown, Color accent, Color dimColor)
        {
            float pulse = .82f + MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * .12f;
            Color glow = (accent with { A = 0 }) * ((cooldown > 0 ? .1f : .24f) * pulse);
            float glowDistance = cooldown > 0 ? 1f : 1.75f;
            for (int i = 0; i < 4; i++)
                spriteBatch.Draw(texture, position + (MathHelper.PiOver2 * i).ToRotationVector2() * glowDistance * scale, frame, glow, 0f, origin, scale, SpriteEffects.None, 0f);
            spriteBatch.Draw(texture, position, frame, cooldown > 0 ? dimColor * .38f : drawColor, 0f, origin, scale, SpriteEffects.None, 0f);
        }
        internal static void DrawFill(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame, Vector2 origin, float scale, int cooldown, int duration, Color accent, Color timerOutline)
        {
            if (cooldown <= 0) return;
            float progress = Progress(cooldown, duration);
            int height = Math.Clamp((int)MathF.Ceiling(frame.Height * progress), 0, frame.Height);
            float pulse = .8f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * .12f;
            if (height > 0)
            {
                int offset = frame.Height - height;
                Rectangle fill = new(frame.X, frame.Y + offset, frame.Width, height);
                Vector2 fillOrigin = origin - new Vector2(0f, offset);
                spriteBatch.Draw(texture, position, fill, Color.White * (.45f + progress * .5f), 0f, fillOrigin, scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(texture, position, fill, (accent with { A = 0 }) * (.55f * pulse), 0f, fillOrigin, scale, SpriteEffects.None, 0f);
            }
            Vector2 topLeft = position - origin * scale;
            Vector2 bottomRight = position + (frame.Size() - origin) * scale;
            int barWidth = Math.Max(16, (int)MathF.Round(frame.Width * scale));
            Rectangle bar = new((int)MathF.Round(topLeft.X), (int)MathF.Round(bottomRight.Y + 2f), barWidth, 3);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, bar, new Color(7, 20, 30) * .9f);
            if (progress > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bar.X + 1, bar.Y + 1, Math.Max(1, (int)MathF.Round((bar.Width - 2) * progress)), 1), accent);
            string timer = MathF.Ceiling(cooldown / 60f).ToString("0");
            float timerScale = .65f * scale;
            Vector2 timerSize = FontAssets.ItemStack.Value.MeasureString(timer) * timerScale;
            Vector2 timerPosition = bottomRight - timerSize + new Vector2(1f, -2f);
            Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.ItemStack.Value, timer, timerPosition.X, timerPosition.Y, Color.White, timerOutline, Vector2.Zero, timerScale);
        }
    }
}
