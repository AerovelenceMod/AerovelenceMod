namespace AerovelenceMod.Content.Items.Armor.Seashine
{
	public class CrabBuff : ModBuff
	{
		public override void SetStaticDefaults()
		{
			LocalizationManager.Bind(DisplayName.Key, DisplayName);
        	LocalizationManager.Bind(Description.Key, Description);
        	LocalizationManager.RegisterTranslation(DisplayName.Key, "Seashine Crab", "default");
        	LocalizationManager.RegisterTranslation(Description.Key, "", "default");
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

        public override void Update(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex] = 18000;
            bool petProjectileNotSpawned = player.ownedProjectileCounts[ModContent.ProjectileType<SeaCrab>()] <= 0;
            if (petProjectileNotSpawned && player.whoAmI == Main.myPlayer)
            {
                Projectile.NewProjectile(player.GetSource_Buff(buffIndex), player.Center, Vector2.Zero, ModContent.ProjectileType<SeaCrab>(), (int)(25f * player.GetDamage(DamageClass.Summon).Multiplicative), player.GetKnockback(DamageClass.Summon).Base, player.whoAmI);
            }
        }
	}
}
