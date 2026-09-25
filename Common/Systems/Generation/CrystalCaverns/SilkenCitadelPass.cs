using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AerovelenceMod.Content.Tiles.Citadel;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Glimmerwood;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture;
using AerovelenceMod.Common.Utilities.Generation;
using AerovelenceMod.Common.Utilities.Generation.StructureStamper;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;
using AerovelenceMod.Content.Walls.CrystalCaverns.Natural;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns;
public sealed class SilkenCitadelWorld : ModSystem
{
    public static Rectangle Bounds { get; internal set; }
    public static Point Altar { get; internal set; }
    public override void ClearWorld() { Bounds = Rectangle.Empty; Altar = Point.Zero; }
    public override void PreWorldGen() => ClearWorld();
    public override void SaveWorldData(TagCompound tag)
    {
        if (Bounds.IsEmpty) return;
        tag["SilkenCitadel"] = new int[] { Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height, Altar.X, Altar.Y };
    }
    public override void LoadWorldData(TagCompound tag)
    {
        ClearWorld();
        if (!tag.ContainsKey("SilkenCitadel")) return;
        int[] values = tag.GetIntArray("SilkenCitadel");
        if (values.Length != 6) return;
        Bounds = new(values[0], values[1], values[2], values[3]);
        Altar = new(values[4], values[5]);
    }
    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Bounds.X); writer.Write(Bounds.Y); writer.Write(Bounds.Width); writer.Write(Bounds.Height);
        writer.Write(Altar.X); writer.Write(Altar.Y);
    }
    public override void NetReceive(BinaryReader reader)
    {
        Bounds = new(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
        Altar = new(reader.ReadInt32(), reader.ReadInt32());
    }
    public static bool Contains(Player player) => !Bounds.IsEmpty && Bounds.Contains(player.Center.ToTileCoordinates());
}

