using AerovelenceMod.Common.Systems;
using static AerovelenceMod.Common.Systems.GlowVineSystem;

namespace AerovelenceMod.Common.Bases
{
    public abstract class BaseGlowVine : ModTile
    {
        public sealed override void SetStaticDefaults()
        {
            SetStaticDefaultsBase();

            RegisterGlowVine();
        }

        public abstract void SetStaticDefaultsBase();

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Tile tile = Framing.GetTileSafely(i, j + 1);
            if (tile.HasTile && tile.TileType == Type)
            {
                WorldGen.KillTile(i, j + 1);
            }
        }

        public abstract override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak);

        public abstract override void RandomUpdate(int i, int j);

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            AddGlowVinePoint(i, j);
            return false;
        }

        public abstract GlowVineData GetGlowVineData(int x, int y);

        public void RegisterGlowVine()
        {
            RegisterGlowVineType(Type, GetGlowVineData);
        }
    }
}
