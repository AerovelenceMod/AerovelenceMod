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
            int direction = MathF.Cos(Projectile.ai[1]) < 0 ? -1 : 1;
            float angle = Projectile.ai[1] + MathF.Cos(progress * MathHelper.Pi) * direction * .8f;
            Vector2 along = angle.ToRotationVector2();
            Vector2 across = new(-along.Y, along.X);
            float reach = MathF.Sin(progress * MathHelper.Pi) * 260;
            for (int i = 0; i <= 16; i++)
            {
                float t = i / 16f;
                points.Add(Projectile.Center + along * (reach * t)
                    + across * (MathF.Sin(t * MathHelper.Pi) * MathF.Sin(progress * MathHelper.TwoPi) * 35));
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
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 edge = points[i + 1] - points[i];
                float rotation = edge.ToRotation() - MathHelper.PiOver2;
                Color light = Lighting.GetColor(points[i].ToTileCoordinates());
                Main.EntitySpriteDraw(line, points[i] - Main.screenPosition, null, light.MultiplyRGB(new Color(115, 140, 170)), rotation,
                    new Vector2(line.Width * .5f, 0), new Vector2(.6f, (edge.Length() + 1) / line.Height), SpriteEffects.None);
                Rectangle frame = i == 0 ? new(0, 0, 26, 30) : new(0, 32 + Math.Min(2, (i - 1) / 5) * 16, 26, 14);
                Vector2 origin = new(13, i == 0 ? 6 : 7);
                Main.EntitySpriteDraw(texture, points[i] - Main.screenPosition, frame, light, rotation, origin, 1, SpriteEffects.None);
                Main.EntitySpriteDraw(glow, points[i] - Main.screenPosition, frame, new Color(105, 190, 255, 0) * .7f, rotation, origin, 1, SpriteEffects.None);
            }
            float tipRotation = (points[^1] - points[^2]).ToRotation() - MathHelper.PiOver2;
            Main.EntitySpriteDraw(texture, points[^1] - Main.screenPosition, new Rectangle(0, 80, 26, 14), lightColor, tipRotation, new Vector2(13, 7), 1, SpriteEffects.None);
            Main.EntitySpriteDraw(glow, points[^1] - Main.screenPosition, new Rectangle(0, 80, 26, 14), new Color(105, 190, 255, 0) * .7f, tipRotation, new Vector2(13, 7), 1, SpriteEffects.None);
            return false;
        }
    }
}
