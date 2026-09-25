using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria.WorldBuilding;
using Terraria.Localization;
using AerovelenceMod.Common.Systems.Generation.CrystalCaverns;

namespace AerovelenceMod.Common.Systems.Generation
{
    public class WorldGenSystem : ModSystem
    {
        public static LocalizedText CrystalCavernsTerrainPassMessage { get; private set; }
        public static LocalizedText CrystalCavernsStructurePassMessage { get; private set; }
        public static LocalizedText LivingTreeIslandsPassMessage { get; private set; }
		public static LocalizedText CrystalCavernsRubblePassMessage { get; private set; }

        public override void SetStaticDefaults()
		{
            LivingTreeIslandsPassMessage = Terraria.Localization.Language.GetOrRegister(Mod.GetLocalizationKey($"WorldGen.{nameof(LivingTreeIslandsPassMessage)}"),
                () => "Growing living tree sky islands");
			CrystalCavernsTerrainPassMessage = Terraria.Localization.Language.GetOrRegister(Mod.GetLocalizationKey($"WorldGen.{nameof(CrystalCavernsTerrainPassMessage)}"));
            CrystalCavernsStructurePassMessage = Terraria.Localization.Language.GetOrRegister(Mod.GetLocalizationKey($"WorldGen.{nameof(CrystalCavernsStructurePassMessage)}"));
			CrystalCavernsRubblePassMessage = Terraria.Localization.Language.GetOrRegister(Mod.GetLocalizationKey($"WorldGen.{nameof(CrystalCavernsRubblePassMessage)}"));
		}

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            totalWeight += InsertAfter(tasks, "Jungle Chests", CCTerrainPass.Instance("Crystal Caverns Terrain", 100f));
            totalWeight += InsertAfter(tasks, "Tile Cleanup", new CCRubblePass("Crystal Caverns Rubble", 102f));
            totalWeight += InsertAfter(tasks, "Final Cleanup",
                new SilkenCitadelPass(),
                new CCStructurePass("Crystal Caverns Polish", 101f),
                new global::AerovelenceMod.Content.Tiles.Citadel.SilkenCachePass(),
                new LivingTreeIslandPass());
        }

        private static double InsertAfter(List<GenPass> tasks, string name, params GenPass[] passes)
        {
            int index = tasks.FindIndex(pass => pass.Name == name);
            if (index < 0) return 0;
            tasks.InsertRange(index + 1, passes);
            double weight = 0;
            foreach (GenPass pass in passes) weight += pass.Weight;
            return weight;
        }
    }
}
