using System;
using System.Collections.Generic;
using System.Linq;
using AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture;
namespace AerovelenceMod.Common.Systems;
public sealed class HolotrackSystem : ModSystem
{
    private const int MinPathLength = 45;
    private const int MinSectionLength = 12;
    private const int MaxSectionLength = 26;
    private const int SectionEndPadding = 8;
    private const int SectionLengthDivisor = 6;
    private const int VisualCacheLifetime = 120;
    private const int RiderHeightTolerance = 64;
    private const int RevealAhead = 85;
    private const int RevealBehind = 40;
    private const float ChargeStep = .2f;
    private const float FadeStep = 1f / 75;
    private const float MinimumCharge = .01f;
    private sealed class Segment(Point tile)
    {
        internal readonly Point Tile = tile;
        internal float Charge;
        internal ulong Lit;
        internal ulong Seen;
    }
    private static readonly List<Point[]> sections = new();
    private static readonly Dictionary<Point, Segment> tiles = new();
    private static readonly List<Point> staleTiles = new();
    private static Rectangle VisibleArea => new((int)Main.screenPosition.X / 16 - 3, (int)Main.screenPosition.Y / 16 - 3, Main.screenWidth / 16 + 6, Main.screenHeight / 16 + 6);
    internal static IReadOnlyList<Point[]> Sections => sections;
    internal static bool Contains(int x, int y) => WorldGen.InWorld(x, y, 2) && Main.tile[x, y].HasTile && Main.tile[x, y].TileType == ModContent.TileType<HolotrackTile>();
    internal static float ChargeAt(int x, int y) => tiles.TryGetValue(new(x, y), out Segment segment) ? segment.Charge : 0;
    public override void Load() => MinecartTrackUtils.Load();
    public override void Unload()
    {
        MinecartTrackUtils.Unload();
        ClearWorld();
    }
    internal static void AddSection(List<Point> path, int limit)
    {
        if (sections.Count >= limit || path.Count < MinPathLength)
            return;
        int length = Math.Clamp(path.Count / SectionLengthDivisor, MinSectionLength, MaxSectionLength);
        List<int> choices = new();
        int bestOpen = -1;
        for (int start = SectionEndPadding; start + length < path.Count - SectionEndPadding; start++)
        {
            bool connected = true;
            int open = 0;
            for (int i = start; i < start + length; i++)
            {
                if (path[i + 1].X != path[i].X + 1 || Math.Abs(path[i + 1].Y - path[i].Y) > 1)
                {
                    connected = false;
                    break;
                }
                if (!WorldGen.SolidTile(path[i].X, path[i].Y + 1))
                    open++;
            }
            if (!connected || open < bestOpen)
                continue;
            if (open > bestOpen)
            {
                choices.Clear();
                bestOpen = open;
            }
            choices.Add(start);
        }
        if (choices.Count == 0)
            return;
        int selected = choices[WorldGen.genRand.Next(choices.Count)];
        Point[] section = path.Skip(selected).Take(length).ToArray();
        foreach (Point point in section)
            Main.tile[point.X, point.Y].TileType = (ushort)ModContent.TileType<HolotrackTile>();
        sections.Add(section);
    }
    public override void ClearWorld()
    {
        sections.Clear();
        tiles.Clear();
        staleTiles.Clear();
    }
    internal static void Remove(int x, int y) => tiles.Remove(new(x, y));
    public override void PostUpdateEverything()
    {
        if (Main.dedServ)
            return;
        staleTiles.Clear();
        Rectangle visible = VisibleArea;
        foreach (Segment segment in tiles.Values)
        {
            if (!Contains(segment.Tile.X, segment.Tile.Y) || !visible.Contains(segment.Tile) && Main.GameUpdateCount - segment.Seen > VisualCacheLifetime)
            {
                staleTiles.Add(segment.Tile);
                continue;
            }
            bool riding = false;
            Vector2 rail = RailDrawUtils.TrackPoint(segment.Tile, 8);
            foreach (Player player in Main.ActivePlayers)
            {
                if (!player.mount.Active || !player.mount.Cart || !Minecart.OnTrack(player.position, player.width, player.height))
                    continue;
                Vector2 magnet = Minecart.GetMagnetPosition(player.position, player.width, player.height);
                float ahead = (rail.X - magnet.X) * (player.velocity.X < 0 ? -1 : 1);
                if (Math.Abs(rail.Y - magnet.Y) < RiderHeightTolerance && ahead >= -RevealBehind && ahead <= RevealAhead)
                {
                    riding = true;
                    break;
                }
            }
            if (riding)
            {
                if (segment.Charge <= MinimumCharge)
                    segment.Lit = Main.GameUpdateCount;
                segment.Charge = Math.Min(1, segment.Charge + ChargeStep);
            }
            else
                segment.Charge = Math.Max(0, segment.Charge - FadeStep);
        }
        foreach (Point point in staleTiles)
            tiles.Remove(point);
    }
    internal static void DrawTile(int x, int y, SpriteBatch batch)
    {
        if (!Contains(x, y))
            return;
        Point point = new(x, y);
        if (!tiles.TryGetValue(point, out Segment segment))
        {
            segment = new(point);
            tiles.Add(point, segment);
        }
        segment.Seen = Main.GameUpdateCount;
        RailDrawUtils.TrackRails(segment.Tile, out Vector2[] upper, out Vector2[] lower);
        Vector2 offset = -Main.screenPosition + (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange));
        Color light = Lighting.GetColor(x, y);
        float visibility = Math.Max(.3f, Math.Max(light.R, Math.Max(light.G, light.B)) / 255f);
        Color color = TumblerVFX.Glow(new Color(100, 205, 244), (.28f + segment.Charge * .45f) * visibility);
        for (int i = 1; i < upper.Length; i++)
        {
            TumblerVFX.DrawLine(batch, upper[i - 1] + offset, upper[i] + offset, color, 1.5f);
            TumblerVFX.DrawLine(batch, lower[i - 1] + offset, lower[i] + offset, color * .65f, 1f);
        }
        TumblerVFX.DrawLine(batch, upper[0] + offset, lower[2] + offset, color * .7f, 1f);
        TumblerVFX.DrawLine(batch, lower[2] + offset, upper[^1] + offset, color * .7f, 1f);
    }
    public override void PostDrawTiles()
    {
        Rectangle screen = VisibleArea;
        foreach (Segment segment in tiles.Values)
        {
            if (!screen.Contains(segment.Tile) || !Contains(segment.Tile.X, segment.Tile.Y) || Main.tile[segment.Tile.X, segment.Tile.Y].IsTileInvisible && !Main.ShouldShowInvisibleWalls())
                continue;
            if (segment.Charge >= MinimumCharge)
            {
                RailDrawUtils.TrackRails(segment.Tile, out Vector2[] upper, out Vector2[] lower);
                RailDrawUtils.Draw(upper, lower, TumblerVFX.PhaseColor(0), TumblerVFX.PhaseColor(1), segment.Charge * .8f, false, RenderLayer.UnderNPCs);
                float flash = Math.Clamp(1 - (Main.GameUpdateCount - segment.Lit) / 10f, 0, 1) * segment.Charge;
                if (flash > 0)
                    ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.UnderNPCs, () => TumblerVFX.DrawCharge(Main.spriteBatch, upper[2] - Main.screenPosition, Color.White, 1, 7, Main.GlobalTimeWrappedHourly, flash));
            }
            if (!HasNeighbor(segment.Tile, -1) || !HasNeighbor(segment.Tile, 1))
            {
                Vector2 point = RailDrawUtils.TrackPoint(segment.Tile, 8) + new Vector2(0, 4);
                Color light = Lighting.GetColor(segment.Tile.X, segment.Tile.Y);
                float opacity = Math.Max(.3f, Math.Max(light.R, Math.Max(light.G, light.B)) / 255f) * (.35f + MathF.Sin(Main.GlobalTimeWrappedHourly * 3) * .08f);
                ModContent.GetInstance<PixelationSystem>().QueueRenderAction(RenderLayer.UnderNPCs, () => TumblerVFX.DrawCharge(Main.spriteBatch, point - Main.screenPosition, new Color(90, 195, 245), .8f, 5, Main.GlobalTimeWrappedHourly, opacity));
            }
        }
    }
    private static bool HasNeighbor(Point point, int direction) => Contains(point.X + direction, point.Y - 1) || Contains(point.X + direction, point.Y) || Contains(point.X + direction, point.Y + 1);
}
