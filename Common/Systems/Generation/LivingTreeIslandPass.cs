using System;
using System.Collections.Generic;
#if !ISLAND_PREVIEW
using AerovelenceMod.Common.Utilities.Generation;
using AerovelenceMod.Common.Utilities.Generation.StructureStamper;
using AerovelenceMod.Content.Items.Weapons.Misc.Ranged.Guns;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
#endif

namespace AerovelenceMod.Common.Systems.Generation;

internal sealed class LivingTreeIslandLayout
{
    internal enum Material : byte { Air, Grass, Dirt, Stone, Wood, Leaves, Platform, Door, Cloud, RainCloud }
    internal const int Width = 196;
    internal const int Height = 128;
    internal const int IslandCenterY = 73;
    internal readonly Material[,] Tiles = new Material[Width, Height];
    internal readonly byte[,] Walls = new byte[Width, Height];
    internal readonly byte[,] Water = new byte[Width, Height];
    internal readonly Material[,] TerrainTiles;
    internal readonly byte[,] TerrainWalls;
    internal readonly byte[,] TerrainWater;
    internal readonly int Center;
    internal readonly int IslandCenter;
    internal readonly int BranchCount;
    internal readonly List<(int X, int Y)> FloatingCloudSpots = new();
    internal const int DoorOffset = 4;
    internal readonly int CellarCenter;
    internal readonly List<(int X, int Y)> TreeSpots = new();
    internal readonly List<(int X, int Y)> SunflowerSpots = new();
    internal int MinX { get; private set; } = Width;
    internal int MinY { get; private set; } = Height;
    internal int MaxX { get; private set; }
    internal int MaxY { get; private set; }
    internal readonly int Ground;
    internal int RoomFloor => Ground - 1;
    internal int CellarFloor => Ground + 13;
    internal int ChestX => CellarCenter + 5;
    internal int ChestY => CellarFloor - 1;

    internal LivingTreeIslandLayout(int seed)
    {
        Random random = new(seed);
        IslandCenter = Width / 2 + random.Next(-3, 4);
        Center = IslandCenter + (random.Next(2) == 0 ? -1 : 1) * random.Next(7, 14);
        int cellarSide = Center > IslandCenter ? -1 : 1;
        CellarCenter = Center + cellarSide * random.Next(17, 21);
        int treeHeight = random.Next(30, 42);
        double phase = random.NextDouble() * Math.PI * 2;
        GenerateIslandBody(random, IslandCenter, IslandCenterY, out int cloudMinX, out int cloudMaxX, out int cloudMinY, out int cloudMaxY, out int dirtMinX, out int dirtMaxX);
        Ground = Height - 1;
        for (int x = Center - 6; x <= Center + 6; x++)
            for (int y = 3; y < Height - 1; y++)
                if (Tiles[x, y] == Material.Dirt)
                {
                    Ground = Math.Min(Ground, y);
                    break;
                }
        if (Ground == Height - 1) Ground = cloudMinY;
        int crown = Ground - treeHeight;
        ShapeTreeTerrace();
        RoughenSurface(random, dirtMinX, dirtMaxX);
        ApplyIslandCloudWalls(cloudMinX, cloudMaxX, cloudMinY, cloudMaxY);
        CarveCloudWater(random, cloudMinX, cloudMaxX, cloudMinY, cloudMaxY);
        RefreshGrass();
        int surfaceMinX = Math.Max(6, dirtMinX + 4);
        int surfaceMaxX = Math.Min(Width - 7, dirtMaxX - 4);
        if (surfaceMaxX < surfaceMinX)
        {
            surfaceMinX = Math.Max(6, IslandCenter - 24);
            surfaceMaxX = Math.Min(Width - 7, IslandCenter + 24);
        }

        TerrainTiles = (Material[,])Tiles.Clone();
        TerrainWalls = (byte[,])Walls.Clone();
        TerrainWater = (byte[,])Water.Clone();
        BranchCount = GrowTree(random, crown, phase);

        BuildInterior(random, cellarSide);
        TrimFoliage(random);

        ChoosePlantSpots(random, surfaceMinX, surfaceMaxX);
        GenerateFloatingClouds(random, cloudMinX, cloudMaxX, cloudMinY);

        AddGroundPonds(random, surfaceMinX, surfaceMaxX);
        FinishLayout();
    }

    private int GrowTree(Random random, int crown, double phase)
    {
        int canopyWidth = random.Next(12, 19);
        Ellipse(Center, crown + 6, canopyWidth, random.Next(6, 10), Material.Leaves);
        int lobes = random.Next(4, 8);
        for (int lobe = 0; lobe < lobes; lobe++)
        {
            int offset = random.Next(-canopyWidth, canopyWidth + 1);
            Ellipse(Center + offset, crown + Math.Abs(offset) / 4 + random.Next(-3, 4),
                random.Next(5, 9), random.Next(5, 9), Material.Leaves);
        }
        for (int y = crown + 5; y <= Ground + 4; y++)
        {
            int bend = (int)(Math.Sin(y * .09 + phase) * 1.5);
            int halfWidth = y > Ground - 11 ? 3 : y > crown + 17 ? 2 : 1;
            for (int x = Center + bend - halfWidth; x <= Center + bend + halfWidth; x++)
            {
                Tiles[x, y] = Material.Wood;
                Walls[x, y] = 1;
            }
        }
        int branchCount = random.Next(3, 5);
        int firstBranchSide = random.Next(2) == 0 ? -1 : 1;
        for (int branch = 0; branch < branchCount; branch++)
        {
            int side = branch % 2 == 0 ? firstBranchSide : -firstBranchSide;
            int startY = crown + 7 + branch / 2 * 12 + branch % 2 * 2 + random.Next(2);
            int reach = random.Next(12, 20), rise = random.Next(5, 8);
            int shoulderX = Center + side * random.Next(3, 6), shoulderY = startY - random.Next(0, 2);
            int elbowX = Center + side * random.Next(Math.Max(6, reach / 2), Math.Max(7, reach - 4)), elbowY = startY - random.Next(2, Math.Max(3, rise - 2));
            int tipX = Center + side * reach, tipY = startY - rise;
            Stroke(Center, startY, shoulderX, shoulderY, 1);
            Stroke(shoulderX, shoulderY, elbowX, elbowY, 1);
            Stroke(elbowX, elbowY, tipX, tipY, 1);
            Ellipse(tipX + side * random.Next(1, 3), tipY - 1, random.Next(4, 8), random.Next(3, 6), Material.Leaves);
        }
        int rootCount = random.Next(2, 4);
        for (int root = 0; root < rootCount; root++)
        {
            int side = root % 2 == 0 ? -1 : 1;
            int startX = Center + random.Next(-3, 4);
            int endX = Center + side * random.Next(10, 20);
            int endY = Ground + random.Next(5, 10);
            int bendX = (startX + endX) / 2 + random.Next(-2, 3);
            int bendY = Ground + random.Next(3, 6);
            Stroke(startX, Ground + 2, bendX, bendY, 1);
            Stroke(bendX, bendY, endX, endY, 1);
        }
        return branchCount;
    }

