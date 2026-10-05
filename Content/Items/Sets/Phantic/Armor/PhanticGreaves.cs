/*




namespace AerovelenceMod.Content.Items.Sets.Phantic.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public class PhanticGreaves : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.value = 10;
            Item.rare = ItemRarities.MidPHM;
            Item.defense = 4;
        }
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.05f;
        }
        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ModContent.ItemType<PhanticBar>(), 15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}*/
