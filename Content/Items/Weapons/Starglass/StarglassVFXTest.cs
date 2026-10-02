using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Terraria.Audio;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Dusts.GlowDusts;
using AerovelenceMod.Content.Projectiles;
using System.Collections.Generic;
using Terraria.Graphics;

namespace AerovelenceMod.Content.Items.Weapons.Starglass
{
    public class StarglassTestVFX : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;

            Projectile.timeLeft = 350;
            Projectile.penetrate = -1;
        }

        BaseTrailInfo trail1 = new BaseTrailInfo();
        BaseTrailInfo trail2 = new BaseTrailInfo();

        int timer = 0;

        public Color trailCol = Main.rand.NextBool() ? new Color(255, 20, 20) : Color.DodgerBlue;

        Vector2 oldPos = Vector2.Zero;

        public override void AI()
        {
            //Trail1 Info Dump
            trail1.trailTexture = ModContent.Request<Texture2D>("AerovelenceMod/Assets/FlamesTextureButBlack").Value;
            trail1.trailColor = Color.White * 0.8f;
            trail1.trailPointLimit = 400;
            trail1.trailWidth = (int)(15 * Projectile.scale);
            trail1.trailMaxLength = 600;
            trail1.timesToDraw = 2;

            trail1.trailTime = timer * 0.02f;
            trail1.trailRot = Projectile.velocity.ToRotation();
            trail1.trailPos = Projectile.Center;
            trail1.TrailLogic();

            //Trail2 Info Dump
            trail2.trailTexture = ModContent.Request<Texture2D>("AerovelenceMod/Assets/LintyTrail").Value;
            trail2.trailColor = trailCol;
            trail2.trailPointLimit = 400;
            trail2.trailWidth = (int)(20 * Projectile.scale);
            trail2.trailMaxLength = 600;
            trail2.timesToDraw = 2;

            trail2.trailTime = timer * 0.04f;
            trail2.trailRot = Projectile.velocity.ToRotation();
            trail2.trailPos = Projectile.Center;
            trail2.TrailLogic();

            Vector2 pos = trailCol == new Color(255, 20, 20) ? new Vector2(-200, 0f) : new Vector2(200f, 0f);

            Projectile.velocity = (Main.MouseWorld - Projectile.Center + pos).SafeNormalize(Vector2.UnitX) * 15;

            oldPos = Projectile.Center;

            timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            trail1.TrailDrawing(Main.spriteBatch);
            trail2.TrailDrawing(Main.spriteBatch);

            Texture2D Ball = Mod.Assets.Request<Texture2D>("Assets/Flare/flare_12").Value;
            Texture2D Glow = Mod.Assets.Request<Texture2D>("Assets/Orbs/feather_circle").Value;

            float scaley = 0.5f;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(Glow, oldPos - Main.screenPosition, null, trailCol * 0.3f, Projectile.rotation + timer * -0.05f, Glow.Size() / 2, Projectile.scale * 0.75f, SpriteEffects.None, 0f);


            Main.spriteBatch.Draw(Glow, oldPos - Main.screenPosition, null, trailCol * 0.45f, Projectile.rotation + timer * -0.05f, Glow.Size() / 2, Projectile.scale * 0.5f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Glow, oldPos - Main.screenPosition, null, Color.White * 0.45f, Projectile.rotation + timer * -0.05f, Glow.Size() / 2, Projectile.scale * 0.35f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Glow, oldPos - Main.screenPosition, null, trailCol * 0.45f, Projectile.rotation + timer * -0.05f, Glow.Size() / 2, Projectile.scale * 0.15f, SpriteEffects.None, 0f);


            Main.spriteBatch.Draw(Ball, oldPos - Main.screenPosition, null, Color.White, Projectile.rotation + timer * -0.05f, Ball.Size() / 2, Projectile.scale * 0.40f * scaley, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Ball, oldPos - Main.screenPosition, null, trailCol, Projectile.rotation + timer * 0.02f, Ball.Size() / 2, Projectile.scale * 0.5f * scaley, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Ball, oldPos - Main.screenPosition, null, trailCol, Projectile.rotation + timer * 0.035f, Ball.Size() / 2, Projectile.scale * 0.5f * scaley, SpriteEffects.None, 0f);


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, null, null, null, null, Main.GameViewMatrix.TransformationMatrix);



            return false;
        }

    }
}