    private void BuildInterior(Random random, int cellarSide)
    {
        Atrium(random);
        Chamber(CellarCenter - 10, CellarFloor - 8, 21, 9);
        int tunnelEnd = CellarCenter - cellarSide * 10;
        for (int x = Math.Min(Center - 2, tunnelEnd); x <= Math.Max(Center + 2, tunnelEnd); x++)
            for (int y = CellarFloor - 6; y <= CellarFloor; y++)
            {
                Tiles[x, y] = y == CellarFloor - 6 || y == CellarFloor ? Material.Wood : Material.Air;
                Walls[x, y] = 1;
            }
        int cellarDoorSide = -cellarSide;
        int cellarDoorX = CellarCenter + cellarDoorSide * 10;
        for (int y = CellarFloor - 5; y <= CellarFloor; y++)
        {
            Tiles[cellarDoorX, y] = y >= CellarFloor - 3 && y < CellarFloor ? Material.Door : Material.Wood;
            if (y >= CellarFloor - 3 && y < CellarFloor) Tiles[cellarDoorX + cellarDoorSide, y] = Material.Air;
        }
        for (int x = Center - 2; x <= Center + 2; x++)
            Tiles[x, CellarFloor] = Material.Wood;
        int tunnelBend = random.Next(2) == 0 ? -1 : 1;
        for (int y = RoomFloor; y < CellarFloor; y++)
        {
            int tunnelX = Center + (int)Math.Round(Math.Sin((y - RoomFloor) * Math.PI / (CellarFloor - RoomFloor)) * tunnelBend);
            for (int offset = -2; offset <= 2; offset++)
            {
                int x = tunnelX + offset;
                bool sideWall = Math.Abs(offset) == 2;
                bool opening = offset == cellarSide * 2 && y >= CellarFloor - 5;
                Tiles[x, y] = sideWall ? (opening ? Material.Air : Material.Wood) :
                    (y - RoomFloor) % 5 == 0 ? Material.Platform : Material.Air;
                Walls[x, y] = 1;
            }
        }
        for (int side = -1; side <= 1; side += 2)
        {
            for (int y = RoomFloor - 3; y < RoomFloor; y++)
            {
                Tiles[Center + side * DoorOffset, y] = Material.Door;
                Tiles[Center + side * (DoorOffset - 1), y] = Material.Air;
            }
        }
        FillTrunkShoulders();
    }

    private void TrimFoliage(Random random)
    {
        List<(int X, int Y)> leafNicks = new();
        for (int x = 2; x < Width - 2; x++)
            for (int y = 2; y < Ground - 4; y++)
            {
                if (Tiles[x, y] != Material.Leaves || Tiles[x, y - 1] != Material.Air) continue;
                int neighbors = 0;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if ((dx != 0 || dy != 0) && Tiles[x + dx, y + dy] == Material.Leaves) neighbors++;
                if (neighbors >= 5 && random.Next(12) == 0) leafNicks.Add((x, y));
            }
        foreach (var nick in leafNicks) Tiles[nick.X, nick.Y] = Material.Air;
        bool[,] attached = new bool[Width, Height];
        Queue<(int X, int Y)> foliage = new();
        for (int x = Center - 4; x <= Center + 4; x++)
            if (Tiles[x, Ground - 16] == Material.Wood) foliage.Enqueue((x, Ground - 16));
        while (foliage.TryDequeue(out var point))
        {
            int x = point.X, y = point.Y;
            if (x < 1 || x >= Width - 1 || y < 1 || y >= Ground || attached[x, y] ||
                Tiles[x, y] is not (Material.Wood or Material.Leaves)) continue;
            attached[x, y] = true;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (dx != 0 || dy != 0) foliage.Enqueue((x + dx, y + dy));
        }
        for (int x = 1; x < Width - 1; x++)
            for (int y = 1; y < Ground; y++)
                if (Tiles[x, y] == Material.Leaves && !attached[x, y]) Tiles[x, y] = Material.Air;
    }

    private void ChoosePlantSpots(Random random, int surfaceMinX, int surfaceMaxX)
    {
        int treeTarget = random.Next(2, 5);
        for (int attempt = 0; attempt < 160 && TreeSpots.Count < treeTarget; attempt++)
            TryAddTreeSpot(random.Next(surfaceMinX, surfaceMaxX + 1));
        if (TreeSpots.Count == 0)
        {
            TryAddTreeSpot(Center - 24);
            TryAddTreeSpot(Center + 24);
        }

        int sunflowerTarget = random.Next(2, 5);
        for (int attempt = 0; attempt < 180 && SunflowerSpots.Count < sunflowerTarget; attempt++)
            TryAddSunflowerSpot(random.Next(surfaceMinX, surfaceMaxX + 1));
        for (int x = 6; x < Width - 7 && SunflowerSpots.Count == 0; x++) TryAddSunflowerSpot(x);
    }

