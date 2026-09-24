using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Language
{
    public class LocalizationManager : ModSystem
    {
        private static readonly Dictionary<string, Dictionary<string, string>> translations = [];
        private static readonly Dictionary<LocalizedText, string> bindings = [];
        private static readonly Dictionary<LocalizedText, Func<string>> derivedText = [];
        private static readonly Action<LocalizedText, string> setValue = typeof(LocalizedText)
            .GetMethod("SetValue", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate<Action<LocalizedText, string>>();
        private static readonly Action<LanguageManager> refreshBoundText = typeof(LanguageManager)
            .GetMethod("RecalculateBoundTextValues", BindingFlags.Instance | BindingFlags.NonPublic)
            .CreateDelegate<Action<LanguageManager>>();

        public override void PostSetupContent()
        {
            ApplyTranslations();
        }

        public override void OnLocalizationsLoaded() => ApplyTranslations();

        public override void Unload()
        {
            translations.Clear();
            bindings.Clear();
            derivedText.Clear();
        }

        public static void RegisterTranslation(string key, string text, string language = "en-US")
        {
            if (!translations.TryGetValue(key, out var values))
                translations[key] = values = [];
            values[language] = text;
        }

        public static bool TryGetTranslation(string key, out string text)
        {
            text = null;
            if (!translations.TryGetValue(key, out var values))
                return false;
            return values.TryGetValue(LanguageManager.Instance.ActiveCulture.Name, out text)
                || values.TryGetValue("default", out text)
                || values.TryGetValue("en-US", out text);
        }

        public static string GetTranslation(string key) => TryGetTranslation(key, out string text) ? text : key;

        internal static LocalizedText Bind(string key, LocalizedText target)
        {
            bindings[target] = key;
            if (TryGetTranslation(key, out string text))
                setValue(target, text);
            return target;
        }

        internal static LocalizedText BindDerived(LocalizedText target, Func<string> getText)
        {
            derivedText[target] = getText;
            return target;
        }

        private void ApplyTranslations()
        {
            foreach (var binding in bindings)
                if (TryGetTranslation(binding.Value, out string text))
                    setValue(binding.Key, text);
            foreach (var binding in derivedText)
                setValue(binding.Key, binding.Value());
            refreshBoundText(LanguageManager.Instance);
            Terraria.UI.ItemTooltip.InvalidateTooltips();
        }
    }

    public enum Language
    {
        /// <summary>
        /// Default/English language, used as fallback
        /// </summary>
        Default,

        /// <summary>
        /// Spanish (es-ES)
        /// </summary>
        Spanish,

        /// <summary>
        /// Russian (ru-RU)
        /// </summary>
        Russian,

        /// <summary>
        /// Chinese - Simplified (zh-Hans)
        /// </summary>
        ChineseSimplified,

        /// <summary>
        /// Chinese - Traditional (zh-Hant)
        /// </summary>
        ChineseTraditional,

        /// <summary>
        /// Portuguese - Brazil (pt-BR)
        /// </summary>
        PortugueseBrazil,

        /// <summary>
        /// German (de-DE)
        /// </summary>
        German,

        /// <summary>
        /// Italian (it-IT)
        /// </summary>
        Italian,

        /// <summary>
        /// French (fr-FR)
        /// </summary>
        French,

        /// <summary>
        /// Polish (pl-PL)
        /// </summary>
        Polish
    }

    public static class LanguageExtensions
    {
        public static string ToCultureCode(this Language language)
        {
            return language switch
            {
                Language.Default => "default",
                Language.Spanish => "es-ES",
                Language.Russian => "ru-RU",
                Language.ChineseSimplified => "zh-Hans",
                Language.ChineseTraditional => "zh-Hant",
                Language.PortugueseBrazil => "pt-BR",
                Language.German => "de-DE",
                Language.Italian => "it-IT",
                Language.French => "fr-FR",
                Language.Polish => "pl-PL",
                _ => "default"
            };
        }

        public static Language FromCultureCode(string cultureCode)
        {
            return cultureCode switch
            {
                "es-ES" => Language.Spanish,
                "ru-RU" => Language.Russian,
                "zh-Hans" => Language.ChineseSimplified,
                "zh-Hant" => Language.ChineseTraditional,
                "pt-BR" => Language.PortugueseBrazil,
                "de-DE" => Language.German,
                "it-IT" => Language.Italian,
                "fr-FR" => Language.French,
                "pl-PL" => Language.Polish,
                _ => Language.Default
            };
        }
    }

    public static class LocalizationExtensions
    {
        public static T ModifyLocalization<T>(this T item, string defaultName, string defaultTooltip = "") where T : ModItem
            => item.AddName(Language.Default, defaultName).AddTooltip(Language.Default, defaultTooltip);

        public static T AddName<T>(this T item, Language language, string name) where T : ModItem
        {
            RegisterItemText(item, "DisplayName", name, language, item.DisplayName);
            return item;
        }

        public static T AddTooltip<T>(this T item, Language language, string tooltip) where T : ModItem
        {
            RegisterItemText(item, "Tooltip", tooltip, language, item.Tooltip);
            return item;
        }

        public static T AddSkillStrike<T>(this T item, Language language, string text) where T : ModItem
        {
            if (!text.Contains("[i:" + ItemID.FallenStar) && !text.Contains("[i:16]"))
                text = $"[i:{ItemID.FallenStar}] {text} [i:{ItemID.FallenStar}]";
            RegisterItemText(item, "SkillStrike", text, language, item.GetLocalization("SkillStrike"));
            return item;
        }

        public static T AddName<T>(this T item, Dictionary<Language, string> translations) where T : ModItem
        {
            foreach (var entry in translations) item.AddName(entry.Key, entry.Value);
            return item;
        }

        public static T AddTooltip<T>(this T item, Dictionary<Language, string> translations) where T : ModItem
        {
            foreach (var entry in translations) item.AddTooltip(entry.Key, entry.Value);
            return item;
        }

        public static T AddSkillStrike<T>(this T item, Dictionary<Language, string> translations) where T : ModItem
        {
            foreach (var entry in translations) item.AddSkillStrike(entry.Key, entry.Value);
            return item;
        }

        internal static string ItemKey(ModItem item, string property) => $"{item.Mod.Name}.{item.Name}.{property}";

        private static void RegisterItemText(ModItem item, string property, string text, Language language, LocalizedText target)
        {
            string key = ItemKey(item, property);
            LocalizationManager.RegisterTranslation(key, text, language.ToCultureCode());
            LocalizationManager.Bind(key, target);
        }

        public static string GetLocalizedSkillStrike(ModItem item)
            => LocalizationManager.TryGetTranslation(ItemKey(item, "SkillStrike"), out string text) ? text : "";
    }

    public static class NPCLocalizationExtensions
    {
        public static T ModifyLocalization<T>(this T npc, string defaultName, string defaultFlavor) where T : ModNPC
            => npc.AddName(Language.Default, defaultName).AddFlavor(Language.Default, defaultFlavor);

        public static T AddName<T>(this T npc, Language language, string name) where T : ModNPC
        {
            string key = $"{npc.Mod.Name}.{npc.Name}.DisplayName";
            LocalizationManager.RegisterTranslation(key, name, language.ToCultureCode());
            LocalizationManager.Bind(key, npc.DisplayName);
            LocalizationManager.Bind(key, LanguageManager.Instance.GetOrRegister($"NPCName.{npc.Type}"));
            return npc;
        }

        public static T AddFlavor<T>(this T npc, Language language, string flavor) where T : ModNPC
        {
            string key = $"{npc.Mod.Name}.{npc.Name}.BestiaryFlavor";
            LocalizationManager.RegisterTranslation(key, flavor, language.ToCultureCode());
            LocalizationManager.Bind(key, npc.GetLocalization("BestiaryFlavor"));
            return npc;
        }
    }

    public static class TileLocalizationExtensions
    {
        public static LocalizedText MapNameFromItem(this ModTile tile, int itemType)
            => LocalizationManager.BindDerived(tile.CreateMapEntryName(), () => Lang.GetItemNameValue(itemType));

        public static T AddMapName<T>(this T tile, Language language, string name) where T : ModTile
        {
            string key = $"{tile.Mod.Name}.{tile.Name}.MapEntry";
            LocalizationManager.RegisterTranslation(key, name, language.ToCultureCode());
            LocalizationManager.Bind(key, tile.CreateMapEntryName());
            return tile;
        }
    }
}
