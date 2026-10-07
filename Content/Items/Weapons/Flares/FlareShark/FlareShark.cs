using AerovelenceMod.Common;
using AerovelenceMod.Common.Bases;
using AerovelenceMod.Common.Systems;
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
using static Terraria.NPC;

namespace AerovelenceMod.Content.Items.Weapons.Flares.FlareShark
{
    public class FlareShark : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Flareshark", "Heats up over time, exploding if overheated too much");
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
        public override void HoldItem(Player player)
        {
            var modPlayer = player.GetModPlayer<OverheatPlayer>();
            if (!player.controlUseItem || player.itemAnimation <= 0)
            {
                modPlayer.OverheatDecay++;
                if (modPlayer.OverheatDecay >= 5)
                {
                    modPlayer.OverheatDecay = 0;
                    modPlayer.Overheat--;
                    if (modPlayer.Overheat < 0)
                        modPlayer.Overheat = 0;
                }
            }
            if (player.controlUseItem)
            {
                modPlayer.Overheat++;
                modPlayer.OverheatDecay = 0;
                if (modPlayer.Overheat >= 300)
                {
                    Projectile.NewProjectile(null, player.Center, Vector2.Zero, ModContent.ProjectileType<OverheatExplosion>(), Item.damage * 2, 0, player.whoAmI);
                    modPlayer.Overheat = 0;
                }
            }
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

        public static Color Additive(Color color, float opacity) => color with { A = 0 } * MathHelper.Clamp(opacity, 0f, 1f);

        public override void Draw(ref Color lightColor)
        {
            Texture2D Texture = TextureAssets.Item[gunID].Value;

            Player Player = Main.player[Projectile.owner];
            SpriteEffects mySE = Player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            Vector2 heldOffset = new Vector2(HoldoutOffset.X, HoldoutOffset.Y * Player.direction).RotatedBy(Projectile.rotation);
            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Player.gfxOffY) + heldOffset;

            Texture2D Overheat = Mod.Assets.Request<Texture2D>("Content/Items/Weapons/Flares/FlareShark/FlareShark_Overheat").Value;
            Main.spriteBatch.Draw(Overheat, drawPos, null, Additive(Color.Orange, Player.GetModPlayer<OverheatPlayer>().Overheat * 0.005f), Projectile.rotation, Texture.Size() / 2, Projectile.scale, mySE, 0f);

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

    public class OverheatExplosion : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 0.1f;
            Projectile.timeLeft = 300;
            Projectile.penetrate = -1;

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }

        int timer = 0;
        bool firstFrame = true;
        float colorIntensity = 1f;

        float randomRot = 0;
        public override void AI()
        {
            if (firstFrame)
            {
                randomRot = Main.rand.NextFloat(6.28f);
                firstFrame = false;

                Projectile.rotation = Main.rand.NextFloat(6.28f);

                //Spawn Dust
                ArmorShaderData dustShader2 = new ArmorShaderData(new Ref<Effect>(Mod.Assets.Request<Effect>("Effects/GlowDustShader", AssetRequestMode.ImmediateLoad).Value), "ArmorBasic");

                for (int i = 0; i < 30; i++)
                {
                    if (Main.rand.NextBool())
                    {
                        Vector2 randomStart = Main.rand.NextVector2CircularEdge(5, 5);
                        Dust gd = GlowDustHelper.DrawGlowDustPerfect(Projectile.Center, ModContent.DustType<LineGlow>(), randomStart * Main.rand.NextFloat(0.65f, 1.35f), Color.OrangeRed, 0.15f, 0.2f, 0f, dustShader2);
                        gd.fadeIn = 45 + Main.rand.NextFloat(-3f, 14f);
                        gd.scale *= Main.rand.NextFloat(0.9f, 2.1f);
                    }
                    else
                    {
                        Vector2 randomStart = Main.rand.NextVector2CircularEdge(6, 6);
                        Dust gd = GlowDustHelper.DrawGlowDustPerfect(Projectile.Center, ModContent.DustType<GlowCircleFlare>(), randomStart * Main.rand.NextFloat(0.65f, 1.35f), Color.Orange, 0.7f, 0.1f, 0f, dustShader2);
                        gd.fadeIn = 1;
                    }
                }

            }

            Projectile.scale = Math.Clamp(MathHelper.Lerp(Projectile.scale, 0.55f, 0.5f), 0f, 2f);

            Projectile.velocity = Vector2.Zero;

            if (timer > 10)
            {
                colorIntensity -= 0.12f;
                if (colorIntensity <= 0)
                    Projectile.active = false;
            }
            Projectile.rotation += 0.06f;

            timer++;
        }

        //OrangeRed, Orange, Gold, Gold, Wheat, White
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D sixStar = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Assets/Flare/star_05");
            Texture2D circle = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Weapons/Ember/MagmaBall");
            Texture2D circle2 = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Content/NPCs/Bosses/Cyvercry/Textures/circle_05");
            Texture2D color = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Weapons/Ember/color_burst_30");

            ModContent.GetInstance<AdditivePixelationSystem>().QueueRenderAction(RenderLayer.Dusts, () =>
            {
                Main.spriteBatch.Draw(circle2, Projectile.Center - Main.screenPosition, null, Color.Black * 0.85f * colorIntensity, Projectile.rotation * 1.5f, circle2.Size() / 2, Projectile.scale * 0.75f, 0, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);

                Main.spriteBatch.Draw(circle2, Projectile.Center - Main.screenPosition, null, Color.OrangeRed * 0.7f * colorIntensity, Projectile.rotation * 2f, sixStar.Size() / 2, Projectile.scale * 0.5f, 0, 0f);

                Main.spriteBatch.Draw(sixStar, Projectile.Center - Main.screenPosition, null, Color.OrangeRed * colorIntensity, Projectile.rotation, sixStar.Size() / 2, Projectile.scale * 0.75f, 0, 0f);
                Main.spriteBatch.Draw(circle, Projectile.Center - Main.screenPosition, null, Color.Orange * colorIntensity, Projectile.rotation, circle.Size() / 2, Projectile.scale * 0.17f, 0, 0f);

                Main.spriteBatch.Draw(color, Projectile.Center - Main.screenPosition, null, Color.Gold * colorIntensity, randomRot, color.Size() / 2, Projectile.scale * 0.5f, 0, 0f);
                Main.spriteBatch.Draw(circle2, Projectile.Center - Main.screenPosition, null, Color.Wheat * 1f * colorIntensity, Projectile.rotation * 1.5f, circle2.Size() / 2, Projectile.scale * 0.5f, 0, 0f);
                Main.spriteBatch.Draw(circle2, Projectile.Center - Main.screenPosition, null, Color.White * 1f * colorIntensity, Projectile.rotation * 1.5f, circle2.Size() / 2, Projectile.scale * 0.25f, 0, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            });

            return false;
        }
    }

    public class OverheatPlayer : ModPlayer
    {
        public int Overheat = 0;

        public int OverheatDecay = 0;
    }
}
