namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Glimmerwood
{
    public class GlimmerwoodBeamTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            AddMapEntry(new Color(061, 079, 110));
            TileID.Sets.IsBeam[Type] = true;
            DustType = 59;
            HitSound = SoundID.Dig;
        }
        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            if (!WorldGen.InWorld(i, j, 2)) return;
            bool trackBelow = MinecartTrackUtils.IsTrack(Main.tile[i, j + 1]);
            bool trackAbove = MinecartTrackUtils.IsTrack(Main.tile[i, j - 1]) &&
                Main.tile[i, j - 2].HasTile && Main.tile[i, j - 2].TileType == Type;
            if (!(trackBelow && Connected(i, j - 1) || trackAbove && Connected(i, j + 1))) return;
            Rectangle frame = CommonTileHelper.VerticalBeamFrame(Main.tile[i, j].TileFrameNumber);
            tileFrameX = (short)frame.X;
            tileFrameY = (short)frame.Y;
        }
        private bool Connected(int i, int j) => Main.tile[i, j].HasTile && (Main.tile[i, j].TileType == Type || WorldGen.SolidTile(i, j));

        public class GlimmerwoodBeamItem : ModItem
        {
            public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodBeamTile>());
        }
    }
}
