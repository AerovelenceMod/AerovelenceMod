using System;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora;
using System.Collections.Generic;
using AerovelenceMod.Common.Utilities.Generation.StructureStamper;




using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns;

public static class LushReservoirGenerator
{
    private static readonly List<Rectangle> reservoirs = new();
    internal static IReadOnlyList<Rectangle> Areas => reservoirs;
    internal static readonly List<(Point Seed, int WaterLine)> WaterBodies = new();
    private static int worldId = int.MinValue;

    public static bool Contains(int x, int y, int padding = 0)
    {
        EnsureWorld();
        foreach (Rectangle reservoir in reservoirs)
        {
            Rectangle area = reservoir;
            area.Inflate(padding, padding);
            if (area.Contains(x, y)) return true;
        }
        return false;
    }

    internal static void RegisterSurfaceLake(Rectangle area)
    {
        EnsureWorld();
        reservoirs.Add(area);
    }

    public static int GenerateCrystalCaverns(CCTerrainPass caverns)
    {
        EnsureWorld();
        int target = Main.maxTilesX < 6000 ? 1 : Main.maxTilesX < 8000 ? 2 : 3;
        Rectangle area = new(caverns.Origin.X - caverns.BiomeWidth / 2 + 28, caverns.Origin.Y + 24,
            caverns.BiomeWidth - 56, Math.Max(48, (int)(caverns.UndergroundHeight * .58f) - 48));
        return Generate(caverns, area, target, 67189, (x, y) =>
            caverns.TotalUnderground.Contains(x - caverns.Origin.X, y - caverns.Origin.Y));
    }

    public static int GenerateCitadel(CCTerrainPass caverns, Rectangle bounds, ShapeData changed, bool[] blocked)
    {
        EnsureWorld();
        int target = Main.maxTilesX >= 8000 ? 2 : 1;
        Rectangle area = bounds;
        area.Inflate(-18, -18);
        area.Y += bounds.Height / 3;
        area.Height -= bounds.Height / 3;
        return Generate(caverns, area, target, 91873, (x, y) =>
        {
            int lx = x - bounds.X, ly = y - bounds.Y;
            return lx >= 0 && ly >= 0 && lx < bounds.Width && ly < bounds.Height &&
                changed.Contains(lx, ly) && !blocked[lx + ly * bounds.Width];
        });
    }

    private static int Generate(CCTerrainPass caverns, Rectangle area, int target, int salt, Func<int, int, bool> allowed)
    {
        if (area.Width < 70 || area.Height < 44) return 0;
        UnifiedRandom random = new(unchecked(Main.ActiveWorldFileData.Seed * 397 ^ salt ^ area.X * 17 ^ area.Y * 31));
        int placed = 0;
        for (int attempt = 0; attempt < target * 320 && placed < target; attempt++)
        {
            int bonus = Main.maxTilesX < 6000 ? 0 : Main.maxTilesX < 8000 ? 12 : 24;
            int width = random.Next(96 + bonus, 116 + bonus);
            int height = random.Next(58 + bonus / 2, 72 + bonus / 2);
            if (width >= area.Width - 8 || height >= area.Height - 8) continue;
            Rectangle bounds = new(random.Next(area.Left + 4, area.Right - width - 3),
                random.Next(area.Top + 4, area.Bottom - height - 3), width, height);
            if (!Candidate(bounds, caverns, allowed)) continue;
            if (!Build(bounds, caverns, random)) continue;
            Rectangle protection = bounds;
            protection.Inflate(3, 3);
            reservoirs.Add(protection);
            new AeroStructure(new Vector2(protection.X, protection.Y), protection.Width, protection.Height, "lushreservoir").ProtectStructure();
            placed++;
        }
        return placed;
    }

