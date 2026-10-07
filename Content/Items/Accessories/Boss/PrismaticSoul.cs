using AerovelenceMod.Common;
using AerovelenceMod.Common.Systems;



using System.Collections.Generic;

using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics;



namespace AerovelenceMod.Content.Items.Accessories.Boss
{
    public class PrismaticSoul : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Prismatic Soul", "Doubles health restored from healing potions\nVastly reduces Potion Sickness duration\nHealing potions now take 5 seconds to consume, preventing movement\nGetting hit cancels your heal\nDisables natural life regen");

            Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(5, 7));
            ItemID.Sets.ItemNoGravity[Item.type] = true;

            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.expert = true;
            Item.accessory = true;
            Item.rare = ItemRarityID.Expert;
            Item.value = Item.sellPrice(0, 2, 50);
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.lifeRegen < 0)
            {
                player.lifeRegen = 0;
            }
            player.lifeRegenTime = 0;
            player.GetModPlayer<PrismaticSoulPlayer>().prismaticSoul = true;
        }
    }

    public class PrismaticSoulPlayer : ModPlayer
    {
        public bool prismaticSoul;

        public override void ResetEffects()
        {
            prismaticSoul = false;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (Player.HasBuff(ModContent.BuffType<PrismaticSoulBuff>()))
            {
                Player.ClearBuff(ModContent.BuffType<PrismaticSoulBuff>());
                SoundEngine.PlaySound(new SoundStyle("AerovelenceMod/Sounds/Effects/star_impact_01"), Player.Center);
            }
        }
    }

    public class PrismaticSoulItem : GlobalItem
    {
        public override bool? UseItem(Item item, Player player)
        {
            PrismaticSoulPlayer modPlayer = player.GetModPlayer<PrismaticSoulPlayer>();
            if (item.healLife > 0)
            {
                if (modPlayer.prismaticSoul)
                {
                    item.healLife *= 0;
                    player.ClearBuff(BuffID.PotionSickness);
                    int potionSickness = 600;
                    if (player.pStone == true)
                    {
                        potionSickness = 450;
                    }
                    player.AddBuff(BuffID.PotionSickness, potionSickness);
                    player.AddBuff(ModContent.BuffType<PrismaticSoulBuff>(), 300);
                }
                return true;
            }
            return base.UseItem(item, player);
        }
    }

    public class PrismaticSoulBuff : ModBuff
    {
        public override string Texture => "AerovelenceMod/Content/Items/Pets/FriendOfTheCaverns";

        public int Duration;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            Item item = new();

            if (player.velocity.X < 0)
            {
                player.velocity.X = 0;
            }
            if (player.velocity.Y < 0)
            {
                player.velocity.Y = 0;
            }

            if (player.ownedProjectileCounts[ModContent.ProjectileType<PrismaticSoulVFX>()] <= 0 && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.Zero, ModContent.ProjectileType<PrismaticSoulVFX>(), 0, 0, player.whoAmI);
            }

            Duration++;
            if (Duration > 299)
            {
                SoundEngine.PlaySound(new SoundStyle("AerovelenceMod/Sounds/Effects/energyClick"), player.Center);
                int heal = (int)((float)item.healLife * 2f);
                ItemLoader.GetHealLife(item, player, false, ref heal);
                Duration = 0;
            }
        }
    }

    public class PrismaticSoulVFX : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public int auraSize = 100;

        public int timer = 0;

        public Vector2 oldPos = Vector2.Zero;

        public override void SetDefaults()
        {
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.width = 20;
            Projectile.height = 20;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || !player.HasBuff(ModContent.BuffType<PrismaticSoulBuff>()))
            {
                Projectile.Kill();
                return;
            }
            if (player.dead)
                player.ClearBuff(ModContent.BuffType<PrismaticSoulBuff>());
            Projectile.position = player.Center;
            timer++;
            auraSize--;
            if (auraSize >= 100)
            {
                var tracker = new ProjectileAudioTracker(Projectile);
                SoundEngine.PlaySound(new SoundStyle("AerovelenceMod/Sounds/Effects/SwooshySwoosh"), Projectile.position, soundInstance => tracker.IsActiveAndInGame());
            }
            if (auraSize <= 0)
            {
                auraSize = 100;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D Ball = CommonTextures.flare_12.Value;

            float scaley = 0.5f;

            ModContent.GetInstance<AdditivePixelationSystem>().QueueRenderAction(RenderLayer.Dusts, () =>
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, Main.GameViewMatrix.EffectMatrix);

                Main.spriteBatch.Draw(Ball, oldPos - Main.screenPosition, null, Color.White, Projectile.rotation + timer * -0.05f, Ball.Size() / 2 * auraSize, Projectile.scale * 0.40f * scaley, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(Ball, oldPos - Main.screenPosition, null, Color.DodgerBlue, Projectile.rotation + timer * 0.02f, Ball.Size() / 2 * auraSize, Projectile.scale * 0.5f * scaley, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(Ball, oldPos - Main.screenPosition, null, Color.DodgerBlue, Projectile.rotation + timer * 0.035f, Ball.Size() / 2 * auraSize, Projectile.scale * 0.5f * scaley, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, AdditivePixelationSystem.AdditiveBlend, null, null, null, null, Main.GameViewMatrix.EffectMatrix);
            });
            return false;
        }
    }
}
