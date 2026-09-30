/*using AerovelenceMod.Common.Systems.Language;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Sets.Slate
{
    public class SlateOre : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Slate Slab", "");
        }

        public override void SetDefaults()
        {
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.consumable = true;

            Item.maxStack = Item.CommonMaxStack;
            Item.useAnimation = 15;
            Item.useTime = 10;

            Item.createTile = ModContent.TileType<SlateOreTile>();

            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.sellPrice(silver: 9);
        }
    }

    public class SlateOreTile : ModTile
    {
        public override void SetStaticDefaults()
        {
			MineResist = 1f;
			MinPick = 35;
            Main.tileSolid[Type] = true;
            Main.tileSpelunker[Type] = true;
            Main.tileMergeDirt[Type] = false;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = false;
			DustType = 4;
			HitSound = SoundID.Tink;			
            AddMapEntry(new Color(108, 114, 116),Terraria.Localization.Language.GetText("Slate Slab"));
        }
    }
}*/