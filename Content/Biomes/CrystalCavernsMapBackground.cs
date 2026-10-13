using AerovelenceMod.Content.Tiles.Citadel;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;
using Terraria.Localization;
using AerovelenceMod.Content.Walls.CrystalCaverns.Natural;
namespace AerovelenceMod.Content.Biomes;
// A map palette carried by ModWall so tModLoader can save and reload its entries, it's never placed in the world
public sealed class CrystalCavernsMapBackground : ModWall
{
    internal static MapBackgroundUtils.TileRegion Region { get; private set; }
    public override string Texture => "AerovelenceMod/Content/Walls/CrystalCaverns/Natural/CavernStoneWallUnsafe";
    public override void SetStaticDefaults()
    {
        AddMapEntry(new Color(50, 50, 74), LocalizedText.Empty);
        AddMapEntry(new Color(50, 50, 74), LocalizedText.Empty); // Same color because I couldn't find a better one (im lazy and didnt feel like looking for one)
        Region = new(new int[]
        {
            ModContent.TileType<CrystalDirtTile>(), ModContent.TileType<CrystalGrassTile>(),
            ModContent.TileType<CavernStoneTile>(),
            ModContent.TileType<CavernSandTile>(), ModContent.TileType<ChargedStoneTile>(),
            ModContent.TileType<CavernCrystalTile>(), ModContent.TileType<LushGrowthTile>(),
            ModContent.TileType<CitadelStone>(), ModContent.TileType<CitadelBrickTile>()
        }, new int[]
        {
            ModContent.WallType<CavernStoneWallUnsafe>(), ModContent.WallType<CavernDirtWallUnsafe>(),
            ModContent.WallType<CrystalGrassWall>(), ModContent.WallType<LushGrowthWall>(),
            ModContent.WallType<CitadelBrickWallUnsafe>()
        });
        MapBackgroundUtils.Register(this, Region.Contains,
            (x, y) => (ushort)(y < Main.rockLayer ? 0 : 1), colorCount: 2);
    }
}
