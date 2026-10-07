using AerovelenceMod.Common.Bases;
using AerovelenceMod.Content.Dusts.GlowDusts;
using ReLogic.Content;
using System;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace AerovelenceMod.Content.Items.Ammo.Flares
{   
    public class ChloroFlareProj : BaseFlare
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
            flareCol = Color.LawnGreen;
            flareColIntensity = 1.5f; //Color intensity for the shader
            dustCol = Color.LawnGreen;
            lightCol = Color.LawnGreen.ToVector3() * 1.5f; //Color of light

            BaseAILogic();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            textureLocation = "Content/Items/Ammo/Flares/ChloroFlareProj";
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

            int a = Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ChloroFlareExplosion>(), 0, 0, Main.myPlayer);
            Main.projectile[a].rotation = Main.rand.NextFloat(6.28f);

            target.AddBuff(ModContent.BuffType<FlareChloro>(), 200);
        }
    }
    
    public class ChloroFlareExplosion : BaseFlareExplosion
    {
        public override void AI()
        {
            col = Color.LawnGreen;
            colMultipliter = 1.5f;
            AILogic();
        }
    }

    public class ChloroFlare : ModItem
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
			Item.shoot = ModContent.ProjectileType<ChloroFlareProj>();
			Item.rare = ItemRarities.RarePrePlant;
		}

		public override void AddRecipes()
		{
			CreateRecipe(100)
			    .AddIngredient(ItemID.Flare, 100)
			    .AddIngredient(ItemID.ChlorophyteBar, 10)
			    .Register();
		}
    }
} 
