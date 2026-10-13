using Terraria.Map;
namespace AerovelenceMod.Common.Systems;
public sealed class MapBackgroundSystem : ModSystem
{
    public override void Load()
    {
        if (Main.dedServ) return;
        IL_MapHelper.CreateMapTile += MapBackgroundUtils.EditBackground;
        On_WorldMap.Load += LoadMap;
    }
    public override void PostWorldGen() => MapBackgroundUtils.ClearTerrain();
    public override void PostUpdateEverything() => MapBackgroundUtils.RefreshChanges();
    public override void ClearWorld() => MapBackgroundUtils.ClearWorld();
    public override void OnWorldLoad()
    {
        if (!Main.dedServ) MapBackgroundUtils.ReadPalette(Mod);
    }
    private void LoadMap(On_WorldMap.orig_Load orig, WorldMap map)
    {
        orig(map);
        MapBackgroundUtils.ReadPalette(Mod);
        MapBackgroundUtils.RefreshExplored(map);
    }
    public override void Unload()
    {
        if (!Main.dedServ)
        {
            IL_MapHelper.CreateMapTile -= MapBackgroundUtils.EditBackground;
            On_WorldMap.Load -= LoadMap;
        }
        MapBackgroundUtils.Unload();
    }
}
