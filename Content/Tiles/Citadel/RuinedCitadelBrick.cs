namespace AerovelenceMod.Content.Tiles.Citadel
{
    public class RuinedCitadelBrick : ModTile
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
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            AddMapEntry(new Color(102, 108, 117));
            DustType = 116;
            HitSound = SoundID.Tink;
        }
        public override bool CanExplode(int i, int j)
        {
            return true;
        }
    }

    public class RuinedCitadelBrickItem : ModItem
    {
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 0, ModContent.TileType<RuinedCitadelBrick>());
        }
    }
}
