using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Audio;
using Microsoft.Xna.Framework.Graphics;
using AerovelenceMod.Common.Utilities;
using System.Collections.Generic;
using Terraria.DataStructures;
using AerovelenceMod.Content.Dusts.GlowDusts;
using System.Linq;
using static AerovelenceMod.Common.Utilities.DustBehaviorUtil;
using static AerovelenceMod.Common.Utilities.ProjectileExtensions;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using AerovelenceMod.Common.Systems.Language;

namespace AerovelenceMod.Content.Items.Weapons.Ocean
{
    public class H2OCaliber : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("H2OCaliber", "Fires bubbles which can merge")
            .AddName(Language.Default, "H2O Caliber")
            .AddTooltip(Language.Default, "Fires bubbles which can merge")
            .AddSkillStrike(Language.Default, "Fully merged bubbles Skill Strike")

            .AddName(Language.Spanish, "Calibre H2O").AddTooltip(Language.Spanish, "Dispara burbujas que pueden fusionarse").AddSkillStrike(Language.Spanish, "Las burbujas completamente fusionadas realizan Golpes de Habilidad")
            .AddName(Language.French, "Calibre H2O").AddTooltip(Language.French, "Tire des bulles qui peuvent fusionner").AddSkillStrike(Language.French, "Les bulles totalement fusionnées déclenchent un Coup de Compétence")
            .AddName(Language.German, "H2O-Kaliber").AddTooltip(Language.German, "Feuert Blasen ab, die sich verbinden können").AddSkillStrike(Language.German, "Vollständig verschmolzene Blasen führen Fähigkeitsschläge aus")
            .AddName(Language.Italian, "Calibro H2O").AddTooltip(Language.Italian, "Spara bolle che possono fondersi").AddSkillStrike(Language.Italian, "Le bolle completamente fuse eseguono un Colpo dell'Abilità")
            .AddName(Language.Polish, "Kaliber H2O").AddTooltip(Language.Polish, "Wystrzeliwuje bąbelki, które mogą się połączyć").AddSkillStrike(Language.Polish, "Całkowicie połączone bąbelki wykonują Cios Umiejętności")
            .AddName(Language.PortugueseBrazil, "Calibre H2O").AddTooltip(Language.PortugueseBrazil, "Dispara bolhas que podem se fundir").AddSkillStrike(Language.PortugueseBrazil, "As bolhas completamente fundidas realizam um Golpe de Habilidade")
            .AddName(Language.Russian, "H2O Калибр").AddTooltip(Language.Russian, "Выпускает пузырьки, которые могут сливаться").AddSkillStrike(Language.Russian, "Полностью слияние пузырьков активирует Навык Удара")
            .AddName(Language.ChineseTraditional, "H2O 口徑").AddTooltip(Language.ChineseTraditional, "發射可以融合的氣泡").AddSkillStrike(Language.ChineseTraditional, "完全融合的氣泡觸發技能打擊")
            .AddName(Language.ChineseSimplified, "H2O 口径").AddTooltip(Language.ChineseSimplified, "发射可以融合的气泡").AddSkillStrike(Language.ChineseSimplified, "完全融合的气泡触发技能打击");
        }

        public override void SetDefaults()
        {
            Item.damage = 12;
            Item.useTime = 20;
            Item.useAnimation = 30;
            Item.reuseDelay = 40;
            Item.shootSpeed = 4;
            Item.knockBack = 2;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<H2OBubble>();

            Item.width = 44;
            Item.height = 26;
            Item.value = Item.sellPrice(0, 0, 55, 40);
            Item.rare = ItemRarities.EarlyPHM;

            Item.noMelee = true;
            Item.autoReuse = true;
            Item.noUseGraphic = true;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAmmo = AmmoID.Bullet;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            Vector2 muzzleOffset = Vector2.Normalize(new Vector2(velocity.X, velocity.Y)) * 2f;
            if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
            {
                position += muzzleOffset;
            }
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            SoundStyle style1 = new SoundStyle("Terraria/Sounds/Custom/dd2_ballista_tower_shot_0") with { Pitch = .9f, PitchVariance = .25f, MaxInstances = -1, Volume = 0.35f };
            SoundEngine.PlaySound(style1, position);

            SoundStyle style2 = SoundID.Item110 with { Volume = 0.35f, PitchVariance = 0.15f, Pitch = 0.25f };
            SoundEngine.PlaySound(style2, position);

            SoundStyle style = new SoundStyle("Terraria/Sounds/Item_5") with { Volume = .4f, Pitch = 1f, PitchVariance = 0.1f };
            SoundEngine.PlaySound(style, position);

            Projectile.NewProjectile(null, position, Vector2.Zero, ModContent.ProjectileType<H2OCaliberHeldProjectile>(), 0, 0, player.whoAmI);

            for (int i = 0; i < 3; i++)
            {
                Dust smoke = Dust.NewDustPerfect(position + velocity.SafeNormalize(Vector2.UnitX) * 15f, ModContent.DustType<HighResSmoke>(),
                    Main.rand.NextVector2CircularEdge(1f, 1f), newColor: Color.Aquamarine, Scale: Main.rand.NextFloat(0.35f, 0.5f));

                smoke.velocity += velocity.SafeNormalize(Vector2.UnitX) * 0.75f;

                smoke.customData = AssignBehavior_HRSBase(5, 20, 0.9f, 0.35f, true, 1f);
            }
            int numBubbles = 5 + Main.rand.Next(2);
            position += Vector2.Normalize(velocity) * 15f;
            float spreadAngle = 15f;
            float startAngle = -spreadAngle / 2;
            float angleStep = spreadAngle / (numBubbles - 1);

            for (int i = 0; i < numBubbles; i++)
            {
                float currentAngle = startAngle + (angleStep * i);
                Vector2 perturbedSpeed = velocity.RotatedBy(MathHelper.ToRadians(currentAngle));
                perturbedSpeed = perturbedSpeed.RotatedByRandom(MathHelper.ToRadians(3));
                perturbedSpeed = perturbedSpeed.SafeNormalize(Vector2.UnitX * player.direction) * 7f;
                float speedMultiplier = Main.rand.NextFloat(0.9f, 1.1f);
                int proj = Projectile.NewProjectile(
                    null,
                    position.X + perturbedSpeed.X * 0.5f,
                    position.Y + perturbedSpeed.Y * 0.5f,
                    perturbedSpeed.X * speedMultiplier,
                    perturbedSpeed.Y * speedMultiplier,
                    ModContent.ProjectileType<H2OBubble>(),
                    damage / 2,
                    knockback,
                    player.whoAmI);
                Main.projectile[proj].ai[2] = 1;
            }
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SandBlock, 25)
                .AddRecipeGroup("AerovelenceMod:GoldOrPlatinum", 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    public class H2OCaliberHeldProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        private bool firstFrame = false;
        public float yRecoilProgress = 0;
        public bool yRecoilDone = false;

        public ref float Angle => ref Projectile.ai[1];
        public Vector2 direction = Vector2.Zero;

        private int timer = 0;

        Player owner => Main.player[Projectile.owner];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.aiStyle = -1;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 999999;

            Projectile.DamageType = DamageClass.Ranged;

            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }
        public override bool? CanDamage() => false;

        public override bool? CanCutTiles() => false;


        public override void AI()
        {
            Player Player = Main.player[Projectile.owner];
            KillHeldProjIfPlayerDeadOrStunned(Projectile);

            if (owner.itemTime <= 1)
                Projectile.active = false;

            Projectile.velocity = Vector2.Zero;

            if (Projectile.owner == Main.myPlayer && timer == 0)
            {
                Angle = (Main.MouseWorld - Player.Center).ToRotation();
            }

            direction = Angle.ToRotationVector2();
            Player.ChangeDir(direction.X > 0 ? 1 : -1);


            if (timer == 2)
            {
                Offset = -10f;
            }
            if (timer > 2)
            {
                float easeProgress = MathHelper.Lerp(0f, 1f, Math.Clamp((timer - 3f) / 20f, 0f, 1f));
                Offset = MathHelper.Lerp(5f, 11f, Easings.easeOutQuart(easeProgress));
            }

            if (timer > 1)
            {
                if (yRecoilDone == false)
                    yRecoilProgress = Math.Clamp(MathHelper.Lerp(yRecoilProgress, 1f, 0.12f), 0, 0.3f);
                else
                    yRecoilProgress = Math.Clamp(MathHelper.Lerp(yRecoilProgress, -0.2f, 0.06f), 0, 0.3f);

                if (yRecoilProgress == 0.3f)
                    yRecoilDone = true;

                if (timer > 3)
                    glowIntensity = Math.Clamp(MathHelper.Lerp(glowIntensity, -0.20f, 0.1f), 0f, 1f);
            }

            direction = Angle.ToRotationVector2().RotatedBy(yRecoilProgress * Player.direction * -1f); ;
            Projectile.Center = Player.MountedCenter + (direction * Offset);
            Projectile.velocity = Vector2.Zero;
            Player.itemRotation = direction.ToRotation();

            if (Player.direction != 1)
                Player.itemRotation -= 3.14f;

            Player.itemRotation = MathHelper.WrapAngle(Player.itemRotation);

            Player.heldProj = Projectile.whoAmI;
            Projectile.rotation = direction.ToRotation();

            timer++;
        }

        private float Offset = 6;
        private float glowIntensity = 1f;
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D Texture = Mod.Assets.Request<Texture2D>("Content/Items/Weapons/Ocean/H2OCaliber").Value;

            Texture2D MuzzleFlash = Mod.Assets.Request<Texture2D>("Assets/MuzzleFlashes/WhitePixelMuzzleFlash").Value;
            Texture2D MuzzleFlashGlow = Mod.Assets.Request<Texture2D>("Assets/MuzzleFlashes/WhitePixelMuzzleFlashGlow").Value;

            Player Player = Main.player[Projectile.owner];
            SpriteEffects mySE = Player.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            Vector2 drawPos = Projectile.Center - Main.screenPosition + new Vector2(0f, Player.gfxOffY);

            Vector2 muzzleFlashPos = drawPos + new Vector2(20f, -1f * Player.direction).RotatedBy(Projectile.rotation);
            Vector2 muzzleFlashOrigin = new Vector2(0f, MuzzleFlash.Height / 2f);
            Main.spriteBatch.Draw(MuzzleFlashGlow, muzzleFlashPos, null, Color.White with { A = 0 } * glowIntensity * 0.5f, Projectile.rotation, muzzleFlashOrigin, Projectile.scale * glowIntensity, mySE, 0f);
            Main.spriteBatch.Draw(MuzzleFlash, muzzleFlashPos, null, Color.White * glowIntensity * 0.75f, Projectile.rotation, muzzleFlashOrigin, Projectile.scale * glowIntensity, mySE, 0f);

            Main.spriteBatch.Draw(Texture, drawPos, null, lightColor, Projectile.rotation, Texture.Size() / 2, Projectile.scale, mySE, 0f);

            return false;
        }
    }

    public class H2OBubble : ModProjectile
    {
        //ai[0] used for targeting logic
        //ai[1] used for scale
        //ai[2] used for growth level/size (1-6)
        //ai[3] used as a timer

        private float targetScale = 1f;
        public static float BubbleScale(float mass) => .25f * Math.Clamp(mass, 1f, 6f);
        private const int MAX_GROWTH_LEVEL = 6;

        public override void SetDefaults()
        {
            Projectile.width = 5;
            Projectile.height = 5;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.alpha = 128;
            Projectile.scale = .25f;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.timeLeft = 180;
            Projectile.noEnchantments = true;
            Projectile.ai[2] = 1;
        }

        public override bool? CanDamage() => Projectile.timeLeft <= 10 ? false : null;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float fade = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
            float charge = (Projectile.ai[2] - 1f) / 5f;
            Color rim = new Color(105, 235, 255, 0) * fade;
            Main.EntitySpriteDraw(glow, drawPos, null, rim * (0.035f + charge * 0.08f), 0f, glow.Size() * 0.5f,
                42f * Projectile.scale / glow.Width, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, Color.Lerp(lightColor, Color.White, 0.65f) * (fade * .5f),
                Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPos, null, rim * (0.07f + charge * 0.1f),
                Projectile.rotation, origin, Projectile.scale * 1.08f, SpriteEffects.None);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[1] == -1f || Main.dedServ) return;
            SoundEngine.PlaySound(SoundID.Item54, Projectile.position);
            for (int i = 0; i < 15; i++)
            {
                int dustIndex = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                    DustID.UnholyWater, 0f, -2f, 0, default, 0.8f);
                Main.dust[dustIndex].alpha = 100;
                Main.dust[dustIndex].velocity.X *= 1.2f;
                Main.dust[dustIndex].velocity *= 2f;
                Main.dust[dustIndex].noGravity = true;
            }

            Vector2 center = Projectile.Center;
            int radius = (int)(8 * Projectile.scale);
            int particleCount = (int)(10 * Math.Min(Projectile.ai[2], 3));

            for (int i = 0; i < particleCount; i++)
            {
                int dustIndex = Dust.NewDust(center - Vector2.One * radius, radius * 2, radius * 2, DustID.BubbleBurst_Blue);
                Dust bubbleDust = Main.dust[dustIndex];
                Vector2 direction = (bubbleDust.position - center).SafeNormalize(Vector2.UnitY);
                bubbleDust.position = center + direction * radius * Projectile.scale;
                bubbleDust.velocity = direction * Main.rand.NextFloat(2f, 5f);
                bubbleDust.color = Color.Lerp(Color.Aquamarine, Color.White, Main.rand.NextFloat(0.3f));
                bubbleDust.noGravity = true;
                bubbleDust.noLight = true;
                bubbleDust.scale = 0.5f * Projectile.scale;
                bubbleDust.alpha = 100;
            }

            if (Projectile.ai[2] >= 3)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 speed = Main.rand.NextVector2CircularEdge(1f, 1f) * 3f;
                    int dustIndex = Dust.NewDust(center, 4, 4, DustID.WaterCandle);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity = speed;
                    Main.dust[dustIndex].scale = 0.8f;
                    Main.dust[dustIndex].color = Color.Aquamarine;
                }
            }
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            targetScale = BubbleScale(Projectile.ai[2]);
            Projectile.scale = MathHelper.Lerp(Projectile.scale, targetScale, .3f);
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = Math.Max(5, (int)(20f * Projectile.scale));
            Projectile.Center = center;
            if (Projectile.owner == Main.myPlayer && Projectile.ai[2] < MAX_GROWTH_LEVEL && Projectile.ai[0] > 18f)
            {
                foreach (Projectile other in Main.ActiveProjectiles)
                {
                    if (other.whoAmI == Projectile.whoAmI || other.type != Type || other.owner != Projectile.owner || other.ai[0] <= 18f || other.ai[2] >= MAX_GROWTH_LEVEL || other.ai[1] == -1f) continue;
                    if (other.ai[2] > Projectile.ai[2] || other.ai[2] == Projectile.ai[2] && other.identity < Projectile.identity) continue;
                    float distance = (Projectile.width + other.width) * .5f + 5f;
                    if (Vector2.DistanceSquared(Projectile.Center, other.Center) > distance * distance) continue;
                    float mass = Projectile.ai[2] + other.ai[2];
                    Projectile.velocity = (Projectile.velocity * Projectile.ai[2] + other.velocity * other.ai[2]) / mass;
                    Projectile.ai[2] = Math.Min(MAX_GROWTH_LEVEL, mass);
                    Projectile.timeLeft = Math.Max(Projectile.timeLeft, other.timeLeft);
                    other.ai[1] = -1f;
                    other.netUpdate = true;
                    other.Kill();
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item54 with { Volume = .25f, Pitch = .4f }, Projectile.Center);
                    break;
                }
            }
            Projectile.velocity *= .97f - (Projectile.ai[2] - 1f) * .012f;
            Projectile.velocity.Y += MathF.Sin(Projectile.ai[0] / 14f) * .025f;
            Lighting.AddLight(Projectile.Center, new Vector3(.025f, .07f, .09f));
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 1f + ((Projectile.ai[2] - 1) * 0.15f);
            if (Projectile.ai[2] >= MAX_GROWTH_LEVEL) SkillStrikeUtil.setSkillStrike(Projectile, 1.5f);

            if (Projectile.ai[2] >= MAX_GROWTH_LEVEL)
            {
                modifiers.Knockback *= 1.5f;
                if (Main.rand.NextBool(3))
                    target.AddBuff(BuffID.Wet, 180);
            }
        }
    }
}