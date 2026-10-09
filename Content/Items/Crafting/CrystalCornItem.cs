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
            Item.maxStack = Item.CommonMaxStack;
            Item.width = 26;
            Item.height = 22;
            Item.value = 10;
            Item.rare = ItemRarities.BasicMaterials;
        }
    }
}
