using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria.GameContent;
using Terraria.Map;
using Terraria.Utilities;

namespace AerovelenceMod.Common.Utilities
{
    /// <summary>Controls how far cracks spread, how chunky they look, and how they close up</summary>
    public sealed record CrackSettings
    {
        public float Length { get; init; } = 64f;
        public int Branches { get; init; } = 6;
        public float Thickness { get; init; } = 6f;
        public float Taper { get; init; } = 1.5f;
        /// <summary>Retraction added per second; leave this at zero to keep the crack around</summary>
        public float DecayRate { get; init; }
        /// <summary>Approximate opening chunk size in pixels</summary>
        public float FragmentSize { get; init; } = 24f;
        /// <summary>Limits opening chunks to this region, or uses the full crack bounds when null</summary>
        public Rectangle? OpeningBounds { get; init; }
    }
    /// <summary>Adds a highlight and shadow along the crack edges for a little depth</summary>
    public sealed record CrackShading(Color Highlight, Color Shadow)
    {
        /// <summary>Optional world-position color sampler so highlights can pick up their surroundings</summary>
        public Func<Vector2, Color> SampleHighlight { get; init; }
    }
    /// <summary>One opening chunk, with its center, break threshold, and pixel spans in crack coordinates</summary>
    public sealed record CrackFragment(Vector2 Center, float BreakRadius, IReadOnlyList<Rectangle> Spans);
    /// <summary>Builds and draws pixel cracks with branching, retraction, and breakable openings</summary>
    public static class CrackUtil
    {
        /// <summary>The crack grid size in pixels; keeps everything on the same chunky grid</summary>
        public const int PixelSize = 2;
        private static readonly HashSet<Texture2D> drawTextures = new();
        private static readonly ConditionalWeakTable<Texture2D, ImagePixels> images = new();
        private sealed record ImagePixels(int Width, int Height, Color[] Pixels);
        internal readonly record struct CrackSegment(Vector2 Start, Vector2 End, float Distance, float WidthScale);
        internal readonly record struct CrackTip(Vector2 Point, float Angle, float Distance, float WidthScale);
        /// <summary>Holds one crack and its draw cache; dispose it when you are done with it</summary>
        public sealed class CrackData : IDisposable
        {
            private readonly Dictionary<Point, int> cells;
            private readonly int seed;
            private CrackFragment[] fragments;
            private readonly List<CrackTip> tips;
            private readonly List<CrackSegment> segments;
            private float growth;
            private readonly UnifiedRandom random;
            private int depth;
            private Rectangle[] spans = Array.Empty<Rectangle>();
            private int lastCutoff = -1, lastOpening = -1;
            private Texture2D drawTexture, lastFill;
            private Color[] drawPixels;
            private readonly HashSet<Point> drawnCells = new();
            private readonly Dictionary<Point, int> edgeCells = new();
            private readonly Dictionary<Point, Color> sampledColors = new(), previousColors = new();
            private IReadOnlyList<Rectangle> drawnSpans;
            private Color lastColor;
            private Color? lastEdge;
            private CrackShading lastShading;
            private Rectangle? lastFillBounds;
            private bool lastLighting, lastMasked;
            public CrackSettings Settings { get; }
            /// <summary>The starting point in the same coordinates as Bounds</summary>
            public Vector2 Origin { get; }
            public Rectangle Bounds { get; }
            /// <summary>How much of the crack is revealed, from 0 to 1</summary>
            public float Reveal { get; set; } = 1f;
            /// <summary>How far the crack has closed back toward its origin, from 0 to 1</summary>
            public float Retraction { get; set; }
            /// <summary>Opening progress in pixels; chunks break away as their thresholds are reached</summary>
            public float OpeningRadius { get; set; }
            /// <summary>Whether retraction is complete and the opening is shut</summary>
            public bool Closed => Retraction >= 1f && OpeningRadius <= 0f;
            internal CrackData(Dictionary<Point, int> cells, List<CrackTip> tips, List<CrackSegment> segments, UnifiedRandom random, int seed,
                Vector2 origin, Rectangle bounds, CrackSettings settings)
            {
                this.cells = cells;
                this.tips = tips;
                this.segments = segments;
                this.random = random;
                this.seed = seed;
                Origin = origin;
                Bounds = bounds;
                Settings = settings;
                MeasureDepth();
            }
            /// <summary>Walks connected cells from the origin so the reveal follows the crack instead of a circle</summary>
            private void MeasureDepth()
            {
                depth = 0;
                if (cells.Count == 0) return;
                Point first = new((int)MathF.Floor(Origin.X / PixelSize), (int)MathF.Floor(Origin.Y / PixelSize));
                if (!cells.ContainsKey(first))
                {
                    float nearest = float.MaxValue;
                    foreach (Point point in cells.Keys)
                    {
                        float distance = Vector2.DistanceSquared(point.ToVector2() * PixelSize + Vector2.One, Origin);
                        if (distance >= nearest) continue;
                        nearest = distance;
                        first = point;
                    }
                }
                HashSet<Point> reached = new() { first };
                Queue<Point> queue = new();
                cells[first] = 0;
                queue.Enqueue(first);
                while (queue.TryDequeue(out Point point))
                {
                    int distance = cells[point] + 1;
                    Visit(new Point(point.X - 1, point.Y));
                    Visit(new Point(point.X + 1, point.Y));
                    Visit(new Point(point.X, point.Y - 1));
                    Visit(new Point(point.X, point.Y + 1));
                    void Visit(Point next)
                    {
                        if (!cells.ContainsKey(next) || !reached.Add(next)) return;
                        cells[next] = distance;
                        depth = Math.Max(depth, distance);
                        queue.Enqueue(next);
                    }
                }
                if (reached.Count == cells.Count) return;
                foreach (Point point in new List<Point>(cells.Keys))
                    if (!reached.Contains(point)) cells.Remove(point);
            }
            /// <summary>Closes the crack using its decay rate. Elapsed time is in seconds</summary>
            public void Update(float elapsedSeconds = 1f / 60f) => Retraction = MathHelper.Clamp(
                Retraction + Math.Max(0f, Settings.DecayRate) * Math.Max(0f, elapsedSeconds), 0f, 1f);
            /// <summary>Extends existing tips and sometimes forks them; longer cracks also get a bit thicker</summary>
            /// <param name="length">Growth amount in pixels</param>
            /// <param name="branchChance">Chance of an extended tip creating another branch, from 0 to 1</param>
            public void Grow(float length, float branchChance = .2f)
            {
                if (length <= 0f || tips.Count == 0) return;
                int count = tips.Count;
                int first = random.Next(count);
                for (int i = 0; i < count; i++)
                {
                    CrackTip tip = tips[i];
                    if (!Bounds.Contains(tip.Point.ToPoint()) || i != first && random.NextFloat() < .35f) continue;
                    float angle = tip.Angle + random.NextFloat(-.45f, .45f);
                    Vector2 end = tip.Point + angle.ToRotationVector2() * (length * random.NextFloat(.65f, 1.35f));
                    segments.Add(new CrackSegment(tip.Point, end, tip.Distance, tip.WidthScale));
                    tips[i] = new CrackTip(end, angle, tip.Distance + Vector2.Distance(tip.Point, end), tip.WidthScale);
                    if (tips.Count >= Math.Max(12, Settings.Branches * 4) || random.NextFloat() >= branchChance) continue;
                    float forkAngle = angle + (random.NextBool() ? 1f : -1f) * random.NextFloat(.65f, 1.1f);
                    Vector2 fork = tip.Point + forkAngle.ToRotationVector2() * (length * random.NextFloat(.5f, .9f));
                    segments.Add(new CrackSegment(tip.Point, fork, tip.Distance, tip.WidthScale * .6f));
                    tips.Add(new CrackTip(fork, forkAngle, tip.Distance + Vector2.Distance(tip.Point, fork), tip.WidthScale * .6f));
                }
                growth += length;
                CrackSettings appearance = Settings with
                {
                    Length = Settings.Length + growth,
                    Thickness = Settings.Thickness * MathF.Sqrt(1f + growth / Math.Max(1f, Settings.Length))
                };
                foreach (CrackSegment segment in segments)
                    Stroke(cells, Bounds, segment.Start, segment.End, segment.Distance, appearance, segment.WidthScale);
                MeasureDepth();
                lastCutoff = -2;
            }
            /// <summary>Gets the visible pixel rows, reusing the cached result until reveal or opening progress changes</summary>
            public IReadOnlyList<Rectangle> GetSpans()
            {
                float progress = MathHelper.Clamp(Reveal, 0f, 1f) * (1f - MathHelper.Clamp(Retraction, 0f, 1f));
                int cutoff = progress <= 0f ? -1 : (int)MathF.Ceiling(progress * depth);
                int opening = Math.Max(0, (int)MathF.Round(OpeningRadius / PixelSize));
                if (cutoff == lastCutoff && opening == lastOpening) return spans;
                lastCutoff = cutoff;
                lastOpening = opening;
                HashSet<Point> visible = new();
                foreach (var cell in cells)
                    if (cell.Value <= cutoff) visible.Add(cell.Key);
                if (opening > 0) AddOpening(visible, opening * PixelSize);
                spans = BuildSpans(visible);
                return spans;
            }
            /// <summary>Bakes fill and edge shading into a cached texture, lighting and masks need a fresh pass</summary>
            internal Texture2D GetDrawTexture(Vector2 worldOrigin, Color color, float rotation, float scale,
                Color? edgeColor, Func<Vector2, bool> mask, bool applyLighting, CrackShading shading, Texture2D fill, Rectangle? fillBounds)
            {
                IReadOnlyList<Rectangle> visible = GetSpans();
                if (drawTexture == null || drawTexture.IsDisposed)
                {
                    drawTexture = new Texture2D(Main.instance.GraphicsDevice, Bounds.Width + PixelSize * 2, Bounds.Height + PixelSize * 2);
                    drawPixels = new Color[drawTexture.Width * drawTexture.Height];
                    drawTextures.Add(drawTexture);
                    drawnSpans = null;
                }
                sampledColors.Clear();
                bool colorsChanged = false;
                if (!applyLighting && mask == null && shading?.SampleHighlight != null && ReferenceEquals(drawnSpans, visible))
                    foreach (var edge in edgeCells)
                        if (edge.Value == 1 && (!previousColors.TryGetValue(edge.Key, out Color previous) || previous != SampleHighlight(edge.Key)))
                            colorsChanged = true;
                if (!applyLighting && mask == null && !lastLighting && !lastMasked && ReferenceEquals(drawnSpans, visible) && lastColor == color
                    && lastEdge == edgeColor && lastShading == shading && lastFill == fill && lastFillBounds == fillBounds && !colorsChanged) return drawTexture;
                Array.Clear(drawPixels);
                drawnCells.Clear();
                edgeCells.Clear();
                previousColors.Clear();
                ImagePixels image = fill == null ? null : images.GetValue(fill, texture =>
                {
                    Color[] pixels = new Color[texture.Width * texture.Height];
                    texture.GetData(pixels);
                    return new ImagePixels(texture.Width, texture.Height, pixels);
                });
                Rectangle imageBounds = fillBounds ?? Bounds;
                foreach (Rectangle span in visible)
                    for (int x = span.Left; x < span.Right; x += PixelSize)
                    {
                        Point cell = new(x / PixelSize, span.Y / PixelSize);
                        if (mask == null || mask(cell.ToVector2() * PixelSize + Vector2.One)) drawnCells.Add(cell);
                    }
                if (edgeColor.HasValue)
                    foreach (Point cell in drawnCells) Paint(cell + new Point(-1, 1), edgeColor.Value, false);
                foreach (Point cell in drawnCells) Paint(cell, color, image != null);
                if (shading != null)
                {
                    foreach (Point cell in drawnCells)
                    {
                        AddEdge(cell + new Point(-1, 1), 1);
                        AddEdge(cell + new Point(1, -1), 2);
                    }
                    foreach (var edge in edgeCells)
                    {
                        if (edge.Value == 3) continue;
                        Color tint = edge.Value == 1 ? shading.Highlight : shading.Shadow;
                        if (edge.Value == 1 && shading.SampleHighlight != null)
                        {
                            Color surface = SampleHighlight(edge.Key);
                            previousColors[edge.Key] = surface;
                            tint = surface * (tint.A / 255f);
                        }
                        Paint(edge.Key, tint, false);
                    }
                }
                drawTexture.SetData(drawPixels);
                drawnSpans = visible;
                lastColor = color;
                lastEdge = edgeColor;
                lastShading = shading;
                lastFill = fill;
                lastFillBounds = fillBounds;
                lastLighting = applyLighting;
                lastMasked = mask != null;
                return drawTexture;
                Color SampleHighlight(Point cell)
                {
                    Vector2 world = worldOrigin + ((cell.ToVector2() * PixelSize + Vector2.One) * scale).RotatedBy(rotation);
                    Point tile = world.ToTileCoordinates();
                    if (!sampledColors.TryGetValue(tile, out Color tint))
                    {
                        tint = shading.SampleHighlight(world);
                        sampledColors.Add(tile, tint);
                    }
                    return tint;
                }
                void AddEdge(Point cell, int side)
                {
                    if (drawnCells.Contains(cell)) return;
                    if (!drawnCells.Contains(cell + new Point(-1, 0)) && !drawnCells.Contains(cell + new Point(1, 0))
                        && !drawnCells.Contains(cell + new Point(0, -1)) && !drawnCells.Contains(cell + new Point(0, 1))) return;
                    edgeCells.TryGetValue(cell, out int previous);
                    edgeCells[cell] = previous | side;
                }
                void Paint(Point cell, Color tint, bool fillImage)
                {
                    Vector2 center = cell.ToVector2() * PixelSize + Vector2.One;
                    if (mask != null && !drawnCells.Contains(cell) && !mask(center)) return;
                    Color light = applyLighting ? Lighting.GetColor((worldOrigin + (center * scale).RotatedBy(rotation)).ToTileCoordinates()) : Color.White;
                    Color cellColor = tint.MultiplyRGB(light);
                    cellColor.A = tint.A;
                    for (int dy = 0; dy < PixelSize; dy++)
                        for (int dx = 0; dx < PixelSize; dx++)
                        {
                            int px = cell.X * PixelSize + dx, py = cell.Y * PixelSize + dy;
                            int index = (py - Bounds.Y + PixelSize) * drawTexture.Width + px - Bounds.X + PixelSize;
                            Color pixel = cellColor;
                            if (fillImage && imageBounds.Contains(px, py))
                            {
                                int sx = (int)((px + .5f - imageBounds.X) * image.Width / imageBounds.Width);
                                int sy = (int)((py + .5f - imageBounds.Y) * image.Height / imageBounds.Height);
                                Color source = image.Pixels[sy * image.Width + sx];
                                Color lit = source.MultiplyRGB(light);
                                lit.A = source.A;
                                pixel = Over(lit, pixel);
                            }
                            drawPixels[index] = Over(pixel, drawPixels[index]);
                        }
                }
            }
            /// <summary>Releases this crack's draw texture on the main thread</summary>
            public void Dispose()
            {
                Texture2D previous = drawTexture;
                drawTexture = null;
                drawPixels = null;
                drawnCells.Clear();
                edgeCells.Clear();
                sampledColors.Clear();
                previousColors.Clear();
                drawnSpans = null;
                if (previous == null) return;
                if (drawTextures.Remove(previous)) Main.QueueMainThreadAction(previous.Dispose);
            }
            /// <summary>Builds opening chunks once and returns them in the order they break away</summary>
            public IReadOnlyList<CrackFragment> GetOpeningFragments() => fragments ??= CreateFragments();
            private void AddOpening(HashSet<Point> visible, float radius)
            {
                foreach (CrackFragment fragment in GetOpeningFragments())
                {
                    if (fragment.BreakRadius > radius) break;
                    foreach (Rectangle span in fragment.Spans)
                        for (int x = span.Left; x < span.Right; x += PixelSize)
                            visible.Add(new Point(x / PixelSize, span.Y / PixelSize));
                }
            }
            /// <summary>Splits the opening around jittered centers, then spreads break thresholds through neighboring chunks</summary>
            private CrackFragment[] CreateFragments()
            {
                UnifiedRandom random = new(seed ^ 1831565813);
                float size = MathHelper.Clamp(Settings.FragmentSize, 8f, 96f);
                Rectangle region = Rectangle.Intersect(Bounds, Settings.OpeningBounds ?? Bounds);
                int left = (int)MathF.Ceiling(region.Left / (float)PixelSize);
                int top = (int)MathF.Ceiling(region.Top / (float)PixelSize);
                int width = (int)MathF.Floor(region.Right / (float)PixelSize) - left;
                int height = (int)MathF.Floor(region.Bottom / (float)PixelSize) - top;
                if (width <= 0 || height <= 0) return Array.Empty<CrackFragment>();
                Dictionary<Point, int> grid = new();
                List<Vector2> centers = new();
                List<HashSet<Point>> pixels = new();
                List<HashSet<int>> neighbors = new();
                List<float> weights = new();
                int gridLeft = (int)MathF.Floor((region.Left - Origin.X) / size) - 1;
                int gridTop = (int)MathF.Floor((region.Top - Origin.Y) / size) - 1;
                int gridRight = (int)MathF.Ceiling((region.Right - Origin.X) / size) + 1;
                int gridBottom = (int)MathF.Ceiling((region.Bottom - Origin.Y) / size) + 1;
                for (int y = gridTop; y <= gridBottom; y++)
                    for (int x = gridLeft; x <= gridRight; x++)
                    {
                        grid.Add(new Point(x, y), centers.Count);
                        centers.Add(Origin + (new Vector2(x + .5f, y + .5f) + random.NextVector2Square(-.4f, .4f)) * size);
                        pixels.Add(new HashSet<Point>());
                        neighbors.Add(new HashSet<int>());
                        weights.Add(random.NextFloat(.65f, 1.45f));
                    }
                int[,] owners = new int[width, height];
                int first = 0;
                float nearestOrigin = float.MaxValue;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        Point pixel = new(left + x, top + y);
                        Vector2 point = pixel.ToVector2() * PixelSize + Vector2.One;
                        Point cell = new((int)MathF.Floor((point.X - Origin.X) / size), (int)MathF.Floor((point.Y - Origin.Y) / size));
                        int owner = 0;
                        float nearest = float.MaxValue;
                        for (int j = cell.Y - 1; j <= cell.Y + 1; j++)
                            for (int i = cell.X - 1; i <= cell.X + 1; i++)
                            {
                                int candidate = grid[new Point(i, j)];
                                float distance = Vector2.DistanceSquared(point, centers[candidate]);
                                if (distance >= nearest) continue;
                                nearest = distance;
                                owner = candidate;
                            }
                        owners[x, y] = owner;
                        pixels[owner].Add(pixel);
                        float originDistance = Vector2.DistanceSquared(point, Origin);
                        if (originDistance < nearestOrigin) { nearestOrigin = originDistance; first = owner; }
                        if (x > 0) Connect(owner, owners[x - 1, y]);
                        if (y > 0) Connect(owner, owners[x, y - 1]);
                    }
                float[] radii = new float[centers.Count];
                Array.Fill(radii, float.MaxValue);
                PriorityQueue<int, float> queue = new();
                radii[first] = Math.Min(8f, size * .3f);
                queue.Enqueue(first, radii[first]);
                while (queue.TryDequeue(out int index, out float radius))
                {
                    if (radius > radii[index]) continue;
                    foreach (int next in neighbors[index])
                    {
                        float cost = Vector2.Distance(centers[index], centers[next]) * weights[next];
                        float nextRadius = radius + cost;
                        if (nextRadius >= radii[next]) continue;
                        radii[next] = nextRadius;
                        queue.Enqueue(next, nextRadius);
                    }
                }
                List<CrackFragment> result = new();
                for (int i = 0; i < centers.Count; i++)
                    if (pixels[i].Count > 0) result.Add(new CrackFragment(centers[i], radii[i], BuildSpans(pixels[i])));
                result.Sort((a, b) => a.BreakRadius.CompareTo(b.BreakRadius));
                return result.ToArray();
                void Connect(int a, int b)
                {
                    if (a == b) return;
                    neighbors[a].Add(b);
                    neighbors[b].Add(a);
                }
            }
        }
        /// <summary>Picks a brightened tile, wall, or sky color for highlights that fit the surrroundings</summary>
        public static Color SampleEnvironmentHighlight(Vector2 position)
        {
            Point point = position.ToTileCoordinates();
            Color color = Main.ColorOfTheSkies;
            if (WorldGen.InWorld(point.X, point.Y))
            {
                Tile tile = Main.tile[point.X, point.Y];
                if (tile.HasTile || tile.WallType != 0)
                {
                    MapTile mapTile = MapHelper.CreateMapTile(point.X, point.Y, byte.MaxValue);
                    color = MapHelper.GetMapTileXnaColor(ref mapTile);
                }
            }
            color.A = byte.MaxValue;
            return Color.Lerp(color, Color.White, .35f);
        }
        /// <summary>Creates a seeded crack pattern; matching inputs give you the same starting shape</summary>
        /// <param name="origin">Starting point in the same coordinates as bounds</param>
        /// <param name="bounds">Region the crack is allowed to occupy, in pixels</param>
        /// <param name="seed">Random seed for the crack and its opening chunks</param>
        /// <param name="settings">Shape and decay settings, or null for the defaults</param>
        public static CrackData Create(Vector2 origin, Rectangle bounds, int seed, CrackSettings settings = null)
        {
            settings ??= new CrackSettings();
            Dictionary<Point, int> cells = new();
            List<CrackTip> tips = new();
            List<CrackSegment> segments = new();
            UnifiedRandom random = new(seed);
            int branches = Math.Max(0, settings.Branches);
            Vector2[] bends = new Vector2[branches];
            float offset = random.NextFloat(MathHelper.TwoPi);
            if (settings.Length > 0f && settings.Thickness > 0f)
                for (int branch = 0; branch < branches; branch++)
                {
                    float angle = offset + MathHelper.TwoPi * branch / branches + random.NextFloat(-.25f, .25f);
                    float reach = settings.Length * random.NextFloat(.65f, 1.15f);
                    Vector2 point = origin;
                    float distance = 0f, lastAngle = angle;
                    for (int step = 0; step < 3; step++)
                    {
                        float turn = angle + random.NextFloat(-.4f, .4f);
                        lastAngle = turn;
                        Vector2 next = point + turn.ToRotationVector2() * (reach / 3f);
                        AddStroke(point, next, distance, settings);
                        if (step == 0) bends[branch] = next;
                        if (step == 1)
                        {
                            float forkAngle = turn + (random.NextBool() ? 1f : -1f) * random.NextFloat(.7f, 1.2f);
                            Vector2 fork = point + forkAngle.ToRotationVector2() * (reach * .25f);
                            Vector2 tip = fork + (forkAngle + random.NextFloat(-.5f, .5f)).ToRotationVector2() * (reach * .2f);
                            AddStroke(point, fork, distance, settings, .6f);
                            AddStroke(fork, tip, distance + Vector2.Distance(point, fork), settings, .6f);
                            tips.Add(new CrackTip(tip, (tip - fork).ToRotation(),
                                distance + Vector2.Distance(point, fork) + Vector2.Distance(fork, tip), .6f));
                        }
                        distance += Vector2.Distance(point, next);
                        point = next;
                    }
                    tips.Add(new CrackTip(point, lastAngle, distance, 1f));
                }
            for (int branch = 1; branch < branches; branch++)
            {
                if (!random.NextBool() || Vector2.Distance(bends[branch - 1], bends[branch]) > settings.Length * .7f) continue;
                Vector2 bend = Vector2.Lerp(bends[branch - 1], bends[branch], .5f) + random.NextVector2Circular(1.5f, 1.5f);
                float distance = Vector2.Distance(origin, bends[branch - 1]);
                AddStroke(bends[branch - 1], bend, distance, settings, .4f);
                AddStroke(bend, bends[branch], distance + Vector2.Distance(bends[branch - 1], bend), settings, .4f);
            }
            return new CrackData(cells, tips, segments, random, seed, origin, bounds, settings);
            void AddStroke(Vector2 start, Vector2 end, float distance, CrackSettings appearance, float widthScale = 1f)
            {
                segments.Add(new CrackSegment(start, end, distance, widthScale));
                Stroke(cells, bounds, start, end, distance, appearance, widthScale);
            }
        }
        private static void Stroke(Dictionary<Point, int> cells, Rectangle bounds, Vector2 start, Vector2 end, float distance, CrackSettings settings, float widthScale = 1f)
        {
            Vector2 delta = end - start;
            float lengthSquared = delta.LengthSquared();
            if (lengthSquared < .001f || settings.Length <= 0f || settings.Thickness <= 0f) return;
            float length = MathF.Sqrt(lengthSquared);
            float padding = settings.Thickness * widthScale * .5f + PixelSize;
            int left = (int)MathF.Floor((Math.Min(start.X, end.X) - padding) / PixelSize);
            int right = (int)MathF.Ceiling((Math.Max(start.X, end.X) + padding) / PixelSize);
            int top = (int)MathF.Floor((Math.Min(start.Y, end.Y) - padding) / PixelSize);
            int bottom = (int)MathF.Ceiling((Math.Max(start.Y, end.Y) + padding) / PixelSize);
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                {
                    Vector2 point = new Vector2(x, y) * PixelSize + Vector2.One;
                    float along = MathHelper.Clamp(Vector2.Dot(point - start, delta) / lengthSquared, 0f, 1f);
                    float traveled = distance + length * along;
                    float taper = MathF.Pow(MathHelper.Clamp(1f - traveled / settings.Length, 0f, 1f), Math.Max(0f, settings.Taper));
                    float radius = Math.Max(PixelSize * .5f, settings.Thickness * taper * widthScale * .5f);
                    if (Vector2.DistanceSquared(point, start + delta * along) <= radius * radius)
                        AddCell(new Point(x, y), traveled);
                }
            int steps = Math.Max(1, (int)MathF.Ceiling(Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) / PixelSize));
            Point previous = new((int)MathF.Floor(start.X / PixelSize), (int)MathF.Floor(start.Y / PixelSize));
            for (int i = 0; i <= steps; i++)
            {
                float along = i / (float)steps;
                Vector2 point = (start + delta * along) / PixelSize;
                Point pixel = new((int)MathF.Floor(point.X), (int)MathF.Floor(point.Y));
                if (pixel.X != previous.X && pixel.Y != previous.Y)
                {
                    Point horizontal = new(pixel.X, previous.Y), vertical = new(previous.X, pixel.Y);
                    AddCell(LineDistance(horizontal) <= LineDistance(vertical) ? horizontal : vertical, distance + length * along);
                }
                AddCell(pixel, distance + length * along);
                previous = pixel;
            }
            float LineDistance(Point pixel)
            {
                Vector2 point = pixel.ToVector2() * PixelSize + Vector2.One;
                float along = MathHelper.Clamp(Vector2.Dot(point - start, delta) / lengthSquared, 0f, 1f);
                return Vector2.DistanceSquared(point, start + delta * along);
            }
            void AddCell(Point pixel, float traveled)
            {
                Rectangle cell = new(pixel.X * PixelSize, pixel.Y * PixelSize, PixelSize, PixelSize);
                if (!bounds.Contains(cell)) return;
                int depth = (int)MathF.Ceiling(traveled / PixelSize);
                if (!cells.TryGetValue(pixel, out int previous) || depth < previous) cells[pixel] = depth;
            }
        }
        /// <summary>Packs neighboring cells into horizontal rows so drawing needs fewer rectangles</summary>
        private static Rectangle[] BuildSpans(HashSet<Point> pixels)
        {
            List<Point> sorted = new(pixels);
            sorted.Sort((a, b) => a.Y == b.Y ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            List<Rectangle> spans = new();
            for (int i = 0; i < sorted.Count;)
            {
                Point first = sorted[i++];
                int width = 1;
                while (i < sorted.Count && sorted[i].Y == first.Y && sorted[i].X == first.X + width)
                {
                    width++;
                    i++;
                }
                spans.Add(new Rectangle(first.X * PixelSize, first.Y * PixelSize, width * PixelSize, PixelSize));
            }
            return [.. spans];
        }
        /// <summary>Draws the visible crack with optional lighting, edge shading, and an image fill</summary>
        /// <param name="data">The crack draw</param>
        /// <param name="spriteBatch">The active sprite batch</param>
        /// <param name="position">Screen-space offset applied to the crack coordinates</param>
        /// <param name="color">Base crack tint</param>
        /// <param name="rotation">Rotation in radians</param>
        /// <param name="scale">Draw scale</param>
        /// <param name="edgeColor">Optional tint for the offset edge</param>
        /// <param name="mask">Optional filter in crack coordinates; return false to skip a cell</param>
        /// <param name="applyLighting">Whether to sample world lighting</param>
        /// <param name="shading">Optional highlight and shadow settings</param>
        /// <param name="fillTexture">Optional image to show inside the crack</param>
        /// <param name="fillBounds">Image region in crack coordinates, or null to use the crack bounds</param>
        public static void Draw(CrackData data, SpriteBatch spriteBatch, Vector2 position, Color color, float rotation = 0f,
            float scale = 1f, Color? edgeColor = null, Func<Vector2, bool> mask = null, bool applyLighting = true,
            CrackShading shading = null, Texture2D fillTexture = null, Rectangle? fillBounds = null)
        {
            if (Main.dedServ || data == null || scale <= 0f) return;
            if (shading != null || fillTexture != null)
            {
                if (data.GetSpans().Count == 0) return;
                Texture2D texture = data.GetDrawTexture(position + Main.screenPosition, color, rotation, scale,
                    edgeColor, mask, applyLighting, shading, fillTexture, fillBounds);
                Vector2 origin = data.Bounds.TopLeft() - new Vector2(PixelSize);
                spriteBatch.Draw(texture, position + (origin * scale).RotatedBy(rotation), null, Color.White,
                    rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
                return;
            }
            if (edgeColor.HasValue) DrawSpans(edgeColor.Value, new Vector2(-PixelSize, PixelSize));
            DrawSpans(color, Vector2.Zero);
            void DrawSpans(Color tint, Vector2 offset)
            {
                foreach (Rectangle span in data.GetSpans())
                {
                    if (mask == null && !applyLighting) DrawSpan(span.X, span.Y, span.Width);
                    else
                        for (int x = span.Left; x < span.Right; x += PixelSize)
                            if (mask == null || mask(new Vector2(x + 1f, span.Y + 1f) + offset)) DrawSpan(x, span.Y, PixelSize);
                }
                void DrawSpan(int x, int y, int width)
                {
                    Vector2 point = new Vector2(x, y) + offset;
                    Vector2 drawPosition = position + (point * scale).RotatedBy(rotation);
                    Color drawColor = tint;
                    if (applyLighting)
                    {
                        Vector2 center = position + ((point + Vector2.One) * scale).RotatedBy(rotation) + Main.screenPosition;
                        drawColor = tint.MultiplyRGB(Lighting.GetColor(center.ToTileCoordinates()));
                    }
                    spriteBatch.Draw(TextureAssets.MagicPixel.Value, drawPosition, new Rectangle(0, 0, 1, 1),
                        drawColor, rotation, Vector2.Zero, new Vector2(width, PixelSize) * scale, SpriteEffects.None, 0f);
                }
            }
        }
        private static Color Over(Color foreground, Color background)
        {
            float remaining = 1f - foreground.A / 255f;
            return new Color(Math.Min(255, foreground.R + (int)(background.R * remaining)),
                Math.Min(255, foreground.G + (int)(background.G * remaining)),
                Math.Min(255, foreground.B + (int)(background.B * remaining)),
                Math.Min(255, foreground.A + (int)(background.A * remaining)));
        }
        /// <summary>Clears shared texture caches and queues draw textures for disposal on the main thread</summary>
        public static void ClearTextures()
        {
            Texture2D[] previous = new Texture2D[drawTextures.Count];
            drawTextures.CopyTo(previous);
            drawTextures.Clear();
            images.Clear();
            if (previous.Length > 0) Main.QueueMainThreadAction(() => { foreach (Texture2D texture in previous) texture.Dispose(); });
        }
        /// <summary>Draws only the parts of an image covered by the visible crack</summary>
        /// <param name="data">The crack used to clip the image</param>
        /// <param name="spriteBatch">The active sprite batch</param>
        /// <param name="position">Screen-space offset applied to the crack coordinates</param>
        /// <param name="texture">The image to draw</param>
        /// <param name="imageBounds">Where the image sits in crack coordinates</param>
        /// <param name="color">Image tint</param>
        /// <param name="rotation">Rotation in radians</param>
        /// <param name="scale">Draw scale</param>
        public static void DrawTexture(CrackData data, SpriteBatch spriteBatch, Vector2 position, Texture2D texture, Rectangle imageBounds,
            Color color, float rotation = 0f, float scale = 1f)
        {
            if (Main.dedServ || data == null || scale <= 0f || imageBounds.Width <= 0 || imageBounds.Height <= 0) return;
            Vector2 ratio = texture.Size() / imageBounds.Size();
            foreach (Rectangle span in data.GetSpans())
            {
                Rectangle clip = Rectangle.Intersect(span, imageBounds);
                if (clip.Width == 0 || clip.Height == 0) continue;
                int left = (int)MathF.Floor((clip.Left - imageBounds.Left) * ratio.X);
                int top = (int)MathF.Floor((clip.Top - imageBounds.Top) * ratio.Y);
                int right = (int)MathF.Floor((clip.Right - imageBounds.Left) * ratio.X);
                int bottom = (int)MathF.Floor((clip.Bottom - imageBounds.Top) * ratio.Y);
                Rectangle source = new(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
                spriteBatch.Draw(texture, position + (clip.TopLeft() * scale).RotatedBy(rotation), source, color, rotation, Vector2.Zero,
                    clip.Size() * scale / source.Size(), SpriteEffects.None, 0f);
            }
        }
    }
}
