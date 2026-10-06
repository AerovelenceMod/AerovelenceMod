namespace AerovelenceMod.Content.Items.Armor.AmbrosiaMiningSet
{
    [AutoloadEquip(EquipType.Head)]
    public class AmbrosiaMiningHelmet : TranslatableModItem
    {
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Ambrosia Mining Helmet", "");
        }
		public override bool IsArmorSet(Item head, Item body, Item legs)
		{
			return body.type == ModContent.ItemType<AmbrosiaMiningChestplate>() && legs.type == ModContent.ItemType<AmbrosiaMiningBoots>() && head.type == ModContent.ItemType<AmbrosiaMiningHelmet>();
		}
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "20% increased digging speed\nDouble tap " + Terraria.Localization.Language.GetTextValue(Main.ReversedUpDownArmorSetBonuses ? "Key.DOWN" : "Key.UP") + " to shoot energy that can explode tiles";
            player.pickSpeed -= 0.59f;
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.value = Item.sellPrice(0, 2, 0, 0);
            Item.rare = ItemRarities.EarlyPHM;
        }
    }

    public class AmbrosiaPlayer : ModPlayer
    {
        public override void SetControls()
        {
            for (int i = 0; i < 4; i++)
            {
                bool JustPressed = false;
                switch (i) {
                    case 0:
                        JustPressed = Player.controlDown && Player.releaseDown;
                        break;
                    case 1:
                        JustPressed = Player.controlUp && Player.releaseUp;
                        break;
                }
                if (JustPressed && Player.doubleTapCardinalTimer[i] > 0 && JustPressed && Player.doubleTapCardinalTimer[i] < 15)
                    KeyDoubleTap(i);
            }
        }

        private void KeyDoubleTap(int keyDir)
        {
            int inputKey = 1;
            if (Main.ReversedUpDownArmorSetBonuses)
                inputKey = 0;
            if (keyDir == inputKey)
            {
				if (Player.armor[0].type == ModContent.ItemType<AmbrosiaMiningHelmet>() && !Player.HasBuff<MiningAbilityCooldown>())
				{
					Terraria.Projectile.NewProjectile(Player.GetSource_Misc("SetBonus_AmbrosiaSetBonus"), Player.Center, (Terraria.Main.MouseWorld - Player.Center) / 10, ModContent.ProjectileType<MiningEnergyBlast>(), 1, 0);
					Player.AddBuff(ModContent.BuffType<MiningAbilityCooldown>(), 300);
				}
            }
		}
    }
}