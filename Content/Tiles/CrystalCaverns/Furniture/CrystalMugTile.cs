using AerovelenceMod.Content.Dusts;
using Terraria.DataStructures;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture
{
    public class CrystalMugTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = false;
            Main.tileLavaDeath[Type] = false;
            HitSound = SoundID.Shatter;
            DustType = ModContent.DustType<CavernCrystalDust>();
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.addTile(Type);
            AddMapEntry(new Color(099, 155, 255));
        }
    }

    public class CrystalMugItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Crystal Mug");
        }
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 0, ModContent.TileType<CrystalMugTile>());
        }
    }
}
