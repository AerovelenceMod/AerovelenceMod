
using System.Collections.Generic;
using Terraria.WorldBuilding;
using LocalizedText = Terraria.Localization.LocalizedText;
using AerovelenceMod.Common.Systems.Generation.CrystalCaverns;


namespace AerovelenceMod.Common.Systems.Generation
{
    public class WorldGenSystem : ModSystem
    {
        public static LocalizedText CrystalCavernsTerrainPassMessage { get; private set; }
        public static LocalizedText CrystalCavernsStructurePassMessage { get; private set; }
        public static LocalizedText LivingTreeIslandsPassMessage { get; private set; }
        public static LocalizedText CrystalCavernsRubblePassMessage { get; private set; }

        public static LocalizedText CrystalFieldsPassMessage { get; private set; }
        public static LocalizedText CavernCrossingsPassMessage { get; private set; }

        public override void SetStaticDefaults()
        {
            LivingTreeIslandsPassMessage = this.Localize("Growing living tree sky islands");
            CrystalCavernsTerrainPassMessage = this.Localize("Shaping Crystal Caverns");
            CrystalCavernsStructurePassMessage = this.Localize("Placing Crystal Caverns structures");
            CrystalCavernsRubblePassMessage = this.Localize("Decorating Crystal Caverns");
            CrystalFieldsPassMessage = this.Localize("Growing Crystal Fields and carving lakes");
            CavernCrossingsPassMessage = this.Localize("Laying cavern bridges and ziplines");
        }

        public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight)
        {
            totalWeight += InsertAfter(tasks, "Jungle Chests",
                CCTerrainPass.Instance("Crystal Caverns Terrain", 100f),
                new SilkenCitadelPass(),
                new CCStructurePass("Crystal Caverns Polish", 101f), new CrystalFieldsPass());
            totalWeight += InsertAfter(tasks, "Tile Cleanup",
                new CCRubblePass("Crystal Caverns Rubble", 102f));
            LivingTreeIslandPass islands = new();
            totalWeight += InsertAfter(tasks, "Floating Islands",
                islands);
            totalWeight += InsertAfter(tasks, "Floating Island Houses",
                new Terraria.GameContent.Generation.PassLegacy("Living Tree Island Structures", islands.Finish, 10f));
            totalWeight += InsertAfter(tasks, "Final Cleanup",
                new global::AerovelenceMod.Content.Tiles.Citadel.SilkenCachePass(), new CrystalFieldsPass(true));
        }

        private static double InsertAfter(List<GenPass> tasks, string name, params GenPass[] passes)
        {
            int index = tasks.FindIndex(pass => pass.Name == name);
            if (index < 0) return 0;
            tasks.InsertRange(index + 1, passes);
            double weight = 0;
            foreach (GenPass pass in passes)
                weight += pass.Weight;
            return weight;
        }
    }
}
