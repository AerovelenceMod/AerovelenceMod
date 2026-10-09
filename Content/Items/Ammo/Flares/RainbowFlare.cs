using AerovelenceMod.Common.Bases;
using AerovelenceMod.Common.Drawing;
using AerovelenceMod.Content.Dusts.GlowDusts;
using ReLogic.Content;
using System;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace AerovelenceMod.Content.Items.Ammo.Flares
{
    public class RainbowFlareProj : BaseFlare
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Summon;

            Projectile.width = 15; //8-20
            Projectile.height = 15;
            Projectile.scale = 1f;
            Projectile.timeLeft = 600;

            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            flareCol = ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects);
            dustCol = ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects);
            lightCol = ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects).ToVector3() * 1.5f;

            BaseAILogic();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            textureLocation = "Content/Items/Ammo/Flares/RainbowFlareProj";
            Texture2D CenterTex = Mod.Assets.Request<Texture2D>(textureLocation).Value;
            Main.spriteBatch.Draw(CenterTex, Projectile.Center - Main.screenPosition, CenterTex.Frame(1, 1, 0, 0), ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects) * alpha, Projectile.rotation, CenterTex.Size() / 2, Projectile.scale * 0.75f, SpriteEffects.None, 0f);

            BaseDrawing();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            noSound = true;
            SoundStyle style = new SoundStyle("Terraria/Sounds/Custom/dd2_betsy_fireball_shot_2") with { Pitch = -.53f, };
            SoundEngine.PlaySound(style, Projectile.Center);
            KillDust();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            noSound = true;
            HitDust();

            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;

            SoundStyle style2 = new SoundStyle("AerovelenceMod/Sounds/Effects/FlareImpact") with { Volume = 0.5f, PitchVariance = 0.1f };
            SoundEngine.PlaySound(style2, Projectile.Center);

            SoundStyle style = new SoundStyle("Terraria/Sounds/Item_45") with { Pitch = .75f, PitchVariance = 0.2f };
            SoundEngine.PlaySound(style, Projectile.Center);

            if (Projectile.owner == Main.myPlayer)
            {
                int a = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<RainbowFlareExplosion>(), 0, 0, Main.myPlayer);
                Main.projectile[a].rotation = Main.rand.NextFloat(6.28f);
                Main.projectile[a].netUpdate = true;
            }

            target.AddBuff(ModContent.BuffType<FlareRainbow>(), 200);
        }
    }

    public class RainbowFlareExplosion : BaseFlareExplosion
    {
        public override void AI()
        {
            col = ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects);
            AILogic();
        }
    }

    public class RainbowFlareReplacer : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstatiation)
        {
            return item.type == ItemID.RainbowFlare;
        }

        public override void SetDefaults(Item item)
        {
            item.StatsModifiedBy.Add(Mod);
            item.shoot = ModContent.ProjectileType<RainbowFlareProj>();
            item.DamageType = DamageClass.Summon;
        }
    }
}