    private void AddGroundPonds(Random random, int surfaceMinX, int surfaceMaxX)
    {
        int groundPonds = random.Next(2);
        for (int pond = 0; pond < groundPonds; pond++)
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = random.Next(surfaceMinX, surfaceMaxX + 1);
                if (Math.Abs(x - Center) < 12) continue;
                bool blocked = false;
                foreach (var spot in TreeSpots)
                    if (Math.Abs(spot.X - x) <= 5) blocked = true;
                foreach (var spot in SunflowerSpots)
                    if (Math.Abs(spot.X - x) <= 3) blocked = true;
                if (blocked) continue;
                int y = Ground - 3;
                while (y < Ground + 10 && Tiles[x, y] != Material.Grass) y++;
                if (y >= Ground + 10) continue;
                if (Basin(x, y, random.Next(1, 3), 1)) break;
            }
    }

    private void FinishLayout()
    {
        for (int x = 1; x < Width - 1; x++)
            for (int y = 1; y < Height - 1; y++)
                if (Tiles[x, y] != Material.Air) Water[x, y] = 0;
        RefreshGrass();

        for (int x = 1; x < Width - 1; x++)
            for (int y = 1; y < Height - 1; y++)
            {
                Material material = Tiles[x, y];
                if (material == Material.Wood)
                {
                    bool edge = false;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if (Tiles[x + dx, y + dy] != Material.Wood && Walls[x + dx, y + dy] != 1) edge = true;
                    if (edge) Walls[x, y] = 0;
                }
                if (!IsStructure(x, y))
                {
                    TerrainTiles[x, y] = material;
                    TerrainWalls[x, y] = Walls[x, y];
                    TerrainWater[x, y] = Water[x, y];
                }
                if (material == Material.Air && Walls[x, y] == 0 && Water[x, y] == 0) continue;
                MinX = Math.Min(MinX, x);
                MinY = Math.Min(MinY, y);
                MaxX = Math.Max(MaxX, x);
                MaxY = Math.Max(MaxY, y);
            }
    }

    internal bool IsStructure(int x, int y) => Walls[x, y] == 1 ||
        Tiles[x, y] is Material.Wood or Material.Leaves or Material.Platform or Material.Door;

    private void Atrium(Random random)
    {
        List<(int X, int Y)> interior = new();
        int top = RoomFloor - random.Next(9, 12);
        int bend = random.Next(2) == 0 ? -1 : 1;
        for (int y = top; y < RoomFloor; y++)
        {
            double height = (y - top) / (double)(RoomFloor - top - 1);
            int halfWidth = y >= RoomFloor - 3 ? 3 : 2 + (int)(Math.Sin(height * Math.PI * .7));
            int offset = y >= RoomFloor - 3 ? 0 : (int)Math.Round(Math.Sin(height * Math.PI) * bend);
            for (int x = Center + offset - halfWidth; x <= Center + offset + halfWidth; x++) interior.Add((x, y));
        }
        foreach (var cell in interior)
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    Tiles[cell.X + dx, cell.Y + dy] = Material.Wood;
                    Walls[cell.X + dx, cell.Y + dy] = 1;
                }
        foreach (var cell in interior) Tiles[cell.X, cell.Y] = Material.Air;
    }

    private bool Basin(int center, int top, int halfWidth, int depth)
    {
        bool Solid(int x, int y) => Tiles[x, y] is Material.Grass or Material.Dirt or Material.Cloud or Material.RainCloud;
        bool Cloud(int x, int y) => Tiles[x, y] is Material.Cloud or Material.RainCloud;
        int left = center - halfWidth, right = center + halfWidth;
        if (left < 2 || right >= Width - 2 || top < 2 || top + depth >= Height - 2) return false;
        if (!Solid(left - 1, top) || !Solid(right + 1, top)) return false;
        bool cloudBasin = true;
        for (int x = left; x <= right; x++)
        {
            if (Tiles[x, top - 1] != Material.Air || Water[x, top - 1] != 0 || !Solid(x, top + depth)) return false;
            for (int y = top; y < top + depth; y++)
            {
                if (!Solid(x, y)) return false;
                if (!Cloud(x, y)) cloudBasin = false;
            }
        }
        for (int x = left; x <= right; x++)
            for (int y = top; y < top + depth; y++)
            {
                Tiles[x, y] = Material.Air;
                Walls[x, y] = cloudBasin ? (byte)3 : (byte)0;
                Water[x, y] = 255;
            }
        return true;
    }

    private int FindSurface(int x, int minY, int maxY)
    {
        for (int y = Math.Max(2, minY); y <= Math.Min(Height - 3, maxY); y++)
            if (Tiles[x, y] == Material.Grass && Tiles[x, y - 1] == Material.Air && Water[x, y - 1] == 0) return y;
        return -1;
    }

    private bool TryAddTreeSpot(int x)
    {
        if (x < 6 || x >= Width - 6 || Math.Abs(x - Center) < 16) return false;
        foreach (var spot in TreeSpots)
            if (Math.Abs(spot.X - x) < 8) return false;
        int ground = FindSurface(x, 3, Ground + 8);
        if (ground < 0) return false;
        for (int column = x - 2; column <= x + 2; column++)
        {
            int columnGround = FindSurface(column, 3, Ground + 8);
            if (columnGround < 0 || Math.Abs(columnGround - ground) > 2) return false;
            for (int y = ground - 16; y < ground; y++)
                if (Tiles[column, y] != Material.Air || Water[column, y] > 0) return false;
        }
        for (int column = x - 2; column <= x + 2; column++)
            for (int y = ground - 2; y <= ground + 2; y++)
            {
                if (y < ground)
                {
                    Tiles[column, y] = Material.Air;
                    Walls[column, y] = 0;
                }
                else Tiles[column, y] = y == ground ? Material.Grass : Material.Dirt;
            }
        TreeSpots.Add((x, ground));
        return true;
    }

    private bool TryAddSunflowerSpot(int x)
    {
        if (x < 6 || x >= Width - 7 || Math.Abs(x - Center) < 12) return false;
        foreach (var spot in TreeSpots)
            if (Math.Abs(spot.X - x) < 6) return false;
        foreach (var spot in SunflowerSpots)
            if (Math.Abs(spot.X - x) < 4) return false;
        int ground = FindSurface(x, 3, Ground + 8);
        if (ground < 0 || Tiles[x + 1, ground] != Material.Grass) return false;
        for (int dx = 0; dx <= 1; dx++)
            for (int dy = 1; dy <= 4; dy++)
                if (Tiles[x + dx, ground - dy] != Material.Air || Water[x + dx, ground - dy] > 0) return false;
        for (int dx = -1; dx <= 2; dx++)
            for (int dy = 0; dy <= 5; dy++) Walls[x + dx, ground - dy] = 0;
        SunflowerSpots.Add((x, ground));
        return true;
    }

    private void FillTrunkShoulders()
    {
        for (int x = Center - 6; x <= Center + 6; x++)
        {
            if (Math.Abs(x - Center) <= 2) continue;
            for (int y = Ground - 7; y <= Ground + 2; y++)
            {
                if (Tiles[x, y] != Material.Air || Water[x, y] > 0 || Walls[x, y] == 1) continue;
                bool wood = false, dirt = false;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        wood |= Tiles[x + dx, y + dy] == Material.Wood;
                        dirt |= Tiles[x + dx, y + dy] is Material.Dirt or Material.Grass;
                    }
                if (wood && dirt) Tiles[x, y] = Material.Dirt;
            }
        }
    }

    private void RoughenSurface(Random random, int minX, int maxX)
    {
        int profile = Ground + random.Next(-1, 2);
        for (int x = Math.Max(4, minX - 2); x <= Math.Min(Width - 5, maxX + 2); x++)
        {
            if (Math.Abs(x - Center) <= 11) continue;
            if (random.Next(3) == 0) profile += random.Next(-1, 2);
            profile = Math.Clamp(profile, Ground - 1, Ground + 3);
            int top = FindSurface(x, Ground - 4, Ground + 8);
            if (top < 0) continue;
            int target = Math.Clamp(profile + (int)Math.Round(Math.Sin((x - minX) * .17) * 1.5), Ground - 1, Ground + 4);
            if (target < top)
            {
                for (int fillY = target; fillY < top; fillY++)
                    if (Tiles[x, fillY] is Material.Air or Material.Cloud or Material.RainCloud) Tiles[x, fillY] = Material.Dirt;
            }
            else if (target > top)
            {
                for (int carveY = top; carveY < target; carveY++)
                    if (Tiles[x, carveY] is Material.Dirt or Material.Grass) Tiles[x, carveY] = Material.Air;
            }
        }
    }

    private void GenerateIslandBody(Random random, int centerX, int centerY, out int minX, out int maxX, out int minY, out int maxY, out int dirtMinX, out int dirtMaxX)
    {
        double strength = random.Next(108, 161);
        int steps = random.Next(22, 31);
        double x = centerX, y = centerY;
        double velocityX;
        do velocityX = random.Next(-20, 21) * .2; while (velocityX > -2 && velocityX < 2);
        double velocityY = random.Next(-20, -9) * .02;
        minX = centerX;
        maxX = centerX;
        minY = centerY;
        maxY = centerY;
        while (strength > 0 && steps-- > 0)
        {
            strength -= random.Next(4);
            double radius = strength * random.Next(80, 120) * .004;
            double cutoff = y + 1;
            int left = Math.Max(3, (int)(x - strength * .5));
            int right = Math.Min(Width - 4, (int)(x + strength * .5));
            int top = Math.Max(3, (int)(y - strength * .5));
            int bottom = Math.Min(Height - 4, (int)(y + strength * .5));
            for (int px = left; px < right; px++)
            {
                if (random.Next(2) == 0) cutoff += random.Next(-1, 2);
                cutoff = Math.Clamp(cutoff, y, y + 2);
                for (int py = top; py < bottom; py++)
                {
                    if (py <= cutoff) continue;
                    double dx = Math.Abs(px - x);
                    double dy = Math.Abs(py - y) * 3;
                    if (Math.Sqrt(dx * dx + dy * dy) >= radius) continue;
                    Tiles[px, py] = Material.Cloud;
                    minX = Math.Min(minX, px);
                    maxX = Math.Max(maxX, px);
                    minY = Math.Min(minY, py);
                    maxY = Math.Max(maxY, py);
                }
            }
            x += velocityX;
            y += velocityY;
            velocityX += random.Next(-20, 21) * .05;
            velocityX = Math.Clamp(velocityX, -1, 1);
            if (velocityY > .2 || velocityY < -.2) velocityY = -.2;
        }

        int patchX = minX + random.Next(5);
        while (patchX < maxX)
        {
            int bottom = FindBottomSolid(patchX, minY, maxY);
            if (bottom < 0) { patchX += random.Next(1, 4); continue; }
            int center = bottom + random.Next(-3, 4);
            int radius = random.Next(4, 8);
            Material material = random.Next(4) == 0 ? Material.RainCloud : Material.Cloud;
            PaintIslandPatch(random, patchX, center, radius, material, false);
            minX = Math.Min(minX, patchX - radius);
            maxX = Math.Max(maxX, patchX + radius);
            maxY = Math.Max(maxY, center + radius / 2 + 2);
            patchX += random.Next(radius, Math.Max(radius + 1, (int)(radius * 1.5) + 1));
        }

        double dirtStrength = random.Next(86, 103);
        int dirtSteps = random.Next(11, 16);
        x = centerX;
        y = minY;
        do velocityX = random.Next(-20, 21) * .2; while (velocityX > -2 && velocityX < 2);
        velocityY = random.Next(-20, -9) * .02;
        dirtMinX = Width;
        dirtMaxX = 0;
        while (dirtStrength > 0 && dirtSteps-- > 0)
        {
            dirtStrength -= random.Next(4);
            double radius = dirtStrength * random.Next(80, 120) * .004;
            double cutoff = y + 1;
            int left = Math.Max(3, (int)(x - dirtStrength * .5));
            int right = Math.Min(Width - 4, (int)(x + dirtStrength * .5));
            int bottom = Math.Min(Height - 4, (int)(y + dirtStrength * .5));
            for (int px = left; px < right; px++)
            {
                if (random.Next(2) == 0) cutoff += random.Next(-1, 2);
                cutoff = Math.Clamp(cutoff, y, y + 2);
                for (int py = Math.Max(3, minY - 1); py < bottom; py++)
                {
                    if (py <= cutoff || Tiles[px, py] != Material.Cloud) continue;
                    double dx = Math.Abs(px - x);
                    double dy = Math.Abs(py - y) * 3;
                    if (Math.Sqrt(dx * dx + dy * dy) >= radius) continue;
                    Tiles[px, py] = Material.Dirt;
                    dirtMinX = Math.Min(dirtMinX, px);
                    dirtMaxX = Math.Max(dirtMaxX, px);
                }
            }
            x += velocityX;
            y += velocityY;
            velocityX += random.Next(-20, 21) * .05;
            velocityX = Math.Clamp(velocityX, -1, 1);
            if (velocityY > .2 || velocityY < -.2) velocityY = -.2;
        }

        if (dirtMinX >= dirtMaxX)
        {
            dirtMinX = centerX - 30;
            dirtMaxX = centerX + 30;
        }

        patchX = minX + random.Next(5);
        while (patchX < maxX)
        {
            int dirtBottom = FindBottomMaterial(patchX, minY, maxY, Material.Dirt);
            if (dirtBottom < 0) { patchX += random.Next(1, 4); continue; }
            int center = dirtBottom + random.Next(0, 4);
            int radius = random.Next(2, 5);
            PaintIslandPatch(random, patchX, center, radius, Material.Cloud, true);
            patchX += random.Next(radius, Math.Max(radius + 1, (int)(radius * 1.5) + 1));
        }
    }

    private void PaintIslandPatch(Random random, int centerX, int centerY, int radius, Material material, bool overwriteDirt)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (x <= 2 || y <= 2 || x >= Width - 3 || y >= Height - 3) continue;
                double dx = Math.Abs(x - centerX);
                double dy = Math.Abs(y - centerY) * 2;
                if (Math.Sqrt(dx * dx + dy * dy) >= radius + random.Next(2)) continue;
                if (!overwriteDirt && Tiles[x, y] == Material.Dirt) continue;
                if (overwriteDirt && Tiles[x, y] is not (Material.Air or Material.Cloud or Material.RainCloud or Material.Dirt)) continue;
                Tiles[x, y] = material;
            }
    }

    private void ShapeTreeTerrace()
    {
        for (int x = Center - 14; x <= Center + 14; x++)
        {
            int edge = Math.Max(0, Math.Abs(x - Center) - 4);
            int naturalSurface = 3;
            while (naturalSurface < Ground && Tiles[x, naturalSurface] == Material.Air) naturalSurface++;
            int surface = Math.Max(naturalSurface, Ground - edge * 2);
            for (int y = 3; y < surface; y++)
                if (Tiles[x, y] is Material.Cloud or Material.RainCloud or Material.Dirt or Material.Grass)
                {
                    Tiles[x, y] = Material.Air;
                    Walls[x, y] = 0;
                    Water[x, y] = 0;
                }
            for (int y = surface; y <= Ground + 5 - Math.Min(5, edge); y++)
                if (Tiles[x, y] is Material.Air or Material.Cloud or Material.RainCloud or Material.Dirt or Material.Grass)
                    Tiles[x, y] = Material.Dirt;
        }
    }

    private void ApplyIslandCloudWalls(int minX, int maxX, int minY, int maxY)
    {
        for (int x = Math.Max(2, minX - 20); x <= Math.Min(Width - 3, maxX + 20); x++)
            for (int y = Math.Max(2, minY - 20); y <= Math.Min(Height - 3, maxY + 20); y++)
            {
                bool enclosed = true;
                for (int dx = -1; dx <= 1 && enclosed; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (Tiles[x + dx, y + dy] == Material.Air) { enclosed = false; break; }
                if (enclosed) Walls[x, y] = 3;
            }
    }

    private void CarveCloudWater(Random random, int minX, int maxX, int minY, int maxY)
    {
        for (int x = Math.Max(3, minX); x <= Math.Min(Width - 4, maxX); x++)
        {
            int top = FindTopCloud(x, Math.Max(3, minY - 10), Math.Min(Height - 4, maxY));
            if (top < 0) continue;
            if (random.Next(10) == 0)
            {
                int halfWidth = random.Next(1, 3);
                int depth = random.Next(1, 4);
                CarveCloudPool(x, top, halfWidth, depth);
            }
            else if (random.Next(5) == 0)
                CarveCloudPool(x, top, 0, 1);
        }
    }

    private bool CarveCloudPool(int centerX, int top, int halfWidth, int depth)
    {
        int left = centerX - halfWidth, right = centerX + halfWidth;
        if (left <= 2 || right >= Width - 3 || top <= 2 || top + depth >= Height - 3) return false;
        for (int x = left; x <= right; x++)
        {
            if (Tiles[x, top - 1] != Material.Air || Water[x, top - 1] != 0) return false;
            for (int y = top; y < top + depth; y++)
                if (!IsCloud(x, y)) return false;
        }
        for (int x = left; x <= right; x++)
            for (int y = top; y < top + depth; y++)
            {
                if (!CanHoldWater(x, y, left, right, top, depth)) continue;
                Tiles[x, y] = Material.Air;
                Walls[x, y] = 3;
                Water[x, y] = 255;
            }
        return true;
    }

    private bool CanHoldWater(int x, int y, int left, int right, int top, int depth)
    {
        bool SolidOrWater(int px, int py)
        {
            if (px >= left && px <= right && py >= top && py < top + depth) return true;
            return Tiles[px, py] != Material.Air || Water[px, py] > 0;
        }
        return SolidOrWater(x, y + 1) && SolidOrWater(x - 1, y) && SolidOrWater(x + 1, y);
    }

    private void GenerateFloatingClouds(Random random, int minX, int maxX, int minY)
    {
        int count = random.Next(1, 5);
        for (int cloud = 0; cloud < count; cloud++)
            for (int attempt = 0; attempt < 80; attempt++)
            {
                int radius = random.Next(4, 8);
                int x = random.Next(Math.Max(radius + 5, minX - 5), Math.Min(Width - radius - 5, maxX + 6));
                int y = minY - random.Next(20, 40);
                if (y - radius < 3) continue;
                bool clear = true;
            foreach (var tree in TreeSpots)
                if (Math.Abs(x - tree.X) < radius + 9 && y + radius >= tree.Y - 30) clear = false;
            for (int px = Math.Max(1, x - radius - 6); px <= Math.Min(Width - 2, x + radius + 6) && clear; px++)
                for (int py = Math.Max(1, y - radius - 6); py <= Math.Min(Height - 2, y + radius + 6); py++)
                    if (Tiles[px, py] is Material.Wood or Material.Leaves) { clear = false; break; }

                for (int px = x - radius - 3; px <= x + radius + 3 && clear; px++)
                    for (int py = y - radius - 3; py <= y + radius + 3; py++)
                        if (px < 2 || px >= Width - 2 || py < 2 || py >= Height - 2 || Tiles[px, py] != Material.Air || Walls[px, py] != 0)
                        { clear = false; break; }
                if (!clear) continue;
                Material material = random.Next(2) == 0 ? Material.Cloud : Material.RainCloud;
                PaintFloatingPatch(random, x, y, radius, material);
                FloatingCloudSpots.Add((x, y));
                for (int px = x - radius + 2; px <= x + radius - 2; px++)
                {
                    int top = FindTopCloud(px, y - radius, y + radius);
                    if (top >= 0 && CanHoldFloatingWater(px, top))
                    {
                        Tiles[px, top] = Material.Air;
                        Water[px, top] = 255;
                    }
                }
                break;
            }
    }

    private void PaintFloatingPatch(Random random, int centerX, int centerY, int radius, Material material)
    {
        for (int x = centerX - radius; x <= centerX + radius; x++)
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                if (x <= 2 || y <= 2 || x >= Width - 3 || y >= Height - 3) continue;
                double dx = Math.Abs(x - centerX);
                double dy = Math.Abs(y - centerY) * 2;
                if (Math.Sqrt(dx * dx + dy * dy) < radius + random.Next(-1, 2))
                    Tiles[x, y] = material;
            }
    }

    private bool CanHoldFloatingWater(int x, int y)
    {
        bool SolidOrWater(int px, int py) => Tiles[px, py] != Material.Air || Water[px, py] > 0;
        return SolidOrWater(x, y + 1) && SolidOrWater(x - 1, y) && SolidOrWater(x + 1, y);
    }

    private int FindBottomSolid(int x, int minY, int maxY)
    {
        for (int y = Math.Min(Height - 4, maxY + 8); y >= Math.Max(3, minY); y--)
            if (Tiles[x, y] != Material.Air) return y;
        return -1;
    }

    private int FindBottomMaterial(int x, int minY, int maxY, Material material)
    {
        for (int y = Math.Min(Height - 4, maxY + 8); y >= Math.Max(3, minY); y--)
            if (Tiles[x, y] == material) return y;
        return -1;
    }

    private int FindTopCloud(int x, int minY, int maxY)
    {
        for (int y = Math.Max(3, minY); y <= Math.Min(Height - 4, maxY); y++)
            if (IsCloud(x, y) && Tiles[x, y - 1] == Material.Air && Water[x, y - 1] == 0) return y;
        return -1;
    }

    private void RefreshGrass()
    {
        bool[,] sky = new bool[Width, Height];
        Queue<(int X, int Y)> air = new();
        for (int x = 1; x < Width - 1; x++) air.Enqueue((x, 1));
        while (air.TryDequeue(out var point))
        {
            int x = point.X, y = point.Y;
            if (x < 1 || y < 1 || x >= Width - 1 || y >= Height - 1 || sky[x, y] || Tiles[x, y] != Material.Air || Water[x, y] > 0) continue;
            sky[x, y] = true;
            air.Enqueue((x - 1, y));
            air.Enqueue((x + 1, y));
            air.Enqueue((x, y - 1));
            air.Enqueue((x, y + 1));
        }
        for (int x = 1; x < Width - 1; x++)
            for (int y = 1; y < Height - 1; y++)
                if (Tiles[x, y] is Material.Dirt or Material.Grass)
                {
                    bool exposed = false;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            exposed |= sky[x + dx, y + dy];
                    Tiles[x, y] = exposed ? Material.Grass : Material.Dirt;
                }
    }

    private bool IsCloud(int x, int y)
    {
        return x > 0 && y > 0 && x < Width - 1 && y < Height - 1 && (Tiles[x, y] is Material.Cloud or Material.RainCloud);
    }

    private void Chamber(int left, int top, int width, int height)
    {
        for (int x = left - 1; x <= left + width; x++)
            for (int y = top - 1; y <= top + height; y++)
            {
                Tiles[x, y] = x <= left || x >= left + width - 1 || y <= top || y >= top + height - 1 ? Material.Wood : Material.Air;
                Walls[x, y] = 1;
            }
    }

    private void Ellipse(int centerX, int centerY, int radiusX, int radiusY, Material material)
    {
        for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
            for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
                if (x > 0 && y > 0 && x < Width - 1 && y < Height - 1 &&
                    (material != Material.Leaves || Tiles[x, y] != Material.Wood) &&
                    Math.Pow((x - centerX) / (double)radiusX, 2) + Math.Pow((y - centerY) / (double)radiusY, 2) <= 1)
                    Tiles[x, y] = material;
    }

    private void Stroke(int x1, int y1, int x2, int y2, int radius)
    {
        int steps = Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
        for (int step = 0; step <= steps; step++)
        {
            int x = (int)Math.Round(x1 + (x2 - x1) * step / (double)Math.Max(1, steps));
            int y = (int)Math.Round(y1 + (y2 - y1) * step / (double)Math.Max(1, steps));
            bool foliage = Tiles[x, y] == Material.Leaves;
            int thickness = foliage ? 1 : radius + 1;
            for (int dy = 0; dy < thickness; dy++)
                if (x > 0 && x < Width - 1 && y + dy > 0 && y + dy < Height - 1)
                    Tiles[x, y + dy] = Material.Wood;
            if (step > 0)
            {
                int previousX = (int)Math.Round(x1 + (x2 - x1) * (step - 1) / (double)Math.Max(1, steps));
                int previousY = (int)Math.Round(y1 + (y2 - y1) * (step - 1) / (double)Math.Max(1, steps));
                if (previousX != x && previousY != y && !foliage) Tiles[x, previousY] = Material.Wood;
            }
        }
    }
}

