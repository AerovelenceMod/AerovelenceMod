using Terraria.GameContent.Bestiary;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Language
{
    public abstract class TranslatableModNPC : ModNPC
    {
        public string GetLocalizedName() => DisplayName.Value;

        public string GetLocalizedBestiaryFlavor()
            => LocalizationManager.TryGetTranslation($"{Mod.Name}.{Name}.BestiaryFlavor", out string text) ? text : "";

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            base.SetBestiary(database, bestiaryEntry);
            if (!string.IsNullOrEmpty(GetLocalizedBestiaryFlavor()))
                bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement(this.GetLocalization("BestiaryFlavor").Key));
        }
    }
}
