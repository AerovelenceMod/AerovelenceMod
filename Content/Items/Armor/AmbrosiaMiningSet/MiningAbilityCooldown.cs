namespace AerovelenceMod.Content.Items.Armor.AmbrosiaMiningSet
{
	public class MiningAbilityCooldown : ModBuff
	{
		public override void SetStaticDefaults()
		{
			LocalizationManager.Bind(DisplayName.Key, DisplayName);
        	LocalizationManager.Bind(Description.Key, Description);
        	LocalizationManager.RegisterTranslation(DisplayName.Key, "Mining Power Withdrawl", "default");
        	LocalizationManager.RegisterTranslation(Description.Key, "Can't use Ambrosia mining set bonus while this is active", "default");
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.LongerExpertDebuff[Type] = false;
		}
	}
}