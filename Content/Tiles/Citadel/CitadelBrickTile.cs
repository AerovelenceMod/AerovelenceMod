namespace AerovelenceMod.Content.Tiles.Citadel
{
    [LegacyName("CitadelBrick")]
    public class CitadelBrickTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            MineResist = 2.5f;
            MinPick = 180;
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = false;
            AddMapEntry(new Color(061, 079, 110));
            DustType = 59;
            HitSound = SoundID.Tink;
        }
    }

    public class CitadelBrickItem : ModItem
    {
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 0, ModContent.TileType<CitadelBrickTile>());
        }
    }
}
