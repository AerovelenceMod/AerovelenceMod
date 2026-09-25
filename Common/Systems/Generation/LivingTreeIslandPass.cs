using System;
using System.Collections.Generic;
#if !ISLAND_PREVIEW
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
    internal const int Width = 144;
    internal const int Height = 128;
    internal readonly Material[,] Tiles = new Material[Width, Height];
    internal readonly byte[,] Walls = new byte[Width, Height];
    internal readonly byte[,] Water = new byte[Width, Height];
    internal readonly int Center;
    internal readonly int CellarCenter;
    internal readonly List<(int X, int Y)> TreeSpots = new();
    internal readonly List<(int X, int Y)> OreSpots = new();
    internal int MinX { get; private set; } = Width;
    internal int MinY { get; private set; } = Height;
    internal int MaxX { get; private set; }
    internal int MaxY { get; private set; }
    internal const int Ground = 77;
    internal const int RoomFloor = Ground - 1;
    internal const int CellarFloor = Ground + 13;
    internal int ChestX => CellarCenter + 5;
    internal int ChestY => CellarFloor - 1;

    internal LivingTreeIslandLayout(int seed)
    {
        Random random = new(seed);
        Center = Width / 2 + random.Next(-4, 5);
        int cellarSide = random.Next(2) == 0 ? -1 : 1;
        CellarCenter = Center + cellarSide * 18;
        int radius = random.Next(44, 49);
        int depth = random.Next(14, 17);
        int crown = Ground - random.Next(30, 35);
        double phase = random.NextDouble() * Math.PI * 2;
        Ellipse(Center, Ground + 12, radius + 11, 15, Material.Cloud);
        for (int puff = -3; puff <= 3; puff++)
        {
            int puffX = Center + (int)(puff * (radius + 2) / 3.0);
            Ellipse(puffX, Ground + 13 + random.Next(-2, 3), random.Next(7, 10), random.Next(7, 10), Material.Cloud);
        }
        for (int puff = -2; puff <= 2; puff++)
            Ellipse(Center + (int)(puff * radius * .45), Ground + 23 + random.Next(-2, 3), random.Next(9, 12), random.Next(5, 8), Material.Cloud);
        for (int x = 1; x < Width - 1; x++)
            for (int y = 1; y < Ground + 3 + (int)(Math.Sin(x * .13 + phase) * 1.5); y++)
                if (Tiles[x, y] == Material.Cloud) Tiles[x, y] = Material.Air;
        for (int x = Center - radius; x <= Center + radius; x++)
        {
            double distance = Math.Abs(x - Center) / (double)radius;
            double taperStart = .6 + Math.Sin(phase + (x < Center ? 0 : 2)) * .06;
            double taper = Math.Clamp((distance - taperStart) / (1 - taperStart), 0, 1);
            taper *= taper * (3 - 2 * taper);
            int top = Ground + (int)(taper * 5 + Math.Sin(x * .17 + phase) * 1.5);
            if (Math.Abs(x - Center) <= 13) top = Ground;
            int bottom = top + 2 + (int)((1 - taper) * depth) +
                (int)(Math.Sin(x * .13 + phase) * 1.5);
            if (Math.Abs(x - CellarCenter) <= 19)
                bottom = Math.Max(bottom, CellarFloor + 3 - (int)(Math.Max(0, Math.Abs(x - CellarCenter) - 11) * 1.5));
            for (int y = top; y <= bottom; y++)
            {
                Tiles[x, y] = y == top ? Material.Grass : Material.Dirt;
                if (y > top + 1 && y < bottom - 1) Walls[x, y] = 2;
            }
        }

        for (int pocket = 0; pocket < 7; pocket++)
        {
            int pocketX = 0, pocketY = 0;
            for (int attempt = 0; attempt < 80; attempt++)
            {
                pocketX = random.Next(Center - radius - 6, Center + radius + 7);
                pocketY = random.Next(Ground + 6, Ground + 26);
                if (Tiles[pocketX, pocketY] == Material.Cloud) break;
            }
            for (int lobe = 0; lobe < 3; lobe++)
            {
                int centerX = pocketX + random.Next(-3, 4);
                int centerY = pocketY + random.Next(-2, 3);
                int width = random.Next(4, 8), height = random.Next(3, 6);
                for (int x = Math.Max(2, centerX - width); x <= Math.Min(Width - 3, centerX + width); x++)
                    for (int y = centerY - height; y <= Math.Min(Height - 3, centerY + height); y++)
                        if (Tiles[x, y] == Material.Cloud && IsCloud(x - 1, y) && IsCloud(x + 1, y) &&
                            IsCloud(x, y - 1) && IsCloud(x, y + 1) &&
                            Math.Pow((x - centerX) / (double)width, 2) + Math.Pow((y - centerY) / (double)height, 2) <= 1)
                            Tiles[x, y] = Material.RainCloud;
            }
        }

        Ellipse(Center, crown + 7, 18, 9, Material.Leaves);
        for (int lobe = -3; lobe <= 3; lobe++)
            Ellipse(Center + lobe * 5 + random.Next(-1, 2), crown + Math.Abs(lobe) * 2 + random.Next(-2, 3),
                random.Next(6, 9), random.Next(6, 9), Material.Leaves);
        for (int y = crown + 5; y <= Ground + 5; y++)
        {
            int bend = (int)(Math.Sin(y * .11 + phase) * 2);
            int halfWidth = y > Ground - 15 ? 6 : y > crown + 19 ? 4 : 2;
            for (int x = Center + bend - halfWidth; x <= Center + bend + halfWidth; x++)
            {
                Tiles[x, y] = Material.Wood;
                Walls[x, y] = 1;
            }
        }
        for (int side = -1; side <= 1; side += 2)
        {
            int branchY = crown + random.Next(11, 16);
            Stroke(Center, branchY + 6, Center + side * 10, branchY, 2);
            Stroke(Center + side * 10, branchY, Center + side * random.Next(17, 22), crown + random.Next(4, 9), 1);
        }
        int rootCount = random.Next(3, 6);
        for (int root = 0; root < rootCount; root++)
        {
            int startX = Center + random.Next(-5, 6);
            int endX = Center + random.Next(-22, 23);
            int endY = Ground + random.Next(6, 11);
            int bendX = (startX + endX) / 2 + random.Next(-3, 4);
            int bendY = Ground + random.Next(4, 8);
            Stroke(startX, Ground + 2, bendX, bendY, 2);
            Stroke(bendX, bendY, endX, endY, 1);
        }

        Atrium(random);
        Chamber(CellarCenter - 10, CellarFloor - 8, 21, 9);
        int tunnelEnd = CellarCenter - cellarSide * 10;
        for (int x = Math.Min(Center - 2, tunnelEnd); x <= Math.Max(Center + 2, tunnelEnd); x++)
            for (int y = CellarFloor - 6; y <= CellarFloor; y++)
            {
                Tiles[x, y] = y == CellarFloor - 6 || y == CellarFloor ? Material.Wood : Material.Air;
                Walls[x, y] = 1;
            }
        int exitX = Math.Clamp(Center + cellarSide * (radius + 14), 2, Width - 3);
        int exitLength = Math.Abs(exitX - CellarCenter);
        int deckY = CellarFloor;
        int nextStep = 11 + random.Next(4, 7);
        for (int offset = 11; offset <= exitLength; offset++)
        {
            if (offset == nextStep)
            {
                deckY++;
                nextStep += random.Next(4, 7);
            }
            double exitProgress = (offset - 11.0) / (exitLength - 11);
            int ceilingY = Math.Min(deckY - 5, CellarFloor - 5 + (int)(Math.Sin(exitProgress * Math.PI * 1.5) * 5));
            for (int y = ceilingY; y <= deckY; y++)
            {
                int x = CellarCenter + cellarSide * offset;
                bool outside = Math.Abs(x - Center) > radius - 3;
                if (y < deckY) Tiles[x, y] = Material.Air;
                Walls[x, y] = outside ? (byte)0 : (byte)2;
            }
        }
        for (int side = -1; side <= 1; side += 2)
        {
            int x = CellarCenter + side * 10;
            for (int y = CellarFloor - 5; y <= CellarFloor; y++)
            {
                Tiles[x, y] = y >= CellarFloor - 3 && y < CellarFloor ? Material.Door : Material.Wood;
                if (y >= CellarFloor - 3 && y < CellarFloor) Tiles[x + side, y] = Material.Air;
            }
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
                Tiles[Center + side * 7, y] = Material.Door;
                Tiles[Center + side * 6, y] = Material.Air;
            }
        }
        for (int x = 1; x < Width - 1; x++)
            for (int y = 1; y < Height - 1; y++)
                if (Tiles[x, y] == Material.Leaves && Tiles[x, y - 1] == Material.Air && random.Next(6) == 0)
                    Tiles[x, y] = Material.Air;

        for (int side = -1; side <= 1; side += 2)
        {
            int spacing = random.Next(8, 10);
            for (int distance = 25 + random.Next(3); distance <= radius - 5; distance++)
            {
                int x = Center + side * distance;
                int ground = Ground - 2;
                while (ground < Ground + 10 && Tiles[x, ground] != Material.Grass) ground++;
                if (ground >= Ground + 10) continue;
                bool clear = true;
                for (int column = x - 3; column <= x + 3; column++)
                    for (int y = ground - 22; y < ground - 2; y++)
                        if (Tiles[column, y] != Material.Air && (Math.Abs(column - x) <= 2 || y < ground - 5)) clear = false;
                if (!clear) continue;
                for (int column = x - 2; column <= x + 2; column++)
                    for (int y = ground - 2; y <= ground + 2; y++)
                    {
                        Tiles[column, y] = y < ground ? Material.Air : y == ground ? Material.Grass : Material.Dirt;
                        if (y <= ground) Walls[column, y] = 0;
                    }
                TreeSpots.Add((x, ground));
                distance += spacing - 1;
            }
        }

        int orePatches = random.Next(1, 3);
        for (int patch = 0; patch < orePatches; patch++)
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int x = Center + (patch % 2 == 0 ? -1 : 1) * random.Next(17, radius - 6);
                int y = Ground + random.Next(3, 10);
                if (Tiles[x, y] != Material.Dirt || Tiles[x - 2, y] != Material.Dirt || Tiles[x + 2, y] != Material.Dirt) continue;
                OreSpots.Add((x, y));
                break;
            }

        for (int side = -1; side <= 1; side += 2)
        {
            int ponds = random.Next(1, 3);
            for (int pond = 0; pond < ponds; pond++)
                for (int attempt = 0; attempt < 120; attempt++)
                {
                    bool cloud = pond > 0;
                    int x = Center + side * (cloud ? random.Next(radius - 1, radius + 8) : random.Next(15, radius - 5));
                    bool nearTree = false;
                    foreach (var spot in TreeSpots)
                        if (Math.Abs(spot.X - x) <= 3) nearTree = true;
                    if (nearTree) continue;
                    int y = Ground - 2;
                    while (y < Ground + 25 && Tiles[x, y] == Material.Air) y++;
                    if (y >= Ground + 25 || (cloud ? !IsCloud(x, y) : Tiles[x, y] != Material.Grass)) continue;
                    if (Basin(x, y, random.Next(1, 3))) break;
                }
        }

        int floatingClouds = random.Next(1, 3);
        int firstSide = random.Next(2) == 0 ? -1 : 1;
        for (int cloud = 0; cloud < floatingClouds; cloud++)
        {
            int cloudX = Center + firstSide * (cloud == 0 ? 1 : -1) * random.Next(32, 41);
            int cloudY = random.Next(18, 25);
            int halfWidth = random.Next(7, 10);
            int cloudHeight = random.Next(4, 7);
            Material material = random.Next(2) == 0 ? Material.Cloud : Material.RainCloud;
            for (int x = cloudX - halfWidth; x <= cloudX + halfWidth; x++)
            {
                int inset = Math.Abs(x - cloudX) >= halfWidth - 1 ? 1 : 0;
                int top = cloudY + inset + random.Next(2);
                int bottom = cloudY + cloudHeight - inset + random.Next(2);
                for (int y = top; y <= bottom; y++) Tiles[x, y] = material;
            }
            for (int x = cloudX - halfWidth + 2; x < cloudX + halfWidth - 1; x++)
            {
                int y = cloudY;
                while (Tiles[x, y] == Material.Air) y++;
                if (Basin(x, y, 2) || Basin(x, y, 1)) break;
            }
        }

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
                if (IsCloud(x, y))
                {
                    bool interior = true;
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            if (!IsCloud(x + dx, y + dy) && Water[x + dx, y + dy] == 0) interior = false;
                    Walls[x, y] = interior ? (byte)3 : (byte)0;
                }
                if (Water[x, y] > 0) Walls[x, y] = 3;
                if (material == Material.Air && Walls[x, y] == 0 && Water[x, y] == 0) continue;
                MinX = Math.Min(MinX, x);
                MinY = Math.Min(MinY, y);
                MaxX = Math.Max(MaxX, x);
                MaxY = Math.Max(MaxY, y);
            }

        bool IsCloud(int x, int y) => Tiles[x, y] is Material.Cloud or Material.RainCloud;
    }

    private void Atrium(Random random)
    {
        List<(int X, int Y)> interior = new();
        int top = RoomFloor - random.Next(9, 12);
        int bend = random.Next(2) == 0 ? -1 : 1;
        for (int y = top; y < RoomFloor; y++)
        {
            double height = (y - top) / (double)(RoomFloor - top - 1);
            int halfWidth = y >= RoomFloor - 3 ? 5 : 3 + (int)(Math.Sin(height * Math.PI * .7) * 2);
            int offset = y >= RoomFloor - 3 ? 0 : (int)Math.Round(Math.Sin(height * Math.PI) * bend);
            for (int x = Center + offset - halfWidth; x <= Center + offset + halfWidth; x++) interior.Add((x, y));
        }
        foreach (var cell in interior)
            for (int dx = -2; dx <= 2; dx++)
                for (int dy = -2; dy <= 2; dy++)
                {
                    Tiles[cell.X + dx, cell.Y + dy] = Material.Wood;
                    Walls[cell.X + dx, cell.Y + dy] = 1;
                }
        foreach (var cell in interior) Tiles[cell.X, cell.Y] = Material.Air;
    }

    private bool Basin(int left, int top, int width)
    {
        bool Solid(int x, int y) => Tiles[x, y] is Material.Grass or Material.Dirt or Material.Cloud or Material.RainCloud;
        if (!Solid(left - 1, top) || !Solid(left + width, top)) return false;
        for (int x = left; x < left + width; x++)
            if (!Solid(x, top) || !Solid(x, top + 1) || Tiles[x, top - 1] != Material.Air || Water[x, top - 1] != 0) return false;
        for (int x = left; x < left + width; x++)
        {
            Tiles[x, top] = Material.Air;
            Walls[x, top] = 0;
            Water[x, top] = 200;
        }
        return true;
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
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                    if (x + dx > 0 && x + dx < Width - 1 && y + dy > 0 && y + dy < Height - 1)
                        Tiles[x + dx, y + dy] = Material.Wood;
        }
    }
}

