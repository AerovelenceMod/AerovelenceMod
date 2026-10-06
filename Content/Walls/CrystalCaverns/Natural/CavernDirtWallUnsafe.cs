
using AerovelenceMod.Content.Dusts;





namespace AerovelenceMod.Content.Walls.CrystalCaverns.Natural
{
    public class CavernDirtWallUnsafe : ModWall
    {
        public override void SetStaticDefaults()
        {
            this.SimpleWall(ModContent.ItemType<CavernDirtWallItem>(), SoundID.Dig,
            DustID.Dirt, new Color(60, 60, 80), false);
            WallID.Sets.Conversion.Dirt[Type] = true;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }
}
