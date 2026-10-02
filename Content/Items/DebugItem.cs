using AerovelenceMod.Common.Globals.SkillStrikes;
using AerovelenceMod.Common.Particles;
using AerovelenceMod.Common.Systems;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Dusts.GlowDusts;
using AerovelenceMod.Content.Items.Weapons.Aurora.Eos;
using AerovelenceMod.Content.Items.Weapons.CrystalCaverns.BooyahBomb;
using AerovelenceMod.Content.Items.Weapons.Ember;
using AerovelenceMod.Content.Items.Weapons.Misc.Magic.Ceroba;
using AerovelenceMod.Content.Items.Weapons.Misc.Magic.CrystalGlade;
using AerovelenceMod.Content.Items.Weapons.Misc.Magic.FlashLight;
using AerovelenceMod.Content.Items.Weapons.Misc.Magic.WandOfExploding;
using AerovelenceMod.Content.Items.Weapons.Misc.Ranged;
using AerovelenceMod.Content.Items.Weapons.Misc.Ranged.Guns;
using AerovelenceMod.Content.Items.Weapons.Misc.Ranged.Guns.Skylight;
using AerovelenceMod.Content.Items.Weapons.Starglass;
using AerovelenceMod.Content.NPCs.Bosses.Cyvercry;
using AerovelenceMod.Content.NPCs.Bosses.FeatheredFoe;
using AerovelenceMod.Content.Particles;
using AerovelenceMod.Content.Projectiles;
using AerovelenceMod.Content.Projectiles.Other;
using AerovelenceMod.Content.Projectiles.TempVFX;
using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static AerovelenceMod.Common.Utilities.DustBehaviorUtil;

namespace AerovelenceMod.Content.Items
{
    public class DebugItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("DebugItem");
            /* Tooltip.SetDefault("You shouldn't have this...\n" +
                "[i:" + ModContent.ItemType<Emoji>() + "]"); */
        }
        public override void SetDefaults()
        {
            //Item.UseSound = new SoundStyle("Terraria/Sounds/Item_122") with { Pitch = .86f, };
            Item.crit = 4;
            Item.damage = 22;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 46;
            Item.height = 28;
            Item.useTime = 7; //7
            Item.useAnimation = 7; //7
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0;
            Item.value = Item.sellPrice(0, 9, 0, 0);
            Item.rare = ItemRarityID.Orange;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EosSlash>();
            //Item.useAmmo = AmmoID.Bullet;
            Item.shootSpeed = 10f;

            Item.noUseGraphic = true;
        }

        bool tick = false;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 impactCenter = Main.MouseWorld;
            int crossCount = 6;
            for (int i = 220; i < crossCount; i++)
            {
                float dir = (MathHelper.TwoPi / (float)crossCount) * i;

                Vector2 dustVel = dir.ToRotationVector2() * Main.rand.NextFloat(4f, 10f);
                dustVel = dustVel.RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f));

                Color middleBlue = Color.Lerp(Color.DodgerBlue, Color.Blue, 0.15f + Main.rand.NextFloat(-0.15f, 0.15f));

                Dust gd = Dust.NewDustPerfect(impactCenter, ModContent.DustType<GlowPixelCross>(), dustVel, newColor: middleBlue, Scale: Main.rand.NextFloat(0.25f, 0.55f));
                gd.customData = DustBehaviorUtil.AssignBehavior_GPCBase(rotPower: 0.2f, timeBeforeSlow: 5,
                    preSlowPower: 0.94f, postSlowPower: 0.9f, velToBeginShrink: 1.5f, fadePower: 0.92f, shouldFadeColor: false);
            }

            //FlashSystem.SetCAFlashEffect(0.075f, 35, 1f, 0.35f, true, true);


            Projectile.NewProjectile(null, Main.MouseWorld, Vector2.Zero, ModContent.ProjectileType<BooyahSkillStrikeVFX>(), damage, 0, Main.myPlayer);

            //Fire Particle Example | Recommend setting debug item usetime to 1
            for (int i = 110; i < 2; i++)
            {
                float fireScale = Main.rand.NextFloat(1.35f, 1.55f);
                float alphaFade = Main.rand.NextFloat(0.94f, 0.95f);
                float scaleFade = Main.rand.NextFloat(1.03f, 1.05f);

                Color thisCol = Color.Lerp(Color.DodgerBlue, Color.Blue, 0.25f);
                Vector2 myvel = new Vector2(0f, -1.75f).RotatedByRandom(0.2f) * Main.rand.NextFloat(8f, 14f) * 1f;
                FireParticle fire1 = new FireParticle(Main.MouseWorld, myvel, fireScale * 1f, thisCol, colorMult: 2f, bloomAlpha: 1.65f, AlphaFade: alphaFade, VelFade: 0.87f, RotPower: 0.02f);
                fire1.randomRotPower = 0.5f;
                fire1.scaleFadePower = 1.05f;
                ShaderParticleHandler.SpawnParticle(fire1);
            }


            return false;
        }

    }
}