#if !ISLAND_PREVIEW
public sealed class LivingTreeIslandPass : GenPass
{
    public LivingTreeIslandPass() : base("Living Tree Sky Islands", 30f) { }

    protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
    {
        progress.Message = WorldGenSystem.LivingTreeIslandsPassMessage.Value;
        int count = Main.maxTilesX >= 8000 ? 3 : 2;
        int placed = 0;
        for (int island = 0; island < count; island++)
        {
            LivingTreeIslandLayout layout = new(WorldGen.genRand.Next());
            int segmentWidth = (Main.maxTilesX - 800) / count;
            int segmentStart = 400 + island * segmentWidth;
            int minY = Math.Max(0, 24 - layout.MinY);
            int maxY = Math.Max(minY, (int)Main.worldSurface - layout.MaxY - 24);
            bool generated = false;
            for (int attempt = 0; attempt < 1200; attempt++)
            {
                int x = WorldGen.genRand.Next(segmentStart, segmentStart + segmentWidth - LivingTreeIslandLayout.Width);
                int y = WorldGen.genRand.Next(minY, maxY + 1);
                if (!TryPlace(layout, x, y)) continue;
                generated = true;
                break;
            }
            for (int y = minY; y <= maxY && !generated; y += 4)
                for (int x = 200; x < Main.maxTilesX - LivingTreeIslandLayout.Width - 200; x += 16)
                    if (TryPlace(layout, x, y))
                    {
                        generated = true;
                        break;
                    }
            if (generated) placed++;
            progress.Set((island + 1f) / count);
        }
        if (placed < count)
            ModContent.GetInstance<AerovelenceMod>().Logger.Warn($"Placed {placed}/{count} living tree sky islands; remaining sky space was occupied.");
    }

