




namespace AerovelenceMod.Common.Systems.Traversal
{
    public abstract class MovingGrappleHook : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.aiStyle == ProjAIStyleID.Hook;

        protected static void Hold(Projectile projectile, Player player, Vector2 anchor)
        {
            projectile.Center = anchor;
            projectile.velocity = Vector2.Zero;
            projectile.timeLeft = 2;
            if (player.grapCount < player.grappling.Length)
                player.grappling[player.grapCount++] = projectile.whoAmI;
        }

        protected static bool Retract(Projectile projectile)
        {
            projectile.ai[0] = 1;
            projectile.timeLeft = 3600;
            projectile.netUpdate = true;
            return true;
        }

        internal abstract bool TryGetAnchor(Projectile projectile, out Vector2 anchor);
    }

    public sealed class MovingGrappleSystem : ModSystem
    {
        public override void Load() => On_Player.GetGrapplingForces += GrapplingForces;

        public override void Unload() => On_Player.GetGrapplingForces -= GrapplingForces;

        private static void GrapplingForces(On_Player.orig_GetGrapplingForces orig, Player player, Vector2 fromPosition, out int? direction, out float velocityX, out float velocityY)
        {
            Vector2? anchor = Main.netMode != NetmodeID.MultiplayerClient || player.whoAmI == Main.myPlayer ? RefreshAnchors(player) : null;
            orig(player, fromPosition, out direction, out velocityX, out velocityY);
            if (!anchor.HasValue || player.grapCount != 1 || player.dead || !player.active
                || Vector2.DistanceSquared(player.Center, fromPosition) > 0.0001f
                || Vector2.DistanceSquared(player.Center, anchor.Value) > 8 * 8) return;
            Vector2 movement = new(velocityX, velocityY);
            if (movement.LengthSquared() > 8 * 8) return;
            Vector2 allowed = Collision.TileCollision(player.position, movement, player.width, player.height, true, true, (int)player.gravDir);
            if (Vector2.DistanceSquared(movement, allowed) > 0.0001f || Collision.SolidCollision(player.position + movement, player.width, player.height)) return;
            player.position += movement;
            velocityX = velocityY = 0;
            player.GoingDownWithGrapple = false;
            player.fallStart = player.fallStart2 = (int)(player.position.Y / 16);
        }

        private static Vector2? RefreshAnchors(Player player)
        {
            Vector2? attached = null;
            for (int i = 0; i < player.grapCount && i < player.grappling.Length; i++)
            {
                int index = player.grappling[i];
                if (index < 0 || index >= Main.maxProjectiles) continue;
                Projectile projectile = Main.projectile[index];
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.aiStyle != ProjAIStyleID.Hook || projectile.ai[0] != 2) continue;
                foreach (GlobalProjectile global in projectile.EntityGlobals)
                {
                    if (global is not MovingGrappleHook hook || !hook.TryGetAnchor(projectile, out Vector2 anchor)) continue;
                    projectile.Center = anchor;
                    attached = anchor;
                    break;
                }
            }
            return attached;
        }
    }
}
