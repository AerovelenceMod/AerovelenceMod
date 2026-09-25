using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Content.Dusts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using static Terraria.ModLoader.ModContent;

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
            DustType = DustType<CavernCrystalDust>();
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
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = 999;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.consumable = true;
            Item.useStyle = ItemUseStyleID.Swing;
			Item.value = Item.sellPrice(0, 0, 0, 0);
            Item.createTile = ModContent.TileType<CrystalMugTile>();
        }
    }
}
