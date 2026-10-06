
using AerovelenceMod.Content.Dusts;





namespace AerovelenceMod.Content.Walls.CrystalCaverns.Natural
{
    public class CavernSandWall : ModWall
    {
        public override void SetStaticDefaults()
        {
            this.SimpleWall(ModContent.ItemType<CavernSandWallItem>(), SoundID.Dig,
            DustID.Dirt, new Color(54, 87, 129), false);
            WallID.Sets.Conversion.Sandstone[Type] = true;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class CavernSandWallItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall(ModContent.WallType<CavernStoneWall>());
        }
    }
}
