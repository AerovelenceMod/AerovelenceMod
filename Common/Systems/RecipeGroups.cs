namespace AerovelenceMod.Common.Systems
{
    public class RecipeGroups : ModSystem
    {
        public override void AddRecipeGroups()
        {
            RecipeGroup group;

            group = new RecipeGroup(() => Terraria.Localization.Language.GetTextValue("LegacyMisc.37") + " Evil Bars",
            [
                ItemID.DemoniteBar,
                ItemID.CrimtaneBar
            ]);
            RecipeGroup.RegisterGroup("AerovelenceMod:EvilBars", group);

            group = new RecipeGroup(() => "Gold or Platinum",
            [
                ItemID.GoldBar,
                ItemID.PlatinumBar,
            ]);
            RecipeGroup.RegisterGroup("AerovelenceMod:GoldOrPlatinum", group);

            group = new RecipeGroup(() => Terraria.Localization.Language.GetTextValue("LegacyMisc.37") + " Mech Souls",
            [
                ItemID.SoulofSight,
                ItemID.SoulofFright,
                ItemID.SoulofMight,
            ]);
            RecipeGroup.RegisterGroup("AerovelenceMod:MechSouls", group);

            group = new RecipeGroup(() => Terraria.Localization.Language.GetTextValue("LegacyMisc.37") + " Silver Bars", new int[]
            {
                ItemID.SilverBar,
                ItemID.TungstenBar
            });
            RecipeGroup.RegisterGroup("AerovelenceMod:SilverBars", group);

            group = new RecipeGroup(() => Terraria.Localization.Language.GetTextValue("LegacyMisc.37") + " Adamantite Bars", new int[]
            {
                ItemID.AdamantiteBar,
                ItemID.TitaniumBar
            });
            RecipeGroup.RegisterGroup("AerovelenceMod:TitaniumBars", group);

            group = new RecipeGroup(() => Terraria.Localization.Language.GetTextValue("LegacyMisc.37") + " Cobalt Bars", new int[]
            {
                ItemID.CobaltBar,
                ItemID.PalladiumBar
            });
            RecipeGroup.RegisterGroup("AerovelenceMod:CobaltBars", group);

            group = new RecipeGroup(() => Terraria.Localization.Language.GetTextValue("LegacyMisc.37") + " Evil Materials", new int[]
            {
                ItemID.ShadowScale,
                ItemID.TissueSample
            });

            RecipeGroup.RegisterGroup("AerovelenceMod:EvilMaterials", group);
        }
    }
}
