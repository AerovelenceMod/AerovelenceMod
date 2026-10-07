using AerovelenceMod.Content.Dusts.GlowDusts;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader.IO;

namespace AerovelenceMod.Content.Items.Tools
{
    public class CrystallineDynamite : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Crystalline Dynamite", "A reusable stick of dynamite\nThe blast scatters crystal fragments that remagnetize toward you\nReforms 30 seconds after being thrown")
                .AddName(Language.Spanish, "Dinamita Cristalina")
                .AddTooltip(Language.Spanish, "Una dinamita reutilizable\nLa explosión dispersa fragmentos que vuelven a magnetizarse hacia ti\nSe reforma 30 segundos después de lanzarse");
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.Dynamite);
            Item.maxStack = 1;
            Item.consumable = false;
            Item.rare = ItemRarities.MidPHM;
            Item.value = Item.sellPrice(gold: 2);
            Item.shoot = ProjectileID.Dynamite;
        }

        public override bool CanUseItem(Player player)
        {
            return player.GetModPlayer<CrystallineDynamitePlayer>().Cooldown <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CrystallineDynamitePlayer state = player.GetModPlayer<CrystallineDynamitePlayer>();
            state.Cooldown = CrystallineDynamitePlayer.CooldownDuration;

            int index = Projectile.NewProjectile(source, position, velocity, ProjectileID.Dynamite, damage, knockback, player.whoAmI);
            if (index >= 0 && index < Main.maxProjectiles)
            {
                Main.projectile[index].GetGlobalProjectile<CrystallineDynamiteGlobalProjectile>().Crystalline = true;
                Main.projectile[index].netUpdate = true;
            }

            if (!Main.dedServ)
            {
                CrystallineDynamiteVFX.Burst(player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, velocity.ToRotation() - MathHelper.PiOver2), 6, 1.8f);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.12f, Pitch = 0.45f, PitchVariance = 0.08f }, player.Center);
            }

            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            if (!Main.gameMenu)
            {
                int cooldown = Main.LocalPlayer.GetModPlayer<CrystallineDynamitePlayer>().Cooldown;
                if (cooldown > 0)
                {
                    float seconds = cooldown / 60f;
                    tooltips.Add(new TooltipLine(Mod, "CrystallineCooldown", $"Reforming: {seconds:0.0}s") { OverrideColor = CrystallineDynamiteVFX.CrystalBlue });
                }
            }
        }

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            Texture2D texture = TextureAssets.Item[Type].Value;
            int cooldown = Main.gameMenu ? 0 : Main.LocalPlayer.GetModPlayer<CrystallineDynamitePlayer>().Cooldown;
            float pulse = 0.82f + MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * 0.12f;
            Color glow = CrystallineDynamiteVFX.Additive(CrystallineDynamiteVFX.CrystalBlue, (cooldown > 0 ? 0.1f : 0.24f) * pulse);
            float glowDistance = cooldown > 0 ? 1f : 1.75f;
            for (int i = 0; i < 4; i++)
                spriteBatch.Draw(texture, position + (MathHelper.PiOver2 * i).ToRotationVector2() * glowDistance * scale, frame, glow, 0f, origin, scale, SpriteEffects.None, 0f);
            Color color = cooldown > 0 ? new Color(105, 155, 180) * 0.38f : drawColor;
            spriteBatch.Draw(texture, position, frame, color, 0f, origin, scale, SpriteEffects.None, 0f);
            return false;
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            if (Main.gameMenu)
                return;

            int cooldown = Main.LocalPlayer.GetModPlayer<CrystallineDynamitePlayer>().Cooldown;
            if (cooldown <= 0)
                return;
            Texture2D texture = TextureAssets.Item[Type].Value;
            float progress = CrystallineDynamitePlayer.RegenerationProgress(cooldown);
            int height = Math.Clamp((int)MathF.Ceiling(frame.Height * progress), 0, frame.Height);
            float pulse = 0.8f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.12f;
            if (height > 0)
            {
                int offset = frame.Height - height;
                Rectangle fill = new(frame.X, frame.Y + offset, frame.Width, height);
                Vector2 fillOrigin = origin - new Vector2(0f, offset);
                spriteBatch.Draw(texture, position, fill, Color.White * (0.45f + progress * 0.5f), 0f, fillOrigin, scale, SpriteEffects.None, 0f);
                spriteBatch.Draw(texture, position, fill, CrystallineDynamiteVFX.Additive(CrystallineDynamiteVFX.CrystalBlue, 0.55f * pulse), 0f, fillOrigin, scale, SpriteEffects.None, 0f);
            }
            Vector2 topLeft = position - origin * scale;
            Vector2 bottomRight = position + (frame.Size() - origin) * scale;
            int barWidth = Math.Max(16, (int)MathF.Round(frame.Width * scale));
            Rectangle bar = new((int)MathF.Round(topLeft.X), (int)MathF.Round(bottomRight.Y + 2f), barWidth, 3);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, bar, new Color(7, 20, 30) * 0.9f);
            if (progress > 0f)
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(bar.X + 1, bar.Y + 1, Math.Max(1, (int)MathF.Round((bar.Width - 2) * progress)), 1), CrystallineDynamiteVFX.CrystalBlue);
            string timer = MathF.Ceiling(cooldown / 60f).ToString("0");
            float timerScale = 0.65f * scale;
            Vector2 timerSize = FontAssets.ItemStack.Value.MeasureString(timer) * timerScale;
            Vector2 timerPosition = bottomRight - timerSize + new Vector2(1f, -2f);
            Utils.DrawBorderStringFourWay(spriteBatch, FontAssets.ItemStack.Value, timer, timerPosition.X, timerPosition.Y, Color.White, new Color(20, 80, 115), Vector2.Zero, timerScale);
        }
    }

    public class CrystallineDynamitePlayer : ModPlayer
    {
        internal const int CooldownDuration = 1800;
        internal int Cooldown;
        internal static float RegenerationProgress(int cooldown) => MathHelper.Clamp(1f - cooldown / (float)CooldownDuration, 0f, 1f);

        public override void PostUpdate()
        {
            if (Cooldown <= 0)
                return;

            Cooldown--;
            if (Cooldown == 0 && Player.whoAmI == Main.myPlayer && !Main.dedServ)
            {
                CrystallineDynamiteVFX.Burst(Player.Center, 14, 3.2f);
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.22f, Pitch = 0.55f, PitchVariance = 0.05f }, Player.Center);
            }
        }

        public override void SaveData(TagCompound tag)
        {
            if (Cooldown > 0)
                tag["CrystallineDynamiteCooldown"] = Cooldown;
        }

        public override void LoadData(TagCompound tag)
        {
            Cooldown = Math.Max(0, tag.GetInt("CrystallineDynamiteCooldown"));
        }
    }

    public class CrystallineDynamiteGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        internal bool Crystalline;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type == ProjectileID.Dynamite;

        public override void AI(Projectile projectile)
        {
            if (!Crystalline || Main.dedServ)
                return;

            Lighting.AddLight(projectile.Center, CrystallineDynamiteVFX.CrystalBlue.ToVector3() * 0.22f);
            Vector2 fuse = projectile.Center - Vector2.UnitY.RotatedBy(projectile.rotation) * 15f * projectile.scale;
            if (Main.rand.NextBool(3))
            {
                Vector2 velocity = -projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f) + Main.rand.NextVector2Circular(0.25f, 0.25f);
                CrystallineDynamiteVFX.Spark(fuse + Main.rand.NextVector2Circular(3f, 3f), velocity, Main.rand.NextFloat(0.09f, 0.16f));
            }
            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(fuse, DustID.BlueTorch, Main.rand.NextVector2Circular(0.45f, 0.45f) - new Vector2(0f, 0.35f), 100, CrystallineDynamiteVFX.CrystalBlue, Main.rand.NextFloat(0.65f, 0.9f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (!Crystalline)
                return true;
            Texture2D texture = TextureAssets.Item[ModContent.ItemType<CrystallineDynamite>()].Value;
            Vector2 position = projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float pulse = 0.82f + MathF.Sin(Main.GlobalTimeWrappedHourly * 7f + projectile.identity) * 0.12f;
            Color glow = CrystallineDynamiteVFX.Additive(CrystallineDynamiteVFX.CrystalBlue, 0.22f * pulse);
            for (int i = 0; i < 4; i++)
                Main.EntitySpriteDraw(texture, position + (MathHelper.PiOver2 * i).ToRotationVector2() * 1.5f, null, glow, projectile.rotation, origin, projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, position, null, Color.Lerp(lightColor, Color.White, 0.35f), projectile.rotation, origin, projectile.scale, SpriteEffects.None);
            return false;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(Crystalline);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            Crystalline = bitReader.ReadBit();
        }

        public override void OnKill(Projectile projectile, int timeLeft)
        {
            if (!Crystalline)
                return;

            if (!Main.dedServ)
            {
                CrystallineDynamiteVFX.Burst(projectile.Center, 24, 5.5f);
                SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.28f, Pitch = 0.3f, PitchVariance = 0.12f }, projectile.Center);
            }

            if (projectile.owner != Main.myPlayer)
                return;

            for (int i = 0; i < 18; i++)
            {
                float angle = MathHelper.TwoPi * i / 18f + Main.rand.NextFloat(-0.14f, 0.14f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3.8f, 8.5f);
                velocity.Y -= Main.rand.NextFloat(0.5f, 2.4f);
                Projectile.NewProjectile(projectile.GetSource_FromThis(), projectile.Center, velocity,
                    ModContent.ProjectileType<CrystallineDynamiteShard>(), 0, 0f, projectile.owner,
                    Main.rand.NextFloat(20f, 34f), Main.rand.NextFloat(0f, MathHelper.TwoPi), Main.rand.NextFloat(0.34f, 0.68f));
            }
        }
    }

    public class CrystallineDynamiteShard : ModProjectile
    {
        public override string Texture => "AerovelenceMod/Content/NPCs/Bosses/CrystalTumbler/CrystalShard";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            Projectile.localAI[0]++;
            float age = Projectile.localAI[0];
            float returnDelay = Projectile.ai[0];
            float phase = Projectile.ai[1];
            Projectile.scale = Projectile.ai[2] <= 0f ? 0.5f : Projectile.ai[2];
            Projectile.rotation += 0.08f + Projectile.velocity.X * 0.025f;
            if (age < returnDelay)
            {
                Projectile.velocity *= 0.965f;
                Projectile.velocity.Y += 0.045f;
            }
            else
            {
                float returnProgress = MathHelper.Clamp((age - returnDelay) / 45f, 0f, 1f);
                Vector2 orbit = new Vector2(MathF.Cos(age * 0.11f + phase), MathF.Sin(age * 0.13f + phase)) * MathHelper.Lerp(20f, 2f, returnProgress);
                Vector2 target = owner.MountedCenter + new Vector2(0f, owner.gfxOffY) + orbit;
                Vector2 desired = (target - Projectile.Center).SafeNormalize(Vector2.Zero) * MathHelper.Lerp(5f, 17f, returnProgress);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.07f + returnProgress * 0.1f);
                if (Vector2.DistanceSquared(Projectile.Center, owner.MountedCenter + new Vector2(0f, owner.gfxOffY)) < 18f * 18f && returnProgress > 0.3f)
                {
                    if (!Main.dedServ)
                        CrystallineDynamiteVFX.Burst(Projectile.Center, 2, 0.8f);
                    Projectile.Kill();
                    return;
                }
            }
            Lighting.AddLight(Projectile.Center, CrystallineDynamiteVFX.CrystalBlue.ToVector3() * 0.13f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Color color = Main.rand.NextBool() ? CrystallineDynamiteVFX.CrystalBlue : CrystallineDynamiteVFX.CrystalViolet;
                CrystallineDynamiteVFX.Spark(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.09f), Main.rand.NextFloat(0.06f, 0.12f), color);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D crystal = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow").Value;
            Vector2 position = Projectile.Center - Main.screenPosition;
            float pulse = 0.82f + MathF.Sin(Main.GlobalTimeWrappedHourly * 7f + Projectile.identity) * 0.12f;
            Color glowColor = CrystallineDynamiteVFX.Additive(CrystallineDynamiteVFX.CrystalBlue, 0.16f * pulse);
            Main.EntitySpriteDraw(glow, position, null, glowColor, 0f, glow.Size() * 0.5f, 0.2f * Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(crystal, position, null, Color.Lerp(lightColor, Color.White, 0.6f), Projectile.rotation, crystal.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            Main.EntitySpriteDraw(crystal, position, null, CrystallineDynamiteVFX.Additive(Color.White, 0.18f * pulse), Projectile.rotation, crystal.Size() * 0.5f, Projectile.scale * 1.06f, SpriteEffects.None);
            return false;
        }
    }

    internal static class CrystallineDynamiteVFX
    {
        internal static readonly Color CrystalBlue = new(105, 225, 255);
        internal static readonly Color CrystalViolet = new(185, 125, 255);

        internal static Color Additive(Color color, float opacity)
        {
            color *= MathHelper.Clamp(opacity, 0f, 1f);
            color.A = 0;
            return color;
        }

        internal static void Spark(Vector2 position, Vector2 velocity, float scale, Color? color = null)
        {
            if (Main.dedServ)
                return;
            Dust dust = Dust.NewDustPerfect(position, ModContent.DustType<GlowPixelCross>(), velocity, 0, color ?? CrystalBlue, scale);
            dust.customData = DustBehaviorUtil.AssignBehavior_GPCBase(rotPower: 0.13f, timeBeforeSlow: 6, preSlowPower: 0.95f,
                postSlowPower: 0.87f, velToBeginShrink: 0.8f, fadePower: 0.88f, shouldFadeColor: false);
        }

        internal static void Burst(Vector2 position, int count, float speed)
        {
            if (Main.dedServ)
                return;
            for (int i = 0; i < count; i++)
            {
                Color color = Main.rand.NextBool(3) ? CrystalViolet : CrystalBlue;
                Spark(position + Main.rand.NextVector2Circular(3f, 3f), Main.rand.NextVector2CircularEdge(speed, speed) * Main.rand.NextFloat(0.35f, 1f), Main.rand.NextFloat(0.08f, 0.18f), color);
            }
        }
    }
}
