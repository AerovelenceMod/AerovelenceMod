namespace AerovelenceMod.Common.Utilities.Generation.StructureStamper
{
    public class TheTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            MineResist = 1f;
            MinPick = 10;
            Main.tileSolid[Type] = false;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = false;
            Main.tileLighted[Type] = false;
            AddMapEntry(new Color(213, 0, 255));
            DustType = 59;
            HitSound = SoundID.Dig;
        }
    }

    public class TheItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.consumable = false;
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 0, ModContent.TileType<TheTile>());
            Item.tileBoost += 20;
        }
    }
}
