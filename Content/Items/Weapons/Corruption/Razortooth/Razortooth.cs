using AerovelenceMod.Common.Globals.SkillStrikes;
using AerovelenceMod.Common.Bases;
using AerovelenceMod.Content.Dusts.GlowDusts;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace AerovelenceMod.Content.Items.Weapons.Corruption.Razortooth
{
    public sealed class Razortooth : TranslatableModItem
    {
        public override string Texture => "AerovelenceMod/Content/Items/Weapons/Corruption/Razortooth/Razortooth";
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Razortooth", "Throws a tethered razor that loses damage as it unwinds")
                .AddSkillStrike(Language.Default, "Skill Strikes at close range");
        }
        public override void SetDefaults()
        {
            Item.width = Item.height = 38;
            Item.damage = 12;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 1.5f;
            Item.useTime = Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noUseGraphic = Item.noMelee = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<RazortoothDisc>();
            Item.shootSpeed = 18;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarities.EarlyPHM;
            Item.value = Item.sellPrice(silver: 20);
        }
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
        public override bool MeleePrefix() => true;
        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.WormTooth, 12)
            .AddIngredient(ItemID.RottenChunk, 6)
            .AddTile(TileID.Anvils)
            .Register();
    }

    public sealed class RazortoothDisc : ModProjectile
    {
        private const int UnwindTime = 22;
        private const int ShredDuration = 16;
        private readonly BaseTrailInfo trail = new() { trailPointLimit = 12, trailMaxLength = 110f, trailWidth = 10, timesToDraw = 1 };
        private int shredTime;
        private float hitFlash;
        private Vector2 hitOffset;
        private bool Returning => Projectile.ai[0] == 1;
        private bool Shredding => Projectile.ai[0] == 2;
        internal float Spin => Math.Max(0, 1 - Projectile.ai[1] / UnwindTime);
        internal bool SkillRange => !Returning && Spin >= .72f;
        public override string Texture => "AerovelenceMod/Content/Items/Weapons/Corruption/Razortooth/RazortoothProj";
        public override Terraria.Localization.LocalizedText DisplayName => ModContent.GetInstance<Razortooth>().DisplayName;
        public override bool ShouldUpdatePosition() => !Shredding;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 22;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }
        public override void OnSpawn(IEntitySource source) => Projectile.direction = Projectile.velocity.X < 0 ? -1 : 1;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || Vector2.DistanceSquared(owner.MountedCenter, Projectile.Center) > 1400f * 1400f)
            {
                Projectile.Kill();
                return;
            }
            hitFlash *= .8f;
            Projectile.hide = Shredding;
            if (Shredding)
            {
                int index = (int)Projectile.ai[2] - 1;
                if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].CanBeChasedBy(Projectile)) Return();
                else
                {
                    Projectile.Center = Main.npc[index].Center + hitOffset;
                    if (++shredTime >= (SkillRange ? ShredDuration : 8)) Return();
                    else if (!Main.dedServ && SkillRange && shredTime % 4 == 0)
                    {
                        Vector2 away = (Projectile.Center - Main.npc[index].Center).SafeNormalize(Vector2.UnitX * Projectile.direction);
                        for (int i = 0; i < 3; i++)
                        {
                            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.CorruptGibs,
                                away.RotatedByRandom(.8f) * Main.rand.NextFloat(2f, 4f), 60, default, .8f);
                            dust.noGravity = true;
                        }
                        Dust spark = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<LineSpark>(), away.RotatedByRandom(.6f) * 5f, 0, new Color(205, 170, 245), .2f);
                        spark.customData = DustBehaviorUtil.AssignBehavior_LSBase(velFadePower: .88f, preShrinkPower: .9f, postShrinkPower: .8f, timeToStartShrink: 3, killEarlyTime: 12, XScale: .5f, YScale: .3f);
                        hitFlash = .65f;
                        SoundEngine.PlaySound(new SoundStyle("AerovelenceMod/Sounds/Effects/Metallic/joker_stab1")
                        { Pitch = .5f, PitchVariance = .15f, Volume = .18f }, Projectile.Center);
                    }
                }
            }
            Projectile.rotation += (Returning ? .45f : Spin * 1.1f) * Projectile.direction;
            if (Returning)
            {
                Projectile.ai[1] = Math.Min(UnwindTime, Projectile.ai[1] + 1);
                Vector2 difference = owner.MountedCenter - Projectile.Center;
                if (difference.LengthSquared() < 24f * 24f)
                {
                    Projectile.Kill();
                    return;
                }
                Vector2 goalVelocity = difference.SafeNormalize(Vector2.Zero) * 24f;
                Vector2 acceleration = goalVelocity - Projectile.velocity;
                Projectile.velocity += acceleration.SafeNormalize(Vector2.Zero) * Math.Min(4f, acceleration.Length());
            }
            else if (!Shredding)
            {
                Projectile.ai[1]++;
                Projectile.velocity = Projectile.velocity.RotatedBy(.008f * Projectile.direction) * .99f;
                if (Projectile.ai[1] >= UnwindTime || Vector2.DistanceSquared(owner.MountedCenter, Projectile.Center) > 340f * 340f) Return();
            }
            Projectile.localNPCHitCooldown = SkillRange ? 4 : 14;
            if (!Main.dedServ)
            {
                float spin = Returning ? .35f : Spin;
                if (!Shredding)
                {
                    trail.trailTexture ??= ModContent.Request<Texture2D>("AerovelenceMod/Assets/Trails/Trail5").Value;
                    trail.trailPos = Projectile.Center;
                    trail.trailRot = Projectile.velocity.LengthSquared() > 1f ? Projectile.velocity.ToRotation() : Projectile.rotation;
                    trail.trailColor = new Color(165, 105, 215) * (spin * .8f);
                    trail.trailTime += .04f;
                    trail.TrailLogic();
                    if (Spin > .5f && (int)Projectile.ai[1] % 3 == 0 && !Returning)
                    {
                        Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f), ModContent.DustType<GlowPixelCross>(), -Projectile.velocity * .08f, 0, new Color(185, 135, 230), .16f);
                        dust.noGravity = true;
                    }
                }
                Lighting.AddLight(Projectile.Center, new Vector3(.2f, .1f, .28f) * (spin + hitFlash));
            }
        }
        private void Return()
        {
            if (Shredding)
                Projectile.velocity = (Main.player[Projectile.owner].MountedCenter - Projectile.Center).SafeNormalize(Vector2.Zero) * 24f;
            Projectile.ai[0] = 1;
            Projectile.hide = false;
            Projectile.tileCollide = false;
            trail.trailPositions?.Clear();
            trail.trailRotations?.Clear();
            if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity * -.25f;
            Return();
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Dig with { Pitch = .3f, Volume = .4f }, Projectile.Center);
            return false;
        }
        public override bool? CanHitNPC(NPC target)
        {
            if (Shredding && target.whoAmI != (int)Projectile.ai[2] - 1) return false;
            if (Returning && target.whoAmI == (int)Projectile.ai[2] - 1) return false;
            return null;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= SkillRange ? .8f : .3f + Spin * 1.1f;
            if (Shredding) modifiers.Knockback *= .15f;
            Projectile.GetGlobalProjectile<SkillStrikeGProj>().SkillStrike = false;
            if (SkillRange) SkillStrikeUtil.setSkillStrike(Projectile, 1f, 1, .15f, .2f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!Main.dedServ)
            {
                hitFlash = 1f;
                Vector2 away = (Projectile.Center - target.Center).SafeNormalize(-Projectile.velocity.SafeNormalize(Vector2.UnitX));
                for (int i = 0; i < (SkillRange ? 7 : 4); i++)
                {
                    Dust spark = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<LineSpark>(), away.RotatedByRandom(.9f) * Main.rand.NextFloat(3f, 7f), 0, new Color(205, 170, 245), .25f);
                    spark.customData = DustBehaviorUtil.AssignBehavior_LSBase(velFadePower: .9f, preShrinkPower: .92f, postShrinkPower: .8f, timeToStartShrink: 4, killEarlyTime: 16, XScale: .55f, YScale: .3f);
                }
                if (!Shredding)
                    SoundEngine.PlaySound(new SoundStyle("AerovelenceMod/Sounds/Effects/hero_butterfly_blade")
                    { Pitch = .25f, PitchVariance = .15f, Volume = .3f }, Projectile.Center);
            }
            if (Projectile.owner != Main.myPlayer || Projectile.ai[0] != 0 || !target.CanBeChasedBy(Projectile)) return;
            Projectile.ai[0] = 2;
            Projectile.ai[2] = target.whoAmI + 1;
            hitOffset = Projectile.Center - target.Center;
            hitOffset.X = MathHelper.Clamp(hitOffset.X, -target.width * .5f, target.width * .5f);
            hitOffset.Y = MathHelper.Clamp(hitOffset.Y, -target.height * .5f, target.height * .5f);
            Projectile.Center = target.Center + hitOffset;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            trail.trailPositions?.Clear();
            trail.trailRotations?.Clear();
            shredTime = 0;
            Projectile.netUpdate = true;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)shredTime);
            writer.Write((sbyte)Projectile.direction);
            writer.Write(hitOffset.X);
            writer.Write(hitOffset.Y);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            shredTime = reader.ReadByte();
            Projectile.direction = reader.ReadSByte() < 0 ? -1 : 1;
            hitOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            Projectile.hide = Shredding;
            if (Shredding && shredTime == 0) hitFlash = 1f;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            if (Shredding) behindNPCs.Add(index);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (!Shredding && trail.trailPositions != null && trail.trailPositions.Count > 2) trail.TrailDrawing(Main.spriteBatch);
            Player owner = Main.player[Projectile.owner];
            Vector2 start = owner.MountedCenter;
            Texture2D line = TextureAssets.FishingLine.Value;
            Vector2 previous = start;
            for (int i = 1; i <= 20; i++)
            {
                float t = i / 20f;
                Vector2 next = Vector2.Lerp(start, Projectile.Center, t) + new Vector2(0, MathF.Sin(t * MathHelper.Pi) * (Returning ? 12 : 5));
                Vector2 edge = next - previous;
                Main.EntitySpriteDraw(line, previous - Main.screenPosition, null, Lighting.GetColor(previous.ToTileCoordinates()).MultiplyRGB(new Color(165, 145, 185)), edge.ToRotation() - MathHelper.PiOver2, new Vector2(line.Width * .5f, 0), new Vector2(.5f, (edge.Length() + 1) / line.Height), SpriteEffects.None);
                previous = next;
            }
            Texture2D disc = TextureAssets.Projectile[Type].Value;
            Vector2 origin = disc.Size() * .5f;
            float spin = Returning ? .35f : Spin;
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, new Color(150, 90, 205, 0) * (.35f * spin + hitFlash * .3f), 0f, glow.Size() * .5f, .55f, SpriteEffects.None);
            Texture2D slash = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Slash/FullSlashTiny").Value;
            Main.EntitySpriteDraw(slash, Projectile.Center - Main.screenPosition, null, new Color(185, 115, 235, 0) * (spin * .5f + hitFlash * .3f), Projectile.rotation, slash.Size() * .5f, .22f, SpriteEffects.None);
            Main.EntitySpriteDraw(slash, Projectile.Center - Main.screenPosition, null, new Color(235, 215, 255, 0) * (spin * .25f + hitFlash * .4f), Projectile.rotation + MathHelper.Pi, slash.Size() * .5f, .17f, SpriteEffects.None);
            Color tint = Color.Lerp(new Color(150, 110, 190), Color.White, spin);
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i -= 2)
                if (!Shredding && Projectile.oldPos[i] != Vector2.Zero)
                    Main.EntitySpriteDraw(disc, Projectile.oldPos[i] + Projectile.Size * .5f - Main.screenPosition, null, new Color(175, 110, 220, 0) * ((1 - i / (float)Projectile.oldPos.Length) * .3f * spin), Projectile.oldRot[i], origin, 1f, SpriteEffects.None);
            Main.EntitySpriteDraw(disc, Projectile.Center - Main.screenPosition, null, lightColor.MultiplyRGB(tint), Projectile.rotation, origin, 1f, SpriteEffects.None);
            if (hitFlash > .05f)
                Main.EntitySpriteDraw(disc, Projectile.Center - Main.screenPosition, null, new Color(230, 205, 255, 0) * hitFlash * .6f, Projectile.rotation, origin, 1f, SpriteEffects.None);
            return false;
        }
    }
}
