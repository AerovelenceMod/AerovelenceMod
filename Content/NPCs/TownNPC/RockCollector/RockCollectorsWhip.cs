using System;
using System.Collections.Generic;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace AerovelenceMod.Content.NPCs.TownNPC.RockCollector
{
    public sealed class RockCollectorsWhip : ModProjectile
    {
        private const int SwingTime = 28;
        private const int Segments = 16;
        private readonly List<Vector2> points = new(17);
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.npcProj = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = SwingTime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override bool ShouldUpdatePosition() => false;
        public override bool CanHitPlayer(Player target) => false;
        public override bool CanHitPvp(Player target) => false;
        public override bool? CanHitNPC(NPC target) => target.friendly || target.townNPC ? false : null;

        public override void OnSpawn(IEntitySource source)
        {
            if (source is EntitySource_Parent { Entity: NPC npc } && npc.type == ModContent.NPCType<RockCollector>())
            {
                Projectile.ai[0] = npc.whoAmI + 1;
                Projectile.ai[1] = Projectile.velocity.ToRotation();
                Projectile.velocity = Vector2.Zero;
            }
        }
        public override void AI()
        {
            int index = (int)Projectile.ai[0] - 1;
            if (index < 0 || index >= Main.maxNPCs || !Main.npc[index].active || Main.npc[index].type != ModContent.NPCType<RockCollector>())
            {
                Projectile.Kill();
                return;
            }
            NPC collector = Main.npc[index];
            Projectile.Center = collector.Center + new Vector2(collector.direction * 12, -8);
            Projectile.ai[2]++;
            FillPoints();
            if (!Main.dedServ && Projectile.ai[2] == SwingTime / 2)
            {
                SoundEngine.PlaySound(SoundID.Item153 with { Pitch = .2f, Volume = .7f }, points[^1]);
                for (int i = 0; i < 6; i++)
                {
                    Dust dust = Dust.NewDustPerfect(points[^1], DustID.BlueCrystalShard, Main.rand.NextVector2Circular(2, 2), 80, default, .7f);
                    dust.noGravity = true;
                }
            }
        }
        private void FillPoints()
        {
            points.Clear();
            float progress = Math.Clamp(Projectile.ai[2] / SwingTime, 0, 1);
            Projectile.spriteDirection = MathF.Cos(Projectile.ai[1]) < 0 ? -1 : 1;
            float curl = MathHelper.Pi * 10f * (1f - progress * 1.5f) * -Projectile.spriteDirection / Segments;
            float extension = progress * 1.5f;
            float retract = 0f;
            if (extension > 1f)
            {
                retract = (extension - 1f) / .5f;
                extension = 1f - retract;
            }
            float length = 260f * progress * extension / Segments;
            Vector2 origin = Projectile.Center;
            Vector2 folded = origin, loop = origin, straight = origin;
            float foldedAngle = -MathHelper.PiOver2;
            float loopAngle = MathHelper.PiOver2 + MathHelper.PiOver2 * Projectile.spriteDirection;
            float straightAngle = MathHelper.PiOver2;
            float blend = 1f - (1f - extension) * (1f - extension);
            float rotation = Projectile.ai[1] + MathHelper.PiOver2 + MathHelper.Pi * 1.5f * retract * retract * Projectile.spriteDirection;
            points.Add(origin);
            for (int i = 0; i < Segments; i++)
            {
                Vector2 nextFolded = folded + foldedAngle.ToRotationVector2() * length;
                Vector2 nextStraight = straight + straightAngle.ToRotationVector2() * length * 2f;
                Vector2 nextLoop = loop + loopAngle.ToRotationVector2() * length * 2f;
                Vector2 point = Vector2.Lerp(nextLoop, Vector2.Lerp(nextStraight, nextFolded, blend * .9f + .1f), blend * .7f + .3f);
                point = origin + (point - origin) * new Vector2(1f, 1.5f);
                points.Add(point.RotatedBy(rotation, origin));
                float turn = curl * (i / (float)Segments);
                foldedAngle += turn;
                straightAngle += turn;
                loopAngle += turn;
                folded = nextFolded;
                straight = nextStraight;
                loop = nextLoop;
            }
        }

        public override bool? CanDamage() => Projectile.ai[2] >= 4 && Projectile.ai[2] <= SwingTime - 4 ? null : false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            FillPoints();
            float collisionPoint = 0;
            for (int i = 1; i < points.Count; i++)
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), points[i - 1], points[i], 10, ref collisionPoint)) return true;
            return false;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            FillPoints();
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
            Texture2D line = TextureAssets.FishingLine.Value;
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            float progress = Projectile.ai[2] / SwingTime;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 edge = points[i + 1] - points[i];
                float rotation = edge.ToRotation() - MathHelper.PiOver2;
                Color light = Lighting.GetColor(points[i].ToTileCoordinates());
                Main.EntitySpriteDraw(line, points[i] - Main.screenPosition, null, light.MultiplyRGB(new Color(115, 140, 170)), rotation,
                    new Vector2(line.Width * .5f, 0), new Vector2(.6f, (edge.Length() + 1) / line.Height), SpriteEffects.None);
                bool tip = i == points.Count - 2;
                Rectangle frame = i == 0 ? new(0, 0, 26, 30) : tip ? new(0, 80, 26, 14) : new(0, 32 + Math.Min(2, (i - 1) / 5) * 16, 26, 14);
                float scale = tip ? MathHelper.Lerp(.5f, 1.5f, Utils.GetLerpValue(.1f, .7f, progress, true)
                    * Utils.GetLerpValue(.9f, .7f, progress, true)) : 1f;
                Vector2 origin = new(13, i == 0 ? 6 : 7);
                Main.EntitySpriteDraw(texture, points[i] - Main.screenPosition, frame, light, rotation, origin, scale, effects);
                Main.EntitySpriteDraw(glow, points[i] - Main.screenPosition, frame, new Color(105, 190, 255, 0) * .7f, rotation, origin, scale, effects);
            }
            return false;
        }
    }
}
