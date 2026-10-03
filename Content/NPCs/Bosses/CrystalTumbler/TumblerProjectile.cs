using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler
{
    public abstract class TumblerProjectile : ModProjectile
    {
        internal virtual float RetirementPhase => Projectile.ai[1];
        internal virtual bool EmitsRetirementSparks => true;
        internal virtual bool ClearForEdgeCharge => false;
        internal virtual bool TryRetire() => false;
        internal virtual bool? PreDrawRetirement(Vector2 velocity, float opacity, Color color) => null;

        protected float OwnerPhase
        {
            get
            {
                int owner = (int)Projectile.ai[0];
                return owner >= 0 && owner < Main.maxNPCs && Main.npc[owner].ModNPC is CrystalTumbler ? Main.npc[owner].ai[2] : 0f;
            }
        }
    }
}
