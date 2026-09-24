using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Graphics.Capture;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Biomes
{
    public class CrystalCavernsTileCount : ModSystem
    {
        public int CrystalTiles;
        public int CitadelTiles;

        public override void ResetNearbyTileEffects()
        {
            CrystalTiles = 0;
            CitadelTiles = 0;
        }

        public override void TileCountsAvailable(ReadOnlySpan<int> tileCounts)
        {
            CrystalTiles = tileCounts[ModContent.TileType<CrystalGrassTile>()] +
                          tileCounts[ModContent.TileType<CrystalDirtTile>()] +
                          tileCounts[ModContent.TileType<CavernStoneTile>()] +
                          tileCounts[ModContent.TileType<CavernSandTile>()];
        }
    }
}