    private static bool TryPlace(LivingTreeIslandLayout layout, int x, int y)
    {
        Rectangle clearance = new(x + layout.MinX, y + layout.MinY, layout.MaxX - layout.MinX + 1, layout.MaxY - layout.MinY + 1);
        clearance.Inflate(16, 12);
        if (!CanPlace(clearance)) return false;
        Place(layout, x, y);
        new AeroStructure(new Vector2(clearance.X, clearance.Y), clearance.Width, clearance.Height, "livingtreeisland").ProtectStructure();
        return true;
    }

    private static bool CanPlace(Rectangle bounds)
    {
        if (!WorldGen.InWorld(bounds.Left, bounds.Top, 10) || !WorldGen.InWorld(bounds.Right, bounds.Bottom, 10) ||
            !GenVars.structures.CanPlace(bounds)) return false;
        foreach (Rectangle protectedArea in AeroStructure.ProtectedStructures)
            if (protectedArea.Intersects(bounds)) return false;
        for (int x = bounds.Left; x < bounds.Right; x++)
            for (int y = bounds.Top; y < bounds.Bottom; y++)
            {
                Tile tile = Main.tile[x, y];
                if (tile.HasTile || tile.WallType != WallID.None || tile.LiquidAmount > 0) return false;
            }
        return true;
    }

