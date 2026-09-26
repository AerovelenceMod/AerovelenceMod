using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Crafting
{
    public class CrystalCornItem : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Crystal Corn", "Plant in soil or feed to a Baby Condurtle")
                .AddTooltip(Language.Spanish, "Plántalo en tierra o dáselo de comer a un Condurtle Bebé");
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<CrystalCorn>());
            Item.maxStack = 9999;
            Item.width = 26;
            Item.height = 22;
            Item.value = 10;
            Item.rare = ItemRarities.BasicMaterials;
        }
    }
}
