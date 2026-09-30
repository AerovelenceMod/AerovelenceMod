using AerovelenceMod.Common.Systems.Language;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

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
            Item.useTurn = true;
            Item.consumable = true;
            Item.autoReuse = true;

            Item.maxStack = Item.CommonMaxStack;
            Item.useAnimation = 15;
            Item.useTime = 10;

            Item.createTile = ModContent.TileType<BurnshockOreTile>();

            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = Item.sellPrice(silver: 12);
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
}