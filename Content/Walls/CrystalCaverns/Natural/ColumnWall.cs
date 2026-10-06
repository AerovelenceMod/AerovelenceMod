
using AerovelenceMod.Content.Dusts;





namespace AerovelenceMod.Content.Walls.CrystalCaverns.Natural
{
    public class ColumnWall : ModWall
    {
        public override void SetStaticDefaults()
        {
            this.SimpleWall(ModContent.ItemType<ColumnWallItem>(), SoundID.Dig,
            DustID.Dirt, new Color(54, 87, 129), true);
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class ColumnWallItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableWall(ModContent.WallType<ColumnWall>());
        }
    }
}
