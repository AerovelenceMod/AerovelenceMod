namespace AerovelenceMod.Common.Globals;
public sealed class MapBackgroundTile : GlobalTile
{
    public override void PlaceInWorld(int i, int j, int type, Item item) => MapBackgroundUtils.TerrainChanged(i, j);
    public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
    {
        if (!fail && !effectOnly)
            MapBackgroundUtils.TerrainChanged(i, j);
    }
}
public sealed class MapBackgroundWall : GlobalWall
{
    public override void PlaceInWorld(int i, int j, int type, Item item) => MapBackgroundUtils.TerrainChanged(i, j);
    public override void KillWall(int i, int j, int type, ref bool fail)
    {
        if (!fail)
            MapBackgroundUtils.TerrainChanged(i, j);
    }
}
