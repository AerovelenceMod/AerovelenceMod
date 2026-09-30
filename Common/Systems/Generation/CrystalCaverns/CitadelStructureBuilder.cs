using System;
using System.Collections.Generic;
using System.Linq;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns;

public sealed class CitadelStructureBuilder
{
    public readonly record struct Box(int X, int Y, int Width, int Height)
    {
        public int Left => X;
        public int Right => X + Width - 1;
        public int Top => Y;
        public int Bottom => Y + Height - 1;
        public int CenterX => X + Width / 2;
        public int CenterY => Y + Height / 2;
    }

    private readonly char[][] cells;
    private readonly Dictionary<(int X, int Y), char> passageFloors = new();
    private readonly Dictionary<(int X, int Y), char> contextWalls = new();
    private readonly HashSet<(int X, int Y)> passageAir = new();
    private readonly int width;
    private readonly int height;

    public int Width => width;
    public int Height => height;

    public CitadelStructureBuilder(int width = 84, int height = 64)
    {
        this.width = width;
        this.height = height;
        cells = new char[height][];
        for (int y = 0; y < height; y++)
            cells[y] = Enumerable.Repeat('.', width).ToArray();
    }

    public char Get(int x, int y)
    {
        return Inside(x, y) ? cells[y][x] : '.';
    }

    public void Set(int x, int y, char cell, bool replace = true)
    {
        if (!Inside(x, y)) return;
        if (!replace && cells[y][x] != '.') return;
        if (cell == 'I' && cells[y][x] is '>' or '<' or 'P') return;
        char current = cells[y][x];
        if (cell is 'a' or 'b') contextWalls[(x, y)] = cell;
        else if ((current is 'a' or 'b') && (cell is 'P' or '>' or '<' or 'I' or 'v' or 'L' or '~' or '!' or 'n' or 't' or 'c' or 'k' or 'j')) contextWalls[(x, y)] = current;
        else if (cell is 'B' or 's' or 'o' or 'D' or 'e' or '.') contextWalls.Remove((x, y));
        cells[y][x] = cell;
    }

