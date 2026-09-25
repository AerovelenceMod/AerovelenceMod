using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Crafting
{
    public class CrystalCornItem : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Crystal Corn", "");
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.maxStack = 9999;
            Item.width = 26;
            Item.height = 22;
            Item.value = 10;
            Item.rare = ItemRarities.BasicMaterials;
        }
    }
}
