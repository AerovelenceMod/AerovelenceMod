using System;



namespace AerovelenceMod.Common.Systems.Traversal
{
    public sealed class RopeSpanNPCs : ModSystem
    {
        private static readonly int[] standingSpans = new int[Main.maxNPCs];
        public override void Load() => On_NPC.UpdateNPC += UpdateNPC;
        public override void Unload()
        {
            On_NPC.UpdateNPC -= UpdateNPC;
            Array.Clear(standingSpans);
        }
        public override void OnWorldUnload() => Array.Clear(standingSpans);

        internal static bool TryGroundContact(NPC npc, out Vector2 point)
        {
            point = npc.Bottom;
            if (!npc.active || npc.whoAmI < 0 || npc.whoAmI >= standingSpans.Length || npc.noTileCollide || npc.noGravity || npc.velocity.Y < 0f) return false;
            RopeSpan span = RopeSpanSystem.Get(standingSpans[npc.whoAmI]);
            if (span == null || span.Zipline || npc.Right.X <= span.Nodes[0].X || npc.Left.X >= span.Nodes[^1].X) return false;
            float surface = span.SurfaceY(npc.Left.X + 3, npc.Right.X - 3);
            if (Math.Abs(npc.Bottom.Y - surface) > 18f
                || Collision.SolidCollision(new Vector2(npc.position.X, surface - npc.height), npc.width, npc.height)) return false;
            point.Y = surface;
            return true;
        }

        private static void UpdateNPC(On_NPC.orig_UpdateNPC orig, NPC npc, int index)
        {
            int previousSpan = standingSpans[index];
            if (previousSpan > 0 && TryGroundContact(npc, out Vector2 contact))
                npc.position.Y = contact.Y - npc.height;
            Vector2 previous = npc.Bottom;
            orig(npc, index);
            standingSpans[index] = 0;
            if (!npc.active || npc.noTileCollide || npc.noGravity || npc.velocity.Y < 0 || npc.boss) return;
            float surface = float.MaxValue;
            RopeSpan standing = null;
            foreach (RopeSpan span in RopeSpanSystem.Spans.Values)
            {
                if (span.Zipline || npc.Right.X <= span.Nodes[0].X || npc.Left.X >= span.Nodes[^1].X) continue;
                float y = span.SurfaceY(npc.Left.X + 3, npc.Right.X - 3);
                bool attached = previousSpan == span.Id && Math.Abs(previous.Y - y) < 18f;
                if (!attached && (previous.Y > y + 7 || npc.Bottom.Y < y - 2) || y >= surface
                    || Collision.SolidCollision(new Vector2(npc.position.X, y - npc.height), npc.width, npc.height)) continue;
                surface = y;
                standing = span;
            }
            if (standing == null) return;
            npc.position.Y = surface - npc.height;
            npc.velocity.Y = 0;
            npc.collideY = true;
            standingSpans[index] = standing.Id;
            standing.AddLoad(standing.Parameter(npc.Center.X), MathHelper.Clamp(npc.width * npc.height / 900f, .2f, 3));
        }
    }
}
