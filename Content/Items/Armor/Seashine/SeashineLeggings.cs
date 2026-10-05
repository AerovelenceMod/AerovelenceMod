namespace AerovelenceMod.Content.Items.Armor.Seashine
{
    [AutoloadEquip(EquipType.Legs)]
    public class SeashineLeggings : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Seashine Leggings", "3% increased movement speed");
        }		
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = 10;
            Item.rare = ItemRarities.EarlyPHM;
            Item.defense = 2;
        }

        public override void UpdateEquip(Player player)
        {
			player.moveSpeed += 0.03f;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.SandBlock, 20)
                .AddIngredient(ItemID.Seashell, 3)
                .AddIngredient(ItemID.Starfish, 3)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}