namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural
{
    [LegacyName("CavernSand")]
    public class CavernSandTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            MineResist = 2.5f;
            Main.tileSolid[Type] = true;
            //Main.tileMerge[Type][Mod.Find<ModTile>("CrystalGrass").Type] = true;
            //Main.tileMerge[Type][Mod.Find<ModTile>("CavernCrystal").Type] = true;
            //Main.tileMerge[Type][Mod.Find<ModTile>("CavernStone").Type] = true;
            //Main.tileMerge[Type][Mod.Find<ModTile>("FieldStone").Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlendAll[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            AddMapEntry(new Color(90, 90, 120));
            DustType = DustID.Stone;
            HitSound = SoundID.Dig;
            TileID.Sets.GeneralPlacementTiles[Type] = false;
        }
        public override bool CanExplode(int i, int j)
        {
            return true;
        }
    }

    public class CavernSandItem : ModItem
    {
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 0, ModContent.TileType<CavernSandTile>());
        }
    }
}
