using System.IO;
using Terraria.ModLoader.IO;

namespace AerovelenceMod.Common.Globals
{
    public sealed class ProjectileAim : GlobalProjectile
    {
        public override bool InstancePerEntity => true;
        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation) => entity.type >= ProjectileID.Count;

        private Vector2 cursor;
        private Vector2 sentCursor;
        private bool initialized;
        private ulong lastSent;

        internal Vector2 MouseWorld(Projectile projectile)
        {
            if (projectile.owner == Main.myPlayer && !Main.dedServ)
            {
                cursor = Main.MouseWorld;
                if (!initialized || Main.GameUpdateCount - lastSent >= 3 && Vector2.DistanceSquared(cursor, sentCursor) >= 4f)
                {
                    initialized = true;
                    sentCursor = cursor;
                    lastSent = Main.GameUpdateCount;
                    projectile.netUpdate = true;
                    projectile.netImportant = true;
                }
                return cursor;
            }
            return initialized ? cursor : projectile.Center + projectile.velocity.SafeNormalize(Vector2.UnitX) * 160f;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
        {
            bitWriter.WriteBit(initialized);
            if (!initialized) return;
            writer.Write(cursor.X);
            writer.Write(cursor.Y);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
        {
            initialized = bitReader.ReadBit();
            if (!initialized) return;
            cursor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            if (!float.IsFinite(cursor.X) || !float.IsFinite(cursor.Y)) initialized = false;
        }
    }
}
