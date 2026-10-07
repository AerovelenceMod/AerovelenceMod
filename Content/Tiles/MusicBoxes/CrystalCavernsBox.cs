using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

namespace AerovelenceMod.Content.Tiles.MusicBoxes
{
    public class CrystalCavernsBox : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileObsidianKill[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.DrawYOffset = 2;
            TileObjectData.addTile(Type);
            TileID.Sets.DisableSmartCursor[Type] = true;
            LocalizedText name = CreateMapEntryName();
            AddMapEntry(new Color(200, 200, 200), name);
            DustType = -1;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ItemType<CrystalCavernsBoxItem>();
        }
    }

    public class CrystalCavernsBoxItem : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Music Box (Crystal Caverns)", "Composed by A44");
            MusicLoader.AddMusicBox(Mod, MusicLoader.GetMusicSlot(Mod, "Sounds/Music/CrystalCaverns"), ModContent.ItemType<CrystalCavernsBoxItem>(), ModContent.TileType<CrystalCavernsBox>());
        }

        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 24, 24, 100000, TileType<CrystalCavernsBox>(), ItemRarities.LatePHM);
            Item.accessory = true;
        }
    }
}
