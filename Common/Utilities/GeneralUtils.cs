using System;

namespace AerovelenceMod.Common.Utilities
{
    public static class GeneralUtils
    {
        public static void strikeNPCsInRadius(Projectile projectile, Vector2 position, float radius, float damage, float knockback, int exceptionID = -1)
        {
            if (projectile.owner != Main.myPlayer) return;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC target = Main.npc[i];
                if (!target.active || target.dontTakeDamage || target.friendly || i == exceptionID || Vector2.Distance(position, target.Center) >= radius) continue;
                NPC.HitInfo hit = new()
                {
                    Damage = (int)(damage * Main.rand.NextFloat(0.85f, 1.15f)),
                    Knockback = knockback * target.knockBackResist,
                    HitDirection = position.X < target.Center.X ? 1 : -1
                };
                target.StrikeNPC(hit);
                target.PlayerInteraction(projectile.owner);
                if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendStrikeNPC(target, hit);
            }
        }
    }
}
