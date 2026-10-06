/*




namespace AerovelenceMod.Content.Items.Sets.Phantic.Armor
{
    //Melee
    [AutoloadEquip(EquipType.Head)]
    public class PhanticHelmet : ModItem
    {
		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<PhanticChestplate>() && legs.type == ModContent.ItemType<PhanticGreaves>() && head.type == ModContent.ItemType<PhanticHelmet>();
		}
		public override void UpdateArmorSet(Player player)
		{

            var ap = player.GetModPlayer<AeroPlayer>();
            player.setBonus = ".";

        } 	
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = 10;
            Item.rare = ItemRarities.MidPHM;
            Item.defense = 3;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.02f;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ModContent.ItemType<PhanticBar>(), 12)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    //Ranger
    [AutoloadEquip(EquipType.Head)]
    public class PhanticHood : ModItem
    {
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<PhanticChestplate>() && legs.type == ModContent.ItemType<PhanticGreaves>() && head.type == ModContent.ItemType<PhanticHood>();
        }
        public override void UpdateArmorSet(Player player)
        {

            var ap = player.GetModPlayer<AeroPlayer>();
            player.setBonus = ".";

        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = 10;
            Item.rare = ItemRarities.MidPHM;
            Item.defense = 3;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.02f;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ModContent.ItemType<PhanticBar>(), 12)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    //Mage
    [AutoloadEquip(EquipType.Head)]
    public class PhanticHat : ModItem
    {
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<PhanticChestplate>() && legs.type == ModContent.ItemType<PhanticGreaves>() && head.type == ModContent.ItemType<PhanticHat>();
        }
        public override void UpdateArmorSet(Player player)
        {

            var ap = player.GetModPlayer<AeroPlayer>();
            player.setBonus = ".";

        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = 10;
            Item.rare = ItemRarities.MidPHM;
            Item.defense = 3;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += 0.02f;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ModContent.ItemType<PhanticBar>(), 12)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }

    //Summoner
    [AutoloadEquip(EquipType.Head)]
    public class PhanticMask : ModItem
    {
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<PhanticChestplate>() && legs.type == ModContent.ItemType<PhanticGreaves>() && head.type == ModContent.ItemType<PhanticMask>();
        }
        public override void UpdateArmorSet(Player player)
        {

            var ap = player.GetModPlayer<AeroPlayer>();
            player.setBonus = ".";

        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = 10;
            Item.rare = ItemRarities.MidPHM;
            Item.defense = 3;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.02f;
        }

        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ModContent.ItemType<PhanticBar>(), 12)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}*/
