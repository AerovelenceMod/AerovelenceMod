/*using AerovelenceMod.Common.Globals.SkillStrikes;
using AerovelenceMod.Content.Dusts;
using AerovelenceMod.Content.Items.Weapons.AreaPistols.ErinGun;


using ReLogic.Content;
using System;
using System.Collections.Generic;

using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;



namespace AerovelenceMod.Content.Items.Weapons.AreaPistols
{
    public class AmmoUI : ModProjectile
    {
        #region unimportant
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("AmmoUI");
        }

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
        }

        public override bool? CanDamage()
        {
            return false;
        }
        #endregion

        //This one projectile is used for every time of weapon,
        //so we need to know which one in order to set things like
        //max ammo count correctly
        public enum whatWeaponEnum
        {
            ErinGun = 0,
        }
        public whatWeaponEnum whatWeapon;

        public float MaxAmmo = 0f;

        //old
        //List containing the number of bullets
        //It is a list instead of an array not because the length will dynamically change based on consumed ammo,
        //but because the max ammo will be different for each weapon
        public List<Bullet> Bullets = new List<Bullet>();

        public bool createdBullets = false;

        public float activeBulletsCount = 0f;


        //also old
        public float halfCurrentBullets = 0f;
        public float currentBullets = 0f;


        public List<bool> areActive = new List<bool>();

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            //Vector2 move = (owner.Center) - Projectile.Center;
            //float scalespeed = 10; //(timer < 20 ? 3f : 7); //5

            //Projectile.velocity.X = (Projectile.velocity.X + move.X) / 20f * scalespeed;
            //Projectile.velocity.Y = (Projectile.velocity.Y + move.Y) / 20f * scalespeed;

            Projectile.Center = owner.Center;

			if (!createdBullets)
			{
				switch (whatWeapon)
				{
					case whatWeaponEnum.ErinGun:
						MaxAmmo = 12;

                        for (int i = 0; i < MaxAmmo; i++)
                        {
                            areActive.Add(true);
                        }

                        break;
                    default:
                        break;
                }

                createdBullets = true;
            }

            switch (whatWeapon)
            {
                case whatWeaponEnum.ErinGun:

                    if (owner.inventory[owner.selectedItem].type != ModContent.ItemType<AntiquePistol>())
                    {
                        Projectile.active = false;
                    }

                    currentBullets = owner.GetModPlayer<AmmoPlayer>().ErinAmmoCount;
                    halfCurrentBullets = currentBullets / 2;

                    float tempCurrentBullets = currentBullets;

                    for (int j = 0; j < areActive.Count; j++)
                    {
                        if (tempCurrentBullets > 0)
                        {
                            areActive[j] = false;
                        }
                        else
                        {
                            areActive[j] = true;
                        }
                    }

                    break;
                default:
                    Main.NewText("not set"); //Debug message
                    break;
            }

        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D BulletTex = Mod.Assets.Request<Texture2D>("Content/Items/Weapons/AreaPistols/AmmoUIBullet").Value;
            Texture2D BulletOpen = Mod.Assets.Request<Texture2D>("Content/Items/Weapons/AreaPistols/AmmoOpen").Value;
            Texture2D BulletUsed = Mod.Assets.Request<Texture2D>("Content/Items/Weapons/AreaPistols/AmmoUsed").Value;

			for (int i = 0; i < areActive.Count; i++)
            {
				if (areActive[i])
                {
					Main.spriteBatch.Draw(BulletOpen, Main.player[Projectile.owner].Center - Main.screenPosition + new Vector2(15f * i, -40) - new Vector2(0, 10 - Main.player[Projectile.owner].gfxOffY), new Rectangle(0, 0, BulletOpen.Width, BulletOpen.Height), Color.White, Projectile.rotation, BulletOpen.Size() / 2, 1 * Projectile.scale, SpriteEffects.None, 0f);
				}
				else
                {
					Main.spriteBatch.Draw(BulletUsed, Main.player[Projectile.owner].Center - Main.screenPosition + new Vector2(15f * i, -40) - new Vector2(0, 10 - Main.player[Projectile.owner].gfxOffY), new Rectangle(0, 0, BulletUsed.Width, BulletUsed.Height), Color.White, Projectile.rotation, BulletUsed.Size() / 2, 1 * Projectile.scale, SpriteEffects.None, 0f);
				}
			}
			
			return false;
		}
	}

    //This class is for storing the placement of drawn bullets of the UI
    public class Bullet
    {
        //General variables 
        public Vector2 velocity;
        public Vector2 center;
        public float rotation;
        public Color color;
        public float scale;
        public int timer;

        //Whether an ammo slot has been consumed or not
        public bool usedUp;
        public Bullet(Vector2 pos, Vector2 vel)
        {
            center = pos;
            velocity = vel;
            scale = 1f;
            rotation = 0f;

            usedUp = false;
        }

        public void Update(Vector2 goal)
        {
            center = goal;
            timer++;
        }

		public void Draw(SpriteBatch sb, Texture2D tex, Player player)
		{
			if (!usedUp)
				sb.Draw(tex, center - Main.screenPosition - new Vector2(0, player.gfxOffY), null, Color.White, rotation, tex.Size() / 2, scale, SpriteEffects.None, 0f);
		}
	}
}*/