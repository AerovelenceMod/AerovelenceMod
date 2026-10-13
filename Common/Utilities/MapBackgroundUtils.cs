using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using MonoMod.Cil;
using Terraria.Map;
namespace AerovelenceMod.Common.Utilities;

public static class MapBackgroundUtils
{
    private const int MapEdgePadding = 40;
    private const byte MinimumExploredLight = 18;
    private const int DefaultTerrainReach = 36;
    private const int TerrainChunkSize = 16;
    private const ulong TerrainCacheLifetime = 60;
    private const int ChangedTilePadding = 2;
    private sealed record Background(ModWall Palette, Func<int, int, bool> Contains, Func<int, int, ushort> Option, int ColorCount)
    {
        public ushort FirstColor;
    }
    private static readonly List<Background> backgrounds = new();
    private static readonly List<TileRegion> regions = new();
    private static readonly List<Rectangle> changes = new();
    private static ushort backgroundStart, backgroundEnd;
    /// <summary>Register during SetStaticDefaults. The walls map entries supply the colors and save identity, no wall is placed</summary>
    /// <param name="contains">Checks the map tiles world coordinates</param>
    /// <param name="option">Selects one of the palette wall's map entries. Defaults to its first entry</param>
    /// <param name="colorCount">The number of map entries added to the palette wall</param>
    public static void Register(ModWall palette, Func<int, int, bool> contains, Func<int, int, ushort> option = null, int colorCount = 1)
    {
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(contains);
        if (!Main.dedServ)
            backgrounds.Add(new(palette, contains, option, Math.Max(1, colorCount)));
    }
    public sealed class TileRegion
    {
        private sealed record TerrainChunk(uint[] Rows, bool HasTerrain, ulong CheckedAt);
        private readonly HashSet<int> tileTypes, wallTypes;
        private readonly ConcurrentDictionary<Point, TerrainChunk> chunks = new();
        private readonly int reach;
        public TileRegion(IEnumerable<int> tileTypes, IEnumerable<int> wallTypes = null, int reach = DefaultTerrainReach)
        {
            ArgumentNullException.ThrowIfNull(tileTypes);
            this.tileTypes = new(tileTypes);
            this.wallTypes = new(wallTypes ?? Array.Empty<int>());
            this.reach = Math.Max(1, reach);
            regions.Add(this);
        }
        private TerrainChunk ReadChunk(Point position)
        {
            ulong now = Main.GameUpdateCount;
            if (chunks.TryGetValue(position, out TerrainChunk cached) && now - cached.CheckedAt < TerrainCacheLifetime)
                return cached;
            uint[] rows = new uint[TerrainChunkSize];
            bool found = false;
            int left = position.X * TerrainChunkSize, top = position.Y * TerrainChunkSize;
            for (int y = Math.Max(0, top); y < Math.Min(Main.maxTilesY, top + TerrainChunkSize); y++)
                for (int x = Math.Max(0, left); x < Math.Min(Main.maxTilesX, left + TerrainChunkSize); x++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!(tile.HasTile && tileTypes.Contains(tile.TileType)) && !wallTypes.Contains(tile.WallType))
                        continue;
                    rows[y - top] |= 1u << (x - left);
                    found = true;
                }
            TerrainChunk chunk = new(rows, found, now);
            chunks[position] = chunk;
            return chunk;
        }
        public bool Contains(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, MapEdgePadding) || y < Main.worldSurface || y >= Main.UnderworldLayer)
                return false;
            Point center = new(x / TerrainChunkSize, y / TerrainChunkSize);
            if (ReadChunk(center).HasTerrain && reach >= TerrainChunkSize * Math.Sqrt(2))
                return true;
            int radiusSquared = reach * reach;
            for (int column = Math.Max(0, x - reach) / TerrainChunkSize; column <= Math.Min(Main.maxTilesX - 1, x + reach) / TerrainChunkSize; column++)
                for (int row = Math.Max(0, y - reach) / TerrainChunkSize; row <= Math.Min(Main.maxTilesY - 1, y + reach) / TerrainChunkSize; row++)
                {
                    int left = column * TerrainChunkSize, top = row * TerrainChunkSize;
                    int nearestX = Math.Clamp(x, left, left + TerrainChunkSize - 1), nearestY = Math.Clamp(y, top, top + TerrainChunkSize - 1);
                    int dx = nearestX - x, dy = nearestY - y;
                    if (dx * dx + dy * dy > radiusSquared)
                        continue;
                    TerrainChunk chunk = ReadChunk(new Point(column, row));
                    if (!chunk.HasTerrain)
                        continue;
                    for (int tileY = Math.Max(top, y - reach); tileY <= Math.Min(top + TerrainChunkSize - 1, y + reach); tileY++)
                    {
                        int horizontal = (int)Math.Sqrt(radiusSquared - (tileY - y) * (tileY - y));
                        int start = Math.Max(0, x - horizontal - left), end = Math.Min(TerrainChunkSize - 1, x + horizontal - left);
                        if (start > end)
                            continue;
                        uint mask = ((1u << (end - start + 1)) - 1) << start;
                        if ((chunk.Rows[tileY - top] & mask) != 0)
                            return true;
                    }
                }
            return false;
        }
        internal void Invalidate(Rectangle area)
        {
            for (int x = Math.Max(0, area.Left) / TerrainChunkSize; x <= Math.Min(Main.maxTilesX - 1, area.Right - 1) / TerrainChunkSize; x++)
                for (int y = Math.Max(0, area.Top) / TerrainChunkSize; y <= Math.Min(Main.maxTilesY - 1, area.Bottom - 1) / TerrainChunkSize; y++)
                    chunks.TryRemove(new Point(x, y), out _);
        }
        internal void Clear() => chunks.Clear();
        internal int Reach => reach;
    }
    public static void TerrainChanged(int x, int y)
    {
        if (Main.dedServ || WorldGen.gen || regions.Count == 0)
            return;
        Rectangle area = new(x - ChangedTilePadding, y - ChangedTilePadding, ChangedTilePadding * 2 + 1, ChangedTilePadding * 2 + 1);
        int reach = 0;
        foreach (TileRegion region in regions)
        {
            region.Invalidate(area);
            reach = Math.Max(reach, region.Reach);
        }
        area.Inflate(reach, reach);
        for (int index = changes.Count - 1; index >= 0; index--)
            if (changes[index].Intersects(area))
            {
                area = Rectangle.Union(area, changes[index]);
                changes.RemoveAt(index);
            }
        changes.Add(area);
    }
    internal static void RefreshChanges()
    {
        if (Main.dedServ || Main.gameMenu || changes.Count == 0)
            return;
        Rectangle area = changes[0];
        changes.RemoveAt(0);
        RefreshExplored(Main.Map, area);
    }
    internal static void ClearTerrain()
    {
        changes.Clear();
        foreach (TileRegion region in regions)
            region.Clear();
    }
    internal static void ReadPalette(Mod mod)
    {
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        if (typeof(MapHelper).GetField("wallLookup", flags)?.GetValue(null) is not ushort[] walls ||
            typeof(MapHelper).GetField("dirtPosition", flags)?.GetValue(null) is not ushort start ||
            typeof(MapHelper).GetField("hellPosition", flags)?.GetValue(null) is not ushort end)
        {
            ClearWorld();
            mod.Logger.Warn("Map background fields were not found. Keeping Terraria's map colors.");
            return;
        }
        backgroundStart = start;
        backgroundEnd = end;
        foreach (Background background in backgrounds)
            background.FirstColor = background.Palette.Type < walls.Length ? walls[background.Palette.Type] : (ushort)0;
    }
    internal static void EditBackground(ILContext il)
    {
        ILCursor cursor = new(il);
        if (!cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchCall<MapTile>(nameof(MapTile.Create))))
        {
            ModContent.GetInstance<AerovelenceMod>().Logger.Warn("MapTile.Create was not found in CreateMapTile. Keeping Terraria's map colors.");
            return;
        }
        cursor.EmitLdarg(0);
        cursor.EmitLdarg(1);
        // let vanilla pick the tile first then swap only its empty underground background
        cursor.EmitDelegate<Func<MapTile, int, int, MapTile>>(ChooseBackground);
    }
    private static MapTile ChooseBackground(MapTile tile, int x, int y)
    {
        if (!TerrainLoaded(x, y))
        {
            MapTile previous = Main.Map[x, y];
            return MapTile.Create(previous.Type, tile.Light, previous.Color);
        }
        if (tile.Type < backgroundStart || tile.Type >= backgroundEnd || y < Main.worldSurface || y >= Main.UnderworldLayer)
            return tile;
        foreach (Background background in backgrounds)
        {
            if (background.FirstColor == 0 || !background.Contains(x, y))
                continue;
            int color = background.FirstColor + Math.Min(background.ColorCount - 1, background.Option?.Invoke(x, y) ?? 0);
            if (color >= MapHelper.LookupCount())
                return tile;
            return MapTile.Create((ushort)color, tile.Light, 0);
        }
        return tile;
    }
    private static bool TerrainLoaded(int x, int y) => Main.netMode != NetmodeID.MultiplayerClient || Main.sectionManager?.TileLoaded(x, y) == true;
    internal static void RefreshExplored(WorldMap map, Rectangle? area = null)
    {
        if (!ReferenceEquals(map, Main.Map) || backgrounds.Count == 0 || backgroundStart == 0)
            return;
        Rectangle bounds = new(MapEdgePadding, Math.Max(MapEdgePadding, (int)Math.Ceiling(Main.worldSurface)),
            map.MaxWidth - MapEdgePadding * 2, Math.Min(map.MaxHeight - MapEdgePadding, Main.UnderworldLayer) - Math.Max(MapEdgePadding, (int)Math.Ceiling(Main.worldSurface)));
        if (area.HasValue)
            bounds = Rectangle.Intersect(bounds, area.Value);
        bool changed = false;
        for (int x = bounds.Left; x < bounds.Right; x++)
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                if (!TerrainLoaded(x, y))
                    continue;
                MapTile tile = map[x, y];
                bool custom = false;
                foreach (Background palette in backgrounds)
                    if (palette.FirstColor > 0 && tile.Type >= palette.FirstColor && tile.Type < palette.FirstColor + palette.ColorCount)
                    {
                        custom = true;
                        break;
                    }
                if (tile.Light <= MinimumExploredLight || !custom && (tile.Type < backgroundStart || tile.Type >= backgroundEnd))
                    continue;
                MapTile background = MapHelper.CreateMapTile(x, y, tile.Light);
                if (background.Type != tile.Type)
                {
                    map.SetTile(x, y, ref background);
                    changed = true;
                }
            }
        if (changed)
            Main.refreshMap = true;
    }
    internal static void ClearWorld()
    {
        backgroundStart = backgroundEnd = 0;
        foreach (Background background in backgrounds)
            background.FirstColor = 0;
        ClearTerrain();
    }
    internal static void Unload()
    {
        ClearWorld();
        backgrounds.Clear();
        regions.Clear();
    }
}
