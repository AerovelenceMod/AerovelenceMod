using AerovelenceMod.Common.Globals.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Shaders;
using ReLogic.Content;
using AerovelenceMod.Content.Dusts.GlowDusts;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Items.Weapons.BossDrops.Cyvercry;
using AerovelenceMod.Content.Projectiles;
using System;
using Terraria.GameContent.Bestiary;

namespace AerovelenceMod.Content.NPCs.Bosses.Cyvercry //Change me
{
    public class LaserExplosionBall : ModProjectile
    {
        //Used in PinkClone
        public float rotationOffset = 0f;
        public int stretchLaserAccelTime = 200;
        public float stretchLaserAccelStrength = 1.01f;
        public int stretchLaserTimeLeft = 400;

        public int numberOfLasers = 12;
        public int projType = ModContent.ProjectileType<CyverLaser>();
        public float vel = 5;
        public bool burstFX = true;

        public int projTimeLeft = -1;
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Energy Ball");
            Main.projFrames[Projectile.type] = 7;
        }
        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 42;
            Projectile.timeLeft = 1;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 54;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            //projectile.netImportant = true;
        }
        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }

        public int CyverIndex = 0;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, (255 - Projectile.alpha) * 0.9f / 255f, (255 - Projectile.alpha) * 0.5f / 255f, (255 - Projectile.alpha) * 0.7f / 255f);
            Projectile.rotation = 0;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }
            Projectile.velocity *= 0.9f;

            if (burstFX && Projectile.timeLeft <= 20)
            {
                scale = MathHelper.Lerp(0f, 1f, Easings.easeOutBack(Projectile.timeLeft / 20f)) * 1.03f;
                //scale -= 0.08f; //MathHelper.Lerp(0f, 1f, Easings.easeInQuint(Projectile.timeLeft / 10f));
                Projectile.scale = scale;
            }
        }
        public override void OnKill(int timeLeft)
        {
            var entitySource = Projectile.GetSource_FromAI();

            SoundEngine.PlaySound(SoundID.Item94 with { Pitch = 0.4f, Volume = 0.35f, PitchVariance = 0.2f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item91 with { Pitch = 0.4f, PitchVariance = 0.2f }, Projectile.Center);
            SoundStyle style = new SoundStyle("Terraria/Sounds/Custom/dd2_explosive_trap_explode_1") with { PitchVariance = .16f, Volume = 0.8f, Pitch = 0.7f };
            SoundEngine.PlaySound(style, Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {

                for (int i = 0; i < 360; i += 360 / numberOfLasers)
                {
                    //For Ball Dash
                    bool aimToPlayer = projType == ModContent.ProjectileType<EnergyBall>();
                    Player player = Main.player[(int)Projectile.ai[0]];
                    Vector2 toPlayer = (player.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);

                    NPC cyver = Main.npc[CyverIndex];
                    int damage = (cyver.ModNPC as Cyvercry).GetDamage("BallDash");

                    int proj = 0;
                    if (aimToPlayer) 
                        proj = Projectile.NewProjectile(entitySource, Projectile.Center, toPlayer.RotatedBy(MathHelper.ToRadians(i) + rotationOffset) * vel, projType, damage, 0);
                    else
                        proj = Projectile.NewProjectile(entitySource, Projectile.Center, new Vector2(vel, 0).RotatedBy(MathHelper.ToRadians(i) + rotationOffset), projType, damage, 0);

                    if (Main.projectile[proj].ModProjectile is StretchLaser laser)
                    {
                        Main.projectile[proj].timeLeft = stretchLaserTimeLeft;
                        laser.accelerateTime = stretchLaserAccelTime;
                        laser.accelerateStrength = stretchLaserAccelStrength;                    
                    }

                    if (projTimeLeft > 0)
                        Main.projectile[proj].timeLeft = projTimeLeft;

                }
            }

            base.OnKill(timeLeft);
        }

        float scale = 1f;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D glow = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/feather_circle128PMA");
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.DeepPink with { A = 0 } * 0.7f, Projectile.rotation, glow.Size() / 2, Projectile.scale * 0.6f * scale, 0, 0f);
            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.HotPink with { A = 0 } * 0.7f, Projectile.rotation, glow.Size() / 2, Projectile.scale * 0.45f * scale, 0, 0f);

            Texture2D BallTexture = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Content/NPCs/Bosses/Cyvercry/LaserExplosionBall").Value;

            int frameHeight = BallTexture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, BallTexture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Main.spriteBatch.Draw(BallTexture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White * 0.7f, Projectile.rotation, origin, Projectile.scale * scale, 0, 0f);
            Main.spriteBatch.Draw(BallTexture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.HotPink with { A = 0 } * 0.8f, Projectile.rotation, origin, Projectile.scale * scale, 0, 0f);

            Main.spriteBatch.Draw(glow, Projectile.Center - Main.screenPosition, null, Color.White with { A = 0 } * 0.35f, Projectile.rotation, glow.Size() / 2, Projectile.scale * 0.35f * scale, 0, 0f);


            return false;
        }
    }
}
