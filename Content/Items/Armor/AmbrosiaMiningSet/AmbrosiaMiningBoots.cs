namespace AerovelenceMod.Content.Items.Armor.AmbrosiaMiningSet
{
    [AutoloadEquip(EquipType.Legs)]
    public class AmbrosiaMiningBoots : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Ambrosia Mining Boots", "5% increased movment speed");
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.value = Item.sellPrice(0, 2, 0, 0);
            Item.rare = ItemRarities.EarlyPHM;
        }
        public override void UpdateAccessory(Player player, bool isVisible)
        {
            player.moveSpeed += 0.05f;
        }
    }
}