/*
namespace AerovelenceMod.Content.Items.Sets.Burnshock
{
    public class BurnshockOre : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Burnshock Ore", "");
        }

        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 20, 18, Item.sellPrice(silver: 12), ModContent.TileType<BurnshockOreTile>(), ItemRarities.EarlyHM);
        }
    }

    public class BurnshockOreTile : ModTile
    {
        public override void SetStaticDefaults()
        {
			MineResist = 1.0f;
			MinPick = 180;
            Main.tileSolid[Type] = true;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = true;
            Main.tileSpelunker[Type] = true;
            Main.tileLighted[Type] = false;
            DustType = 59;
            HitSound = SoundID.Tink;
			
			
            AddMapEntry(new Color(255, 090, 090), Terraria.Localization.Language.GetText("Burnshock Ore"));
        }

        public override bool CanExplode(int i, int j)
        {
            return false;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0.0f;
            g = 0.6f;
            b = 0.9f;
        }
    }
}*/