    private static void Place(LivingTreeIslandLayout layout, int left, int top)
    {
        ushort[] types = [0, TileID.Grass, TileID.Dirt, TileID.Stone, TileID.LivingWood, TileID.LeafBlock, TileID.Platforms, 0, TileID.Cloud, TileID.RainCloud];
        for (int x = 0; x < LivingTreeIslandLayout.Width; x++)
            for (int y = 0; y < LivingTreeIslandLayout.Height; y++)
            {
                Tile tile = Main.tile[left + x, top + y];
                LivingTreeIslandLayout.Material material = layout.Tiles[x, y];
                if (material == LivingTreeIslandLayout.Material.Air && layout.Walls[x, y] == 0 && layout.Water[x, y] == 0) continue;
                if (material != LivingTreeIslandLayout.Material.Air && material != LivingTreeIslandLayout.Material.Door)
                {
                    tile.HasTile = true;
                    tile.TileType = types[(int)material];
                }
                tile.WallType = layout.Walls[x, y] == 1 ? WallID.LivingWood : layout.Walls[x, y] == 2 ? WallID.DirtUnsafe : layout.Walls[x, y] == 3 ? WallID.Cloud : WallID.None;
                if (layout.Water[x, y] > 0)
                {
                    tile.LiquidType = LiquidID.Water;
                    tile.LiquidAmount = layout.Water[x, y];
                }
            }

        foreach (var spot in layout.OreSpots)
            WorldGen.OreRunner(left + spot.X, top + spot.Y, WorldGen.genRand.Next(6, 9), WorldGen.genRand.Next(5, 9),
                (ushort)WorldGen.SavedOreTiers.Gold);

        int center = left + layout.Center;
        int roomFloor = top + LivingTreeIslandLayout.RoomFloor;
        int cellarFloor = top + LivingTreeIslandLayout.CellarFloor;
        int cellarCenter = left + layout.CellarCenter;
        WorldGen.PlaceTile(center - 7, roomFloor - 2, TileID.ClosedDoor, mute: true);
        WorldGen.PlaceTile(center + 7, roomFloor - 2, TileID.ClosedDoor, mute: true);
        WorldGen.PlaceTile(cellarCenter - 10, cellarFloor - 2, TileID.ClosedDoor, mute: true);
        WorldGen.PlaceTile(cellarCenter + 10, cellarFloor - 2, TileID.ClosedDoor, mute: true);
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

        for (int x = 1; x < LivingTreeIslandLayout.Width - 1; x++)
            for (int y = 1; y < LivingTreeIslandLayout.Height - 1; y++)
            {
                int worldX = left + x;
                int worldY = top + y;
                WorldGen.SquareTileFrame(worldX, worldY);
                WorldGen.SquareWallFrame(worldX, worldY);
            }
        foreach (var spot in layout.TreeSpots)
        {
            int x = left + spot.X, y = top + spot.Y - 1;
            WorldGen.PlaceTile(x, y, TileID.Saplings, mute: true);
            WorldGen.GrowTree(x, y);
        }
        for (int x = 1; x < LivingTreeIslandLayout.Width - 1; x++)
            for (int y = LivingTreeIslandLayout.Ground - 3; y < LivingTreeIslandLayout.Ground + 12; y++)
            {
                int worldX = left + x;
                int worldY = top + y;
                if (layout.Tiles[x, y] == LivingTreeIslandLayout.Material.Grass &&
                    !Main.tile[worldX, worldY - 1].HasTile && Main.tile[worldX, worldY - 1].LiquidAmount == 0 && !WorldGen.genRand.NextBool(5))
                    WorldGen.PlaceTile(worldX, worldY - 1, WorldGen.genRand.NextBool(3) ? TileID.Plants2 : TileID.Plants,
                        mute: true, style: WorldGen.genRand.Next(6));
            }
    }
}
#endif
