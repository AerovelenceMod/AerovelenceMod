using AerovelenceMod.Common.Globals.SkillStrikes;
using AerovelenceMod.Content.Items.Accessories.SmallAccessories;
using System;
using System.IO;
using System.Collections.Generic;
using AerovelenceMod.Content.Dusts.GlowDusts;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using LocalizedText = Terraria.Localization.LocalizedText;

namespace AerovelenceMod.Content.Items.Weapons.Corruption
{
    public sealed class WormWhacker : TranslatableModItem, IWinchWhip
    {
		public override string Texture => "AerovelenceMod/Content/Items/Weapons/Corruption/WormWhacker/WormWhacker";
        public int WinchChargeDuration => 12;
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Worm Whacker", "1 summon tag damage\nEvery twelfth swing sheds a thrashing worm\nCharges rapidly with the Repurposed Winch")
                .AddSkillStrike(Language.Default, "Shed worms latch onto your minions and deal Skill Strikes");
        }
        public override void SetDefaults()
        {
            Item.DefaultToWhip(ModContent.ProjectileType<WormWhackerWhip>(), 8, 1.5f, 4f, 10);
            Item.rare = ItemRarities.MidPHM;
            Item.value = Item.sellPrice(gold: 1, silver: 75);
            Item.autoReuse = true;
        }
        public override bool MeleePrefix() => true;
        public override float UseSpeedMultiplier(Player player) => player.GetModPlayer<WormWhackerPlayer>().WillShed ? .8f : 1f;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            bool shed = player.GetModPlayer<WormWhackerPlayer>().NextSwing();
            int index = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, ai1: player.itemAnimationMax, ai2: shed ? 1f : 0f);
            if (shed && index < Main.maxProjectiles)
            {
                Projectile whip = Main.projectile[index];
                Projectile.NewProjectile(whip.GetSource_FromThis(), position, Vector2.Zero, ModContent.ProjectileType<WhackerWorm>(), Math.Max(1, damage * 3), 2f, player.whoAmI, -whip.identity - 1, whip.type);
            }
            return false;
        }
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.RottenChunk, 12)
                .AddIngredient(ItemID.DemoniteBar, 10)
                .AddIngredient(ItemID.ShadowScale, 8)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
    public sealed class WormWhackerPlayer : ModPlayer
    {
        private int swings;
        internal bool WillShed => swings == 11;

        internal bool NextSwing()
        {
            if (++swings < 12) return false;
            swings = 0;
            return true;
        }

        public override void UpdateDead() => swings = 0;
    }
    public sealed class WhackerWorm : ModProjectile
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.DevourerHead;
        public override LocalizedText DisplayName => ModContent.GetInstance<WormWhacker>().DisplayName;
        private int hostSlot = -1;
        private bool wasAttatched;
        private bool latchShown;
        private Vector2 latchOffset;
        private float latchAngle;
        private readonly List<Vector2> whipPoints = new(17);
        private readonly Vector2[] releasePoints = new Vector2[5];
        private Vector2 previousTip, releaseCenter;
        private bool hasTip, hasReleasePose;
        private bool Bound => Projectile.ai[0] < 0f;
        private bool Attached => Projectile.ai[0] > 0f;
        public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        public override bool ShouldUpdatePosition() => !Attached && !Bound;
        public override bool? CanDamage() => Bound ? false : null;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }
            if (Bound)
            {
                FollowWhip();
                return;
            }
            Projectile.tileCollide = !Attached;
            Projectile.ai[2]++;
            Projectile host = Attached ? FindHost() : null;
            if (Attached && host == null)
            {
                if (Projectile.owner != Main.myPlayer)
                {
                    Projectile.friendly = false;
                    Projectile.velocity = Vector2.Zero;
                    return;
                }
                Projectile.ai[0] = Projectile.ai[1] = 0f;
                Projectile.tileCollide = true;
                Projectile.netUpdate = true;
            }
            Projectile.friendly = true;
            if (!Attached && !wasAttatched && Projectile.owner == Main.myPlayer)
            {
                host = NearbyMinion();
                if (host != null)
                {
                    Projectile.ai[0] = host.identity + 1;
                    Projectile.ai[1] = host.type;
                    hostSlot = host.whoAmI;
                    Vector2 outward = (SegmentPoint(0) - host.Center).SafeNormalize(Vector2.UnitX);
                    latchOffset = outward * new Vector2(host.width, host.height) * .35f;
                    latchAngle = (-outward).ToRotation();
                    Projectile.ai[2] = 0f;
                    wasAttatched = true;
                    hasReleasePose = false;
                    Projectile.timeLeft += 90;
                    Projectile.netUpdate = true;
                }
            }
            if (host != null)
            {
                wasAttatched = true;
                Projectile.tileCollide = false;
                Projectile.velocity = host.velocity;
                Vector2 attachment = host.Center + latchOffset;
                Projectile.rotation = AttachedAngle(1);
                Projectile.Center = attachment - Projectile.rotation.ToRotationVector2() * 14f;
                if (!latchShown && !Main.dedServ)
                {
                    latchShown = true;
                    Burst(Projectile.Center, 12);
                    SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = .65f, Volume = .45f }, Projectile.Center);
                }
            }
            else
            {
                Projectile.velocity.X *= .98f;
                Projectile.velocity.Y = Math.Min(12f, Projectile.velocity.Y + .3f);
                Projectile.rotation = Projectile.velocity.ToRotation() + MathF.Sin(Projectile.ai[2] * .35f) * .35f;
            }
            if (!Main.dedServ && Attached)
                Lighting.AddLight(Projectile.Center, .18f, .1f, .03f);
        }
        private void FollowWhip()
        {
            Projectile.tileCollide = false;
            Projectile.timeLeft = 180;
            Projectile parent = null;
            int identity = -(int)Projectile.ai[0] - 1;
            foreach (Projectile candidate in Main.ActiveProjectiles)
            {
                if (candidate.owner == Projectile.owner && candidate.identity == identity && candidate.type == (int)Projectile.ai[1])
                {
                    parent = candidate;
                    break;
                }
            }
            if (parent == null)
            {
                if (Projectile.owner == Main.myPlayer) Projectile.Kill();
                return;
            }
            whipPoints.Clear();
            Projectile.FillWhipControlPoints(parent, whipPoints);
            Vector2 tip = whipPoints[^1];
            Vector2 motion = hasTip ? tip - previousTip : Vector2.Zero;
            previousTip = tip;
            hasTip = true;
            for (int i = 0; i < releasePoints.Length; i++)
                releasePoints[i] = WhipPoint(i * 7f);
            Projectile.Center = WhipPoint(14f);
            Projectile.rotation = (tip - WhipPoint(7f)).ToRotation();
            Projectile.GetWhipSettings(parent, out float duration, out _, out _);
            if (parent.ai[0] < duration * .7f || Projectile.owner != Main.myPlayer) return;
            releaseCenter = Projectile.Center;
            hasReleasePose = true;
            Vector2 outward = (tip - whipPoints[^2]).SafeNormalize(parent.velocity.SafeNormalize(Vector2.UnitX));
            Projectile.velocity = motion.SafeNormalize(outward) * Math.Min(3f, motion.Length() * .12f) + outward * 2f;
            Projectile.velocity.Y -= .5f;
            Projectile.ai[0] = Projectile.ai[1] = Projectile.ai[2] = 0f;
            Projectile.tileCollide = true;
            Projectile.netUpdate = true;
        }
        private Vector2 WhipPoint(float distance)
        {
            for (int i = whipPoints.Count - 1; i > 0; i--)
            {
                Vector2 edge = whipPoints[i - 1] - whipPoints[i];
                float length = edge.Length();
                if (length > distance) return whipPoints[i] + edge * (distance / length);
                distance -= length;
            }
            return whipPoints[0];
        }
        private bool IsHost(Projectile candidate) => candidate.active && candidate.minion && !candidate.npcProj && !candidate.trap
            && candidate.owner == Projectile.owner && candidate.identity + 1 == (int)Projectile.ai[0] && candidate.type == (int)Projectile.ai[1];
        private Projectile FindHost()
        {
            if (hostSlot >= 0 && hostSlot < Main.maxProjectiles && IsHost(Main.projectile[hostSlot])) return Main.projectile[hostSlot];
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (!IsHost(Main.projectile[i])) continue;
                hostSlot = i;
                return Main.projectile[i];
            }
            hostSlot = -1;
            return null;
        }
        private Projectile NearbyMinion()
        {
            Projectile nearest = null;
            float closest = float.MaxValue;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];
                if (!candidate.active || !candidate.minion || candidate.npcProj || candidate.trap || candidate.owner != Projectile.owner) continue;
                float distance = Vector2.DistanceSquared(candidate.Center, Projectile.Center);
                float reach = Math.Max(candidate.width, candidate.height) * .5f + 32f;
                if (distance >= reach * reach || distance >= closest) continue;
                closest = distance;
                nearest = candidate;
            }
            return nearest;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (oldVelocity.X != Projectile.velocity.X) Projectile.velocity.X = -oldVelocity.X * .65f;
            if (oldVelocity.Y != Projectile.velocity.Y)
            {
                Projectile.velocity.Y = oldVelocity.Y > 0f ? -MathHelper.Clamp(Math.Abs(oldVelocity.Y) * .55f, 2.2f, 5.5f) : .5f;
                if (oldVelocity.Y > 0f)
                    Projectile.velocity.X = MathF.Sin(Projectile.ai[2] * .7f + Projectile.identity) * 2.4f;
            }
            return false;
        }
        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            fallThrough = false;
            return true;
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Projectile.GetGlobalProjectile<SkillStrikeGProj>().SkillStrike = false;
            if (Attached) SkillStrikeUtil.setSkillStrike(Projectile, 1.75f, 1, .15f, .35f);
        }
        private float AttachedAngle(int index) => latchAngle
            + MathF.Sin(Projectile.ai[2] * .38f - index * .8f + Projectile.identity) * (.25f + index * .15f)
            + MathF.Sin(Projectile.ai[2] * .79f + index * .6f + Projectile.identity) * index * .06f;
        private Vector2 SegmentPoint(int index)
        {
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            Vector2 normal = new(-direction.Y, direction.X);
            if (Bound) return hasTip ? releasePoints[index] : Projectile.Center;
            if (Attached)
            {
                Vector2 head = Projectile.Center + direction * 14f;
                for (int segment = 1; segment <= index; segment++)
                    head -= AttachedAngle(segment).ToRotationVector2() * 7f;
                return head;
            }
            Vector2 point = Projectile.Center + direction * (14f - index * 7f)
                + normal * (MathF.Sin(Projectile.ai[2] * .45f - index * .95f) * index * 1.8f);
            return hasReleasePose && Projectile.ai[2] < 6f
                ? Vector2.Lerp(releasePoints[index] + Projectile.Center - releaseCenter, point, Projectile.ai[2] / 6f) : point;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = 0;
            for (int i = 0; i < 4; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), SegmentPoint(i), SegmentPoint(i + 1), 9f, ref distance)) return true;
            return false;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(wasAttatched);
            writer.Write((short)Projectile.timeLeft);
            writer.Write(latchOffset.X);
            writer.Write(latchOffset.Y);
            writer.Write(latchAngle);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            wasAttatched = reader.ReadBoolean();
            Projectile.timeLeft = reader.ReadInt16();
            latchOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            latchAngle = reader.ReadSingle();
            if (!Bound && !Attached && hasTip && !hasReleasePose)
            {
                releaseCenter = Projectile.Center;
                hasReleasePose = true;
            }
        }
        public override void OnKill(int timeLeft) => Burst(Projectile.Center, 6);
        internal static void Burst(Vector2 center, int count)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < count; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, DustID.CorruptGibs, Main.rand.NextVector2Circular(2.5f, 2.5f), 90, default, .8f);
                dust.noGravity = true;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            if (Bound && !hasTip) return false;
            float fade = Math.Min(1f, Projectile.timeLeft / 20f);
            if (Attached)
            {
                Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
                Main.EntitySpriteDraw(glow, SegmentPoint(0) - Main.screenPosition, null, new Color(255, 170, 55, 0) * (.4f * fade), 0f, glow.Size() * .5f, .6f, SpriteEffects.None);
            }
            for (int i = 4; i >= 0; i--)
            {
                int npc = i == 0 ? NPCID.DevourerHead : i == 4 ? NPCID.DevourerTail : NPCID.DevourerBody;
                Texture2D texture = i == 0 ? TextureAssets.Projectile[Type].Value : ModContent.Request<Texture2D>("Terraria/Images/NPC_" + npc).Value;
                Rectangle frame = texture.Frame(1, Main.npcFrameCount[npc]);
                Vector2 point = SegmentPoint(i);
                Vector2 direction = i == 0 ? SegmentPoint(0) - SegmentPoint(1) : SegmentPoint(i - 1) - point;
                Color color = Lighting.GetColor(point.ToTileCoordinates());
                if (Attached)
                    Main.EntitySpriteDraw(texture, point - Main.screenPosition, frame, new Color(240, 180, 70, 0) * (.2f * fade), direction.ToRotation() + MathHelper.PiOver2, frame.Size() * .5f, .47f, SpriteEffects.None);
                Main.EntitySpriteDraw(texture, point - Main.screenPosition, frame, color * fade, direction.ToRotation() + MathHelper.PiOver2, frame.Size() * .5f, .4f, SpriteEffects.None);
            }
            return false;
        }
    }
    public sealed class WormWhackerTag : ModBuff
    {
        public override string Texture => "Terraria/Images/Buff_" + BuffID.BlandWhipEnemyDebuff;
        public override LocalizedText DisplayName => ModContent.GetInstance<WormWhacker>().DisplayName;
        public override LocalizedText Description => this.Localize("Minions deal 1 extra damage");
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsATagBuff[Type] = true;
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }
    }
    public sealed class WormWhackerTagNPC : GlobalNPC
    {
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!projectile.npcProj && !projectile.trap && projectile.IsMinionOrSentryRelated && npc.HasBuff<WormWhackerTag>())
                modifiers.FlatBonusDamage += ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
        }
    }
    public sealed class WormWhackerWhip : ModProjectile
    {
        public override string Texture => "Terraria/Images/NPC_" + NPCID.DevourerBody;
        public override LocalizedText DisplayName => ModContent.GetInstance<WormWhacker>().DisplayName;
        private bool shed;
        public override void SetStaticDefaults() => ProjectileID.Sets.IsAWhip[Type] = true;
        public override void SetDefaults()
        {
            Projectile.DefaultToWhip();
            Projectile.WhipSettings.Segments = 16;
            Projectile.WhipSettings.RangeMultiplier = 1.3f;
        }
        public override bool PreAI()
        {
            Main.player[Projectile.owner].itemAnimationMax = Math.Max(1, (int)Projectile.ai[1]);
            return true;
        }
        public override void AI()
        {
            Projectile.GetWhipSettings(Projectile, out float duration, out _, out _);
            if (Projectile.ai[2] != 1f || shed || Projectile.ai[0] < duration * .7f) return;
            shed = true;
            if (Main.dedServ) return;
            var points = Projectile.WhipPointsForCollision;
            points.Clear();
            Projectile.FillWhipControlPoints(Projectile, points);
            Vector2 tip = points[^1];
            SoundEngine.PlaySound(SoundID.NPCHit1 with { Pitch = .4f, Volume = .55f }, tip);
            WhackerWorm.Burst(tip, 9);
            for (int i = 0; i < 5; i++)
                Dust.NewDustPerfect(tip, ModContent.DustType<GlowPixelCross>(), Main.rand.NextVector2Circular(2f, 2f), 0, new Color(165, 105, 225), .25f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<WormWhackerTag>(), 240);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
            Projectile.damage = Math.Max(1, (int)(Projectile.damage * .8f));
        }
        private void FillPoints()
        {
            var points = Projectile.WhipPointsForCollision;
            points.Clear();
            Projectile.FillWhipControlPoints(Projectile, points);
            if (Projectile.ai[2] != 1f) return;
            float remaining = 28f;
            while (points.Count > 2)
            {
                Vector2 edge = points[^1] - points[^2];
                float length = edge.Length();
                if (length > remaining)
                {
                    points[^1] -= edge * (remaining / length);
                    break;
                }
                remaining -= length;
                points.RemoveAt(points.Count - 1);
            }
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!shed) return null;
            FillPoints();
            var points = Projectile.WhipPointsForCollision;
            float collisionPoint = 0;
            for (int i = 1; i < points.Count; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), points[i - 1], points[i], 12f, ref collisionPoint)) return true;
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            FillPoints();
            var points = Projectile.WhipPointsForCollision;
            Texture2D body = TextureAssets.Projectile[Type].Value;
            Texture2D head = ModContent.Request<Texture2D>("Terraria/Images/NPC_" + NPCID.DevourerHead).Value;
            Rectangle bodyFrame = body.Frame(1, Main.npcFrameCount[NPCID.DevourerBody]);
            Rectangle headFrame = head.Frame(1, Main.npcFrameCount[NPCID.DevourerHead]);
            int last = points.Count - 1;
            for (int i = 0; i < last; i++)
            {
                Vector2 segment = points[i + 1] - points[i];
                float length = segment.Length();
                for (float offset = 0; offset < length; offset += 6f)
                {
                    Vector2 point = points[i] + segment.SafeNormalize(Vector2.UnitX) * offset;
                    Main.EntitySpriteDraw(body, point - Main.screenPosition, bodyFrame, Lighting.GetColor(point.ToTileCoordinates()), segment.ToRotation() + MathHelper.PiOver2, bodyFrame.Size() * .5f, .3f, SpriteEffects.None);
                }
            }
            if (Projectile.ai[2] != 1f)
                Main.EntitySpriteDraw(head, points[^1] - Main.screenPosition, headFrame, Lighting.GetColor(points[^1].ToTileCoordinates()), (points[^1] - points[^2]).ToRotation() + MathHelper.PiOver2, headFrame.Size() * .5f, .35f, SpriteEffects.None);
            return false;
        }
    }
}
