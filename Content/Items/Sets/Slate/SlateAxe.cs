/*



namespace AerovelenceMod.Content.Items.Sets.Slate
{
    public class SlateAxe : ModItem
    {
        public override void SetDefaults()
        {
			Item.crit = 3;
            Item.damage = 6;
            Item.DamageType = DamageClass.Melee;
            Item.width = 40;
            Item.useTurn = true;
            Item.height = 34;
            Item.useTime = 26;
            Item.useAnimation = 26;
			Item.axe = 13;
			Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 6;
            Item.value = Item.sellPrice(0, 0, 20, 0);
            Item.rare = ItemRarities.EarlyPHM;
            Item.autoReuse = true;
        }


		public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ModContent.ItemType<SlateOre>(), 35)
                .AddRecipeGroup(RecipeGroupID.Wood, 15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}*/
