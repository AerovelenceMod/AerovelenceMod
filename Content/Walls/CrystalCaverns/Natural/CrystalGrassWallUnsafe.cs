
using AerovelenceMod.Content.Dusts;





namespace AerovelenceMod.Content.Walls.CrystalCaverns.Natural
{
    public class CrystalGrassWallUnsafe : ModWall
    {
        public override void SetStaticDefaults()
        {
            this.SimpleWall(ModContent.ItemType<CrystalGrassWallItem>(), SoundID.Dig,
            DustID.Dirt, new Color(54, 87, 129), false);
            WallID.Sets.Conversion.Grass[Type] = true;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }
}
