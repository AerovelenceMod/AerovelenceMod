using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Language
{
    public static class TooltipHelper
    {
        public static void ApplyTranslations(ModItem item, List<TooltipLine> tooltips)
        {
            if (LocalizationManager.TryGetTranslation(LocalizationExtensions.ItemKey(item, "DisplayName"), out _))
            {
                TooltipLine name = tooltips.Find(line => line.Mod == "Terraria" && line.Name == "ItemName");
                if (name != null)
                    name.Text = item.Item.HoverName;
            }
            string skill = LocalizationExtensions.GetLocalizedSkillStrike(item);
            if (!string.IsNullOrEmpty(skill))
            {
                tooltips.RemoveAll(line => line.Mod == item.Mod.Name && line.Name == "SkillStrike");
                int insertion = tooltips.FindLastIndex(line => IsDescription(item, line));
                insertion = insertion < 0 ? FooterIndex(tooltips) : insertion + 1;
                tooltips.Insert(insertion, new TooltipLine(item.Mod, "SkillStrike", skill) { OverrideColor = Color.Gold });
            }

            int footer = FooterIndex(tooltips);
            List<TooltipLine> trailing = [];
            for (int i = footer; i < tooltips.Count;)
            {
                if (tooltips[i].Mod == item.Mod.Name)
                {
                    trailing.Add(tooltips[i]);
                    tooltips.RemoveAt(i);
                }
                else i++;
            }
            tooltips.InsertRange(footer, trailing);
        }

        private static bool IsDescription(ModItem item, TooltipLine line)
            => (line.Mod == "Terraria" || line.Mod == item.Mod.Name)
                && line.Name.StartsWith("Tooltip", StringComparison.Ordinal)
                && int.TryParse(line.Name.AsSpan(7), out _);

        private static int FooterIndex(List<TooltipLine> tooltips)
        {
            int index = tooltips.FindIndex(line => line.Mod == "Terraria"
                && (line.Name is "JourneyResearch" or "Expert" or "Master" or "Price" or "SpecialPrice" or "OneDropLogo"
                    || line.Name.StartsWith("Prefix", StringComparison.Ordinal)));
            return index < 0 ? tooltips.Count : index;
        }
    }

    public class InFileLocalizationItem : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.ModItem is { } modItem && modItem.Mod == Mod)
                TooltipHelper.ApplyTranslations(modItem, tooltips);
        }
    }
}