    public bool Inside(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public void Fill(Box box, char cell, bool replace = true)
    {
        for (int y = box.Top; y <= box.Bottom; y++)
            for (int x = box.Left; x <= box.Right; x++)
                Set(x, y, cell, replace);
    }

    public void Frame(Box box, char edge = 'B', char interior = 'w')
    {
        int thickness = box.Width >= 10 && box.Height >= 8 ? 2 : 1;

        for (int y = box.Top; y <= box.Bottom; y++)
            for (int x = box.Left; x <= box.Right; x++)
            {
                Set(x, y, y == box.Bottom || x - box.Left < thickness || box.Right - x < thickness || y - box.Top < thickness ? edge : interior);
            }
    }

    public void Room(Box box, bool leftDoor = false, bool rightDoor = false, bool reinforced = false)
    {
        Frame(box);
        RoofHorns(box);
        CornerWebs(box);
        VerticalWindows(box, Math.Max(1, box.Width / 9));

        if (reinforced)
        {
            for (int x = box.Left + 2; x < box.Right; x += 4)
            {
                Set(x, box.Top, 'B');
                Set(x, box.Bottom, 'B');
            }
        }

        if (leftDoor) Door(box.Left, box.Bottom);
        if (rightDoor) Door(box.Right, box.Bottom);
    }

    public void Tower(Box box, bool leftDoor, bool rightDoor, int floors, bool brokenTop)
    {
        Frame(box);
        RoofHorns(box);
        CornerWebs(box);
        int usable = Math.Max(4, box.Height - 2);
        floors = Math.Clamp(Math.Max(floors, (usable + Math.Max(3, box.Width - 7) - 1) / Math.Max(3, box.Width - 7)), 1, Math.Max(1, usable / 3));
        int spacing = Math.Max(3, usable / floors);

        for (int i = 1; i < floors; i++)
        {
            int y = box.Bottom - i * spacing;
            PlatformLine(box.Left + 1, box.Right - 1, y);
            RoomStairs(box, y, Math.Min(box.Bottom, y + spacing), i % 2 == 0 ? 1 : -1);
        }

        if (leftDoor) Door(box.Left, box.Bottom);
        if (rightDoor) Door(box.Right, box.Bottom);

        Crenellate(box.Left, box.Right, box.Top - 1, 2);

        if (brokenTop)
        {
            int cut = Math.Max(3, box.Width / 3);
            int start = box.Left + box.Width / 2;
            for (int x = start; x < Math.Min(box.Right + 1, start + cut); x++)
                for (int y = box.Top - 1; y <= box.Top + 2; y++)
                    Set(x, y, 'w');
        }

        VerticalWindows(box, Math.Max(2, floors));
    }

    public void GrandHall(Box box, int bays, bool roseWindow)
    {
        Frame(box);
        RoofHorns(box);
        CornerWebs(box);
        bays = Math.Max(2, bays);
        int interiorWidth = box.Width - 4;

        for (int bay = 1; bay < bays; bay++)
        {
            int x = box.Left + 2 + interiorWidth * bay / bays;
            for (int y = box.Top + 2; y < box.Bottom; y++)
                Set(x, y, y >= box.Bottom - 1 ? 'B' : 'I');
        }

        int archY = box.Top + Math.Max(3, box.Height / 3);
        for (int bay = 0; bay < bays; bay++)
        {
            int x1 = box.Left + 2 + interiorWidth * bay / bays;
            int x2 = box.Left + 2 + interiorWidth * (bay + 1) / bays - 1;
            Arch(x1, x2, archY, box.Bottom - 1);
        }

        if (roseWindow)
        {
            int cx = box.CenterX;
            int cy = box.Top + Math.Max(3, box.Height / 3);
            RoseWindow(cx, cy, box.Width >= 20 ? 2 : 1);
        }

        if (box.Width >= 18)
        {
            Workbench(box.Left + 4, box.Bottom);
            Chair(box.Left + 7, box.Bottom);
            Workbench(box.Right - 5, box.Bottom);
            Chair(box.Right - 8, box.Bottom);
            Lantern(box.CenterX, box.Top + 2);
        }
    }

    public void Chapel(Box box, int facing)
    {
        Frame(box);
        RoofHorns(box);
        CornerWebs(box);
        int center = box.CenterX;
        int windowY = box.Top + 3;
        RoseWindow(center, windowY, box.Width >= 13 ? 2 : 1);

        int altarX = facing >= 0 ? box.Right - 4 : box.Left + 4;
        Set(altarX, box.Bottom - 1, 'o');
        Set(altarX + (facing >= 0 ? -1 : 1), box.Bottom - 1, 'o');
        Chair(altarX + (facing >= 0 ? -3 : 3), box.Bottom);
        Lantern(center, box.Top + 2);

        int beamX1 = box.Left + 3;
        int beamX2 = box.Right - 3;
        Beam(beamX1, box.Top + 1, box.Bottom - 1);
        Beam(beamX2, box.Top + 1, box.Bottom - 1);
    }

    public void Balcony(int wallX, int floorY, int direction, int length, bool roofed)
    {
        int end = wallX + direction * length;
        int left = Math.Min(wallX, end);
        int right = Math.Max(wallX, end);

        Clear(left, floorY - 4, right - left + 1, 4, 'e');
        ReservePassage(left, floorY - 3, right - left + 1, 3);
        PlatformLine(left, right, floorY);
        Set(end, floorY + 1, 'I');
        Set(end, floorY + 2, 'I');

        if (roofed)
        {
            for (int x = left; x <= right; x++)
                Set(x, floorY - 5, 'B', false);
            Set(end, floorY - 4, 'I', false);
            Set(end, floorY - 3, 'I', false);
            Set(end, floorY - 2, 'I', false);
            Set(end, floorY - 1, 'I', false);
        }
    }

    public void Bridge(int x1, int x2, int y, bool enclosed)
    {
        if (x2 < x1) (x1, x2) = (x2, x1);

        for (int x = x1 + 1; x < x2; x++)
            for (int yy = y - 4; yy < y; yy++)
                if (Get(x, yy) is '.' or 'w') Set(x, yy, 'e');
        ReservePassage(x1, y - 3, x2 - x1 + 1, 3);
        for (int x = x1; x <= x2; x++) Set(x, y, 'P');

        if (!enclosed) return;

        for (int x = x1; x <= x2; x++)
        {
            if ((x - x1) % 5 == 0)
            {
                Set(x, y - 4, 'B');
                Set(x, y - 3, 'I');
                Set(x, y - 2, 'I');
                Set(x, y - 1, 'I');
            }
            else
                Set(x, y - 4, 'B', false);
        }
    }

    public void StoneBridge(int x1, int x2, int y, int depth)
    {
        if (x2 < x1) (x1, x2) = (x2, x1);
        int span = Math.Max(1, x2 - x1);

        for (int x = x1; x <= x2; x++)
        {
            double t = (x - x1) / (double)span;
            double arch = Math.Sin(t * Math.PI);
            int thickness = 2 + (int)Math.Round((1 - arch) * depth);
            for (int yy = y; yy < y + thickness; yy++)
                Set(x, yy, 'B');
        }
    }

    public void Courtyard(Box outer, int wallThickness, int gateDirection)
    {
        wallThickness = Math.Clamp(wallThickness, 1, 3);

        for (int t = 0; t < wallThickness; t++)
        {
            for (int x = outer.Left + t; x <= outer.Right - t; x++)
            {
                Set(x, outer.Top + t, 'B');
                Set(x, outer.Bottom - t, 'B');
            }

            for (int y = outer.Top + t; y <= outer.Bottom - t; y++)
            {
                Set(outer.Left + t, y, 'B');
                Set(outer.Right - t, y, 'B');
            }
        }

        for (int y = outer.Top + wallThickness; y <= outer.Bottom - wallThickness; y++)
            for (int x = outer.Left + wallThickness; x <= outer.Right - wallThickness; x++)
                Set(x, y, 'w');

        if (gateDirection < 0) Door(outer.Left, outer.Bottom);
        else Door(outer.Right, outer.Bottom);
    }

    public void Basement(Box box, int entranceX)
    {
        Frame(box);
        int direction = entranceX < box.CenterX ? 1 : -1;
        int startX = Math.Clamp(entranceX, box.Left + 3, box.Right - 3);
        for (int x = box.Left + 3; x < box.Right - 2; x += 4)
            Beam(x, box.Top + 1, box.Bottom - 1);
        Clear(startX - 1, box.Top - 1, 3, 2, 'w');
        RoomStairs(box, box.Top - 1, box.Bottom, direction);
        Workbench(box.Left + 3, box.Bottom);
        Chair(box.Left + 6, box.Bottom);
        if (box.Width >= 13)
            Bookcase(box.Right - 5, box.Bottom);
    }

    public void LoomHall(Box box, Random random)
    {
        GrandHall(box, Math.Max(2, box.Width / 7), false);

        int x = box.Left + random.Next(3, 6);
        while (x <= box.Right - 4)
        {
            if (random.NextDouble() < .72) Beam(x, box.Top + 1, box.Bottom - 1);
            x += random.Next(4, 8);
        }

        int clusters = random.Next(2, Math.Max(3, box.Width / 8));
        for (int i = 0; i < clusters; i++)
        {
            int centerX = random.Next(box.Left + 4, box.Right - 3);
            HangingChainCluster(centerX, box.Top + 1 + random.Next(0, 3), random, random.Next(1, 4), 2, Math.Min(6, box.Height - 3));
            if (random.NextDouble() < .7) SilkPatch(centerX + random.Next(-1, 2), box.Top + random.Next(2, 5), random.Next(2, 4), random.Next(1, 3), random);
        }

        PlatformLine(box.Left + 3, box.Right - 3, box.CenterY);
        if (random.NextDouble() < .75) SilkPatch(random.Next(box.Left + 4, box.Right - 3), box.CenterY - 2, random.Next(2, 5), random.Next(1, 3), random);

        Workbench(box.Left + 4, box.Bottom);
        Workbench(box.CenterX - 1, box.Bottom);
        Chair(box.Left + 7, box.Bottom);
        Chair(box.CenterX + 2, box.Bottom);
        if (box.Width >= 24)
            Bookcase(box.Right - 6, box.Bottom);
    }

    public void Nursery(Box box)
    {
        Frame(box);
        RoofHorns(box);
        CornerWebs(box);
        VerticalWindows(box, Math.Max(1, box.Width / 10));
        int tier1 = box.Top + box.Height / 3;
        int tier2 = box.Top + box.Height * 2 / 3;
        PlatformLine(box.Left + 3, box.CenterX - 2, tier1);
        PlatformLine(box.CenterX + 2, box.Right - 3, tier2);
        Beam(box.Left + 3, box.Top + 1, box.Bottom - 1);
        Beam(box.Right - 3, box.Top + 1, box.Bottom - 1);

        RoomStairs(box, tier1, tier2, 1);
        RoomStairs(box, tier2, box.Bottom, -1);

        Lantern(box.CenterX, box.Top + 2);
    }

    public void Cistern(Box box)
    {
        Frame(box);
        RoofHorns(box);
        CornerWebs(box);
        VerticalWindows(box, Math.Max(1, box.Width / 11));
        int basinTop = box.Bottom - Math.Max(4, box.Height / 3);
        for (int y = basinTop; y < box.Bottom; y++)
            for (int x = box.Left + 2; x < box.Right - 1; x++)
                Set(x, y, '~');

        for (int x = box.Left + 2; x <= box.Right - 2; x++)
            Set(x, basinTop, 'P');

        Arch(box.Left + 2, box.CenterX - 1, box.Top + 3, basinTop);
        Arch(box.CenterX + 1, box.Right - 2, box.Top + 3, basinTop);
        Beam(box.CenterX, box.Top + 2, basinTop - 1);
        Lantern(box.CenterX, box.Top + 1);
        Workbench(box.Left + 4, basinTop);
    }

    public void Archive(Box box)
    {
        Frame(box);
        RoofHorns(box);
        CornerWebs(box);
        int levels = Math.Max(2, box.Height / 7);
        int spacing = Math.Max(4, (box.Height - 3) / levels);

        for (int x = box.Left + 4; x < box.Right - 3; x += 5)
            Beam(x, box.Top + 1, box.Bottom - 1);

        for (int i = 1; i < levels; i++)
        {
            int y = box.Bottom - i * spacing;
            PlatformLine(box.Left + 2, box.Right - 2, y);
            RoomStairs(box, y, Math.Min(box.Bottom, y + spacing), i % 2 == 0 ? 1 : -1);
        }

        VerticalWindows(box, Math.Max(2, levels));

        if (box.Width >= 14)
        {
            Bookcase(box.Left + 3, box.Bottom);
            Bookcase(box.Right - 5, box.Bottom);
        }
        Workbench(box.CenterX - 1, box.Bottom);
        Chair(box.CenterX + 2, box.Bottom);
        Lantern(box.CenterX, box.Top + 2);
    }

    public void Gatehouse(Box leftTower, Box rightTower, int gateTop, int floorY)
    {
        Tower(leftTower, true, true, 2, false);
        Tower(rightTower, true, true, 2, false);

        int left = leftTower.Right;
        int right = rightTower.Left;

        for (int x = left; x <= right; x++)
        {
            Set(x, gateTop, 'B');
            Set(x, floorY, 'B');
        }

        for (int y = gateTop + 1; y < floorY; y++)
        {
            Set(left, y, 'B');
            Set(right, y, 'B');
        }

        int cx = (left + right) / 2;
        int archRadius = Math.Max(3, (right - left) / 3);

        for (int y = gateTop + 2; y < floorY; y++)
            for (int x = left + 1; x < right; x++)
            {
                double nx = (x - cx) / (double)archRadius;
                double ny = (y - (gateTop + archRadius)) / (double)archRadius;
                if (ny >= 0 || nx * nx + ny * ny <= 1.0)
                    Set(x, y, 'w');
            }

        RoseWindow(cx, gateTop + 2, 1);
        StoneBridge(leftTower.Left, rightTower.Right, floorY, 2);
        Lantern(cx, gateTop + 1);
    }

    public void AddOuterButtresses(Box box, int size)
    {
        Buttress(box.Left, box.Bottom, -1, size);
        Buttress(box.Right, box.Bottom, 1, size);
    }

    public void Buttress(int rootX, int rootY, int direction, int size)
    {
        for (int d = 0; d < size; d++)
        {
            int y = rootY + d;
            int extent = Math.Max(1, d / 2 + 1);
            for (int i = 0; i <= extent; i++)
                Set(rootX + direction * i, y, 'B', false);
        }
    }

    public void Beam(int x, int y1, int y2)
    {
        if (y2 < y1) (y1, y2) = (y2, y1);
        for (int y = y1; y <= y2; y++)
            Set(x, y, 'I');
    }

    public void Lantern(int x, int y)
    {
        Set(x, y, 'n');
    }

    public void Workbench(int x, int floorY)
    {
        Set(x, floorY - 1, 't');
    }

    public void Chair(int x, int floorY)
    {
        Set(x, floorY - 2, 'c');
    }

    public void Bookcase(int x, int floorY)
    {
        Set(x, floorY - 4, 'k');
    }

    public void PlatformLine(int x1, int x2, int y)
    {
        if (x2 < x1) (x1, x2) = (x2, x1);
        for (int x = x1; x <= x2; x++)
            Set(x, y, 'P');
    }

    public void StairRun(int topX, int topY, int direction, int verticalDrop)
    {
        direction = direction >= 0 ? 1 : -1;
        verticalDrop = Math.Max(1, verticalDrop);

        PassageFloor(topX - direction, topY, 'P');

        char stair = direction > 0 ? '>' : '<';
        for (int i = 0; i <= verticalDrop; i++)
        {
            int x = topX + direction * i;
            int y = topY + i;
            PassageFloor(x, y, stair);
        }

        int bottomX = topX + direction * verticalDrop;
        int bottomY = topY + verticalDrop;
        PassageFloor(bottomX + direction, bottomY, 'P');
    }

    private void PassageFloor(int x, int y, char cell)
    {
        if (!Inside(x, y)) return;
        passageFloors[(x, y)] = cell;
        Set(x, y, cell);
        ReservePassage(x - 1, y - 3, 3, 3);
    }

    public void ReservePassage(int x, int y, int clearWidth, int clearHeight)
    {
        for (int yy = y; yy < y + clearHeight; yy++)
            for (int xx = x; xx < x + clearWidth; xx++)
                if (Inside(xx, yy)) passageAir.Add((xx, yy));
    }

    public void FinishPassages()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                char cell = Get(x, y);
                if (cell is not ('t' or 'c' or 'k' or 'n' or 'j')) continue;
                int furnitureWidth = cell == 'k' ? 3 : cell == 't' ? 2 : 1;
                int furnitureHeight = cell == 'k' ? 4 : cell is 't' or 'j' ? 1 : 2;
                for (int dx = -1; dx < furnitureWidth; dx++)
                    for (int dy = 0; dy < furnitureHeight; dy++)
                        if (passageAir.Contains((x + dx, y + dy))) Set(x, y, 'w');
            }
        foreach (var (x, y) in passageAir)
            if (!passageFloors.ContainsKey((x, y)) && Get(x, y) is not ('a' or 'b' or 'e'))
                Set(x, y, 'w');
        foreach (var (point, cell) in passageFloors)
            Set(point.X, point.Y, cell);
    }

    public void RoomStairs(Box box, int topY, int bottomY, int direction)
    {
        int left = box.Left + 3, right = box.Right - 3;
        if (right - left < 2 || bottomY <= topY) return;
        bool switchback = bottomY - topY > right - left;
        if (switchback && right - left <= 6)
        {
            for (int y = topY; y < bottomY; y += 4)
                for (int platformX = left; platformX <= right; platformX++) PassageFloor(platformX, y, 'P');
            return;
        }
        if (switchback) { left += 3; right -= 3; }
        direction = direction >= 0 ? 1 : -1;
        int x = direction > 0 ? left : right;
        while (topY < bottomY)
        {
            int drop = Math.Min(direction > 0 ? right - x : x - left, bottomY - topY);
            StairRun(x, topY, direction, drop);
            topY += drop;
            if (topY >= bottomY) break;
            int endX = x + direction * drop;
            for (int step = 0; step < 4; step++) PassageFloor(endX + direction * step, topY, 'P');
            x = endX + direction * 4;
            direction = -direction;
        }
    }

    public void Door(int x, int floorY)
    {
        passageFloors[(x, floorY)] = 'B';
        passageFloors[(x, floorY - 4)] = 'B';
        for (int y = floorY - 3; y < floorY; y++) passageFloors[(x, y)] = 'D';
        ReservePassage(x - 3, floorY - 3, 7, 3);
        FinishPassages();
    }

    public void RoseWindow(int centerX, int centerY, int radius)
    {
        for (int y = centerY - radius; y <= centerY + radius; y++)
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy <= radius * radius + 1)
                {
                    char wall = dx == 0 && dy == 0 ? 'b' : 'a';
                    char cell = Get(x, y);
                    if (cell is 'w' or 'v' or 'I' or 'P' or '>' or '<' or 'j' or 't' or 'c' or 'k' or 'n')
                    {
                        contextWalls[(x, y)] = wall;
                        if (cell == 'w') Set(x, y, wall);
                    }
                }
            }
    }

    public void VerticalWindows(Box box, int count)
    {
        int padding = box.Width >= 16 ? 3 : 2;
        int available = box.Width - padding * 2;
        if (available < 3 || box.Height < 6) return;
        int windowWidth = available >= 12 ? 3 : 2;
        int gap = 2;
        count = Math.Clamp(count, 1, Math.Max(1, (available + gap) / (windowWidth + gap)));
        int total = count * windowWidth + (count - 1) * gap;
        int startX = box.CenterX - total / 2;
        int top = box.Top + 2;
        int bottom = Math.Min(box.Bottom - 2, top + Math.Clamp(box.Height - 5, 3, 7));

        for (int i = 0; i < count; i++)
        {
            int left = startX + i * (windowWidth + gap);
            for (int y = top; y <= bottom; y++)
                for (int x = left; x < left + windowWidth; x++)
                {
                    char cell = Get(x, y);
                    if (cell is not ('w' or 'v' or 'I' or 'P' or '>' or '<' or 'j' or 't' or 'c' or 'k' or 'n')) continue;
                    char wall = y == top || y == bottom ? 'b' : 'a';
                    contextWalls[(x, y)] = wall;
                    if (cell == 'w') Set(x, y, wall);
                }
        }
    }

    public void RoofHorns(Box box)
    {
        if (box.Width < 6) return;
        Set(box.Left - 1, box.Top, 'B', false);
        Set(box.Right + 1, box.Top, 'B', false);
        Set(box.Left, box.Top - 1, 'B', false);
        Set(box.Right, box.Top - 1, 'B', false);
        if ((box.X + box.Y) % 3 != 0)
            for (int x = box.Left + 2; x < box.Right - 1; x += 2)
                Set(x, box.Top - 1, 'B', false);
    }

    public void CornerWebs(Box box)
    {
        if (box.Width < 7 || box.Height < 6) return;
        int left = box.Left + 2, right = box.Right - 2, top = box.Top + 2;
        if ((box.X + box.Y) % 2 == 0)
        {
            if (Get(left, top) == 'w') Set(left, top, 'v');
            if (Get(left + 1, top) == 'w') Set(left + 1, top, 'v');
            if (Get(left, top + 1) == 'w') Set(left, top + 1, 'v');
        }
        if ((box.X + box.Y + box.Width) % 3 != 0)
        {
            if (Get(right, top) == 'w') Set(right, top, 'v');
            if (Get(right - 1, top) == 'w') Set(right - 1, top, 'v');
            if (Get(right, top + 1) == 'w') Set(right, top + 1, 'v');
        }
    }

    public void Arch(int x1, int x2, int topY, int floorY)
    {
        if (x2 < x1) (x1, x2) = (x2, x1);
        int width = Math.Max(3, x2 - x1 + 1);
        int cx = (x1 + x2) / 2;
        double radius = width / 2.0;

        for (int x = x1; x <= x2; x++)
        {
            double nx = (x - cx) / radius;
            double cap = Math.Sqrt(Math.Max(0, 1 - nx * nx));
            int archY = topY + (int)Math.Round((1 - cap) * Math.Min(4, floorY - topY));
            Set(x, archY, 'u');
        }

        for (int y = topY + 1; y <= floorY; y++)
        {
            Set(x1, y, 'u');
            Set(x2, y, 'u');
        }
    }

    public void Crenellate(int x1, int x2, int y, int spacing)
    {
        spacing = Math.Max(2, spacing);
        for (int x = x1; x <= x2; x++)
            if ((x - x1) % spacing == 0)
                Set(x, y, 'B');
    }

    public void Clear(int x, int y, int clearWidth, int clearHeight, char replacement = '.')
    {
        for (int yy = y; yy < y + clearHeight; yy++)
            for (int xx = x; xx < x + clearWidth; xx++)
                Set(xx, yy, replacement);
    }

    public void JaggedCollapse(int centerX, int centerY, int radiusX, int radiusY, Random random)
    {
        for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                if (!Inside(x, y)) continue;
                double nx = (x - centerX) / (double)Math.Max(1, radiusX);
                double ny = (y - centerY) / (double)Math.Max(1, radiusY);
                double edge = nx * nx + ny * ny + random.NextDouble() * 0.45;
                char current = Get(x, y);
                if (edge <= 1.0 && current != '.' && current != 'D' && !passageFloors.ContainsKey((x, y))) Set(x, y, 'e');
            }
    }

    public bool DamageOuterWall(Random random, int radiusX, int radiusY)
    {
        List<(int X, int Y)> candidates = new();
        for (int y = 2; y < height - 2; y++)
            for (int x = 2; x < width - 2; x++)
            {
                if (Get(x, y) is not ('B' or 's' or 'o' or 'I')) continue;
                if (Get(x - 1, y) == '.' || Get(x + 1, y) == '.' || Get(x, y - 1) == '.' || Get(x, y + 1) == '.')
                    candidates.Add((x, y));
            }
        if (candidates.Count == 0) return false;
        var center = candidates[random.Next(candidates.Count)];
        JaggedCollapse(center.X, center.Y, radiusX, radiusY, random);
        return true;
    }

    public void SilkPatch(int centerX, int centerY, int radiusX, int radiusY, Random random)
    {
        List<(int X, int Y)> available = new();
        for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
            for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            {
                if (!Inside(x, y) || Get(x, y) != 'w' || passageAir.Contains((x, y))) continue;
                double nx = (x - centerX) / (double)Math.Max(1, radiusX);
                double ny = (y - centerY) / (double)Math.Max(1, radiusY);
                if (nx * nx + ny * ny <= 1.0) available.Add((x, y));
            }
        if (available.Count == 0) return;
        HashSet<(int X, int Y)> allowed = new(available);
        List<(int X, int Y)> frontier = [available.OrderBy(p => Math.Abs(p.X - centerX) + Math.Abs(p.Y - centerY)).First()];
        HashSet<(int X, int Y)> web = new();
        int target = Math.Clamp((int)Math.Round(available.Count * random.NextDouble() * .25 + available.Count * .35), 1, available.Count);
        while (frontier.Count > 0 && web.Count < target)
        {
            int index = random.Next(frontier.Count);
            var point = frontier[index]; frontier.RemoveAt(index);
            if (!allowed.Contains(point) || !web.Add(point)) continue;
            Set(point.X, point.Y, 'v');
            (int X, int Y)[] neighbors = [(point.X - 1, point.Y), (point.X + 1, point.Y), (point.X, point.Y - 1), (point.X, point.Y + 1)];
            for (int i = neighbors.Length - 1; i > 0; i--)
            {
                int swap = random.Next(i + 1);
                (neighbors[i], neighbors[swap]) = (neighbors[swap], neighbors[i]);
            }
            int added = 0;
            foreach (var next in neighbors)
                if (allowed.Contains(next) && !web.Contains(next) && random.NextDouble() < .82) { frontier.Add(next); added++; }
            if (added == 0 && web.Count < target)
                foreach (var next in neighbors)
                    if (allowed.Contains(next) && !web.Contains(next)) { frontier.Add(next); break; }
        }
    }

    public void HangingChainCluster(int centerX, int startY, Random random, int strands, int minLength, int maxLength)
    {
        HashSet<int> columns = new();
        for (int attempt = 0; attempt < strands * 5 && columns.Count < strands; attempt++) columns.Add(centerX + random.Next(-2, 3));
        foreach (int x in columns)
        {
            int y = startY + random.Next(0, 3);
            int length = random.Next(minLength, maxLength + 1);
            for (int i = 0; i < length; i++)
            {
                int yy = y + i;
                if (!Inside(x, yy) || passageAir.Contains((x, yy))) break;
                if (Get(x, yy) is 'w' or 'e' or '.') Set(x, yy, 'L', false);
                else if (i > 0) break;
            }
        }
    }

    public void RockRoot(int rootX, int rootY, int direction, int depth, int widthAtTop)
    {
        for (int d = 0; d < depth; d++)
        {
            int half = Math.Max(0, widthAtTop - d / 2);
            int shift = direction * d / 3;
            for (int x = rootX + shift - half; x <= rootX + shift + half; x++)
                Set(x, rootY + d, 'B', false);
        }
    }


    public void RootExistingFoundation(Random random, int roots, int minDepth, int maxDepth)
    {
        List<(int X, int Y)> candidates = new();

        for (int x = 2; x < width - 2; x++)
        {
            int lowest = -1;
            for (int y = height - 3; y >= 2; y--)
            {
                char cell = Get(x, y);
                if (cell is 'B' or 's' or 'o')
                {
                    lowest = y;
                    break;
                }
                if (cell is 'P' or '>' or '<' or 'I' or 'L' or 'v' or '~' or 't' or 'c' or 'k' or 'n')
                    continue;
            }

            if (lowest < 0) continue;
            bool neighbor = Get(x - 1, lowest) is 'B' or 's' or 'o' || Get(x + 1, lowest) is 'B' or 's' or 'o';
            if (neighbor) candidates.Add((x, lowest));
        }

        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swap = random.Next(i + 1);
            (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
        }

        List<int> used = new();
        foreach (var candidate in candidates)
        {
            if (used.Exists(x => Math.Abs(x - candidate.X) < 5)) continue;
            used.Add(candidate.X);

            int depth = random.Next(minDepth, maxDepth + 1);
            int direction = random.Next(2) == 0 ? -1 : 1;
            int widthAtTop = random.Next(1, 3);
            RockRoot(candidate.X, candidate.Y + 1, direction, depth, widthAtTop);

            if (used.Count >= roots) break;
        }
    }

    public void WeatherMasonry(Random random, double chance)
    {
        List<(int X, int Y)> chips = new();
        for (int y = 2; y < height - 2; y++)
            for (int x = 2; x < width - 2; x++)
            {
                if (Get(x, y) != 'B' || passageFloors.ContainsKey((x, y)) || passageAir.Contains((x, y))) continue;
                bool exposed = Get(x - 1, y) is '.' or 'e' || Get(x + 1, y) is '.' or 'e' ||
                    Get(x, y - 1) is '.' or 'e';
                bool carriesFloor = "PD<>".Contains(Get(x, y - 1)) || "PD<>".Contains(Get(x - 1, y)) || "PD<>".Contains(Get(x + 1, y));
                if (exposed && !carriesFloor && random.NextDouble() < chance) chips.Add((x, y));
            }
        foreach (var (x, y) in chips) Set(x, y, 'e');
    }

    public void FinishPlatformSupports()
    {
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (Get(x, y) == 'I') Set(x, y, 'w');
        for (int y = 1; y < height - 3; y++)
        {
            int last = -8;
            for (int x = 2; x < width - 2; x++)
            {
                if (Get(x, y) != 'P' || x - last < 4) continue;
                bool end = Get(x - 1, y) != 'P' || Get(x + 1, y) != 'P';
                if (!end && (x + y) % 6 != 0) continue;
                int floor = y + 1;
                while (floor < Math.Min(height - 1, y + 19) && Get(x, floor) is 'w' or 'e' or 'u')
                {
                    if (passageAir.Contains((x, floor))) break;
                    floor++;
                }
                if (floor - y < 3 || Get(x, floor) is not ('B' or 's' or 'o' or 'P')) continue;
                for (int yy = y + 1; yy < floor; yy++) Set(x, yy, 'I');
                last = x;
            }
        }
    }

    public void FurnishShelves(Random random)
    {
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if (Get(x, y) == 'j' && (Get(x, y + 1) != 'P' || passageAir.Contains((x, y)))) Set(x, y, 'w');
        for (int y = 3; y < height - 3; y++)
            for (int x = 3; x < width - 4; x++)
            {
                if (Get(x, y) != 'P' || Get(x + 1, y) != 'P' || Get(x, y - 1) != 'w' ||
                    Get(x + 1, y - 1) != 'w' || passageAir.Contains((x, y - 1)) || passageAir.Contains((x + 1, y - 1))) continue;
                if (random.NextDouble() > .14) continue;
                Set(x, y - 1, 'j');
                if (random.NextDouble() < .65) Set(x + 1, y - 1, 'j');
                x += 3;
            }
    }

    public void BookShelves(int x, int top, int bottom, int shelfWidth = 3)
    {
        for (int y = top + 2; y < bottom; y += 3)
        {
            PlatformLine(x, x + shelfWidth - 1, y);
            for (int xx = x; xx < x + shelfWidth; xx++)
                if (Get(xx, y - 1) is 'w' or 'e') Set(xx, y - 1, 'j');
        }
    }

    public bool LavaBasin(int x, int y, int basinWidth)
    {
        for (int yy = y - 1; yy <= y + 2; yy++)
            for (int xx = x - 1; xx <= x + basinWidth; xx++)
                if (!Inside(xx, yy) || passageAir.Contains((xx, yy)) || passageFloors.ContainsKey((xx, yy)) ||
                    Get(xx, yy) is not ('.' or 'w' or 'B' or 'e')) return false;
        Fill(new Box(x - 1, y - 1, basinWidth + 2, 4), 'B');
        for (int xx = x; xx < x + basinWidth; xx++) Set(xx, y, '!');
        for (int yy = y - 1; yy <= y + 2; yy++)
            for (int xx = x - 1; xx <= x + basinWidth; xx++) passageFloors[(xx, yy)] = Get(xx, yy);
        return true;
    }

    public void AddRubbleBelowDamage(Random random, int attempts)
    {
        for (int i = 0; i < attempts; i++)
        {
            int x = random.Next(2, width - 2);
            int y = random.Next(2, height - 3);
            if (Get(x, y) != '.') continue;
            if (Get(x, y - 1) != '.' || Get(x, y - 2) != '.') continue;
            if (Get(x, y + 1) == '.') continue;
            Set(x, y, 'B');
            if (random.NextDouble() < 0.4) Set(x + (random.Next(2) == 0 ? -1 : 1), y, 'B', false);
        }
    }

    public void NormalizeInteriorBackground()
    {
        bool[,] exterior = new bool[width, height];
        Queue<(int X, int Y)> pending = new();
        void Add(int x, int y)
        {
            if (!Inside(x, y) || exterior[x, y] || Get(x, y) is not ('.' or 'e')) return;
            exterior[x, y] = true;
            pending.Enqueue((x, y));
        }
        for (int x = 0; x < width; x++) { Add(x, 0); Add(x, height - 1); }
        for (int y = 0; y < height; y++) { Add(0, y); Add(width - 1, y); }
        while (pending.Count > 0)
        {
            var point = pending.Dequeue();
            Add(point.X - 1, point.Y); Add(point.X + 1, point.Y);
            Add(point.X, point.Y - 1); Add(point.X, point.Y + 1);
        }
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if (Get(x, y) == 'e' && !exterior[x, y]) Set(x, y, 'w');
    }

    public CitadelHouseStamp ToStamp(string id, int previewX, int previewY, int padding = 1)
    {
        int left = width;
        int top = height;
        int right = -1;
        int bottom = -1;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (cells[y][x] != '.')
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }

        if (right < left || bottom < top)
            return new CitadelHouseStamp(id, previewX, previewY, ["B"]);

        left = Math.Max(0, left - padding);
        right = Math.Min(width - 1, right + padding);
        top = Math.Max(0, top - padding);
        bottom = Math.Min(height - 1, bottom + padding);

        string[] rows = new string[bottom - top + 1];
        for (int y = top; y <= bottom; y++)
        {
            char[] row = new char[right - left + 1];
            for (int x = left; x <= right; x++)
                row[x - left] = cells[y][x];
            rows[y - top] = new string(row);
        }

        return new CitadelHouseStamp(id, previewX, previewY, rows)
        {
            PassageCells = passageAir.Select(p => (p.X - left, p.Y - top)).ToHashSet(),
            ContextWalls = contextWalls.Where(p => p.Key.X >= left && p.Key.X <= right && p.Key.Y >= top && p.Key.Y <= bottom)
                .ToDictionary(p => (p.Key.X - left, p.Key.Y - top), p => p.Value)
        };
    }
}
