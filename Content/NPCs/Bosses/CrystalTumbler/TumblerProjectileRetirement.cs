using System;
using System.IO;
using AerovelenceMod.Common.Systems;



using Terraria.GameContent;

using Terraria.ModLoader.IO;

namespace AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler
{
    public class TumblerProjectileRetirement : GlobalProjectile
    {
        private int remaining;
        private Vector2 extent;
        private float phase;
        private bool exitPlayed;
        internal static float VisualOpacity(Projectile projectile) => projectile.TryGetGlobalProjectile(out TumblerProjectileRetirement fade) && fade.remaining > 0 ? MathHelper.SmoothStep(0f, 1f, fade.remaining / 36f) : 1f;
        public override bool InstancePerEntity => true;
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => ProjectileLoader.GetProjectile(entity.type)?.GetType().Namespace == typeof(CrystalTumbler).Namespace;
        public static void Begin(Projectile projectile)
        {
            TumblerProjectileRetirement fade = projectile.GetGlobalProjectile<TumblerProjectileRetirement>();
            if (fade.remaining > 0)
                return;
            fade.PlayExit(projectile);
            if (projectile.ModProjectile is TumblerProjectile encounter && encounter.TryRetire())
                return;
            fade.remaining = 36;
            fade.extent = projectile.velocity;
            fade.phase = PhaseFor(projectile);
            projectile.hostile = projectile.friendly = false;
            projectile.damage = 0;
            projectile.tileCollide = false;
            projectile.timeLeft = Math.Max(80, projectile.timeLeft);
            projectile.netUpdate = true;
        }
        public override bool? CanDamage(Projectile projectile) => remaining > 0 ? false : null;
        public override bool ShouldUpdatePosition(Projectile projectile) => remaining <= 0;
        public override bool PreAI(Projectile projectile)
        {
            if (remaining <= 0)
                return true;
            if (remaining == 1)
                projectile.Kill();
            else
                remaining--;
            return false;
        }
        public override bool PreKill(Projectile projectile, int timeLeft)
        {
            PlayExit(projectile);
            return remaining <= 0;
        }

        private void PlayExit(Projectile projectile)
        {
            if (exitPlayed || Main.dedServ || projectile.friendly || projectile.TryGetGlobalProjectile(out TumblerSharedProjectile shared) && !shared.FromEncounter)
                return;
            exitPlayed = true;
            TumblerLightningSystem.Release(projectile);
            if (projectile.ModProjectile is TumblerProjectile { EmitsRetirementSparks: false })
                return;
            float phase = PhaseFor(projectile);
            Color color = TumblerVFX.PhaseColor(phase);
            for (int i = 0; i < 6; i++)
            {
                Vector2 direction = (i * MathHelper.TwoPi / 6f).ToRotationVector2();
                TumblerVFX.SpawnSpark(projectile.Center + direction * Math.Min(28f, projectile.width * 0.5f), direction * Main.rand.NextFloat(1.5f, 3f), Color.Lerp(color, Color.White, 0.65f), 0.21f);
            }
        }
        private static float PhaseFor(Projectile projectile) => projectile.ModProjectile is TumblerProjectile encounter ? encounter.RetirementPhase : projectile.ai[1];

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
        {
            writer.Write(remaining);
            writer.WriteVector2(extent);
            writer.Write(phase);
        }
        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
        {
            remaining = reader.ReadInt32();
            extent = reader.ReadVector2();
            phase = reader.ReadSingle();
            if (remaining > 0)
            {
                PlayExit(projectile);
                projectile.damage = 0;
                projectile.hostile = projectile.friendly = false;
                projectile.tileCollide = false;
            }
        }
        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            TumblerLightningSystem.BeginCapture(projectile);
            if (remaining <= 0)
                return true;
            float opacity = MathHelper.SmoothStep(0f, 1f, remaining / 36f);
            if (projectile.ModProjectile is TumblerProjectile encounter)
            {
                Color color = TumblerVFX.PhaseColor(phase >= 1f ? 1f : 0f);
                bool? draw = encounter.PreDrawRetirement(extent, opacity, color);
                if (draw.HasValue)
                    return draw.Value;
            }
            Vector2 center = projectile.Center - Main.screenPosition;
            if (projectile.ModProjectile.Texture != "Terraria/Images/Projectile_0")
            {
                Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
                Rectangle frame = texture.Frame(1, Math.Max(1, Main.projFrames[projectile.type]), 0, projectile.frame);
                Main.EntitySpriteDraw(texture, center, frame, lightColor * opacity, projectile.rotation, frame.Size() * 0.5f, projectile.scale, SpriteEffects.None);
            }
            return false;
        }

        public override void PostDraw(Projectile projectile, Color lightColor) => TumblerLightningSystem.EndCapture();
    }
}
