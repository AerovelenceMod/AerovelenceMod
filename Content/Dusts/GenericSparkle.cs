using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Dusts;

public class GenericSparkle : ModDust
{
    internal const int Lifetime = 24;
    private const int FrameHeight = 14;

    public override void OnSpawn(Dust dust)
    {
        dust.noGravity = true;
        dust.noLight = true;
        dust.fadeIn = 0;
        dust.frame = new Rectangle(0, 0, 10, FrameHeight);
    }

    public override bool Update(Dust dust)
    {
        dust.position += dust.velocity;
        dust.velocity *= .9f;
        if (++dust.fadeIn >= Lifetime) dust.active = false;
        return false;
    }

    public override bool PreDraw(Dust dust)
    {
        Draw(Main.spriteBatch, dust.position - Main.screenPosition, (int)dust.fadeIn, dust.scale, dust.rotation, Color.White * (1 - dust.alpha / 255f));
        return false;
    }

    internal static void Draw(SpriteBatch spriteBatch, Vector2 position, int age, float scale, float rotation, Color color)
    {
        if (age < 0 || age >= Lifetime) return;
        Texture2D texture = ModContent.Request<Texture2D>("AerovelenceMod/Content/Dusts/GenericSparkle").Value;
        Rectangle frame = new(0, age / 6 * FrameHeight, 10, FrameHeight);
        spriteBatch.Draw(texture, position, frame, color, rotation, frame.Size() * .5f, scale, SpriteEffects.None, 0);
    }
}
