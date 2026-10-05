using AerovelenceMod.Content.Tiles.CrystalCaverns.Rubble;

using System;
using System.Linq;


using Terraria.IO;

using Terraria.WorldBuilding;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns
{
    public class CCRubblePass : GenPass
    {
        public CCRubblePass(string name, float loadWeight) : base(name, loadWeight) { }

        internal static int DecorateCaves(Rectangle bounds, Func<int, int, bool> allowed)
        {
            CCTerrainPass terrain = CCTerrainPass.Instance();
            int[] types = [ModContent.TileType<CavernStone1x1FloorRubbleNatural>(), ModContent.TileType<CavernStone1x2FloorRubbleNatural>(),
                ModContent.TileType<CavernStone3x2FloorRubbleNatural>(), ModContent.TileType<CrystalGrowthTile>(), // CrystalGrowth functions as both grounded and hanging.
                ModContent.TileType<CavernStone1x1CeilingRubbleNatural>(), ModContent.TileType<CavernStone1x2CeilingRubbleNatural>(),
                ModContent.TileType<CavernPot2x2Rubble>()];
            int count = 0;
            for (int y = bounds.Top + 3; y < bounds.Bottom - 3; y++)
                for (int x = bounds.Left + 3; x < bounds.Right - 3; x++)
                {
                    if (Main.tile[x, y].HasTile || Main.tile[x, y].LiquidAmount > 0 || LushReservoirGenerator.Contains(x, y, 2) || !WorldGen.genRand.NextBool(2) || !allowed(x, y)) continue;
                    bool floor = Natural(x, y + 1), ceiling = Natural(x, y - 1), left = Natural(x - 1, y), right = Natural(x + 1, y);

                    // If there is floor beneath, 1/7 chance to pick a pot, 6/7 chance to pick a random grounded rubble. If no floor, pick a random hanging rubble.
                    int choice = floor ? (WorldGen.genRand.NextBool(7) ? 6 : WorldGen.genRand.Next(4)) : WorldGen.genRand.Next(3, 6);

                    if (!floor && !ceiling && !(choice == 3 && (left || right))) continue; // CrystalGrowth can be placed on the side of tiles

                    bool safe = true;
                    for (int dx = -2; dx <= 2 && safe; dx++)
                        for (int dy = -2; dy <= 2; dy++)
                            if (!allowed(x + dx, y + dy)) { safe = false; break; }
                    if (!safe) continue;

                    // Set the number of possible placement styles based on the rubble's spritesheet
                    int style = WorldGen.genRand.Next(choice == 0 ? 12 : choice == 5 ? 9 : choice == 3 ? 13 : 6);
                    WorldGen.PlaceTile(x, y, types[choice], mute: true, style: style);
                    if (Main.tile[x, y].HasTile && Main.tile[x, y].TileType == types[choice]) count++;
                }
            return count;

            bool Natural(int x, int y)
            {
                Tile tile = Main.tile[x, y];
                return WorldGen.SolidTile(x, y) && (tile.TileType == terrain.StoneTile || tile.TileType == terrain.ChargedTile ||
                    tile.TileType == terrain.LushTile || tile.TileType == terrain.DirtTile || tile.TileType == terrain.SandTile);
            }
        }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = WorldGenSystem.CrystalCavernsRubblePassMessage.Value;

            CCTerrainPass mainPass = CCTerrainPass.Instance();

            // Sets of rubble to be placed
            int[] potTileTypes = [ModContent.TileType<CavernPot2x2Rubble>()];
            int[] rubbleTileTypes = [
                ModContent.TileType<CavernStone1x1FloorRubbleNatural>(),
                ModContent.TileType<CavernStone1x1CeilingRubbleNatural>(),
                ModContent.TileType<CavernStone1x2FloorRubbleNatural>(),
                ModContent.TileType<CavernStone1x2CeilingRubbleNatural>(),
                ModContent.TileType<CavernStone3x2FloorRubbleNatural>(),
                ModContent.TileType<CavernStone3x2FloorRubbleNatural>()];

            // Tiles that rubble can be placed on
            int[] validRubblePlacementTiles = [
                mainPass.StoneTile, mainPass.ChargedTile, mainPass.LushTile];
            int[] validPotPlacementTiles = [
                mainPass.DirtTile, mainPass.GrassTile, mainPass.StoneTile,
                mainPass.ChargedTile, mainPass.LushTile, mainPass.SandTile];

            // Vanilla rubble to be removed
            ushort[] rubbleRemovalTypes = [
                TileID.Pots,
                TileID.Stalactite,
                TileID.SmallPiles,
                TileID.LargePiles,
                TileID.LargePiles2];

            // Remove vanilla rubble
            WorldUtils.Gen(mainPass.Origin, new ModShapes.All(mainPass.TotalBiome), Actions.Chain(new GenAction[]
            {
                new Modifiers.OnlyTiles(rubbleRemovalTypes),
                new Actions.ClearTile()
            }));

            // Place rubble
            for (int i = 0; i < 1250 * Math.Pow(mainPass.WorldSizeScale, 2); i++)
            {
                bool success = false;
                int attempts = 0;
                while (!success)
                {
                    attempts++;
                    if (attempts > 1000)
                    {
                        break;
                    }
                    int x = WorldGen.genRand.Next(mainPass.Origin.X - mainPass.BiomeWidth / 2, mainPass.Origin.X + mainPass.BiomeWidth / 2);
                    int y = WorldGen.genRand.Next(mainPass.Origin.Y - (int)(mainPass.SurfaceHeight), mainPass.Origin.Y + mainPass.UndergroundHeight);
                    if (LushReservoirGenerator.Contains(x, y, 3)) continue;

                    // Ensure rubble is only placed within the biome
                    // TotalUnderground is relative to the origin, not the world, so subtract the origin
                    if (y > mainPass.Origin.Y && !mainPass.TotalUnderground.Contains(x - mainPass.Origin.X, y - mainPass.Origin.Y))
                        continue;

                    // Ensure it is placed on valid tiles, compatible with 1 and 2 tile high rubble
                    if (!validRubblePlacementTiles.Contains(Main.tile[x, y + 1].TileType) && !validRubblePlacementTiles.Contains(Main.tile[x, y + 2].TileType))
                        continue;

                    int tileType = WorldGen.genRand.Next(rubbleTileTypes);
                    int placeStyle = 0; // Default value

                    if (Main.tile[x, y].TileType == tileType)
                        continue;

                    if (tileType == ModContent.TileType<CavernStone1x1CeilingRubbleNatural>() ||
                        tileType == ModContent.TileType<CavernStone1x2FloorRubbleNatural>() ||
                        tileType == ModContent.TileType<CavernStone1x2CeilingRubbleNatural>() ||
                        tileType == ModContent.TileType<CavernStone3x2FloorRubbleNatural>())
                    {
                        // These tile types have 6 variants each so pick one variant at random
                        placeStyle = WorldGen.genRand.Next(6);
                    }
                    else if (tileType == ModContent.TileType<CavernStone1x1FloorRubbleNatural>())
                    {
                        placeStyle = WorldGen.genRand.Next(12);
                    }

                    WorldGen.PlaceTile(x, y, tileType, mute: true, style: placeStyle);
                    success = Main.tile[x, y].TileType == tileType;
                }
            }

            // Place crystal growths
            for (int i = 0; i < 300 * Math.Pow(mainPass.WorldSizeScale, 2); i++)
            {
                bool success = false;
                int attempts = 0;
                while (!success)
                {
                    attempts++;
                    if (attempts > 1000)
                    {
                        break;
                    }
                    int x = WorldGen.genRand.Next(mainPass.Origin.X - mainPass.BiomeWidth / 2, mainPass.Origin.X + mainPass.BiomeWidth / 2);
                    int y = WorldGen.genRand.Next(mainPass.Origin.Y - (int)(mainPass.SurfaceHeight), mainPass.Origin.Y + mainPass.UndergroundHeight);
                    if (LushReservoirGenerator.Contains(x, y, 3)) continue;

                    // Ensure rubble is only placed within the biome
                    // TotalUnderground is relative to the origin, not the world, so subtract the origin
                    if (y > mainPass.Origin.Y && !mainPass.TotalUnderground.Contains(x - mainPass.Origin.X, y - mainPass.Origin.Y))
                        continue;

                    // Ensure it is placed on valid tiles
                    if (!validRubblePlacementTiles.Contains(Main.tile[x, y + 1].TileType) || Main.tile[x, y].HasTile)
                        continue;

                    int tileType = ModContent.TileType<CrystalGrowthTile>();
                    int placeStyle = 0; // Default value

                    if (Main.tile[x, y].TileType == tileType)
                        continue;

                    placeStyle = WorldGen.genRand.Next(13);

                    WorldGen.PlaceTile(x, y, tileType, mute: true, style: placeStyle);
                    success = Main.tile[x, y].TileType == tileType;
                }
            }

            // Place pots
            for (int i = 0; i < 250 * Math.Pow(mainPass.WorldSizeScale, 2); i++)
            {
                bool success = false;
                int attempts = 0;
                while (!success)
                {
                    attempts++;
                    if (attempts > 1000)
                    {
                        break;
                    }
                    int x = WorldGen.genRand.Next(mainPass.Origin.X - mainPass.BiomeWidth / 2, mainPass.Origin.X + mainPass.BiomeWidth / 2);
                    int y = WorldGen.genRand.Next(mainPass.Origin.Y - (int)(mainPass.SurfaceHeight), mainPass.Origin.Y + mainPass.UndergroundHeight);
                    if (LushReservoirGenerator.Contains(x, y, 3)) continue;

                    // Ensure rubble is only placed within the biome
                    // TotalUnderground is relative to the origin, not the world, so subtract the origin
                    if (y > mainPass.Origin.Y && !mainPass.TotalUnderground.Contains(x - mainPass.Origin.X, y - mainPass.Origin.Y))
                        continue;

                    // Ensure it is placed on valid tiles, compatible with 1 and 2 tile high rubble
                    if (!validPotPlacementTiles.Contains(Main.tile[x, y + 1].TileType) && !validPotPlacementTiles.Contains(Main.tile[x, y + 2].TileType))
                        continue;

                    // Prevent pots from spawning on the surface but allow them in caves still
                    if (y < mainPass.Origin.Y && Main.tile[x, y].WallType == WallID.None)
                        continue;

                    int tileType = WorldGen.genRand.Next(potTileTypes);
                    int placeStyle = 0; // Default value

                    if (Main.tile[x, y].TileType == tileType)
                        continue;

                    if (tileType == ModContent.TileType<CavernPot2x2Rubble>())
                    {
                        placeStyle = WorldGen.genRand.Next(9);
                    }

                    WorldGen.PlaceTile(x, y, tileType, mute: true, style: placeStyle);
                    success = Main.tile[x, y].TileType == tileType;
                }
            }
        }
    }
}
