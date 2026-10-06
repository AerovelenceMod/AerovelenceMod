




namespace AerovelenceMod.Content.Items.Crafting
{
    public class PrismaticAsterItem : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Prismatic Aster", "");
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 34;
            Item.value = 1000;
            Item.rare = ItemRarities.BasicMaterials;
            Item.maxStack = Item.CommonMaxStack;
        }
    }
}