#if !ISLAND_PREVIEW
public sealed class LivingTreeIslandPass : GenPass
{
    private readonly List<(LivingTreeIslandLayout Layout, int X, int Y)> pending = new();
    private static readonly ushort[] types = [0, TileID.Grass, TileID.Dirt, TileID.Stone, TileID.LivingWood, TileID.LeafBlock, TileID.Platforms, 0, TileID.Cloud, TileID.RainCloud];

    public LivingTreeIslandPass() : base("Living Tree Sky Islands", 30f) { }

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = WorldGenSystem.LivingTreeIslandsPassMessage.Value;
        pending.Clear();
        int count = Main.maxTilesX >= 8000 ? 3 : 2;
        int placed = 0;
        for (int island = 0; island < count; island++)
        {
            LivingTreeIslandLayout layout = new(WorldGen.genRand.Next());
            int segmentWidth = (Main.maxTilesX - 800) / count;
            int segmentStart = 400 + island * segmentWidth;
            bool generated = false;
            for (int attempt = 0; attempt < 1200; attempt++)
            {
                int x = WorldGen.genRand.Next(segmentStart, segmentStart + segmentWidth - LivingTreeIslandLayout.Width);
                if (!TryGetVanillaYRange(layout, x, out int minY, out int maxY)) continue;
                int y = WorldGen.genRand.Next(minY, maxY + 1);
                if (!TryPlace(layout, x, y)) continue;
                generated = true;
                break;
            }
            for (int x = 200; x < Main.maxTilesX - LivingTreeIslandLayout.Width - 200 && !generated; x += 16)
            {
                if (!TryGetVanillaYRange(layout, x, out int minY, out int maxY)) continue;
                for (int y = minY; y <= maxY; y += 4)
                    if (TryPlace(layout, x, y))
                    {
                        generated = true;
                        break;
                    }
            }
            if (generated) placed++;
            progress.Set((island + 1f) / count);
        }
        if (placed < count)
            ModContent.GetInstance<AerovelenceMod>().Logger.Warn($"Placed {placed}/{count} living tree sky islands; remaining sky space was occupied.");
    }

    private static bool TryGetVanillaYRange(LivingTreeIslandLayout layout, int x, out int minY, out int maxY)
    {
        int surfaceY = 0;
        int centerX = x + layout.IslandCenter;
        for (int y = 200; y < Main.worldSurface; y++)
            if (Main.tile[centerX, y].HasTile)
            {
                surfaceY = y;
                break;
            }
        minY = 90 - LivingTreeIslandLayout.IslandCenterY;
        maxY = Math.Min(surfaceY - 101, (int)GenVars.worldSurfaceLow - 50) - LivingTreeIslandLayout.IslandCenterY;
        return surfaceY > 0 && maxY >= minY;
    }

    private bool TryPlace(LivingTreeIslandLayout layout, int x, int y)
    {
        Rectangle clearance = new(x + layout.MinX, y + layout.MinY, layout.MaxX - layout.MinX + 1, layout.MaxY - layout.MinY + 1);
        clearance.Inflate(16, 12);
        if (!AeroGenUtils.CanPlaceInEmptyArea(clearance)) return false;
        Stamp(layout, x, y, false);
        pending.Add((layout, x, y));
        new AeroStructure(new Vector2(clearance.X, clearance.Y), clearance.Width, clearance.Height, "livingtreeisland").ProtectStructure();
        return true;
    }

    internal void Finish(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = WorldGenSystem.LivingTreeIslandsPassMessage.Value;
        for (int i = 0; i < pending.Count; i++)
        {
            var island = pending[i];
            Place(island.Layout, island.X, island.Y);
            progress.Set((i + 1f) / pending.Count);
        }
        pending.Clear();
    }

    private static void Stamp(LivingTreeIslandLayout layout, int left, int top, bool structures)
    {
        for (int x = 0; x < LivingTreeIslandLayout.Width; x++)
            for (int y = 0; y < LivingTreeIslandLayout.Height; y++)
            {
                if (structures && !layout.IsStructure(x, y)) continue;
                var material = structures ? layout.Tiles[x, y] : layout.TerrainTiles[x, y];
                byte wall = structures ? layout.Walls[x, y] : layout.TerrainWalls[x, y];
                byte water = structures ? layout.Water[x, y] : layout.TerrainWater[x, y];
                if (!structures && material == LivingTreeIslandLayout.Material.Air && wall == 0 && water == 0) continue;
                Tile tile = Main.tile[left + x, top + y];
                tile.ClearEverything();
                if (material is not (LivingTreeIslandLayout.Material.Air or LivingTreeIslandLayout.Material.Door))
                {
                    tile.HasTile = true;
                    tile.TileType = types[(int)material];
                }
                tile.WallType = wall == 1 ? WallID.LivingWood : wall == 2 ? WallID.DirtUnsafe : wall == 3 ? WallID.Cloud : WallID.None;
                tile.LiquidType = LiquidID.Water;
                tile.LiquidAmount = water;
            }
    }

    private static void Place(LivingTreeIslandLayout layout, int left, int top)
    {
        Stamp(layout, left, top, true);

        for (int x = 2; x < LivingTreeIslandLayout.Width - 2; x++)
            for (int y = 2; y < LivingTreeIslandLayout.Height - 2; y++)
            {
                if (layout.Tiles[x, y] is not (LivingTreeIslandLayout.Material.Grass or LivingTreeIslandLayout.Material.Dirt or
                    LivingTreeIslandLayout.Material.Cloud or LivingTreeIslandLayout.Material.RainCloud)) continue;
                bool anchored = false;
                foreach (var spot in layout.TreeSpots)
                    if (Math.Abs(x - spot.X) <= 2 && Math.Abs(y - spot.Y) <= 2) anchored = true;
                foreach (var spot in layout.SunflowerSpots)
                    if (x >= spot.X - 1 && x <= spot.X + 2 && Math.Abs(y - spot.Y) <= 1) anchored = true;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        anchored |= layout.Water[x + dx, y + dy] > 0 || layout.Tiles[x + dx, y + dy] is LivingTreeIslandLayout.Material.Door or LivingTreeIslandLayout.Material.Wood;
                if (!anchored) Tile.SmoothSlope(left + x, top + y, false, false);
            }

        int center = left + layout.Center;
        int roomFloor = top + layout.RoomFloor;
        int cellarFloor = top + layout.CellarFloor;
        int cellarCenter = left + layout.CellarCenter;
        WorldGen.PlaceTile(center - LivingTreeIslandLayout.DoorOffset, roomFloor - 2, TileID.ClosedDoor, mute: true);
        WorldGen.PlaceTile(center + LivingTreeIslandLayout.DoorOffset, roomFloor - 2, TileID.ClosedDoor, mute: true);
        int cellarDoorX = cellarCenter + Math.Sign(center - cellarCenter) * 10;
        WorldGen.PlaceTile(cellarDoorX, cellarFloor - 2, TileID.ClosedDoor, mute: true);
        WorldGen.PlaceTile(cellarCenter - 6, cellarFloor - 1, TileID.Tables, mute: true);
        WorldGen.PlaceTile(cellarCenter - 6, cellarFloor - 3, TileID.Candles, mute: true);
        WorldGen.PlaceTile(cellarCenter - 1, cellarFloor - 1, TileID.LivingLoom, mute: true);
        WorldGen.PlaceTile(cellarCenter + 8, cellarFloor - 1, TileID.Pots, mute: true);
        WorldGen.PlaceTile(cellarCenter + 6, cellarFloor - 6, TileID.Torches, mute: true);

        int chestX = left + layout.ChestX;
        int chestY = top + layout.ChestY;
        if (WorldGen.AddBuriedChest(chestX, chestY, ItemID.LivingWoodWand, false, 12))
        {
            foreach (Chest chest in Main.chest)
            {
                if (chest == null || chest.x < cellarCenter + 3 || chest.x > cellarCenter + 7 ||
                    chest.y < cellarFloor - 3 || chest.y >= cellarFloor) continue;
                for (int slot = chest.item.Length - 1; slot > 0; slot--)
                    chest.item[slot] = chest.item[slot - 1];
                chest.item[0] = new Item(ModContent.ItemType<ShotgunAxe>());
                break;
            }
        }

        AeroGenUtils.FrameArea(new Rectangle(left + 1, top + 1, LivingTreeIslandLayout.Width - 2, LivingTreeIslandLayout.Height - 2));
        foreach (var spot in layout.TreeSpots)
        {
            int x = left + spot.X, y = top + spot.Y - 1;
            WorldGen.PlaceTile(x, y, TileID.Saplings, mute: true);
            WorldGen.GrowTree(x, y);
        }
        foreach (var spot in layout.SunflowerSpots)
            WorldGen.PlaceSunflower(left + spot.X, top + spot.Y - 1);
        for (int x = 1; x < LivingTreeIslandLayout.Width - 1; x++)
            for (int y = 4; y < layout.Ground + 12; y++)
            {
                int worldX = left + x;
                int worldY = top + y;
                if (layout.Tiles[x, y] == LivingTreeIslandLayout.Material.Grass && Main.tile[worldX, worldY].HasTile &&
                    Main.tile[worldX, worldY].TileType == TileID.Grass && !Main.tile[worldX, worldY - 1].HasTile &&
                    Main.tile[worldX, worldY - 1].LiquidAmount == 0 && !WorldGen.genRand.NextBool(5))
                    WorldGen.PlaceTile(worldX, worldY - 1, WorldGen.genRand.NextBool(3) ? TileID.Plants2 : TileID.Plants,
                        mute: true, style: WorldGen.genRand.Next(6));
            }
    }
}
#endif
