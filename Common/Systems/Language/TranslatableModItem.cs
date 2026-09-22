using System.Collections.Generic;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Language
{
    public abstract class TranslatableModItem : ModItem
    {
        public string GetLocalizedName() => DisplayName.Value;

        protected static void ApplyTranslations(ModItem item, List<TooltipLine> tooltips)
            => TooltipHelper.ApplyTranslations(item, tooltips);
    }
}
