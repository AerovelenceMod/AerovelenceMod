/*




namespace AerovelenceMod.Content.Items.Sets.Phantic.Armor
{
    public class PhanticDiamond : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        public int timer = 0;

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.scale = 1f;
            Projectile.timeLeft = 500;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
        }

        public float offset = 20;
        public float rotation = Main.rand.NextFloat(6.28f);
        public float scale = 1f;

        float progress = 0f;

        public override void AI()
        {
            if (timer == 0)
            {
                for (int i = 0; i < 0; i++)
                {
                    Vector2 randomStart = Main.rand.NextVector2CircularEdge(2f, 2f) * 2f;
                    Dust dust1 = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<MuraLineBasic>(), randomStart * Main.rand.NextFloat(0.5f, 2.5f));
                    dust1.scale = 0.4f + Main.rand.NextFloat(-0.1f, 0.1f);
                    dust1.alpha = 2 + Main.rand.Next(0, 8);
                    dust1.color = Color.Red;
                    dust1.noGravity = true;
                }
            }



            rotation += 0.045f;
            offset += 4f;

            if (timer > 3)
                progress = Math.Clamp(progress + 0.04f, 0f, 1f);

            if (timer > 1)
                initalProgress = Math.Clamp(MathHelper.Lerp(initalProgress, 2, 0.05f), 0, 1);

            timer++;
        }


        float initalProgress = 0;
        float overallAlpha = 1f;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D Diamond = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Sets/Phantic/Armor/StretchDiamondEdit");
            Texture2D White = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Sets/Phantic/Armor/StretchDiamondWhite");
            Texture2D Orb = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/impact_2fade2");
            Texture2D specil = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/TestTex");

            Color backCol = Color.Lerp(Color.White, Color.Crimson, initalProgress);


            int numberOfSpikes = 5;
            for (int i = 0; i < numberOfSpikes; i++)
            {
                float rot = rotation + MathHelper.ToRadians((360 / numberOfSpikes) * i);
                Vector2 position = (rot.ToRotationVector2() * offset) - Main.screenPosition + Main.MouseWorld;

                Vector2 vec2Scale = new Vector2(1f - progress, 1f + (0.5f * (progress / 1))) * scale * 0.5f;


                Main.spriteBatch.Draw(White, position, null, backCol with { A = 0 } * (1f - initalProgress), rot + MathHelper.PiOver2, White.Size() / 2, vec2Scale * 1.1f, 0, 0f);
                Main.spriteBatch.Draw(Diamond, position, null, Color.Red with { A = 0 } * initalProgress, rot + MathHelper.PiOver2, Diamond.Size() / 2, vec2Scale, 0, 0f);
                Main.spriteBatch.Draw(Diamond, position, null, Color.Red with { A = 150 } * initalProgress, rot + MathHelper.PiOver2, Diamond.Size() / 2, vec2Scale, 0, 0f);
            }
            //float prog = Math.Clamp(progress * 2, 0, 1);
            //Main.spriteBatch.Draw(Orb, -Main.screenPosition + Main.MouseWorld, null, Color.Red with { A = 0 } * (1f - prog), rotation, Orb.Size() / 2, 0.2f + (0.7f * progress), 0, 0f);

            return false;
        }

    }
}*/
