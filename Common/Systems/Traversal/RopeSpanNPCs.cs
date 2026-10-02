using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Traversal
{
    public sealed class RopeSpanNPCs : ModSystem
    {
        public override void Load() => On_NPC.UpdateNPC += UpdateNPC;
        public override void Unload() => On_NPC.UpdateNPC -= UpdateNPC;
        private static void UpdateNPC(On_NPC.orig_UpdateNPC orig, NPC npc, int index)
        {
            Vector2 previous = npc.Bottom;
            orig(npc, index);
            if (!npc.active || npc.noTileCollide || npc.noGravity || npc.velocity.Y < 0 || npc.boss) return;
            float surface = float.MaxValue;
            RopeSpan standing = null;
            foreach (RopeSpan span in RopeSpanSystem.Spans.Values)
            {
                if (span.Zipline || npc.Right.X <= span.Nodes[0].X || npc.Left.X >= span.Nodes[^1].X) continue;
                float y = span.SurfaceY(npc.Left.X + 3, npc.Right.X - 3);
                if (previous.Y > y + 7 || npc.Bottom.Y < y - 2 || y >= surface
                    || Collision.SolidCollision(new Vector2(npc.position.X, y - npc.height), npc.width, npc.height)) continue;
                surface = y;
                standing = span;
            }
            if (standing == null) return;
            npc.position.Y = surface - npc.height;
            npc.velocity.Y = 0;
            npc.collideY = true;
            standing.AddLoad(standing.Parameter(npc.Center.X), MathHelper.Clamp(npc.width * npc.height / 900f, .2f, 3));
        }
    }
}
