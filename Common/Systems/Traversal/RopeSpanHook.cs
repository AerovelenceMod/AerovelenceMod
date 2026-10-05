using System.IO;



using Terraria.ModLoader.IO;

namespace AerovelenceMod.Common.Systems.Traversal
{
    public sealed class RopeSpanHook : MovingGrappleHook
    {
        private int spanId = -1;
        private float parameter;
        public override bool PreAI(Projectile projectile)
        {
            if (projectile.aiStyle != ProjAIStyleID.Hook || projectile.owner < 0 || projectile.owner >= Main.maxPlayers) return true;
            Player player = Main.player[projectile.owner];
            if (spanId < 0 && projectile.ai[0] == 0)
            {
                foreach (RopeSpan candidate in RopeSpanSystem.Spans.Values)
                {
                    if (candidate.Zipline || !candidate.Bounds.Intersects(projectile.Hitbox)) continue;
                    float t = candidate.Closest(projectile.Center);
                    if (Vector2.DistanceSquared(projectile.Center, candidate.At(t)) > 14 * 14) continue;
                    spanId = candidate.Id;
                    parameter = t;
                    projectile.ai[0] = 2;
                    projectile.netUpdate = true;
                    break;
                }
            }
            if (spanId < 0) return true;
            RopeSpan span = RopeSpanSystem.Get(spanId);
            if (span == null || player.dead || !player.active || player.controlJump || projectile.ai[0] == 1 || Vector2.DistanceSquared(player.Center, span.At(parameter)) > 650 * 650)
            {
                spanId = -1;
                return Retract(projectile);
            }
            Hold(projectile, player, span.At(parameter));
            span.AddLoad(parameter, 0.4f);
            return false;
        }

        internal override bool TryGetAnchor(Projectile projectile, out Vector2 anchor)
        {
            RopeSpan span = RopeSpanSystem.Get(spanId);
            anchor = span?.At(parameter) ?? default;
            return span != null && !span.Zipline;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
        {
            writer.Write(spanId);
            writer.Write(parameter);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
        {
            spanId = reader.ReadInt32();
            parameter = MathHelper.Clamp(reader.ReadSingle(), 0, 1);
        }
    }
}
