using System.IO;
using AerovelenceMod.Common.Globals.Worlds;
using AerovelenceMod.Content.Biomes;

namespace AerovelenceMod.Common.Globals.Players
{
    /// <summary>
    /// ModPlayer responsible for handling biome logic.
    /// </summary>
    public class ZonePlayer : ModBiome
    {
        public const int DEFAULT_TILE_REQUIREMENT = 100;

        public bool ZoneCrystalCaverns { get; private set; }
        public bool ZoneCrystalCitadel { get; private set; }

    }
}