public sealed class SilkenCitadelPass : GenPass
{
    public SilkenCitadelPass() : base("Moth's Nest and Silken Citadel", 180f) { }

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = "Growing the Moth's Nest";
        CCTerrainPass caverns = CCTerrainPass.Instance();
        if (caverns.Origin == Point.Zero) return;
        int seed = Main.ActiveWorldFileData.Seed;
        var layout = new SilkenCitadelLayout(seed, compact: true);
        double scale = caverns.BiomeWidth / (double)SilkenCitadelLayout.Width;
        int top = caverns.Origin.Y + (int)(caverns.UndergroundHeight * .65);
        scale = Math.Min(scale, (Main.UnderworldLayer - 35 - top) / (double)SilkenCitadelLayout.Height);
        if (scale < .75) throw new InvalidOperationException("Insufficient depth beneath Crystal Caverns for the Moth's Nest baseline.");
        int width = (int)Math.Round(SilkenCitadelLayout.Width * scale), height = (int)Math.Round(SilkenCitadelLayout.Height * scale);
        Rectangle bounds = new(caverns.Origin.X - width / 2, top, width, height);
        if (!WorldGen.InWorld(bounds.Left, bounds.Top, 20) || !WorldGen.InWorld(bounds.Right, bounds.Bottom, 20))
            throw new InvalidOperationException("Moth's Nest bounds extend outside this world.");
        float[] blend = layout.CreateBlendWeights();
        bool[] blocked = ProtectedMask(bounds);
        var changed = new ShapeData();
        int stamped = 0, skipped = 0, preservedCave = 0;
        bool oldNoTileActions = WorldGen.noTileActions;
        WorldGen.noTileActions = true;
        try
        {
            for (int y = 0; y < height; y++)
            {
                progress.Set(.05 + .45 * y / height);
                for (int x = 0; x < width; x++)
                {
                    int source = layout.TerrainSourceCell(x, y, width, height);
                    float weight = blend[source];
                    if (weight <= 0 && !layout.Air[source]) continue;
                    if (blocked[x + y * width]) { skipped++; continue; }
                    Tile tile = Main.tile[bounds.X + x, bounds.Y + y];
                    bool material = layout.BlendNoise(x * 400.0 / width, y * 300.0 / height) < weight;
                    if (layout.Air[source])
                    {
                        ushort oldWall = tile.WallType;
                        tile.ClearEverything();
                        tile.WallType = material ? caverns.StoneWall : oldWall;
                    }
                    else if (weight >= .999f)
                    {
                        ushort type = tile.HasTile ? caverns.UndergroundMaterial(tile.TileType) : caverns.StoneTile;
                        tile.ClearEverything(); tile.ResetToType(type); tile.WallType = caverns.StoneWall;
                    }
                    else
                    {
                        if (!tile.HasTile) { preservedCave++; continue; }
                        if (!material || !Main.tileSolid[tile.TileType] || Main.tileFrameImportant[tile.TileType] || TileID.Sets.Ore[tile.TileType]) continue;
                        tile.TileType = caverns.UndergroundMaterial(tile.TileType);
                        tile.WallType = caverns.StoneWall;
                    }
                    changed.Add(x, y);
                    stamped++;
                }
            }
            if (stamped < width * height / 12) throw new InvalidOperationException("Protected terrain leaves too little room for the Moth's Nest.");
            Point entry = new(bounds.X + 198 * width / 400, bounds.Y + 24 * height / 300);
            int entranceLength = ConnectToCaverns(entry, bounds, caverns);
            FinishTerrain(changed, bounds, caverns);
            PlaceNaturalCrystals(changed, bounds, blocked, caverns);
            progress.Set(.7);
            int bridges = PlaceBridges(bounds, blocked, layout);
            List<SettlementHouse> settlement = new();
            int houses = PlaceExistingHouses(bounds, blocked, settlement);
            houses += PlaceExistingHouses(bounds, blocked, settlement, layout);
            int ruins = PlaceExistingHouses(bounds, blocked, settlement, layout, true);
            int extensions = ExtendSettlement(settlement, bounds, blocked, layout);
            int pools = PlacePools(layout, changed, bounds, blocked);
            int rubble = CCRubblePass.DecorateCaves(bounds, (x, y) => changed.Contains(x - bounds.X, y - bounds.Y) &&
                SafeArea(new Rectangle(x, y, 1, 1), bounds, blocked));
            for (int y = 1; y < height - 1; y++)
                for (int x = 1; x < width - 1; x++)
                    if (changed.Contains(x, y))
                    {
                        WorldGen.SquareTileFrame(bounds.X + x, bounds.Y + y, true);
                        WorldGen.SquareWallFrame(bounds.X + x, bounds.Y + y, true);
                    }
            SilkenCitadelWorld.Bounds = bounds;
            SilkenCitadelWorld.Altar = Point.Zero;
            for (int y = 0; y < height; y += 16)
                for (int x = 0; x < width; x += 16)
                    if (changed.Contains(x, y))
                        GenVars.structures.AddProtectedStructure(new Rectangle(bounds.X + x, bounds.Y + y, Math.Min(16, width - x), Math.Min(16, height - y)), 2);
            ModContent.GetInstance<AerovelenceMod>().Logger.Info($"Moth's Nest cellular terrain generated: seed={seed}, origin={bounds.X},{bounds.Y}, size={width}x{height}, stamped={stamped}, protected={skipped}, native cave cells preserved={preservedCave}, entrance={entranceLength}, houses={houses}, ruins={ruins}, extensions={extensions}, bridges={bridges}, pools={pools}, rubble={rubble}, lobes={layout.Nests.Count}, islands={layout.Islands.Count}.");
            progress.Set(1);
        }
        finally { WorldGen.noTileActions = oldNoTileActions; }
    }

    private static bool Important(Tile tile) => (tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Shimmer) ||
        (tile.HasTile && (Main.tileDungeon[tile.TileType] || tile.TileType == TileID.LihzahrdBrick ||
            tile.TileType == TileID.DemonAltar || tile.TileType == TileID.ShadowOrbs));

    private static bool[] ProtectedMask(Rectangle bounds)
    {
        bool[] blocked = new bool[bounds.Width * bounds.Height];
        foreach (Rectangle area in AeroStructure.ProtectedStructures) Mark(area, 8);
        foreach (Point16 entity in TileEntity.ByPosition.Keys) Mark(new Rectangle(entity.X, entity.Y, 1, 1), 3);
        for (int x = bounds.Left; x < bounds.Right; x++)
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                Tile tile = Main.tile[x, y];
                if (Important(tile)) Mark(new Rectangle(x, y, 1, 1), 7);
                else if (tile.HasTile && (TileID.Sets.BasicChest[tile.TileType] || TileID.Sets.BasicDresser[tile.TileType]))
                    Mark(new Rectangle(x, y, 1, 2), 1);
            }
        return blocked;

        void Mark(Rectangle area, int padding)
        {
            area.Inflate(padding, padding);
            area = Rectangle.Intersect(bounds, area);
            for (int y = area.Top; y < area.Bottom; y++)
                for (int x = area.Left; x < area.Right; x++) blocked[x - bounds.X + (y - bounds.Y) * bounds.Width] = true;
        }
    }

    private static int ConnectToCaverns(Point entry, Rectangle bounds, CCTerrainPass caverns)
    {
        Rectangle search = new(bounds.X - 30, Math.Max(40, caverns.Origin.Y + caverns.UndergroundHeight / 3), bounds.Width + 60, 1);
        search.Height = entry.Y - search.Y + 6;
        int width = search.Width, height = search.Height;
        bool[] forbidden = ProtectedMask(search);
        int[] previous = new int[width * height];
        Array.Fill(previous, -1);
        Queue<int> queue = new();
        int start = entry.X - search.X + (entry.Y - search.Y) * width;
        for (int dx = -3; dx <= 3; dx++)
            for (int dy = -3; dy <= 3; dy++)
                if (forbidden[start + dx + dy * width])
                    throw new InvalidOperationException("Protected structure intersects the Moth's Nest entrance.");
        previous[start] = start;
        queue.Enqueue(start);
        int target = -1;
        while (queue.TryDequeue(out int current))
        {
            int x = current % width, y = current / width;
            int wx = search.X + x, wy = search.Y + y;
            if (wy < bounds.Top - 5 && caverns.TotalUnderground.Contains(wx - caverns.Origin.X, wy - caverns.Origin.Y) &&
                !Main.tile[wx, wy].HasTile && !Main.tile[wx, wy - 1].HasTile && !Main.tile[wx + 1, wy].HasTile)
            { target = current; break; }
            for (int d = 0; d < 4; d++)
            {
                int xx = x + (d == 0 ? -1 : d == 1 ? 1 : 0), yy = y + (d == 2 ? -1 : d == 3 ? 1 : 0);
                if (xx < 4 || xx >= width - 4 || yy < 4 || yy >= height - 4) continue;
                int next = xx + yy * width;
                if (previous[next] >= 0) continue;
                bool blocked = false;
                for (int dx = -3; dx <= 3 && !blocked; dx++)
                    for (int dy = -3; dy <= 3; dy++)
                        if (forbidden[xx + dx + (yy + dy) * width]) { blocked = true; break; }
                if (blocked) continue;
                previous[next] = current; queue.Enqueue(next);
            }
        }
        if (target < 0) throw new InvalidOperationException("Moth's Nest could not safely connect to the Crystal Caverns.");
        int length = 0;
        for (int current = target; ; current = previous[current])
        {
            int x = search.X + current % width, y = search.Y + current / width;
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    Tile tile = Main.tile[x + dx, y + dy];
                    tile.ClearEverything(); tile.WallType = caverns.StoneWall;
                    WorldGen.SquareTileFrame(x + dx, y + dy, true);
                    WorldGen.SquareWallFrame(x + dx, y + dy, true);
                }
            length++;
            if (current == start) break;
        }
        return length;
    }

    private static void PlaceNaturalCrystals(ShapeData changed, Rectangle bounds, bool[] blocked, CCTerrainPass caverns)
    {
        ShapeData upper = new(), lower = new();
        for (int x = 15; x < bounds.Width - 15; x++)
            for (int y = 15; y < bounds.Height - 15; y++)
            {
                if (!changed.Contains(x, y)) continue;
                bool safe = true;
                for (int dx = -8; dx <= 8 && safe; dx++)
                    for (int dy = -14; dy <= 14; dy++)
                        if (blocked[x + dx + (y + dy) * bounds.Width]) { safe = false; break; }
                if (!safe) continue;
                if (y < bounds.Height / 2) upper.Add(x, y);
                else lower.Add(x, -y);
            }
        foreach (bool bottom in new[] { false, true })
            foreach (bool down in new[] { false, true })
            {
                List<GenAction> actions = new();
                if (bottom) actions.Add(new Modifiers.Flip(false, true));
                actions.Add(down ? new AeroGenUtils.NotSolidBelow(20) : new AeroGenUtils.NotSolidAbove(20));
                actions.Add(new Modifiers.Offset(0, down ? -2 : 2));
                actions.Add(new Modifiers.Dither(bottom ? .985 : .99));
                actions.Add(new Modifiers.OnlyTiles(caverns.StoneTile, caverns.ChargedTile, caverns.LushTile));
                actions.Add(new AeroGenUtils.PlaceTail(caverns.CrystalTile, 4, new Vector2D(0, down ? 10 : -10), 0, 4, 3));
                WorldUtils.Gen(bounds.Location, new ModShapes.All(bottom ? lower : upper), Actions.Chain(actions.ToArray()));
            }
    }

    private sealed class WithinTerrain(Point origin, ShapeData terrain) : GenAction
    {
        public override bool Apply(Point actionOrigin, int x, int y, params object[] args) =>
            terrain.Contains(x - origin.X, y - origin.Y) && WorldGen.InWorld(x, y, 2) ? UnitApply(actionOrigin, x, y, args) : Fail();
    }

    private static void FinishTerrain(ShapeData changed, Rectangle bounds, CCTerrainPass caverns)
    {
        List<Point> cells = new();
        for (int y = 1; y < bounds.Height - 1; y++)
            for (int x = 1; x < bounds.Width - 1; x++)
                if (changed.Contains(x, y)) cells.Add(new Point(bounds.X + x, bounds.Y + y));
        if (cells.Count == 0) return;
        ushort[] natural = [caverns.StoneTile, caverns.DirtTile, caverns.SandTile, caverns.ChargedTile];
        Run(caverns.DirtTile, caverns.DirtWall, cells.Count / 2600, 9, 9);
        Run(caverns.SandTile, caverns.StoneWall, cells.Count / 4200, 7, 7);
        Run(caverns.ChargedTile, caverns.StoneWall, cells.Count / 6500, 5, 5);
        Run(caverns.LushTile, caverns.LushWall, cells.Count / 9000, 8, 8, true);
        ushort[] ores = [(ushort)WorldGen.SavedOreTiers.Copper, (ushort)WorldGen.SavedOreTiers.Iron,
            (ushort)WorldGen.SavedOreTiers.Silver, (ushort)WorldGen.SavedOreTiers.Gold];
        foreach (ushort ore in ores) Run(ore, 0, cells.Count / 4800, 3, 5);
        for (int i = 0; i < cells.Count / 4500; i++)
        {
            Point start = cells[WorldGen.genRand.Next(cells.Count)];
            for (int step = 0; step < 9; step++)
            {
                WorldUtils.Gen(start, new Shapes.Circle(12, 8), Actions.Chain(
                    new WithinTerrain(bounds.Location, changed), new Modifiers.OnlyWalls(caverns.StoneWall, caverns.DirtWall),
                    new Modifiers.IsNotSolid(), new Modifiers.RadialDither(6, 12), new Actions.ClearWall()));
                start += new Point(WorldGen.genRand.Next(-7, 8), WorldGen.genRand.Next(-4, 5));
            }
        }
        foreach (Point point in cells)
        {
            Tile tile = Main.tile[point.X, point.Y];
            if (!tile.HasTile || !natural.Contains(tile.TileType)) continue;
            int x = point.X, y = point.Y;
            if (WorldGen.genRand.Next(12) == 0 && !Main.tile[x, y - 1].HasTile &&
                (!Main.tile[x - 1, y].HasTile || !Main.tile[x + 1, y].HasTile) &&
                WorldGen.SolidTile(x, y + 1) && WorldGen.SolidTile(x, y + 2)) tile.ClearTile();
        }
        foreach (Point point in cells)
        {
            Tile tile = Main.tile[point.X, point.Y];
            if (tile.HasTile && natural.Contains(tile.TileType)) Tile.SmoothSlope(point.X, point.Y, false, false);
        }

        void Run(ushort type, ushort wall, int attempts, int radius, int steps, bool surface = false)
        {
            for (int i = 0; i < attempts; i++)
            {
                Point start = cells[WorldGen.genRand.Next(cells.Count)];
                int vx = WorldGen.genRand.NextBool() ? 2 : -2;
                for (int step = 0; step < steps; step++)
                {
                    List<GenAction> filters = [new WithinTerrain(bounds.Location, changed), new Modifiers.OnlyTiles(natural)];
                    if (surface) filters.Add(new Modifiers.IsTouchingAir(true));
                    WorldUtils.Gen(start, new Shapes.Rectangle(1, 1), new AeroGenUtils.PlaceBlob(type, radius, radius / 2 + 1, filters.ToArray()));
                    if (wall != 0)
                        WorldUtils.Gen(start, new Shapes.Rectangle(1, 1), new AeroGenUtils.PlaceBlobWall(wall, radius, radius / 2 + 1,
                            [new WithinTerrain(bounds.Location, changed), new Modifiers.OnlyWalls(caverns.StoneWall, caverns.DirtWall)]));
                    start += new Point(vx + WorldGen.genRand.Next(-2, 3), WorldGen.genRand.Next(-2, 3));
                    if (!bounds.Contains(start)) break;
                }
            }
        }
    }

    private static int PlacePools(SilkenCitadelLayout layout, ShapeData changed, Rectangle bounds, bool[] blocked)
    {
        int pools = 0;
        var random = new SilkenCitadelLayout.PythonRandom((long)layout.Seed + 91021);
        foreach (var nest in layout.Nests)
        {
            if (random.NextDouble() > .45) continue;
            int x = bounds.X + (int)(nest.X * bounds.Width / 400), y = bounds.Y + (int)(nest.Y * bounds.Height / 300);
            int bottom = y;
            while (bottom < y + 14 && bounds.Contains(x, bottom) && !WorldGen.SolidTile(x, bottom)) bottom++;
            if (bottom >= y + 14) continue;
            Point start = new(x, bottom - random.Int(2, 4));
            Queue<Point> pending = new(); HashSet<Point> wet = new();
            pending.Enqueue(start);
            bool sealedBasin = true;
            while (pending.Count > 0 && sealedBasin)
            {
                Point point = pending.Dequeue();
                if (point.Y < start.Y || wet.Contains(point)) continue;
                if (!bounds.Contains(point) || Math.Abs(point.X - x) > 28 || point.Y > start.Y + 12) { sealedBasin = false; break; }
                if (WorldGen.SolidTile(point.X, point.Y)) continue;
                Tile tile = Main.tile[point.X, point.Y];
                if ((tile.HasTile && !WorldGen.SolidOrSlopedTile(tile)) || !changed.Contains(point.X - bounds.X, point.Y - bounds.Y) ||
                    !SafeArea(new Rectangle(point.X, point.Y, 1, 1), bounds, blocked) || tile.LiquidAmount > 0 || wet.Count >= 240)
                { sealedBasin = false; break; }
                wet.Add(point);
                pending.Enqueue(new Point(point.X - 1, point.Y)); pending.Enqueue(new Point(point.X + 1, point.Y));
                pending.Enqueue(new Point(point.X, point.Y - 1)); pending.Enqueue(new Point(point.X, point.Y + 1));
            }
            if (!sealedBasin || wet.Count < 8) continue;
            foreach (Point point in wet)
            {
                Tile tile = Main.tile[point.X, point.Y]; tile.LiquidType = LiquidID.Water; tile.LiquidAmount = 255;
            }
            pools++;
        }
        return pools;
    }

    private sealed record SettlementHouse(CitadelHouseStamp Stamp, Point Origin);

    private static int ExtendSettlement(List<SettlementHouse> houses, Rectangle bounds, bool[] blocked, SilkenCitadelLayout layout)
    {
        ushort brick = (ushort)ModContent.TileType<CitadelBrickTile>(), wall = (ushort)ModContent.WallType<CitadelBrickWallUnsafe>();
        List<SettlementHouse> linked = new(); int links = 0, overlooks = 0, fires = 0;
        foreach (SettlementHouse house in houses.Where(h => h.Stamp.Id.StartsWith("H")))
        {
            if (linked.Contains(house)) continue;
            foreach (SettlementHouse other in houses.Where(h => h != house && !linked.Contains(h) && h.Origin.X > house.Origin.X)
                .OrderBy(h => h.Origin.X - house.Origin.X))
            {
                bool joined = false;
                foreach (Point first in Portals(house, 1))
                    foreach (Point second in Portals(other, -1))
                        if (first.Y == second.Y && second.X - first.X >= 9 && second.X - first.X <= 48 &&
                            Annex(first, second, house, other))
                        { joined = true; break; }
                if (!joined) continue;
                linked.Add(house); linked.Add(other); links++; break;
            }
        }
        foreach (SettlementHouse house in houses)
        {
            if (!linked.Contains(house) && house.Stamp.Id.StartsWith("H") && WorldGen.genRand.NextBool(2))
            {
                int direction = WorldGen.genRand.NextBool() ? 1 : -1;
                foreach (Point portal in Portals(house, direction))
                {
                    Point end = new(portal.X + direction * WorldGen.genRand.Next(8, 12), portal.Y);
                    if (!Annex(direction > 0 ? portal : end, direction > 0 ? end : portal, house, null)) continue;
                    overlooks++; break;
                }
            }
            if (!WorldGen.genRand.NextBool(2)) continue;
            bool placed = false;
            for (int y = house.Stamp.Height - 2; y >= 6 && !placed; y--)
                for (int x = 1; x < house.Stamp.Width - 3 && !placed; x++)
                {
                    int wx = house.Origin.X + x, floor = house.Origin.Y + y;
                    bool room = true;
                    for (int dx = 0; dx < 3; dx++)
                    {
                        if (!WorldGen.SolidTile(wx + dx, floor) || Main.tile[wx + dx, floor].TileType != brick) room = false;
                        for (int dy = 1; dy <= 2; dy++)
                            if (Main.tile[wx + dx, floor - dy].HasTile || !"wRH".Contains(house.Stamp.Cell(x + dx, y - dy))) room = false;
                    }
                    if (!room) continue;
                    WorldGen.PlaceTile(wx + 1, floor - 1, TileID.Campfire, mute: true);
                    Tile fire = Main.tile[wx + 1, floor - 1];
                    if (!fire.HasTile || fire.TileType != TileID.Campfire) continue;
                    global::AerovelenceMod.Common.Utilities.CommonTileHelper.ToggleTile(wx + 1, floor - 1);
                    fires++; placed = true;
                }
        }
        ModContent.GetInstance<AerovelenceMod>().Logger.Info($"Citadel settlement additions: horizontal links={links}, overlooks={overlooks}, unlit campfires={fires}.");
        return links + overlooks;

        IEnumerable<Point> Portals(SettlementHouse house, int direction)
        {
            CitadelHouseStamp stamp = house.Stamp;
            for (int y = stamp.Height - 2; y >= 6; y--)
                for (int i = 0; i < stamp.Width; i++)
                {
                    int x = direction > 0 ? stamp.Width - i - 1 : i;
                    if (stamp.Cell(x, y) != 'B' || stamp.Cell(x, y - 4) != 'B') continue;
                    bool valid = true;
                    for (int dy = 1; dy <= 3; dy++)
                    {
                        if (!"BD".Contains(stamp.Cell(x, y - dy))) valid = false;
                        int inside = x - direction;
                        if (!"wRH".Contains(stamp.Cell(inside, y - dy)) || Main.tile[house.Origin.X + inside, house.Origin.Y + y - dy].HasTile) valid = false;
                        for (int outside = x + direction; outside >= 0 && outside < stamp.Width; outside += direction)
                            if (stamp.Cell(outside, y - dy) != '.') valid = false;
                    }
                    if (valid) { yield return new Point(house.Origin.X + x, house.Origin.Y + y); break; }
                }
        }

        bool Annex(Point left, Point right, SettlementHouse first, SettlementHouse second)
        {
            int width = right.X - left.X + 1, height = second == null ? 5 : 7;
            Rectangle area = new(left.X + 1, left.Y - 8, width - 2, height + 9);
            if (!bounds.Contains(area)) return false;
            bool Own(Rectangle rectangle) => rectangle.Contains(first.Origin) || (second != null && rectangle.Contains(second.Origin));
            foreach (Rectangle rectangle in AeroStructure.ProtectedStructures)
                if (rectangle.Intersects(area) && !Own(rectangle)) return false;
            int open = 0;
            for (int x = area.Left; x < area.Right; x++)
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    if (blocked[x - bounds.X + (y - bounds.Y) * bounds.Width] || Important(Main.tile[x, y]) ||
                        (Main.tile[x, y].HasTile && Main.tileFrameImportant[Main.tile[x, y].TileType])) return false;
                    if (y < left.Y && !Main.tile[x, y].HasTile) open++;
                }
            if (open < (width - 2) * 3) return false;
            bool[] shape = CitadelBridgeShape.Create(width, height, 2);
            int previousClearance = 5;
            for (int x = 1; x < width - 1; x++)
            {
                double broad = layout.SurfaceNoise((left.X + x) / 12.0, left.Y / 10.0);
                double detail = layout.SurfaceNoise((left.X + x) / 5.0 + 23, left.Y / 7.0 + 41);
                int clearance = Math.Clamp(5 + (int)Math.Round(broad * 1.8 + detail), 4, 8);
                clearance = Math.Clamp(clearance, previousClearance - 1, previousClearance + 1);
                if (x % 3 != 0 && Math.Abs(clearance - previousClearance) == 1) clearance = previousClearance;
                previousClearance = clearance;
                for (int y = -clearance; y < height; y++)
                {
                    Tile tile = Main.tile[left.X + x, left.Y + y]; tile.ClearEverything();
                    tile.WallType = (ushort)ModContent.WallType<CavernStoneWallUnsafe>();
                    if (y >= 0 && shape[x + y * width]) { tile.ResetToType(brick); tile.WallType = wall; }
                }
            }
            Point firstDoor = second != null || first.Origin.X < left.X ? left : right;
            Door(firstDoor, firstDoor == left ? 1 : -1);
            if (second != null) Door(right, -1);
            else
            {
                Point end = firstDoor == left ? right : left;
                for (int y = 0; y < 3; y++)
                { Tile tile = Main.tile[end.X, end.Y + y]; tile.ClearEverything(); tile.ResetToType(brick); tile.WallType = wall; }
                Tile rail = Main.tile[end.X, end.Y - 1]; rail.ClearEverything(); rail.ResetToType(brick);
            }
            Rectangle protection = new(left.X, area.Y, width, area.Height);
            for (int x = protection.Left; x < protection.Right; x++)
                for (int y = protection.Top; y < protection.Bottom; y++) WorldGen.SquareTileFrame(x, y, true);
            new AeroStructure(new Vector2(protection.X, protection.Y), protection.Width, protection.Height, second == null ? "citadeloverlook" : "citadellink").ProtectStructure();
            return true;
        }

        void Door(Point floor, int outward)
        {
            for (int y = floor.Y - 3; y < floor.Y; y++)
            { Tile tile = Main.tile[floor.X, y]; tile.ClearEverything(); tile.WallType = wall; }
            WorldGen.PlaceTile(floor.X, floor.Y - 2, ModContent.TileType<GlimmerwoodDoorTileClosed>(), mute: true, forced: true);
            for (int step = 1; step <= 3; step++)
            {
                Tile tile = Main.tile[floor.X - outward * step, floor.Y];
                if (tile.HasTile) break;
                tile.ResetToType((ushort)ModContent.TileType<GlimmerwoodPlatformTile>());
                WorldGen.SquareTileFrame(floor.X - outward * step, floor.Y, true);
            }
        }
    }

    private static int PlaceExistingHouses(Rectangle bounds, bool[] blocked, List<SettlementHouse> settlement, SilkenCitadelLayout layout = null, bool ruins = false)
    {
        int placed = 0, attempted = 0;
        var sites = layout == null ? CitadelHouseSites.Houses.Select(stamp => (Stamp: stamp, X: stamp.PreviewX + stamp.Width / 2.0, Y: stamp.PreviewY + stamp.Height / 2.0)) : CitadelHouseSites.ExtraSites(layout);
        foreach (var (original, xCenter, yCenter) in sites)
        {
            CitadelHouseSite baseStamp = ruins ? CitadelHouseSites.Ruins[attempted % CitadelHouseSites.Ruins.Count] : original;
            attempted++;
            int siteWorldX = bounds.X + (int)(xCenter * bounds.Width / 400);
            int siteWorldY = bounds.Y + (int)(yCenter * bounds.Height / 300);
            CitadelSiteProfile profile = CitadelSiteProfile.Analyze(new Point(siteWorldX, siteWorldY));
            CitadelHouseStamp stamp = CitadelStructureGenerator.Create(baseStamp, Main.ActiveWorldFileData.Seed, (int)Math.Round(xCenter), (int)Math.Round(yCenter), profile, ruins);
            int anchorX = siteWorldX - stamp.Width / 2;
            int anchorY = siteWorldY - stamp.Height / 2;
            Point best = Point.Zero;
            double bestScore = double.MinValue;
            List<Point> bestFoundation = null;
            for (int dx = -18; dx <= 18; dx++)
                for (int dy = -16; dy <= 24; dy++)
                {
                    Rectangle area = new(anchorX + dx - 4, anchorY + dy - 4, stamp.Width + 8, stamp.Height + 8);
                    if (!SafeArea(area, bounds, blocked) || settlement.Any(h =>
                    {
                        Rectangle occupied = new(h.Origin.X, h.Origin.Y, h.Stamp.Width, h.Stamp.Height);
                        occupied.Inflate(5, 4);
                        return occupied.Intersects(area);
                    })) continue;
                    int air = 0, interior = 0;
                    for (int y = 0; y < stamp.Height; y++)
                        for (int x = 0; x < stamp.Width; x++)
                            if ("wRabHPD~<>".Contains(stamp.Cell(x, y)))
                            {
                                interior++;
                                if (!WorldGen.SolidTile(anchorX + dx + x, anchorY + dy + y)) air++;
                            }
                    if (interior == 0 || air < interior * .38) continue;
                    Point origin = new(anchorX + dx, anchorY + dy);
                    if (!PlanFoundation(stamp, origin, bounds, blocked, out List<Point> foundation)) continue;
                    double score = air * 5.0 / interior - .45 * (Math.Abs(dx) + Math.Abs(dy)) - foundation.Count * .025;
                    if (score > bestScore) { bestScore = score; best = origin; bestFoundation = foundation; }
                }
            if (best != Point.Zero && HouseGenerator.GenerateCitadelHouse(stamp, best))
            {
                int bottom = best.Y + stamp.Height;
                foreach (Point point in bestFoundation)
                {
                    Tile tile = Main.tile[point.X, point.Y]; tile.ClearEverything();
                    tile.ResetToType((ushort)ModContent.TileType<CavernStoneTile>());
                    tile.WallType = (ushort)ModContent.WallType<CavernStoneWallUnsafe>();
                    bottom = Math.Max(bottom, point.Y + 1);
                }
                foreach (Point point in bestFoundation) WorldGen.SquareTileFrame(point.X, point.Y, true);
                new AeroStructure(new Vector2(best.X, best.Y), stamp.Width, bottom - best.Y, "citadelrockfooting").ProtectStructure();
                ModContent.GetInstance<AerovelenceMod>().Logger.Info($"Citadel structure {stamp.Id}: grounded at {best.X},{best.Y}, size={stamp.Width}x{stamp.Height}, foundation tiles={bestFoundation.Count}.");
                placed++;
                settlement.Add(new(stamp, best));
                if (layout != null && placed >= (ruins ? 6 : 12)) break;
            }
        }
        return placed;
    }
    private static bool PlanFoundation(CitadelHouseStamp stamp, Point origin, Rectangle bounds, bool[] blocked, out List<Point> fill)
    {
        fill = new List<Point>();
        int columns = 0, anchored = 0, central = 0, rootX = origin.X + stamp.Width / 2, rootDistance = int.MaxValue;
        foreach (var (x, y) in stamp.FoundationColumns())
        {
            columns++;
            int wx = origin.X + x, start = origin.Y + y + 1, floor = start;
            while (floor < start + 16 && floor < bounds.Bottom - 1 && !WorldGen.SolidTile(wx, floor)) floor++;
            if (floor >= start + 16 || floor >= bounds.Bottom - 1 || !SafeArea(new Rectangle(wx, start, 1, floor - start + 1), bounds, blocked)) continue;
            anchored++;
            if (x >= stamp.Width / 4 && x <= stamp.Width * 3 / 4)
            {
                central++;
                int distance = Math.Abs(x - stamp.Width / 2);
                if (distance < rootDistance) { rootX = wx; rootDistance = distance; }
            }
            for (int yy = start; yy < floor; yy++) fill.Add(new Point(wx, yy));
        }
        if (anchored < Math.Max(2, (columns + 1) / 2) || central < 2) return false;
        if (fill.Count > 0)
        {
            int left = int.MaxValue, right = int.MinValue, top = int.MaxValue, bottom = int.MinValue;
            foreach (Point point in fill) { left = Math.Min(left, point.X); right = Math.Max(right, point.X); top = Math.Min(top, point.Y); bottom = Math.Max(bottom, point.Y); }
            fill.RemoveAll(point =>
            {
                double depth = Math.Clamp((point.Y - top - 2.0) / Math.Max(1, bottom - top - 1), 0, 1);
                double lowerLeft = left + (rootX - 1 - left) * depth, lowerRight = right + (rootX + 1 - right) * depth;
                return point.X < lowerLeft || point.X > lowerRight;
            });
        }
        return true;
    }
    private static bool SafeArea(Rectangle area, Rectangle bounds, bool[] blocked)
    {
        if (!bounds.Contains(area)) return false;
        foreach (Rectangle existing in AeroStructure.ProtectedStructures) if (existing.Intersects(area)) return false;
        for (int x = area.Left; x < area.Right; x++)
            for (int y = area.Top; y < area.Bottom; y++) if (blocked[x - bounds.X + (y - bounds.Y) * bounds.Width]) return false;
        return true;
    }
    private static int PlaceBridges(Rectangle bounds, bool[] blocked, SilkenCitadelLayout layout)
    {
        int count = 0;
        var random = new SilkenCitadelLayout.PythonRandom((long)layout.Seed + 48031);
        List<SilkenCitadelLayout.Nest> candidates = layout.Nests
            .Where(n => n.Y >= 145 && n.Y <= 255 && n.X >= 112 && n.X <= 310)
            .OrderBy(_ => random.NextDouble()).ToList();
        foreach (var nest in candidates)
        {
            if (count >= 2) break;
            int spanWidth = random.Int(42, 66);
            int center = (int)Math.Round(nest.X + random.Range(-8, 8));
            int sourceLeft = Math.Clamp(center - spanWidth / 2, 92, 304 - spanWidth);
            int sourceRight = sourceLeft + spanWidth;
            int sourceY = Math.Clamp((int)Math.Round(nest.Y + random.Range(-7, 7)), 150, 252);
            int left = bounds.X + sourceLeft * bounds.Width / 400, right = bounds.X + sourceRight * bounds.Width / 400;
            int width = right - left + 1, height = Math.Clamp(bounds.Height / 30, 10, 16);
            int targetY = bounds.Y + sourceY * bounds.Height / 300;
            bool[] shape = CitadelBridgeShape.Create(width, height);
            List<Point> best = null, bestCarve = null;
            int bestY = 0, bestBottom = 0; double bestScore = double.MinValue;
            for (int offset = -9; offset <= 9; offset++)
            {
                int top = targetY + offset, open = 0, anchors = 0, bottom = top + height;
                if (!SafeArea(new Rectangle(left - 3, top - 11, width + 6, height + 28), bounds, blocked)) continue;
                List<Point> plan = new(), carve = new();
                int previousClearance = 6;
                for (int x = 0; x < width; x++)
                {
                    if (!WorldGen.SolidTile(left + x, top - 2)) open++;
                    double broad = layout.SurfaceNoise((sourceLeft + x * 400.0 / bounds.Width) / 13.0, sourceY / 11.0);
                    double detail = layout.SurfaceNoise((sourceLeft + x * 400.0 / bounds.Width) / 5.5 + 37, sourceY / 7.0 + 19);
                    int clearance = Math.Clamp(6 + (int)Math.Round(broad * 2.2 + detail * 1.1), 4, 9);
                    clearance = Math.Clamp(clearance, previousClearance - 1, previousClearance + 1);
                    if (x % 3 != 0 && Math.Abs(clearance - previousClearance) == 1) clearance = previousClearance;
                    previousClearance = clearance;
                    for (int y = top - clearance; y < top; y++) carve.Add(new Point(left + x, y));
                    for (int y = 0; y < height; y++)
                    {
                        if (!SafeArea(new Rectangle(left + x, top + y, 1, 1), bounds, blocked)) continue;
                        if (shape[x + y * width]) plan.Add(new Point(left + x, top + y));
                        else carve.Add(new Point(left + x, top + y));
                    }
                    if (!shape[x + (height - 1) * width])
                        for (int y = height; y < height + 1 + (int)(2 * (1 + layout.SurfaceNoise((left + x) / 6.0, top / 6.0))); y++)
                            if (SafeArea(new Rectangle(left + x, top + y, 1, 1), bounds, blocked)) carve.Add(new Point(left + x, top + y));
                }
                int bays = Math.Max(1, width / 24);
                for (int pier = 0; pier <= bays; pier++)
                {
                    int wx = left + (int)Math.Round(pier * (width - 1.0) / bays);
                    int floor = top + height;
                    while (floor < top + height + 16 && floor < bounds.Bottom - 1 && !WorldGen.SolidTile(wx, floor)) floor++;
                    int embedded = floor + 3 + pier % 3;
                    if (floor >= top + height + 16 || embedded >= bounds.Bottom - 1 ||
                        !SafeArea(new Rectangle(Math.Max(left, wx - 1), top, Math.Min(3, right - Math.Max(left, wx - 1) + 1), embedded - top + 1), bounds, blocked)) continue;
                    anchors++;
                    for (int xx = Math.Max(left, wx - 1); xx <= Math.Min(right, wx + 1); xx++)
                        for (int yy = top + height - 2; yy <= embedded - Math.Abs(xx - wx); yy++) plan.Add(new Point(xx, yy));
                    bottom = Math.Max(bottom, embedded + 1);
                }
                if (anchors < 2 || open < width * .35 || plan.Count < width * 3) continue;
                double score = open + anchors * 8 - Math.Abs(offset) * 2;
                if (score > bestScore) { best = plan; bestCarve = carve; bestScore = score; bestY = top; bestBottom = bottom; }
            }
            if (best == null) continue;
            foreach (Point point in bestCarve)
            {
                Tile tile = Main.tile[point.X, point.Y]; tile.ClearEverything();
                tile.WallType = (ushort)ModContent.WallType<CavernStoneWallUnsafe>();
            }
            foreach (Point point in best)
            {
                Tile tile = Main.tile[point.X, point.Y]; tile.ClearEverything();
                tile.ResetToType((ushort)ModContent.TileType<CitadelBrickTile>());
                tile.WallType = (ushort)ModContent.WallType<CitadelBrickWallUnsafe>();
            }
            foreach (Point point in best) WorldGen.SquareTileFrame(point.X, point.Y, true);
            foreach (Point point in bestCarve)
                for (int dy = -1; dy <= 1; dy += 2)
                {
                    Tile edge = Main.tile[point.X, point.Y + dy];
                    if (edge.HasTile && edge.TileType == ModContent.TileType<CavernStoneTile>() && SafeArea(new Rectangle(point.X, point.Y + dy, 1, 1), bounds, blocked))
                        Tile.SmoothSlope(point.X, point.Y + dy, false, false);
                }
            new AeroStructure(new Vector2(left, bestY - 11), width, bestBottom - bestY + 12, "archedcitadelbridge").ProtectStructure();
            count++;
        }
        return count;
    }

}