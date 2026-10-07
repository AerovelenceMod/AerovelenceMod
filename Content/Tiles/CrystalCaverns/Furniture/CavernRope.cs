namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture
{
    public class CavernRope : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileSolid[Type] = false;
            Main.tileCut[Type] = false;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileRope[Type] = true;
            Main.tileFrameImportant[Type] = false;

            AddMapEntry(new Color(123, 123, 123));

            DustType = DustID.Dirt;
            HitSound = SoundID.Dig;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = 3;
    }

    public class CavernRopeItem : ModItem
    {
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 10, ModContent.TileType<CavernRope>());
            Item.tileBoost = 3;
        }
    }
}
