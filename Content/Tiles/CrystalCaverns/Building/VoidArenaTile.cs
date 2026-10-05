




namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Building
{
    public class VoidArenaTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            MineResist = 2.5f;
            MinPick = 59;
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileLighted[Type] = false;
            AddMapEntry(new Color(061, 079, 110));
            DustType = 59;
            HitSound = SoundID.Tink;
        }
    }
}