    private static bool Candidate(Rectangle bounds, CCTerrainPass caverns, Func<int, int, bool> allowed)
    {
        if (!WorldGen.InWorld(bounds.Left, bounds.Top, 15) || !WorldGen.InWorld(bounds.Right, bounds.Bottom, 15)) return false;
        Rectangle padded = bounds;
        padded.Inflate(8, 8);
        foreach (Rectangle reservoir in reservoirs) if (reservoir.Intersects(padded)) return false;
        foreach (Rectangle structure in AeroStructure.ProtectedStructures) if (structure.Intersects(padded)) return false;
        for (int y = padded.Top; y < padded.Bottom; y++)
            for (int x = padded.Left; x < padded.Right; x++)
                if (caverns.LightningCave.Contains(x - caverns.Origin.X, y - caverns.Origin.Y + caverns.SurfaceHeight)) return false;
        int samples = 0, natural = 0, solid = 0, invalid = 0, liquid = 0;
        for (int y = bounds.Top + 2; y < bounds.Bottom - 2; y += 3)
            for (int x = bounds.Left + 2; x < bounds.Right - 2; x += 3)
            {
                samples++;
                if (!allowed(x, y)) { invalid++; continue; }
                Tile tile = Main.tile[x, y];
                if (tile.LiquidAmount > 0) liquid++;
                if (Forbidden(tile)) return false;
                if (!tile.HasTile) natural++;
                else if (Natural(tile, caverns)) { natural++; if (WorldGen.SolidOrSlopedTile(tile)) solid++; }
            }
        return invalid <= samples / 5 && liquid <= samples / 12 && natural >= samples * 9 / 10 && solid >= samples / 3;
    }

