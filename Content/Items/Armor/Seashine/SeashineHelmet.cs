namespace AerovelenceMod.Content.Items.Armor.Seashine
{
    [AutoloadEquip(EquipType.Head)]
    public class SeashineHelmet : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Seashine Helmet", "6% increased summoning damage\n8% less mana cost\n+1 summon slot");
        }
		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<SeashineBodyArmor>() && legs.type == ModContent.ItemType<SeashineLeggings>() && head.type == ModContent.ItemType<SeashineHelmet>();
		}
		public override void UpdateArmorSet(Player player)
		{
            player.setBonus = "Summons a flying crab to protect you\nMovement speed in water heavily increased";
            if (player.wet)
            {
                player.moveSpeed += 0.2f;
            }
            if (Main.myPlayer == player.whoAmI && player.FindBuffIndex(ModContent.BuffType<CrabBuff>()) == -1)
            {
                player.AddBuff(ModContent.BuffType<CrabBuff>(), 2, false);
                if(player.ownedProjectileCounts[ModContent.ProjectileType<SeaCrab>()] <= 0)
                {
                    Projectile.NewProjectile(player.GetSource_FromThis(), player.Center.X, player.Center.Y, 0f, 0f, ModContent.ProjectileType<SeaCrab>(), (int)(25f * player.GetDamage(DamageClass.Summon).Multiplicative), player.GetKnockback(DamageClass.Summon).Base, player.whoAmI);
                }
            }
        } 	

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = 10;
            Item.rare = ItemRarities.EarlyPHM;
            Item.defense = 1;
        }
		public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.06f;
            player.manaCost -= 0.08f;
            player.maxMinions += 1;
        }
        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ItemID.SandBlock, 20)
                .AddIngredient(ItemID.Seashell, 5)
                .AddIngredient(ItemID.Starfish, 3)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}