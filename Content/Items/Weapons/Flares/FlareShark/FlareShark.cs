using AerovelenceMod.Common;
using AerovelenceMod.Common.Bases;
using AerovelenceMod.Content.Dusts;
using AerovelenceMod.Content.Dusts.GlowDusts;
using AerovelenceMod.Content.Items.Ammo.Flares;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;

namespace AerovelenceMod.Content.Items.Weapons.Flares.FlareShark
{
    public class FlareShark : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Flareshark");
            // Tooltip.SetDefault("33% chance to not consume ammo\nShoots flares alongside bullets");
        }
        public override void SetDefaults()
        {
            Item.noUseGraphic = true;
            Item.UseSound = SoundID.Item110;
            Item.crit = 4;
            Item.damage = 11;
            Item.DamageType = DamageClass.Ranged;
            Item.width = 46;
            Item.height = 28;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 0;
            Item.value = Item.sellPrice(0, 9, 0, 0);
            Item.rare = ItemRarities.LatePHM;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<FireFlareProj>();
            Item.useAmmo = AmmoID.Flare;
            Item.shootSpeed = 13f;
        }
        public override bool CanConsumeAmmo(Item ammo, Player player)
        {
            return Main.rand.NextFloat() >= .33f;
        }
        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-2, 0);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int heldProj = Projectile.NewProjectile(null, position, Vector2.Zero, ModContent.ProjectileType<FlareSharkRecoilProjectile>(), 0, 0, player.whoAmI);

            if (Main.projectile[heldProj].ModProjectile is FlareSharkRecoilProjectile held)
            {
                held.SetProjInfo(
                    GunID: ModContent.ItemType<FlareShark>(),
                    AnimTime: 24,
                    NormalXOffset: 16f,
                    DestXOffset: -1f,
                    YRecoilAmount: 0.35f,
                    HoldOffset: new Vector2(0f, 2f),
                    TipPos: new Vector2(31f, -5f),
                    StarPos: new Vector2(22f, -5f)
                    );

                held.timeToStartFade = 1;
                held.quickFade = true; //Recommended for slower firing guns with large YRecoil
            }

            //Explosion
            int dir = velocity.X > 0 ? 1 : -1;
            Vector2 muzzlePos = position + new Vector2(32f, -3f * dir).RotatedBy(velocity.ToRotation());

            for (int i = 0; i < 11; i++) //16
            {
                Color col1 = Color.Lerp(Color.OrangeRed, Color.Orange, 0.35f);

                float progress = (float)i / 10;
                Color col = Color.Lerp(Color.Brown * 0.5f, col1 with { A = 0 }, progress);

                Dust d = Dust.NewDustPerfect(muzzlePos, ModContent.DustType<MediumSmoke>(), Velocity: Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.35f, 1f) * 1f,
                    newColor: col, Scale: Main.rand.NextFloat(0.9f, 1.5f) * 0.4f);
                d.customData = new MediumSmokeBehavior(Main.rand.Next(4, 18), 0.98f, 0.01f, 0.75f); //12 28

                d.rotation = Main.rand.NextFloat(6.28f);

                d.velocity += velocity.SafeNormalize(Vector2.UnitX) * 0.85f;
            }

            //Light Dust
            Dust softGlow = Dust.NewDustPerfect(muzzlePos, ModContent.DustType<SoftGlowDust>(), Vector2.Zero, newColor: Color.OrangeRed, Scale: 0.1f);

            softGlow.customData = DustBehaviorUtil.AssignBehavior_SGDBase(timeToStartFade: 3, timeToChangeScale: 0, fadeSpeed: 0.9f, sizeChangeSpeed: 0.95f, timeToKill: 10,
                overallAlpha: 0.1f, DrawWhiteCore: true, 1f, 1f);

            for (int i = 0; i < 2 + Main.rand.Next(0, 3); i++)
            {
                Color col1 = Color.Lerp(Color.OrangeRed, Color.Orange, 0.15f);


                Vector2 randomStart = Main.rand.NextVector2Circular(1.5f, 1.5f) * 1f;
                Dust dust = Dust.NewDustPerfect(muzzlePos, ModContent.DustType<GlowPixelCross>(), randomStart, newColor: col1, Scale: Main.rand.NextFloat(0.25f, 0.5f) * 1.5f);
                dust.noLight = false;
                dust.customData = DustBehaviorUtil.AssignBehavior_GPCBase(rotPower: 0.2f, preSlowPower: 0.99f, timeBeforeSlow: 0, postSlowPower: 0.89f,
                    velToBeginShrink: 10f, fadePower: 0.9f, shouldFadeColor: false);

                dust.velocity += velocity.SafeNormalize(Vector2.UnitX) * 2f;
            }

            return true;
        }
        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.Minishark)
                .AddIngredient(ItemID.FlareGun)
                .AddIngredient(ItemID.IllegalGunParts)
                .AddIngredient(ItemID.HellstoneBar, 15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public class FlareSharkRecoilProjectile : BaseRecoilProj
    {
        //Makes the glow on MuzzleFlash fade faster
        public bool quickFade = false;

        //Which muzzle flash texture to use
        public int muzzleFlashFrame = Main.rand.Next(0, 3);

        float pullBackRotOffsetAmount = 0f;
        bool hasDoneClickSound = false;

        public override void RecoilAI()
        {
            Player Player = Main.player[Projectile.owner];
            float goalX = GoalXOffset;
            float baseX = BaseXOffset;

            #region compositeArms

            float totalProgress = (float)timer / (float)Player.itemAnimationMax;
            bool doPullClickAnim = totalProgress >= 0.6f && totalProgress <= 0.8f;


            float armRot = GunDirection.ToRotation() - MathHelper.PiOver2;
            armRot += (-0.35f * pullBackRotOffsetAmount) * Player.direction;

            if (doPullClickAnim)
            {
                float pullProg = Utils.GetLerpValue(0.6f, 0.8f, totalProgress, true);
                pullProg = Easings.easeOutSine(pullProg);

                if (pullProg > 0.75f)
                    Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None, armRot);
                else if (pullProg > 0.5f)
                    Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, armRot);
                else if (pullProg > 0.25f)
                    Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRot);
                else
                    Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);

                if (!hasDoneClickSound && pullProg > 0.5f)
                {
                    SoundStyle style = new SoundStyle("Terraria/Sounds/Menu_Tick") with { Volume = 0.66f, Pitch = -.4f, PitchVariance = .25f, MaxInstances = -1 };
                    SoundEngine.PlaySound(style, Player.Center);
                    hasDoneClickSound = true;
                }


                pullBackRotOffsetAmount = 1f;
            }
            else
            {
                float Xprog = Utils.GetLerpValue(goalX, baseX, XOffset, true);

                if (Xprog > 0.75f)
                    Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot);
                else if (Xprog > 0.5f)
                    Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.ThreeQuarters, armRot);
                else if (Xprog > 0.25f)
                    Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Quarter, armRot);
                else
                    Player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.None, armRot);

                pullBackRotOffsetAmount = Math.Clamp(MathHelper.Lerp(pullBackRotOffsetAmount, -0.75f, 0.12f), 0f, 1f);
            }
            #endregion

            if (timer > timeToStartFade)
                muzzleFlashPower = Math.Clamp(MathHelper.Lerp(muzzleFlashPower, -0.5f, 0.15f), 0f, 1f);
        }

        public override void Draw(ref Color lightColor)
        {
            Texture2D Texture = TextureAssets.Item[gunID].Value;

            Player Player = Main.player[Projectile.owner];
            SpriteEffects mySE = Player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            Vector2 heldOffset = new Vector2(HoldoutOffset.X, HoldoutOffset.Y * Player.direction).RotatedBy(Projectile.rotation);
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Player.gfxOffY) + heldOffset;

            Color between = Color.Lerp(Color.Orange, Color.OrangeRed, 0.75f);
            Color[] colors = { between, Color.OrangeRed, Color.Orange, Color.White };

            //Muzzle Flash
            #region Muzzle Flash
            Texture2D MuzzleFlash = Mod.Assets.Request<Texture2D>("Assets/MuzzleFlashes/Sprite/MiddleMuzzleFlash").Value;
            Texture2D MuzzleFlashGlow = Mod.Assets.Request<Texture2D>("Assets/MuzzleFlashes/Sprite/MiddleMuzzleFlashGlow").Value;

            int frameHeight = MuzzleFlash.Height / 3;
            Rectangle muzzleFlashSourceRect = new Rectangle(0, frameHeight * muzzleFlashFrame, MuzzleFlash.Width, frameHeight);
            Vector2 muzzleFlashOrigin = muzzleFlashSourceRect.Size() / 2f;

            Vector2 muzzleFlashPos = drawPos + new Vector2(TipPosition.X, TipPosition.Y * Player.direction).RotatedBy(Projectile.rotation); //33 -3

            float easedMuzzleFlashAlpha = Easings.easeInSine(muzzleFlashPower);
            float muzzleFlashScale = Projectile.scale * 2f * Easings.easeOutSine(muzzleFlashPower);


            Main.spriteBatch.Draw(MuzzleFlashGlow, muzzleFlashPos + Main.rand.NextVector2Circular(3f, 3f), muzzleFlashSourceRect, colors[0] with { A = 0 } * easedMuzzleFlashAlpha * 0.75f, Projectile.rotation, muzzleFlashOrigin, muzzleFlashScale, mySE, 0f);

            Main.spriteBatch.Draw(MuzzleFlash, muzzleFlashPos, muzzleFlashSourceRect, colors[3] * easedMuzzleFlashAlpha * 1f, Projectile.rotation, muzzleFlashOrigin, muzzleFlashScale, mySE, 0f);

            float overglowAlpha = 1f * bonusPower;

            if (quickFade)
                overglowAlpha = Easings.easeInQuad(overglowAlpha);
            Main.spriteBatch.Draw(MuzzleFlashGlow, muzzleFlashPos, muzzleFlashSourceRect, colors[0] with { A = 0 } * overglowAlpha, Projectile.rotation, muzzleFlashOrigin, 3f * (1f - bonusPower), mySE, 0f);
            #endregion


            //Star on tip of gun
            Texture2D Star = CommonTextures.CrispStarPMA.Value;

            Vector2 starPos = drawPos + new Vector2(StarPosition.X, StarPosition.Y * Player.direction).RotatedBy(Projectile.rotation);

            float starRot = (float)Main.timeForVisualEffects * 0.15f * Player.direction;

            float starAlpha = 0.65f * Easings.easeInSine(bonusPower);

            Main.spriteBatch.Draw(Star, starPos, null, colors[1] with { A = 0 } * starAlpha, starRot, Star.Size() / 2, 0.4f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Star, starPos, null, colors[2] with { A = 0 } * starAlpha, starRot, Star.Size() / 2, 0.3f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(Star, starPos, null, Color.White with { A = 0 } * starAlpha, starRot, Star.Size() / 2, 0.2f, SpriteEffects.None, 0f);
        }

    }
}