    private static bool Build(Rectangle bounds, CCTerrainPass caverns, UnifiedRandom random)
    {
        int width = bounds.Width, height = bounds.Height;
        bool[] carve = new bool[width * height];
        int cx = width / 2 + random.Next(-5, 6), cy = height / 2 + random.Next(-3, 4);
        List<Point> centers = [new Point(cx, cy)];
        List<Point> parents = [Point.Zero];
        List<(int RX, int RY)> sizes = [];
        int rootRX = random.Next(7, 11), rootRY = random.Next(5, 8);
        sizes.Add((rootRX, rootRY));
        CarveEllipse(carve, null, width, height, cx, cy, rootRX, rootRY);
        int targetChambers = random.Next(10, 15) + (width >= 125 ? 1 : 0);
        for (int misses = 0; centers.Count < targetChambers && misses < 260;)
        {
            int parentIndex = random.NextDouble() < .68 ? random.Next(Math.Max(0, centers.Count - 5), centers.Count) : random.Next(centers.Count);
            Point parent = centers[parentIndex];
            double angle = Range(random, -.72f, .72f);
            if (random.NextDouble() < .38) angle += random.NextBool() ? Math.PI * .72 : Math.PI * 1.28;
            if (random.NextDouble() < .28) angle += Math.PI / 2 * (random.NextBool() ? 1 : -1);
            double distance = Range(random, 14f, 23f);
            int nx = (int)Math.Round(parent.X + Math.Cos(angle) * distance);
            int ny = (int)Math.Round(parent.Y + Math.Sin(angle) * distance * .68);
            int rx = random.Next(6, 10), ry = random.Next(4, 7);
            if (nx - rx < 8 || ny - ry < 7 || nx + rx >= width - 8 || ny + ry >= height - 7) { misses++; continue; }
            bool close = false;
            for (int i = 0; i < centers.Count; i++)
            {
                int dx = centers[i].X - nx, dy = centers[i].Y - ny;
                int spacing = Math.Max(9, sizes[i].RX + rx - 3);
                if (dx * dx + dy * dy < spacing * spacing) { close = true; break; }
            }
            if (close) { misses++; continue; }
            Point next = new(nx, ny);
            CarveTunnel(carve, null, width, height, parent, next, random, random.Next(2, 4));
            CarveEllipse(carve, null, width, height, nx, ny, rx, ry);
            centers.Add(next); parents.Add(parent); sizes.Add((rx, ry)); misses = 0;
        }
        if (centers.Count < 8) return false;
        int loops = Math.Max(1, centers.Count / 4);
        for (int loop = 0; loop < loops; loop++)
        {
            int a = random.Next(1, centers.Count), best = -1, bestDistance = int.MaxValue;
            for (int b = 0; b < centers.Count; b++)
            {
                if (b == a || centers[b] == parents[a]) continue;
                int dx = centers[a].X - centers[b].X, dy = centers[a].Y - centers[b].Y;
                int distance = dx * dx + dy * dy;
                if (distance < 15 * 15 || distance > 31 * 31 || distance >= bestDistance) continue;
                best = b; bestDistance = distance;
            }
            if (best >= 0) CarveTunnel(carve, null, width, height, centers[a], centers[best], random, random.Next(2, 4));
        }
        bool[] body = GrowShell(carve, width, height, random.Next(4, 7));
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (!body[Index(x, y, width)]) continue;
                Tile tile = Main.tile[bounds.X + x, bounds.Y + y];
                if (Forbidden(tile) || (tile.HasTile && Main.tileFrameImportant[tile.TileType])) return false;
            }
        int carved = 0;
        for (int i = 0; i < carve.Length; i++) if (carve[i]) carved++;
        if (carved < width * height / 10 || ConnectedCarve(carve, width, height, centers[0]) < carved * .92f) return false;
        int bodyTop = height, bodyBottom = 0;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (body[Index(x, y, width)]) { bodyTop = Math.Min(bodyTop, y); bodyBottom = Math.Max(bodyBottom, y); }
        int waterLine = bounds.Top + bodyTop + (int)((bodyBottom - bodyTop) * Range(random, .34f, .43f));
        ushort gravel = Gravel(caverns);
        int textureSeed = unchecked(Main.ActiveWorldFileData.Seed ^ bounds.X * 73471 ^ bounds.Y * 91283);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int index = Index(x, y, width);
                if (!body[index]) continue;
                Tile tile = Main.tile[bounds.X + x, bounds.Y + y];
                ushort previousWall = tile.WallType;
                if (carve[index])
                {
                    tile.ClearEverything();
                    tile.WallType = ValueNoise(bounds.X + x, bounds.Y + y, textureSeed + 113, .09) > .37 ? caverns.LushWall : caverns.StoneWall;
                }
                else
                {
                    tile.ClearEverything();
                    tile.ResetToType(caverns.StoneTile);
                    tile.WallType = previousWall == 0 ? (ushort)0 : caverns.StoneWall;
                }
            }
        Point seed = new(bounds.X + centers[0].X, bounds.Y + centers[0].Y);
        List<Point> connected = Flood(seed, bounds, body);
        if (connected.Count < carved * .9f) return false;
        HashSet<Point> open = new(connected);
        foreach (Point point in connected)
        {
            Tile tile = Main.tile[point.X, point.Y];
            tile.WallType = ValueNoise(point.X, point.Y, textureSeed + 317, .075) > .32 ? caverns.LushWall : caverns.StoneWall;
            tile.LiquidAmount = point.Y >= waterLine ? byte.MaxValue : (byte)0;
            if (tile.LiquidAmount > 0) tile.LiquidType = LiquidID.Water;
        }
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                int wx = bounds.X + x, wy = bounds.Y + y;
                Tile tile = Main.tile[wx, wy];
                if (!body[Index(x, y, width)] || !tile.HasTile || !Natural(tile, caverns)) continue;
                bool exposed = false;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        exposed |= open.Contains(new Point(wx + dx, wy + dy));
                if (!exposed) continue;
                double gravelNoise = ValueNoise(wx, wy, textureSeed, .085) * .7 + ValueNoise(wx, wy, textureSeed + 719, .19) * .3;
                ushort type = gravelNoise > .59 ? gravel : caverns.LushTile;
                tile.ResetToType(type);
                if (type == caverns.LushTile)
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            Tile backing = Main.tile[wx + dx, wy + dy];
                            if (backing.HasTile && backing.TileType == caverns.StoneTile) backing.TileType = caverns.DirtTile;
                        }
                if (tile.WallType != 0) tile.WallType = type == caverns.LushTile ? caverns.LushWall : caverns.StoneWall;
            }
        foreach (Point point in connected)
        {
            int x = point.X, y = point.Y + 1;
            if (point.Y < waterLine || !WorldGen.SolidOrSlopedTile(Main.tile[x, y]) ||
                ValueNoise(x, y, textureSeed + 401, .11) < .48) continue;
            int depth = 2 + (int)(ValueNoise(x, y, textureSeed + 521, .17) * 3);
            for (int dy = 0; dy < depth && y + dy < bounds.Bottom - 1; dy++)
            {
                Tile bed = Main.tile[x, y + dy];
                if (!body[Index(x - bounds.X, y + dy - bounds.Y, width)] || !bed.HasTile || !Natural(bed, caverns)) break;
                bed.ResetToType(caverns.SandTile);
            }
        }
        foreach (Point point in connected)
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = point.X + dx, y = point.Y + dy;
                    if (!WorldGen.InWorld(x, y, 2)) continue;
                    Tile tile = Main.tile[x, y];
                    int lx = x - bounds.X, ly = y - bounds.Y;
                    if (lx >= 0 && ly >= 0 && lx < width && ly < height && body[Index(lx, ly, width)] &&
                        tile.HasTile && (tile.TileType == caverns.StoneTile || tile.TileType == caverns.LushTile || tile.TileType == gravel))
                        Tile.SmoothSlope(x, y, false, false);
                    WorldGen.SquareTileFrame(x, y, true);
                    WorldGen.SquareWallFrame(x, y, true);
                }
        WaterBodies.Add((connected.Find(p => p.Y >= waterLine + 3), waterLine));
        int vines = 0;
        foreach (Point point in connected)
        {
            if ((point.X + textureSeed) % 4 != 0 || random.Next(3) != 0 || point.Y < waterLine + 3) continue;
            int length = random.Next(4, 10);
            if (random.NextBool(3) ? LuminVines.Plant(point.X, point.Y, length) : Electrakelp.Plant(point.X, point.Y, length)) vines++;
        }
        ModContent.GetInstance<AerovelenceMod>().Logger.Info($"Lush reservoir at {bounds.X},{bounds.Y}: chambers={centers.Count}, waterline={waterLine}, algae={vines}.");
        return true;
    }

    private static bool[] GrowShell(bool[] carve, int width, int height, int radius)
    {
        bool[] body = (bool[])carve.Clone();
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
            {
                if (!carve[Index(x, y, width)]) continue;
                for (int dy = -radius; dy <= radius; dy++)
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius + radius) continue;
                        int nx = x + dx, ny = y + dy;
                        if (nx > 0 && ny > 0 && nx < width - 1 && ny < height - 1) body[Index(nx, ny, width)] = true;
                    }
            }
        Smooth(body, width, height);
        return body;
    }

    private static int ConnectedCarve(bool[] carve, int width, int height, Point seed)
    {
        Queue<Point> queue = new();
        bool[] visited = new bool[carve.Length];
        queue.Enqueue(seed);
        int count = 0;
        while (queue.Count > 0)
        {
            Point point = queue.Dequeue();
            if (point.X < 0 || point.Y < 0 || point.X >= width || point.Y >= height) continue;
            int index = Index(point.X, point.Y, width);
            if (visited[index] || !carve[index]) continue;
            visited[index] = true; count++;
            queue.Enqueue(new Point(point.X - 1, point.Y)); queue.Enqueue(new Point(point.X + 1, point.Y));
            queue.Enqueue(new Point(point.X, point.Y - 1)); queue.Enqueue(new Point(point.X, point.Y + 1));
        }
        return count;
    }

    private static List<Point> Flood(Point seed, Rectangle bounds, bool[] body)
    {
        int width = bounds.Width;
        Queue<Point> queue = new();
        HashSet<Point> visited = new();
        if (WorldGen.SolidOrSlopedTile(Main.tile[seed.X, seed.Y])) return [];
        queue.Enqueue(seed);
        while (queue.Count > 0)
        {
            Point point = queue.Dequeue();
            if (!bounds.Contains(point) || visited.Contains(point)) continue;
            int lx = point.X - bounds.X, ly = point.Y - bounds.Y;
            if (!body[Index(lx, ly, width)] || WorldGen.SolidOrSlopedTile(Main.tile[point.X, point.Y])) continue;
            visited.Add(point);
            queue.Enqueue(new Point(point.X - 1, point.Y));
            queue.Enqueue(new Point(point.X + 1, point.Y));
            queue.Enqueue(new Point(point.X, point.Y - 1));
            queue.Enqueue(new Point(point.X, point.Y + 1));
        }
        return [.. visited];
    }

    private static void FillEllipse(bool[] mask, int width, int height, int cx, int cy, int rx, int ry)
    {
        for (int y = Math.Max(1, cy - ry); y <= Math.Min(height - 2, cy + ry); y++)
            for (int x = Math.Max(1, cx - rx); x <= Math.Min(width - 2, cx + rx); x++)
            {
                double dx = (x - cx) / (double)rx, dy = (y - cy) / (double)ry;
                if (dx * dx + dy * dy <= 1) mask[Index(x, y, width)] = true;
            }
    }

    private static void CarveEllipse(bool[] carve, bool[] interior, int width, int height, int cx, int cy, int rx, int ry)
    {
        for (int y = Math.Max(1, cy - ry); y <= Math.Min(height - 2, cy + ry); y++)
            for (int x = Math.Max(1, cx - rx); x <= Math.Min(width - 2, cx + rx); x++)
            {
                double dx = (x - cx) / (double)rx, dy = (y - cy) / (double)ry;
                int index = Index(x, y, width);
                if (dx * dx + dy * dy <= 1 && (interior == null || interior[index])) carve[index] = true;
            }
    }

    private static void CarveTunnel(bool[] carve, bool[] interior, int width, int height, Point a, Point b, UnifiedRandom random, int radius = 2)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y, distance = Math.Sqrt(dx * dx + dy * dy);
        int steps = Math.Max(2, (int)Math.Ceiling(distance * 1.45));
        double bend = Range(random, -3.8f, 3.8f);
        for (int i = 0; i <= steps; i++)
        {
            double t = i / (double)steps;
            double x = a.X + dx * t - (distance == 0 ? 0 : dy / distance) * Math.Sin(t * Math.PI) * bend;
            double y = a.Y + dy * t + (distance == 0 ? 0 : dx / distance) * Math.Sin(t * Math.PI) * bend;
            int rx = Math.Max(2, radius + (i % 4 == 0 ? 1 : 0));
            int ry = Math.Max(2, radius - (i % 5 == 0 ? 1 : 0));
            CarveEllipse(carve, interior, width, height, (int)Math.Round(x), (int)Math.Round(y), rx, ry);
        }
    }

    private static void Smooth(bool[] mask, int width, int height)
    {
        for (int pass = 0; pass < 2; pass++)
        {
            bool[] next = (bool[])mask.Clone();
            for (int y = 2; y < height - 2; y++)
                for (int x = 2; x < width - 2; x++)
                {
                    int neighbors = 0;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if ((dx != 0 || dy != 0) && mask[Index(x + dx, y + dy, width)]) neighbors++;
                    if (neighbors >= 5) next[Index(x, y, width)] = true;
                    else if (neighbors <= 2) next[Index(x, y, width)] = false;
                }
            Array.Copy(next, mask, mask.Length);
        }
    }

    private static Point FindInterior(bool[] interior, int width, int height, int cx, int cy)
    {
        for (int radius = 0; radius < Math.Max(width, height); radius++)
            for (int y = Math.Max(1, cy - radius); y <= Math.Min(height - 2, cy + radius); y++)
                for (int x = Math.Max(1, cx - radius); x <= Math.Min(width - 2, cx + radius); x++)
                    if ((Math.Abs(x - cx) == radius || Math.Abs(y - cy) == radius) && interior[Index(x, y, width)]) return new Point(x, y);
        return Point.Zero;
    }

    private static bool Natural(Tile tile, CCTerrainPass caverns) => tile.HasTile &&
        (tile.TileType == caverns.StoneTile || tile.TileType == caverns.DirtTile || tile.TileType == caverns.SandTile ||
        tile.TileType == caverns.ChargedTile || tile.TileType == caverns.LushTile || tile.TileType == caverns.CrystalTile ||
        tile.TileType == caverns.GrassTile || TileID.Sets.Ore[tile.TileType]);

    private static bool Forbidden(Tile tile) => tile.HasTile &&
        (Main.tileDungeon[tile.TileType] || tile.TileType == TileID.LihzahrdBrick || tile.TileType == TileID.DemonAltar ||
        tile.TileType == TileID.ShadowOrbs || TileID.Sets.BasicChest[tile.TileType] || TileID.Sets.BasicDresser[tile.TileType]);

    private static ushort Gravel(CCTerrainPass caverns)
    {
        foreach (string name in new[] { "CavernGravelTile", "CavernGravel", "CavernGravelBlock" })
            if (ModContent.TryFind<ModTile>($"AerovelenceMod/{name}", out ModTile tile)) return (ushort)tile.Type;
        return caverns.StoneTile;
    }

    internal static double ValueNoise(int x, int y, int seed, double scale)
    {
        double fx = x * scale, fy = y * scale;
        int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy);
        double tx = fx - x0, ty = fy - y0;
        tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
        double a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed), c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
        return MathHelper.Lerp((float)MathHelper.Lerp((float)a, (float)b, (float)tx),
            (float)MathHelper.Lerp((float)c, (float)d, (float)tx), (float)ty);
    }

    private static double Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint n = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
            n = (n ^ (n >> 13)) * 1274126177;
            return (n ^ (n >> 16)) / (double)uint.MaxValue;
        }
    }

    private static float Range(UnifiedRandom random, float min, float max) => min + (float)random.NextDouble() * (max - min);
    private static int Index(int x, int y, int width) => x + y * width;

    private static void EnsureWorld()
    {
        if (worldId == Main.worldID) return;
        worldId = Main.worldID;
        reservoirs.Clear();
        WaterBodies.Clear();
    }
}
