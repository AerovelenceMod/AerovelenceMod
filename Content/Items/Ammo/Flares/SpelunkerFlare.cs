using AerovelenceMod.Common.Bases;
using AerovelenceMod.Content.Dusts.GlowDusts;
using ReLogic.Content;
using System;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace AerovelenceMod.Content.Items.Ammo.Flares
{
    public class SpelunkerFlareProj : BaseFlare
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
            flareCol = Color.Yellow;
            flareColIntensity = 1.5f; //Color intensity for the shader
            dustCol = Color.Yellow;
            lightCol = Color.Yellow.ToVector3() * 1.5f;

            BaseAILogic();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            textureLocation = "Content/Items/Ammo/Flares/SpelunkerFlareProj";
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
                int a = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<SpelunkerFlareExplosion>(), 0, 0, Main.myPlayer);
                Main.projectile[a].rotation = Main.rand.NextFloat(6.28f);
                Main.projectile[a].netUpdate = true;
            }
            if (Main.rand.NextBool(5))
                Main.player[Projectile.owner].AddBuff(BuffID.Spelunker, 300);
            target.AddBuff(ModContent.BuffType<FlareSpelunker>(), 200);
        }
    }

    public class SpelunkerFlareExplosion : BaseFlareExplosion
    {
        public override void AI()
        {
            col = Color.Yellow;
            colMultipliter = 1.5f;
            AILogic();
        }
    }

    public class SpelunkerFlareReplacer : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstatiation)
        {
            return item.type == ItemID.SpelunkerFlare;
        }

        public override void SetDefaults(Item item)
        {
            item.StatsModifiedBy.Add(Mod);
            item.shoot = ModContent.ProjectileType<SpelunkerFlareProj>();
            item.DamageType = DamageClass.Summon;
        }
    }
}