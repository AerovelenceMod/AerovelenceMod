using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.Trophies
{
    public class CrystalTumblerTrophy : ModItem
    {
        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 16, 50000, ModContent.TileType<CrystalTumblerTrophyPlaced>(), ItemRarities.EarlyPHM);
        }
    }

    public class CrystalTumblerTrophyPlaced : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleWrapLimit = 36;
            TileObjectData.addTile(Type);
            DustType = 7;
            TileID.Sets.DisableSmartCursor[Type] = true;
            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(120, 85, 60), name);
        }
    }
}
