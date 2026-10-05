using ReLogic.Content;
using Terraria.Graphics.Shaders;
using AerovelenceMod.Common.Bases;
using System;
using AerovelenceMod.Content.Dusts.GlowDusts;
using Terraria.GameContent;
using Terraria.Audio;
using AerovelenceMod.Content.Dusts;
using static Terraria.ModLoader.PlayerDrawLayer;
using AerovelenceMod.Content.Items.Weapons.Aurora.DeepFreeze;
using XPT.Core.Audio.MP3Sharp.Decoding.Decoders.LayerIII;

namespace AerovelenceMod.Content.Items.Ammo.Flares
{
    public class FrostFlareProj : BaseFlare
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
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;

        }


        public override void AI()
        {
            flareCol = Color.DeepSkyBlue;
            flareColIntensity = 1.5f; //Color intensity for the shader
            dustCol = Color.DeepSkyBlue;
            lightCol = Color.SkyBlue.ToVector3() * 1f; //Color of light

            baseAILogic();

            int modulo = timer < 60 ? 18 : 24;
            if (timer % modulo == 0 && timer != 0)
            {
                int a = Projectile.NewProjectile(null, Projectile.Center, new Vector2(0, 2).RotatedBy(Main.rand.NextFloat(-0.25f, 0.25f)), ModContent.ProjectileType<FrostFlareIcicle>(), Projectile.damage / 2, 0, Main.myPlayer);

                int b = Projectile.NewProjectile(null, Projectile.Center, new Vector2(0, 0.5f).RotatedBy(Main.rand.NextFloat(-0.25f, 0.25f)), ModContent.ProjectileType<DeepFreezeProj>(), 0, 0, Main.myPlayer);
                if (Main.projectile[b].ModProjectile is DeepFreezeProj p)
                {
                    p.multiplier = 1.5f;
                    p.size = 0.25f;
                }

            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            textureLocation = "Content/Items/Ammo/Flares/FrostFlareProj";
            baseDrawing();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            noSound = true;
            SoundStyle style = new SoundStyle("Terraria/Sounds/Custom/dd2_betsy_fireball_shot_1") with { Pitch = -.53f, PitchVariance = 0.2f };
            SoundEngine.PlaySound(style, Projectile.Center);
            KillDust();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            noSound = true;
            HitDust();

            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI; ;

            SoundStyle style2 = new SoundStyle("AerovelenceMod/Sounds/Effects/FlareImpact") with { Volume = 0.3f, PitchVariance = 0.1f };
            SoundEngine.PlaySound(style2, Projectile.Center);

            SoundStyle style = new SoundStyle("Terraria/Sounds/Item_45") with { Pitch = .75f, PitchVariance = 0.2f };
            SoundEngine.PlaySound(style, Projectile.Center);

            SoundStyle style3 = new SoundStyle("Terraria/Sounds/Custom/deerclops_ice_attack_0") with { Volume = .1f, Pitch = .81f, PitchVariance = 0.34f };
            SoundEngine.PlaySound(style3, Projectile.Center);

            int b = Projectile.NewProjectile(null, Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DeepFreezeProj>(), 0, 0, Main.myPlayer);
            if (Main.projectile[b].ModProjectile is DeepFreezeProj p2)
            {
                p2.multiplier = 1f;
                p2.size = 0.35f;
            }
            Main.projectile[b].scale = 0.1f;

            int a = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<FrostFlareExplosion>(), 0, 0, Main.myPlayer);
            Main.projectile[a].rotation = Main.rand.NextFloat(6.28f);


            target.AddBuff(ModContent.BuffType<FlareFrostburn>(), 200);


            ArmorShaderData dustShader = new ArmorShaderData(new Ref<Effect>(Mod.Assets.Request<Effect>("Effects/GlowDustShader", AssetRequestMode.ImmediateLoad).Value), "ArmorBasic");
            for (int i = 0; i < 3; i++)
            {
                Dust p = GlowDustHelper.DrawGlowDustPerfect(target.Center, ModContent.DustType<GlowCircleRise>(),
                    Main.rand.NextVector2Circular(5, 5), Color.DeepSkyBlue, Main.rand.NextFloat(0.4f, 0.7f), 0.8f, 0f, dustShader);
                p.alpha = 0;
            }
        }
    }

    public class FrostFlareExplosion : BaseFlareExplosion
    {
        public override void AI()
        {
            col = Color.DeepSkyBlue;
            colMultipliter = 2f;
            aiLogic();
        }
    }

    public class FrostFlareIcicle : ModProjectile
    {
        int timer = 0;
        int frame = Main.rand.Next(3);

        float alpha = 0;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Frost Spike");
            ProjectileID.Sets.TrailingMode[Projectile.type] = 1;
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 12;

        }

        public override void SetDefaults()
        {
            Projectile.width = 25;
            Projectile.height = 25;
            Projectile.timeLeft = 320;
            Projectile.penetrate = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.scale = 0.7f;
        }


        public override void AI()
        {
            if (timer % 12 == 0)
            {
                int penis = GlowDustHelper.DrawGlowDust(Projectile.position, Projectile.width, Projectile.height, ModContent.DustType<GlowCircleDust>(), Color.DeepSkyBlue, 0.5f * Main.rand.NextFloat(0.7f, 1.3f), 0.55f, 0f, new ArmorShaderData(new Ref<Effect>(Mod.Assets.Request<Effect>("Effects/GlowDustShader", AssetRequestMode.ImmediateLoad).Value), "ArmorBasic"));
                Main.dust[penis].noLight = true;
            }
            if (timer % 8 == 0)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.AncientLight, 0f, 0f, 255, Scale: 0.85f);
                dust.noGravity = true;
                dust.color = Color.AliceBlue;

            }

            alpha = Math.Clamp(MathHelper.Lerp(alpha, 1.35f, 0.1f), 0, 1);

            //terminal velocity is 23
            Projectile.velocity.Y += 0.03f;
            Projectile.velocity.Y = Math.Abs(Math.Clamp(Projectile.velocity.Y * 1.03f, -23, 23));
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            timer++;
        }

        public override bool PreDraw(ref Color lightColor)
        {

            Texture2D texture = Mod.Assets.Request<Texture2D>("Content/Items/Ammo/Flares/FrostFlareIcicle").Value;

            SpriteEffects spriteEffects = Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor * alpha, Projectile.rotation, texture.Size() / 2, Projectile.scale, spriteEffects, 0.0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, null, null, null, null, Main.GameViewMatrix.TransformationMatrix);
            for (int j = 0; j < 10; j++)
            {
                float intensity = j == 0 ? 0.7f : 0.2f;

                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, Color.SkyBlue * alpha * intensity, Projectile.rotation, texture.Size() / 2, Projectile.scale, spriteEffects, 0);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.GameViewMatrix.TransformationMatrix);


            return false;
        }

        public override void OnKill(int timeLeft)
        {

            SoundStyle style = new SoundStyle("Terraria/Sounds/Custom/deerclops_ice_attack_0") with { Volume = .05f, Pitch = .81f, PitchVariance = 0.34f };
            SoundEngine.PlaySound(style, Projectile.Center);
            SoundStyle style2 = new SoundStyle("Terraria/Sounds/Item_107") with { Volume = .29f, Pitch = .81f, PitchVariance = 0.2f };
            SoundEngine.PlaySound(style2, Projectile.Center);

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.AncientLight, 0f, 0f, 255, Scale: 0.8f);
                dust.noGravity = true;
                dust.velocity *= 2;
                dust.color = Color.SkyBlue;
            }

        }
    }
    
    public class FrostFlare : ModItem
    {
		public override void SetDefaults() 
        {
			Item.width = 20;
			Item.height = 14;
			Item.damage = 5;
			Item.knockBack = 1.5f;
            Item.shootSpeed = 6;
			Item.consumable = true;
            Item.DamageType = DamageClass.Summon;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(copper: 1);
			Item.ammo = AmmoID.Flare;
			Item.shoot = ModContent.ProjectileType<FrostFlareProj>();
			Item.rare = ItemRarities.EarlyPHM;
		}

		public override void AddRecipes()
		{
			CreateRecipe(10)
			    .AddIngredient(ItemID.Flare, 10)
			    .AddIngredient(ItemID.IceBlock)
			    .Register();
		}
    }
}
