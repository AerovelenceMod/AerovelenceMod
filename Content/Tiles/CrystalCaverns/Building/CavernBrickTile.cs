using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Building
{
    [LegacyName("CavernBrick")]
    public class CavernBrickTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            MineResist = 2.5f;
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            Main.tileMerge[Type][ModContent.TileType<CrackedCavernBrickTile>()] = true;
            AddMapEntry(new Color(061, 079, 110));
            DustType = 59;
            HitSound = SoundID.Tink;
        }
    }

    public class CavernBrickItem : ModItem
    {
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 0, ModContent.TileType<CavernBrickTile>());
        }
    }
}
