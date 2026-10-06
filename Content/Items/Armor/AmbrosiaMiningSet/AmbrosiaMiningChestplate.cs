namespace AerovelenceMod.Content.Items.Armor.AmbrosiaMiningSet
{
    [AutoloadEquip(EquipType.Body)]
    public class AmbrosiaMiningChestplate : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Ambrosia Mining Chestplate", "Increased jump height");
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 18;
            Item.value = Item.sellPrice(0, 2, 0, 0);
            Item.rare = ItemRarities.EarlyPHM;
        }
        public override void UpdateEquip(Player player)
        {
            player.jumpSpeedBoost += 1.5f;
        }
    }
}