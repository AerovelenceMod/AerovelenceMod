using AerovelenceMod.Common.Bases;
using AerovelenceMod.Content.Dusts.GlowDusts;
using ReLogic.Content;
using System;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace AerovelenceMod.Content.Items.Ammo.Flares
{   
    public class CursedFlareProj : BaseFlare
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.scale = 1f;
            Projectile.timeLeft = 600;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;

        }
        public override void AI()
        {
            flareCol = Color.Green;
            dustCol = Color.Green;
            lightCol = Color.Green.ToVector3() * 1.5f; //Color of light
            BaseAILogic();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            textureLocation = "Content/Items/Ammo/Flares/CursedFlareProj";
            BaseDrawing();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            noSound = true;
            SoundStyle style = new SoundStyle("Terraria/Sounds/Custom/dd2_betsy_fireball_shot_1") with { Pitch = -.53f, };
            SoundEngine.PlaySound(style, Projectile.Center);
            KillDust();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            noSound = true;
            HitDust();

            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;

            if (Projectile.owner == Main.myPlayer)
            {
                int a = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CursedFlareExplosion>(), 0, 0, Main.myPlayer);
                Main.projectile[a].rotation = Main.rand.NextFloat(6.28f);
                Main.projectile[a].netUpdate = true;
            }

            target.AddBuff(ModContent.BuffType<FlareCursed>(), 200);
            target.AddBuff(BuffID.CursedInferno, 20);
        }
    }
    
    public class CursedFlareExplosion : BaseFlareExplosion
    {
        public override void AI()
        {
            col = Color.Green;
            colMultipliter = 2;
            AILogic();
        }
    }

    public class CursedFlareReplacer : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstatiation)
        {
            return item.type == ItemID.CursedFlare;
        }

        public override void SetDefaults(Item item)
        {
            item.StatsModifiedBy.Add(Mod);
            item.shoot = ModContent.ProjectileType<CursedFlareProj>();
            item.DamageType = DamageClass.Summon;
        }
    }
} 
