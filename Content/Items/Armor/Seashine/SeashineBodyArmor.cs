namespace AerovelenceMod.Content.Items.Armor.Seashine
{
    [AutoloadEquip(EquipType.Body)]
    public class SeashineBodyArmor : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Seashine Body Armor", "2% increased minion knockback");
        } 			
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.value = 10;
            Item.rare = ItemRarities.EarlyPHM;
            Item.defense = 3;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetKnockback(DamageClass.Summon).Base += 0.02f;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.SandBlock, 25)
                .AddIngredient(ItemID.Seashell, 5)
                .AddIngredient(ItemID.Starfish, 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}