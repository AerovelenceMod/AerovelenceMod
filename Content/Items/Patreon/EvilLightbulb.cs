using AerovelenceMod.Common.Systems;
using AerovelenceMod.Content.Dusts.GlowDusts;
using System;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace AerovelenceMod.Content.Items.Patreon
{
    public class EvilLightbulb : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Evil Lightbulb", "Left-click to shoot a small laser\nRight-click to conjure an aura; shoot it three times to charge it for 5 seconds")
                .AddSkillStrike(Language.Default, "Charge the aura with light, then stand inside and shoot to Skill Strike");
            base.SetStaticDefaults();
        }
        public override void SetDefaults()
        {
            Item.crit = 7;
            Item.damage = 10;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 5;
            Item.width = 40;
            Item.height = 40;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.UseSound = SoundID.Item20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.knockBack = 6;
            Item.value = Item.sellPrice(0, 5, 30, 0);
            Item.rare = ItemRarities.EarlyPHM;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<EvilLightbulbHeld>();
            Item.shootSpeed = 12f;
        }
        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player)
        {
            bool alternate = player.altFunctionUse == 2;
            Item.damage = alternate ? 55 : 10;
            Item.mana = alternate ? 0 : 5;
            if (alternate && EvilAura.Find(player.whoAmI) != null) return false;
            return base.CanUseItem(player);
        }
        public override void HoldItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer || player.noItems || player.CCed || player.ownedProjectileCounts[Item.shoot] > 0) return;
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, Vector2.Zero, Item.shoot, 0, 0f, player.whoAmI);
        }
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            foreach (Projectile held in Main.ActiveProjectiles)
                if (held.owner == player.whoAmI && held.ModProjectile is EvilLightbulbHeld staff)
                {
                    staff.Aim(player, direction);
                    held.ai[1] = 1f;
                    held.netUpdate = true;
                    position = staff.Bulb;
                    break;
                }
            if (player.altFunctionUse == 2)
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<EvilAura>(), damage, knockback, player.whoAmI);
            else
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<EvilRay>(), damage, knockback, player.whoAmI);
            if (!Main.dedServ)
            {
                int count = player.altFunctionUse == 2 ? 8 : 3;
                for (int i = 0; i < count; i++)
                {
                    Vector2 spark = player.altFunctionUse == 2 ? (i * MathHelper.TwoPi / count).ToRotationVector2() : direction.RotatedBy(Main.rand.NextFloat(-.3f, .3f));
                    Dust.NewDustPerfect(position, ModContent.DustType<GlowPixelCross>(), spark * Main.rand.NextFloat(2f, 4f), 0, new Color(255, 190, 90), .16f);
                }
            }
            return false;
        }
    }
    public class EvilLightbulbHeld : ModProjectile
    {
        public override string Texture => "AerovelenceMod/Content/Items/Patreon/EvilLightbulbEmpty";
        private int age;
        private float sentAngle;
        private float filamentRotation;
        private float filamentVelocity;
        private float previousAim;
        private float previousShot;
        private Vector2 previousVelocity;
        internal Vector2 Bulb => Projectile.Center + new Vector2(-5f * Projectile.spriteDirection, -19f * Main.player[Projectile.owner].gravDir).RotatedBy(Projectile.rotation);
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }
        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;
        public override bool ShouldUpdatePosition() => false;
        internal void Aim(Player player, Vector2 direction)
        {
            Projectile.ai[0] = direction.ToRotation();
            player.ChangeDir(direction.X >= 0f ? 1 : -1);
            float armRotation = Projectile.ai[0] - MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
            Projectile.Center = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation) + Vector2.UnitY * player.gfxOffY;
            Projectile.spriteDirection = player.direction;
            Projectile.rotation = Projectile.ai[0] - new Vector2(-5f * player.direction, -19f * player.gravDir).ToRotation();
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed || player.HeldItem.type != ModContent.ItemType<EvilLightbulb>())
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;
            Vector2 direction = Projectile.ai[0].ToRotationVector2();
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 cursor = Main.MouseWorld;
                if (player.gravDir == -1f) cursor.Y = Main.screenPosition.Y * 2f + Main.screenHeight - cursor.Y;
                direction = (cursor - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                float angle = direction.ToRotation();
                if (age % 6 == 0 && Math.Abs(MathHelper.WrapAngle(angle - sentAngle)) > .02f)
                {
                    sentAngle = angle;
                    Projectile.netUpdate = true;
                }
            }
            Aim(player, direction);
            player.heldProj = Projectile.whoAmI;
            if (age > 0)
            {
                Vector2 acceleration = (player.velocity - previousVelocity).RotatedBy(-Projectile.rotation);
                float turn = MathHelper.WrapAngle(Projectile.ai[0] - previousAim) * player.direction * player.gravDir;
                filamentVelocity += MathHelper.Clamp(-acceleration.X * player.direction * .02f - turn * .2f, -.12f, .12f);
            }
            filamentVelocity += Math.Max(0f, Projectile.ai[1] - previousShot) * .08f;
            filamentVelocity = (filamentVelocity - filamentRotation * .16f) * .8f;
            filamentRotation = MathHelper.Clamp(filamentRotation + filamentVelocity, -.25f, .25f);
            previousAim = Projectile.ai[0];
            previousVelocity = player.velocity;
            Projectile.ai[1] = Math.Max(0f, Projectile.ai[1] - .12f);
            previousShot = Projectile.ai[1];
            Lighting.AddLight(Bulb, new Vector3(.12f, .06f, .015f) * (1f + Projectile.ai[1]));
            age++;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D filament = ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Patreon/EvilLightbulbBulbFilament").Value;
            Texture2D bulb = ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Patreon/EvilLightbulbBulb").Value;
            Player player = Main.player[Projectile.owner];
            Vector2 origin = new(21f, 32f);
            Vector2 filamentOrigin = new(20f, 22f);
            SpriteEffects flip = SpriteEffects.None;
            if (Projectile.spriteDirection < 0)
            {
                flip |= SpriteEffects.FlipHorizontally;
                origin.X = texture.Width - origin.X;
                filamentOrigin.X = filament.Width - filamentOrigin.X;
            }
            if (player.gravDir < 0f)
            {
                flip |= SpriteEffects.FlipVertically;
                origin.Y = texture.Height - origin.Y;
                filamentOrigin.Y = filament.Height - filamentOrigin.Y;
            }
            Vector2 position = Projectile.Center - Main.screenPosition;
            Vector2 filamentPosition = position + new Vector2(-Projectile.spriteDirection, -10f * player.gravDir).RotatedBy(Projectile.rotation);
            Main.EntitySpriteDraw(texture, position, null, lightColor, Projectile.rotation, origin, 1f, flip);
            Main.EntitySpriteDraw(filament, filamentPosition, null, Color.Lerp(lightColor, Color.White, Projectile.ai[1]), Projectile.rotation + filamentRotation * Projectile.spriteDirection * player.gravDir, filamentOrigin, 1f, flip);
            Main.EntitySpriteDraw(bulb, position, null, lightColor * .5f, Projectile.rotation, origin, 1f, flip);
            if (Projectile.ai[1] > 0f)
            {
                Texture2D lit = TextureAssets.Item[ModContent.ItemType<EvilLightbulb>()].Value;
                Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
                float flash = Utils.GetLerpValue(.45f, .8f, Projectile.ai[1], true);
                Color color = Color.Lerp(new Color(255, 165, 65, 0), new Color(255, 255, 255, 0), flash);
                Main.EntitySpriteDraw(lit, position, null, color * Projectile.ai[1], Projectile.rotation, origin, 1f, flip);
                Main.EntitySpriteDraw(glow, Bulb - Main.screenPosition, null, color * (Projectile.ai[1] * .35f), 0f, glow.Size() * .5f, .7f, SpriteEffects.None);
            }
            return false;
        }
    }
    public class EvilAura : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        internal const float Radius = 100f;
        private int age;
        private int seenHits;
        private float hitPulse;
        internal bool Charged => Projectile.active && Projectile.ai[0] >= 3f;
        internal bool Contains(Vector2 point) => Projectile.active && Vector2.DistanceSquared(point, Projectile.Center) <= Radius * Radius;
        internal static EvilAura Find(int owner)
        {
            foreach (Projectile projectile in Main.ActiveProjectiles)
                if (projectile.owner == owner && projectile.ModProjectile is EvilAura aura) return aura;
            return null;
        }
        internal void Charge()
        {
            if (!Projectile.active || Charged) return;
            Projectile.ai[0]++;
            if (Charged) Projectile.timeLeft = 300;
            Projectile.netUpdate = true;
        }
        public override void SendExtraAI(BinaryWriter writer) => writer.Write((ushort)Projectile.timeLeft);
        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadUInt16();
        public override bool ShouldUpdatePosition() => false;
        public override void SetDefaults()
        {
            Projectile.width = 220;
            Projectile.height = 220;
            Projectile.aiStyle = -1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.damage = 20;
            Projectile.timeLeft = 180;
            Projectile.alpha = 0;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 center = Projectile.Center;
            float opacity = Math.Min(1f, age / 10f) * Math.Min(1f, Projectile.timeLeft / 18f);
            float radius = Radius * MathHelper.SmoothStep(.7f, 1f, Math.Min(1f, age / 12f));
            float rotation = age * .012f;
            Color auraColor = Charged ? new Color(255, 126, 45, 0) : new Color(255, 232, 174, 0);
            float glowStrength = Charged ? .2f + MathF.Sin(age * .09f) * .025f : .08f;
            Texture2D ring = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Ring/GlowRing").Value;
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
            Vector2 position = center - Main.screenPosition;
            Main.EntitySpriteDraw(glow, position, null, auraColor * ((glowStrength + hitPulse * .08f) * opacity), 0f, glow.Size() * .5f, 3.6f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, position, null, auraColor * ((.5f + hitPulse * .25f) * opacity), rotation, ring.Size() * .5f, radius / 190f, SpriteEffects.None);
            if (hitPulse > 0f)
                Main.EntitySpriteDraw(ring, position, null, new Color(255, 150, 55, 0) * (hitPulse * .35f * opacity), -rotation, ring.Size() * .5f, radius * (1.16f - hitPulse * .12f) / 190f, SpriteEffects.None);
            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.UnderProjectiles, () =>
            {
                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Texture2D star = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/GlowStarPMA").Value;
                for (int i = 0; i < 48; i++)
                {
                    float angle = rotation + i * MathHelper.TwoPi / 48f;
                    Vector2 a = center + angle.ToRotationVector2() * radius;
                    Vector2 b = center + (angle + MathHelper.TwoPi / 48f).ToRotationVector2() * radius;
                    Vector2 delta = b - a;
                    Color color = auraColor * ((.35f + .15f * MathF.Sin(i * .6f - rotation * 5f)) * opacity);
                    Main.EntitySpriteDraw(pixel, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(), new Vector2(0f, .5f), new Vector2(delta.Length() + 1f, 2f), SpriteEffects.None);
                }
                for (int i = 0; i < 6; i++)
                {
                    float angle = -rotation + i * MathHelper.TwoPi / 6f;
                    Vector2 spark = center + angle.ToRotationVector2() * radius;
                    float pulse = .8f + .2f * MathF.Sin(age * .12f + i);
                    Main.EntitySpriteDraw(star, spark - Main.screenPosition, null, auraColor * (opacity * pulse), angle, star.Size() * .5f, .15f * pulse, SpriteEffects.None);
                }
            });
            return false;
        }
        public override void AI()
        {
            age++;
            hitPulse = Math.Max(0f, hitPulse - .08f);
            if (seenHits != (int)Projectile.ai[0])
            {
                seenHits = (int)Projectile.ai[0];
                hitPulse = 1f;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = Charged ? .35f : .2f, Pitch = -.2f + seenHits * .2f }, Projectile.Center);
                    for (int i = 0; i < (Charged ? 18 : 6); i++)
                    {
                        Vector2 direction = Main.rand.NextVector2Unit();
                        Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(20f, 60f), ModContent.DustType<GlowPixelCross>(), direction * (Charged ? 3f : 1.5f), 0, Charged ? new Color(255, 140, 55) : new Color(255, 235, 175), Charged ? .18f : .13f);
                    }
                }
            }
            Lighting.AddLight(Projectile.Center, Charged ? new Vector3(.45f, .16f, .025f) : new Vector3(.22f, .18f, .1f));
            if (!Main.dedServ && age % 6 == 0)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust.NewDustPerfect(Projectile.Center + direction * Radius, ModContent.DustType<GlowPixelCross>(), direction.RotatedBy(MathHelper.PiOver2) * 1.5f, 0, Charged ? new Color(255, 145, 55) : new Color(255, 235, 175), .13f);
            }
        }
    }
    public class EvilRay : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";
        private Vector2 origin;
        private EvilAura aura;
        private bool hitAura;
        public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4800;
        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.extraUpdates = 100;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = true;
            Projectile.penetrate = 300;
            Projectile.damage = 50;

        }
        public override void OnSpawn(IEntitySource source)
        {
            origin = Projectile.Center;
            aura = EvilAura.Find(Projectile.owner);
        }
        public override void SendExtraAI(BinaryWriter writer) { writer.Write(origin.X); writer.Write(origin.Y); }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            origin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            if (Projectile.ai[1] != 0f)
            {
                Projectile.extraUpdates = 0;
                Projectile.timeLeft = 18;
                Projectile.friendly = false;
                Projectile.tileCollide = false;
            }
        }
        public override bool ShouldUpdatePosition() => Projectile.ai[1] == 0f;
        public override bool? CanDamage() => Projectile.ai[1] == 0f ? null : false;
        public override void AI()
        {
            if (Projectile.ai[1] != 0f) return;
            if (Projectile.owner == Main.myPlayer && aura != null && aura.Projectile.ModProjectile == aura)
            {
                if (!hitAura && aura.Contains(Projectile.Center))
                {
                    hitAura = true;
                    aura.Charge();
                }
                if (Projectile.ai[0] == 0f && aura.Charged && aura.Contains(Main.player[Projectile.owner].Center))
                {
                    Projectile.ai[0] = 1f;
                    SkillStrikeUtil.setSkillStrike(Projectile, 1.5f, 300, .25f, .3f);
                }
            }
            if (Projectile.timeLeft <= 1) Fade();
            if (Projectile.numUpdates == 0 && !Main.dedServ)
            {
                Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<GlowPixelCross>(), -Projectile.velocity * .08f, 0,
                    Projectile.ai[0] != 0f ? new Color(255, 140, 55) : new Color(255, 200, 105), .14f);
            }
        }
        private void Fade()
        {
            Projectile.ai[1] = 1f;
            Projectile.velocity = Vector2.Zero;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 0;
            Projectile.numUpdates = 0;
            Projectile.timeLeft = 18;
            Projectile.netUpdate = true;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Fade();
            if (!Main.dedServ)
                for (int i = 0; i < 5; i++)
                    Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<GlowPixelCross>(), Main.rand.NextVector2Circular(2.5f, 2.5f) - oldVelocity * .12f, 0, new Color(255, 205, 120), Main.rand.NextFloat(.12f, .2f));
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.dedServ)
                for (int i = 0; i < 3; i++)
                    Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<GlowPixelCross>(), Main.rand.NextVector2Circular(2f, 2f), 0, new Color(255, 220, 145), .16f);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = origin;
            Vector2 end = Projectile.Center;
            float opacity = Projectile.ai[1] == 0f ? 1f : MathF.Pow(Projectile.timeLeft / 18f, 1.5f);
            bool charged = Projectile.ai[0] != 0f;
            ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.UnderProjectiles, () =>
            {
                Vector2 delta = end - start;
                float length = delta.Length();
                if (length < 1f) return;
                Texture2D trail = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Trails/ThinGlowLine").Value;
                Texture2D star = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/GlowStarPMA").Value;
                float rotation = delta.ToRotation();
                Main.EntitySpriteDraw(trail, start - Main.screenPosition, null, new Color(255, charged ? 125 : 165, 60, 0) * (.55f * opacity), rotation, new Vector2(0f, trail.Height * .5f), new Vector2(length / trail.Width, (charged ? .12f : .08f) * opacity), SpriteEffects.None);
                Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), (charged ? new Color(255, 205, 130, 0) : new Color(255, 241, 186, 0)) * opacity, rotation, new Vector2(0f, .5f), new Vector2(length, charged ? 3f : 2f), SpriteEffects.None);
                Main.EntitySpriteDraw(star, end - Main.screenPosition, null, new Color(255, 222, 140, 0) * opacity, rotation, star.Size() * .5f, new Vector2(charged ? .5f : .4f, .18f) * opacity, SpriteEffects.None);
            });
            return false;
        }
    }
}
