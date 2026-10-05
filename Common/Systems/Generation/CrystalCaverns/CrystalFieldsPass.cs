using System;
using System.Collections.Generic;
using System.Linq;
using AerovelenceMod.Common.Systems.Traversal;
using AerovelenceMod.Common.Utilities.Generation.StructureStamper;
using AerovelenceMod.Content.Tiles.Traversal;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora;



using Terraria.IO;

using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns
{
    public sealed class CrystalFieldsPass : GenPass
    {
        private readonly List<Rectangle> features = new();
        private UnifiedRandom random;
        private CCTerrainPass caverns;
        private int fieldBridges, caveBridges, caveZiplines;
        private readonly bool crossingsOnly;
        private bool[] crossingTiles;

        public CrystalFieldsPass(bool crossingsOnly = false) : base(crossingsOnly ? "Cavern Crossings" : "Crystal Fields", crossingsOnly ? 30 : 10)
        {
            this.crossingsOnly = crossingsOnly;
        }

        protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
        {
            progress.Message = (crossingsOnly ? WorldGenSystem.CavernCrossingsPassMessage : WorldGenSystem.CrystalFieldsPassMessage).Value;
            caverns = CCTerrainPass.Instance();
            random = new UnifiedRandom(unchecked(Main.ActiveWorldFileData.Seed * 397 ^ 721903));
            features.Clear();
            fieldBridges = caveBridges = caveZiplines = 0;
            if (!crossingsOnly && caverns.Origin != Point.Zero && caverns.TotalSurface != null)
            {
                GenerateFields();
                DecorateFields();
            }
            if (crossingsOnly) GenerateCavernCrossings(progress);
            ModContent.GetInstance<global::AerovelenceMod.AerovelenceMod>().Logger.Info(crossingsOnly ? $"Cavern crossings: {caveBridges} bridges, {caveZiplines} ziplines" : $"Crystal Fields: {fieldBridges} lake bridges");
            progress.Set(1);
        }

        private int Surface(int x)
        {
            for (int y = Math.Max(20, caverns.Origin.Y - caverns.SurfaceHeight); y < caverns.Origin.Y; y++)
            {
                Tile tile = Main.tile[x, y];
                if (!tile.HasTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) continue;
                return FieldGround(tile.TileType) ? y : -1;
            }
            return -1;
        }

        private bool Free(Rectangle area, bool field)
        {
            if (!WorldGen.InWorld(area.Left, area.Top, 20) || !WorldGen.InWorld(area.Right, area.Bottom, 20)
                || features.Any(r => r.Intersects(area)) || AeroStructure.ProtectedStructures.Any(r => r.Intersects(area))) return false;
            if (!field)
            {
                crossingTiles ??= Enumerable.Repeat(true, TileLoader.TileCount).ToArray();
                if (!GenVars.structures.CanPlace(area, crossingTiles)) return false;
            }
            for (int x = area.Left; x < area.Right; x++)
            {
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (tile.HasTile && (field && (TileID.Sets.IsATreeTrunk[tile.TileType] || Main.tileFrameImportant[tile.TileType] && !Main.tileCut[tile.TileType]) || TileID.Sets.BasicChest[tile.TileType])) return false;
                    if (tile.WallType != WallID.None && Main.wallHouse[tile.WallType] && !(field && tile.WallType == caverns.GrassWall)
                        || tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Lava) return false;
                    if (field && caverns.LightningCave.Contains(x - caverns.Origin.X, y - caverns.Origin.Y + caverns.SurfaceHeight)) return false;
                }
            }
            return true;
        }

        private void GenerateFields()
        {
            int target = random.Next(1, 4);
            int left = caverns.Origin.X - caverns.BiomeWidth / 2 + 12;
            int right = caverns.Origin.X + caverns.BiomeWidth / 2 - 12;
            List<int> candidates = Enumerable.Range(left, Math.Max(0, right - left - 16)).ToList();
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
            foreach (int x in candidates)
            {
                if (fieldBridges >= target) break;
                int width = random.Next(16, 49);
                if (x + width >= right) continue;
                int y1 = Surface(x), y2 = Surface(x + width);
                if (y1 < 0 || y2 < 0 || Math.Abs(y1 - y2) > 3) continue;
                int top = Math.Min(y1, y2), bottom = Math.Max(y1, y2);
                Rectangle area = new(x - 3, top - 7, width + 7, bottom - top + 25);
                if (!Free(area, true)) continue;
                bool valid = true;
                for (int column = x; column <= x + width && valid; column++)
                {
                    int ground = Surface(column);
                    if (ground < top - 1 || ground > bottom + 1) { valid = false; break; }
                    for (int y = top - 5; y <= bottom + 18; y++)
                    {
                        Tile tile = Main.tile[column, y];
                        if (tile.HasTile && !FieldGround(tile.TileType) && !Main.tileCut[tile.TileType])
                        { valid = false; break; }
                    }
                }
                if (!valid || !CanPlacePost(x, y1 - 1, false) || !CanPlacePost(x + width, y2 - 1, false)) continue;
                if (!BuildLake(x, width, y1, y2)) continue;
                PlacePost(new Point(x, y1 - 1), false);
                PlacePost(new Point(x + width, y2 - 1), false);
                if (!RopeSpanSystem.Create(new Point(x, y1 - 1), new Point(x + width, y2 - 1), false))
                    throw new InvalidOperationException("Validated Crystal Fields crossing could not be placed.");
                features.Add(area);
                Protect(area, "crystalfieldlake");
                LushReservoirGenerator.RegisterSurfaceLake(area);
                fieldBridges++;
            }
        }

        private bool FieldGround(int type) => type == caverns.GrassTile || type == caverns.DirtTile || type == caverns.StoneTile || type == caverns.SandTile;

        private bool BuildLake(int left, int width, int y1, int y2)
        {
            int waterline = Math.Max(y1, y2) + 2;
            int depth = random.Next(4, Math.Min(11, width / 3 + 2));
            int textureSeed = random.Next();
            float center = width * random.NextFloat(.34f, .66f);
            float leftCurve = random.NextFloat(1.6f, 3.2f), rightCurve = random.NextFloat(1.6f, 3.2f);
            int[] beds = new int[width + 1];
            beds[0] = y1;
            beds[width] = y2;
            for (int column = 1; column < width; column++)
            {
                float radius = column < center ? center - 1 : width - 1 - center;
                float distance = (column - center) / radius;
                float curve = Math.Max(0, 1 - MathF.Pow(Math.Abs(distance), column < center ? leftCurve : rightCurve));
                float bank = MathHelper.Lerp(y1, y2, column / (float)width) + 1;
                float noise = (float)LushReservoirGenerator.ValueNoise(left + column, waterline, textureSeed, .1) - .5f;
                beds[column] = (int)MathF.Round(bank + (waterline + depth - bank + noise * 4) * curve);
            }
            for (int column = 0; column <= width; column++)
            {
                int bed = beds[column];
                int neighbor = Math.Max(beds[Math.Max(0, column - 1)], beds[Math.Min(width, column + 1)]);
                for (int y = bed; y <= Math.Max(bed + 2, neighbor + 1); y++)
                    if (!WorldGen.SolidOrSlopedTile(Main.tile[left + column, y]) || !FieldGround(Main.tile[left + column, y].TileType)) return false;
            }
            for (int column = 1; column < width; column++)
            {
                int x = left + column, bed = beds[column];
                for (int y = Math.Min(y1, y2) - 4; y < bed; y++)
                {
                    Tile tile = Main.tile[x, y];
                    tile.ClearTile();
                    tile.WallType = WallID.None;
                    tile.WallColor = PaintID.None;
                    tile.LiquidType = LiquidID.Water;
                    tile.LiquidAmount = (byte)(y >= waterline ? 255 : 0);
                }
                int sediment = 1 + (LushReservoirGenerator.ValueNoise(x, bed, textureSeed + 719, .2) > .6 ? 1 : 0);
                if (bed > waterline)
                {
                    for (int y = bed; y < bed + sediment; y++) Fill(x, y, caverns.SandTile);
                }
                else
                {
                    Fill(x, bed, caverns.GrassTile);
                }
            }
            int lowestBed = beds.Max();
            for (int x = left; x <= left + width; x++)
            {
                for (int y = waterline + 1; y <= lowestBed + 1; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile || !FieldGround(tile.TileType)) continue;
                    if (Main.tile[x - 1, y].LiquidAmount > 0 || Main.tile[x + 1, y].LiquidAmount > 0
                        || Main.tile[x, y - 1].LiquidAmount > 0) tile.TileType = caverns.SandTile;
                }
            }
            for (int column = 1; column < width; column++)
            {
                int x = left + column, bed = beds[column];
                if (bed > waterline) Tile.SmoothSlope(x, bed, false, false);
                if (bed >= waterline + 3 && column % 3 == 0 && random.NextBool(2))
                {
                    int length = random.Next(3, 7);
                    if (random.NextBool(3))
                    {
                        if (LuminVines.Plant(x, bed - 1, length))
                            for (int y = Math.Max(waterline, bed - length); y < bed; y++)
                                Main.tile[x, y].LiquidAmount = 255;
                    }
                    else Electrakelp.Plant(x, bed - 1, length);
                }
            }
            Frame(new Rectangle(left - 1, Math.Min(y1, y2) - 5, width + 3,
                lowestBed + 4 - Math.Min(y1, y2) + 5));
            return true;
        }

        private static void Fill(int x, int y, int type)
        {
            Tile tile = Main.tile[x, y];
            tile.ClearTile();
            tile.HasTile = true;
            tile.TileType = (ushort)type;
            tile.LiquidAmount = 0;
        }

        private void DecorateFields()
        {
            int left = caverns.Origin.X - caverns.BiomeWidth / 2 + 10;
            int right = caverns.Origin.X + caverns.BiomeWidth / 2 - 10;
            for (int x = left + random.Next(8, 20); x < right - 8; x += random.Next(32, 58))
            {
                int y = Surface(x);
                if (y < 0 || Main.tile[x, y].TileType == caverns.StoneTile) continue;
                int radius = random.Next(2, 4), height = random.Next(2, 4);
                Rectangle area = new(x - radius - 1, y - height - 2, radius * 2 + 3, height + 5);
                if (!Free(area, true)) continue;
                int[] ground = new int[radius * 2 + 1];
                bool clear = true;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    ground[dx + radius] = Surface(x + dx);
                    if (Math.Abs(ground[dx + radius] - y) > 1) clear = false;
                }
                if (!clear) continue;
                float peak = random.NextFloat(-.8f, .8f);
                int seed = random.Next();
                for (int dx = -radius; dx <= radius; dx++)
                {
                    float distance = (dx - peak) / (radius + .5f);
                    float weathering = (float)LushReservoirGenerator.ValueNoise(x + dx, y, seed, .5) - .5f;
                    int cap = Math.Max(0, (int)MathF.Round(height * (1 - distance * distance) + weathering));
                    for (int dy = 0; dy <= cap; dy++)
                        Fill(x + dx, ground[dx + radius] - dy, caverns.StoneTile);
                }
                for (int dx = -radius; dx <= radius; dx++)
                    for (int dy = 0; dy <= height; dy++)
                        if (Main.tile[x + dx, y - dy].HasTile && Main.tile[x + dx, y - dy].TileType == caverns.StoneTile)
                            Tile.SmoothSlope(x + dx, y - dy, false, false);
                Frame(area);
                features.Add(area);
            }
            List<int> candidates = Enumerable.Range(left + 5, Math.Max(0, right - left - 10)).ToList();
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
            }
            int bushes = 0, target = Math.Max(2, (right - left) / 55);
            foreach (int x in candidates)
            {
                if (bushes >= target) break;
                int y = Surface(x);
                if (y < 0 || Main.tile[x, y].TileType == caverns.StoneTile) continue;
                int radius = random.Next(3, 5);
                Rectangle area = new(x - radius - 1, y - 5, radius * 2 + 3, 9);
                if (!Free(area, true)) continue;
                int[] ground = new int[radius * 2 + 1];
                bool clear = true;
                for (int dx = -radius; dx <= radius; dx++)
                {
                    ground[dx + radius] = Surface(x + dx);
                    if (Math.Abs(ground[dx + radius] - y) > 2) clear = false;
                }
                if (!clear) continue;
                var lobes = new (int Center, int Radius, int Height)[random.Next(2, 4)];
                for (int i = 0; i < lobes.Length; i++)
                    lobes[i] = (random.Next(-radius + 1, radius), random.Next(2, 4), random.Next(1, 4));
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int cap = 1;
                    foreach (var lobe in lobes)
                    {
                        float distance = (dx - lobe.Center) / (float)lobe.Radius;
                        cap = Math.Max(cap, (int)MathF.Ceiling(lobe.Height * (1 - distance * distance)));
                    }
                    int surface = ground[dx + radius];
                    for (int dy = 0; dy <= cap; dy++)
                    {
                        Tile tile = Main.tile[x + dx, surface - dy];
                        if (tile.WallType == WallID.None || tile.WallType == caverns.GrassWall)
                            tile.WallType = caverns.GrassWall;
                    }
                    if (Math.Abs(dx) < radius && random.NextBool(2)
                        && WorldGen.SolidTile(x + dx, surface) && Main.tile[x + dx, surface].TileType == caverns.GrassTile
                        && !Main.tile[x + dx, surface - 1].HasTile)
                    {
                        Fill(x + dx, surface - 1, ModContent.TileType<CrystalFlora>());
                        Main.tile[x + dx, surface - 1].TileFrameX = (short)(random.Next(8) * 18);
                    }
                }
                Frame(area);
                features.Add(area);
                bushes++;
            }
        }

        private void GenerateCavernCrossings(GenerationProgress progress)
        {
            int top = (int)Main.rockLayer + 30;
            int bottom = Main.maxTilesY - 220;
            if (bottom <= top) return;
            int regions = Math.Max(1, (Main.maxTilesX - 360) / 720);
            for (int region = 0; region < regions; region++)
            {
                int left = 160 + (Main.maxTilesX - 360) * region / regions;
                int right = 160 + (Main.maxTilesX - 360) * (region + 1) / regions;
                for (int layer = 0; layer < 2; layer++)
                {
                    int layerTop = top + (bottom - top) * layer / 2;
                    int layerBottom = top + (bottom - top) * (layer + 1) / 2;
                    List<Point> banks = new(), ceilings = new();
                    for (int x = left; x < right; x += 2)
                    {
                        for (int y = layerTop; y < layerBottom; y++)
                        {
                            if (!NaturalGround(x, y)) continue;
                            if (CanPlacePost(x, y - 1, false)) banks.Add(new Point(x, y - 1));
                            if (CanPlacePost(x, y + 4, true, true)) ceilings.Add(new Point(x, y + 4));
                        }
                    }
                    List<Point> ziplinePosts = [.. ceilings, .. banks];
                    foreach (List<Point> candidates in new[] { banks, ziplinePosts })
                    {
                        for (int i = candidates.Count - 1; i > 0; i--)
                        {
                            int j = random.Next(i + 1);
                            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                        }
                    }
                    foreach (Point bank in banks)
                        if (TryCavernCrossing(bank, false)) break;
                    foreach (Point post in ziplinePosts)
                        if (TryCavernCrossing(post, true)) break;
                    progress.Set((region * 2 + layer + 1) / (double)(regions * 2));
                }
            }
        }

        private bool TryCavernCrossing(Point a, bool zipline)
        {
            bool ceiling = zipline && CanPlacePost(a.X, a.Y, true, true);
            if (!ceiling) return TryCavernSpan(a, zipline, false, zipline);
            bool otherCeiling = random.NextBool();
            return TryCavernSpan(a, true, true, otherCeiling) || TryCavernSpan(a, true, true, !otherCeiling);
        }

        private bool TryCavernSpan(Point a, bool zipline, bool leftCeiling, bool rightCeiling)
        {
            if (!NaturalGround(a.X, a.Y + (leftCeiling ? -4 : 1))
                || !CanPlacePost(a.X, a.Y, zipline, leftCeiling)) return false;
            int minWidth = zipline ? 24 : 16, maxWidth = zipline ? 112 : 48;
            int heightRange = zipline ? 64 : RopeSpanSystem.MaximumBridgeHeight;
            for (int width = minWidth + random.Next(4); width <= maxWidth; width += 3)
            {
                int x = a.X + width;
                if (x >= Main.maxTilesX - 160) break;
                int slopeLimit = zipline ? heightRange : Math.Min(heightRange, width / 2);
                for (int offset = -slopeLimit; offset <= slopeLimit; offset++)
                {
                    Point b = new(x, a.Y + offset);
                    if (!NaturalGround(b.X, b.Y + (rightCeiling ? -4 : 1)) || !CanPlacePost(b.X, b.Y, zipline, rightCeiling)) continue;
                    Rectangle area = new(a.X - 3, Math.Min(a.Y, b.Y) - 7, width + 7, Math.Abs(offset) + 17);
                    if (!Free(area, false)) continue;
                    RopeSpan preview = new(1, a, b, zipline, ItemID.Rope, leftCeiling: leftCeiling, rightCeiling: rightCeiling);
                    bool clear = true;
                    int deep = 0;
                    for (int i = 1; i < preview.Rest.Length - 1 && clear; i++)
                    {
                        Vector2 point = preview.Rest[i];
                        Point tile = point.ToTileCoordinates();
                        if (Collision.SolidCollision(point + new Vector2(-10, zipline ? 2 : -46), 20, 46)) clear = false;
                        foreach (Point marker in zipline ? new[] { tile } : new[] { tile, new Point(tile.X, tile.Y - 2) })
                            if (Main.tile[marker.X, marker.Y].HasTile && !Main.tileCut[Main.tile[marker.X, marker.Y].TileType]) clear = false;
                        if (!Main.tile[tile.X, tile.Y + (zipline ? 5 : 4)].HasTile) deep++;
                    }
                    if (!clear || deep < width / 2) continue;
                    PlacePost(a, zipline, leftCeiling);
                    PlacePost(b, zipline, rightCeiling);
                    if (!RopeSpanSystem.Create(a, b, zipline))
                    {
                        ClearPost(a, zipline);
                        ClearPost(b, zipline);
                        continue;
                    }
                    features.Add(area);
                    Protect(area, zipline ? "cavernzipline" : "cavernropebridge");
                    if (zipline) caveZiplines++; else caveBridges++;
                    return true;
                }
            }
            return false;
        }

        private static bool NaturalGround(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 20) || !WorldGen.SolidTile(x, y)) return false;
            int type = Main.tile[x, y].TileType;
            return type == TileID.Stone || type == TileID.Dirt || type == TileID.ClayBlock || type == TileID.Silt
                || TileID.Sets.Conversion.Moss[type];
        }

        private static bool CanPlacePost(int x, int bottom, bool zipline, bool ceiling = false)
        {
            int height = zipline ? 4 : 3;
            if (!WorldGen.InWorld(x, bottom - height, 20) || !WorldGen.SolidTile(x, ceiling ? bottom - height : bottom + 1)) return false;
            for (int row = 0; row < height; row++)
                if (Main.tile[x, bottom - row].HasTile && !Main.tileCut[Main.tile[x, bottom - row].TileType] || Main.tile[x, bottom - row].LiquidAmount > 0) return false;
            return true;
        }

        private static void PlacePost(Point point, bool zipline, bool ceiling = false)
        {
            int height = zipline ? 4 : 3;
            int type = zipline ? ModContent.TileType<ZiplinePostTile>() : ModContent.TileType<RopeBridgePostTile>();
            for (int row = 0; row < height; row++)
            {
                Fill(point.X, point.Y - height + row + 1, type);
                Main.tile[point.X, point.Y - height + row + 1].TileFrameY = (short)(row * 18);
                Main.tile[point.X, point.Y - height + row + 1].TileFrameX = (short)(ceiling ? 18 : 0);
            }
        }
        private static void ClearPost(Point point, bool zipline)
        {
            for (int row = 0; row < (zipline ? 4 : 3); row++) Main.tile[point.X, point.Y - row].ClearTile();
        }

        private static void Protect(Rectangle area, string name)
            => new AeroStructure(new Vector2(area.X, area.Y), area.Width, area.Height, name).ProtectStructure();

        private static void Frame(Rectangle area)
        {
            for (int x = area.Left; x < area.Right; x++)
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    WorldGen.SquareTileFrame(x, y);
                    WorldGen.SquareWallFrame(x, y);
                }
        }
    }
}
