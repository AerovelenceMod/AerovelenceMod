using System;

namespace AerovelenceMod.Content.Dusts.OverTile
{
    public class PaintSplotch : DrawOverTilesDust
    {
        public override string Texture => "AerovelenceMod/Content/Dusts/OverTile/PaintSplotch";

        public override void OnSpawn(Dust dust)
        {
            dust.customData = new PaintSplotchDustBehavior();
        }

        public override bool Update(Dust dust)
        {
            PaintSplotchDustBehavior behavoir = (PaintSplotchDustBehavior)dust.customData;

            if (behavoir.timer == 0)
                dust.rotation = Main.rand.NextFloat(6.28f);

            dust.velocity = Vector2.Zero;

            if (behavoir.timer > behavoir.timeBeforeFadeOut)
            {
                behavoir.opacity *= 0.93f;

                if (behavoir.opacity < 0.05f || behavoir.timer >= 600)
                {
                    dust.active = false;
                }
            }

            float fadeInTime = Math.Clamp((behavoir.timer + 10f) / 25f, 0f, 1f);
            behavoir.drawScale = Easings.easeInOutBack(fadeInTime, 0f, 1f);

            dust.position += dust.velocity;
            behavoir.timer++;
            return false;
        }


        public override bool PreDraw(Dust dust)
        {
            return false;
        }

        public override void DrawOverTiles(SpriteBatch spriteBatch, Dust dust)
        {
            Texture2D TexMain = Texture2D.Value;

            if (dust.customData is PaintSplotchDustBehavior psdb)
            {
                Vector2 drawPos = dust.position - Main.screenPosition;

                int frameHeight = TexMain.Height / 3;
                int startY = frameHeight * (int)psdb.frame;
                Rectangle sourceRectangle = new Rectangle(0, startY, TexMain.Width, frameHeight);
                Vector2 origin = sourceRectangle.Size() / 2f;

                Color lightColor = Lighting.GetColor((int)dust.position.X / 16, (int)dust.position.Y / 16);

                Color col = lightColor.MultiplyRGBA(dust.color);

                Main.EntitySpriteDraw(TexMain, drawPos, sourceRectangle, col * psdb.opacity, dust.rotation, origin, dust.scale * psdb.drawScale * 1f, SpriteEffects.None);
            }
        }
    }

    public class PaintSplotchDustBehavior
    {
        public int timer = 0;
        public float opacity = 1f;
        public float drawScale = 1f;

        public int frame = 0;

        public int timeBeforeFadeOut = 45;

        public PaintSplotchDustBehavior()
        {
            frame = Main.rand.Next(0, 3);

            timeBeforeFadeOut = Main.rand.Next(120, 135);
        }
    }

}