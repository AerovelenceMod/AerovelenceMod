using System;
using System.Collections.Generic;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns;

public sealed class SilkenCitadelLayout
{
    public const int Width = 400, Height = 300, ReferenceSeed = 240940;
    public readonly record struct Nest(double X, double Y, double RX, double RY);
    public readonly record struct Cell(int X, int Y);
    public static IReadOnlyList<Cell> MainNodes { get; } =
    [
        new(198,24), new(184,44), new(220,50), new(169,66), new(247,76),
        new(146,94), new(206,98), new(273,108), new(126,126), new(172,125),
        new(218,132), new(262,137), new(304,146), new(138,158), new(190,160),
        new(236,168), new(282,174), new(126,191), new(168,190), new(214,198),
        new(261,204), new(305,211), new(146,224), new(191,224), new(236,232),
        new(281,238), new(167,258), new(217,257), new(267,264), new(309,271)
    ];
    public static IReadOnlyList<Cell> OuterNodes { get; } =
    [
        new(96,99), new(315,99), new(94,139), new(326,139), new(96,181),
        new(322,183), new(104,218), new(320,223), new(114,254), new(326,257)
    ];
    public static IReadOnlyList<Nest> CitadelPockets { get; } =
    [
        new(155,140,18,9), new(203,136,20,10), new(251,148,18,9), new(287,157,17,9),
        new(145,173,17,9), new(190,171,20,10), new(236,181,19,10), new(283,173,18,9),
        new(165,204,18,10), new(220,207,19,10), new(275,220,18,9), new(305,188,13,7), new(125,206,12,7)
    ];
    public static IReadOnlyList<Nest> IslandCandidates { get; } =
    [
        new(178,63,6,3), new(228,63,7,3), new(132,121,6,3), new(280,121,7,3),
        new(166,156,5,3), new(222,160,7,3), new(295,154,6,3), new(150,191,6,3),
        new(254,194,6,3), new(300,208,7,3), new(177,238,6,3), new(230,244,7,3),
        new(290,258,6,3), new(201,93,5,3), new(244,100,5,3), new(182,128,5,3), new(245,228,5,3), new(130,224,5,3)
    ];
    public int Seed { get; }
    public bool[] Air { get; } = new bool[Width * Height];
    public bool[] BranchMask { get; } = new bool[Width * Height];
    public bool[] BaseAir { get; private set; }
    public bool[] BeforeSmoothing { get; private set; }
    public bool[] AfterSmoothing { get; private set; }
    public List<Nest> Nests { get; } = new();
    public List<Nest> Islands { get; } = new();
    public int Branches { get; private set; }
    public int RejoinedBranches { get; private set; }
    private readonly PythonRandom random;
    private const double Tau = Math.PI * 2;

    public SilkenCitadelLayout(int seed, bool compact = false)
    {
        Seed = seed;
        random = new PythonRandom(seed);
        if (compact) GenerateCompactNests();
        else GenerateNests();
        BaseAir = (bool[])Air.Clone();
        if (compact) GenerateCompactBranches();
        else GenerateBranches();
        BeforeSmoothing = (bool[])Air.Clone();
        SmoothBranches();
        AfterSmoothing = (bool[])Air.Clone();
        if (!compact)
            for (int i = 0; i < CitadelPockets.Count; i++) CarveNest(CitadelPockets[i], 7000 + i, random.Int(0, 1), true);
        if (compact) GenerateCompactIslands();
        else GenerateIslands();
    }
    private static int Index(int x, int y) => x + y * Width;
    public static bool Inside(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;
    public bool IsAir(int x, int y) => Inside(x, y) && Air[Index(x, y)];

    private void GenerateCompactNests()
    {
        Nests.Add(new Nest(198, 24, 9, 6));
        for (int row = 0, y = 36; y < 290; row++, y += 14)
            for (int x = 77 + row % 2 * 9; x < 338; x += 17)
            {
                double cy = y + random.Range(-4, 4), cx = x + random.Range(-5, 5);
                double halfWidth = Math.Min(111, 23 + (cy - 25) * 1.05) * Math.Clamp((299 - cy) / 70, 0, 1);
                double center = 207 + 8 * Math.Sin(cy * .033) + 6 * Math.Sin(cy * .079);
                if (Math.Abs(cx - center) > halfWidth + random.Range(-7, 7) || random.NextDouble() < .06) continue;
                double rx = random.Range(5.6, 8.0), ry = random.Range(3.7, 5.4);
                Nests.Add(new(cx, cy, rx, ry));
            }
        for (int i = 0; i < Nests.Count; i++) CarveNest(Nests[i], 21000 + i, 0, i % 3 != 0);
    }
    private void GenerateCompactBranches()
    {
        bool[] linked = new bool[Nests.Count]; linked[0] = true;
        for (int edge = 1; edge < Nests.Count; edge++)
        {
            int from = 0, to = -1;
            double nearest = double.MaxValue;
            for (int a = 0; a < Nests.Count; a++)
            {
                if (!linked[a]) continue;
                for (int b = 0; b < Nests.Count; b++)
                {
                    if (linked[b]) continue;
                    double distance = Math.Pow(Nests[a].X - Nests[b].X, 2) + Math.Pow(Nests[a].Y - Nests[b].Y, 2);
                    if (distance < nearest) { nearest = distance; from = a; to = b; }
                }
            }
            Connect(Nests[from], Nests[to]); linked[to] = true;
        }
        for (int i = 0; i < Nests.Count; i++)
        {
            Nest nest = Nests[i];
            if (random.NextDouble() < .5)
            {
                int closest = -1; double distance = 30 * 30;
                for (int j = i + 1; j < Nests.Count; j++)
                {
                    double d = Math.Pow(nest.X - Nests[j].X, 2) + Math.Pow(nest.Y - Nests[j].Y, 2);
                    if (d < distance) { closest = j; distance = d; }
                }
                if (closest >= 0) Connect(nest, Nests[closest]);
            }
            double angle = random.Range(0, Tau);
            Branch(nest.X + Math.Cos(angle) * nest.RX * .9, nest.Y + Math.Sin(angle) * nest.RY * .9,
                angle, random.Int(5, 9), random.Range(.85, 1.1), 31000 + i, .10, 0, 1);
        }
        void Connect(Nest a, Nest b)
        {
            Branches++;
            double dx = b.X - a.X, dy = b.Y - a.Y, distance = Math.Sqrt(dx * dx + dy * dy);
            double bend = random.Range(-2.8, 2.8);
            int steps = Math.Max(1, (int)Math.Ceiling(distance * 1.5));
            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps, offset = Math.Sin(t * Math.PI) * bend;
                Ellipse(a.X + dx * t - dy / distance * offset, a.Y + dy * t + dx / distance * offset, 1.35, 1.2, true, true);
            }
        }
    }
    private void GenerateCompactIslands()
    {
        for (int i = 0; i < Nests.Count; i++)
        {
            Nest cell = Nests[i];
            if (cell.RX < 6.7 || random.NextDouble() > .32) continue;
            double x = cell.X + random.Range(-1.5, 1.5), y = cell.Y + random.Range(-1, 1);
            if (AirRatio(x, y, 4, 3) < .92) continue;
            StampIsland(new Nest(x, y, random.Range(2.2, 3.3), random.Range(1.2, 1.8)), 41000 + i, true);
            if (random.NextDouble() < .6) Ellipse(x + random.Range(-4, 4), y + random.Range(2, 3), .9, .65, false);
        }
    }
    public float[] CreateBlendWeights()
    {
        int[] distance = new int[Air.Length]; Array.Fill(distance, 1000);
        Queue<int> frontier = new();
        for (int i = 0; i < Air.Length; i++) if (Air[i]) { distance[i] = 0; frontier.Enqueue(i); }
        while (frontier.TryDequeue(out int cell))
        {
            if (distance[cell] >= 18) continue;
            int x = cell % Width, y = cell / Width;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (!Inside(x + dx, y + dy)) continue;
                    int next = Index(x + dx, y + dy);
                    if (distance[next] <= distance[cell] + 1) continue;
                    distance[next] = distance[cell] + 1; frontier.Enqueue(next);
                }
        }
        float[] weights = new float[Air.Length];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                double edge = Math.Clamp(Math.Min(Math.Min(x, Width - 1 - x), Math.Min(y, Height - 1 - y)) / 12.0, 0, 1);
                double value = Math.Clamp((18 - distance[Index(x, y)]) / 15.0, 0, 1);
                weights[Index(x, y)] = (float)(value * value * (3 - 2 * value) * edge);
            }
        return weights;
    }
    public double BlendNoise(double x, double y)
    {
        x /= 4.5; y /= 4.5;
        int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
        double fx = x - ix, fy = y - iy;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        return (Hash(ix, iy) * (1 - fx) + Hash(ix + 1, iy) * fx) * (1 - fy) +
            (Hash(ix, iy + 1) * (1 - fx) + Hash(ix + 1, iy + 1) * fx) * fy;
        double Hash(int xx, int yy)
        {
            uint n = unchecked((uint)Seed ^ (uint)xx * 374761393U ^ (uint)yy * 668265263U);
            n = unchecked((n ^ (n >> 13)) * 1274126177U);
            return (n ^ (n >> 16)) / (double)uint.MaxValue;
        }
    }

    public double SurfaceNoise(double x, double y)
    {
        int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
        double fx = x - ix, fy = y - iy;
        double u = Fade(fx), v = Fade(fy);
        double top = Gradient(ix, iy, fx, fy) * (1 - u) + Gradient(ix + 1, iy, fx - 1, fy) * u;
        double bottom = Gradient(ix, iy + 1, fx, fy - 1) * (1 - u) + Gradient(ix + 1, iy + 1, fx - 1, fy - 1) * u;
        return top * (1 - v) + bottom * v;
        static double Fade(double t) => t * t * t * (t * (t * 6 - 15) + 10);
        double Gradient(int xx, int yy, double dx, double dy)
        {
            uint hash = unchecked((uint)Seed ^ (uint)xx * 374761393U ^ (uint)yy * 668265263U);
            hash = unchecked((hash ^ (hash >> 13)) * 1274126177U);
            return ((hash ^ (hash >> 16)) & 3) switch { 0 => dx + dy, 1 => dx - dy, 2 => -dx + dy, _ => -dx - dy };
        }
    }

    public int TerrainSourceCell(int x, int y, int width, int height)
    {
        double dx = 1.8 * SurfaceNoise(x / 8.0, y / 8.0) + .45 * SurfaceNoise(x / 2.7 + 51, y / 2.7);
        double dy = 1.8 * SurfaceNoise(x / 8.0 + 127, y / 8.0 + 73) + .45 * SurfaceNoise(x / 2.7, y / 2.7 + 91);
        int sx = Math.Clamp((int)((x + dx) * Width / width), 0, Width - 1);
        int sy = Math.Clamp((int)((y + dy) * Height / height), 0, Height - 1);
        return sx + sy * Width;
    }

    private void GenerateNests()
    {
        for (int i = 0; i < MainNodes.Count; i++)
        {
            Cell node = MainNodes[i];
            int rx = random.Int(11, 21), ry = random.Int(Math.Max(5, rx / 3), Math.Max(9, (int)(rx * .72)));
            Nest nest = new(node.X, node.Y, rx, ry);
            CarveNest(nest, 100 + i, random.Int(1, 2), i % 3 == 0);
            Nests.Add(nest);
            if (random.NextDouble() < .76)
            {
                int ox = random.Int(-12, 12), oy = random.Int(-9, 9);
                nest = new(node.X + ox, node.Y + oy, Math.Max(7, rx - random.Int(2, 5)), Math.Max(4, ry - random.Int(1, 3)));
                CarveNest(nest, 900 + i, random.Int(0, 1), true);
                Nests.Add(nest);
            }
        }
        for (int i = 0; i < OuterNodes.Count; i++)
        {
            Cell node = OuterNodes[i];
            Nest nest = new(node.X, node.Y, random.Int(10, 16), random.Int(5, 9));
            CarveNest(nest, 1500 + i, random.Int(0, 1), i % 2 == 0);
            Nests.Add(nest);
        }
    }
    public static double[] SmoothNoise(double[] values, int passes)
    {
        double[] current = (double[])values.Clone(), next = new double[values.Length];
        for (int pass = 0; pass < passes; pass++)
        {
            for (int i = 0; i < current.Length; i++)
                next[i] = (current[(i + current.Length - 1) % current.Length] + 2 * current[i] + current[(i + 1) % current.Length]) / 4;
            (current, next) = (next, current);
        }
        return current;
    }
    private void CarveNest(Nest nest, int offset, int satellites, bool smoother)
    {
        var r = new PythonRandom((long)Seed + offset);
        Lobe(nest);
        for (int i = 0; i < satellites; i++)
        {
            double angle = r.Range(0, Tau), distance = r.Range(nest.RX * .08, nest.RX * .40);
            Nest satellite = new(nest.X + Math.Cos(angle) * distance,
                nest.Y + Math.Sin(angle) * distance * r.Range(.75, 1.05), nest.RX * r.Range(.34, .66), nest.RY * r.Range(.52, .92));
            Lobe(satellite);
            Ellipse((nest.X + satellite.X) / 2, (nest.Y + satellite.Y) / 2,
                Math.Max(1.5, satellite.RX * .25), Math.Max(1.2, satellite.RY * .22), true);
        }
        void Lobe(Nest lobe)
        {
            int count = r.Int(30, 46);
            double[] noise = new double[count];
            for (int i = 0; i < count; i++) noise[i] = r.Range(-1, 1);
            noise = SmoothNoise(noise, smoother ? 5 : r.Int(3, 5));
            double p1 = r.Range(0, Tau), p2 = r.Range(0, Tau);
            var vertices = new (double X, double Y)[count];
            for (int i = 0; i < count; i++)
            {
                double t = Tau * i / count;
                double radial = 1 + (smoother ? .10 : .15) * noise[i] + .11 * Math.Cos(2 * t + p1) + .07 * Math.Sin(3 * t + p2);
                double x = Math.Cos(t) * lobe.RX * radial * r.Range(.97, smoother ? 1.04 : 1.07);
                double y = Math.Sin(t) * lobe.RY * (smoother ? .88 + .20 * r.NextDouble() : .83 + .29 * r.NextDouble()) * (1 + .08 * noise[i * 3 % count]);
                x += .08 * lobe.RY * Math.Sin(t + p1);
                y += .018 * lobe.RX * Math.Cos(2 * t + p2);
                vertices[i] = (lobe.X + x, lobe.Y + y);
            }
            Polygon(vertices, true);
        }
    }

    private readonly record struct Edge(int X, int Y, int MinY, int MaxY, float Slope);

    private void Polygon((double X, double Y)[] vertices, bool air)
    {
        List<Edge> edges = new();
        int minY = Height - 1, maxY = 0;
        for (int i = 0; i < vertices.Length; i++)
        {
            var a = vertices[i]; var b = vertices[(i + 1) % vertices.Length];
            int x0 = (int)a.X, y0 = (int)a.Y, x1 = (int)b.X, y1 = (int)b.Y;
            minY = Math.Min(minY, Math.Min(y0, y1)); maxY = Math.Max(maxY, Math.Max(y0, y1));
            if (y0 == y1) { Line(Math.Min(x0, x1), y0, Math.Max(x0, x1)); continue; }
            edges.Add(new(x0, y0, Math.Min(y0, y1), Math.Max(y0, y1), (float)(x1 - x0) / (y1 - y0)));
        }
        List<float> intersections = new();
        maxY = Math.Min(Height, maxY);
        for (int y = Math.Max(0, minY); y <= maxY; y++)
        {
            intersections.Clear();
            for (int i = 0; i < edges.Count; i++)
            {
                Edge e = edges[i];
                if (y < e.MinY || y > e.MaxY) continue;
                float x = (y - e.Y) * e.Slope + e.X;
                intersections.Add(x);
                if (y == e.MaxY && y < maxY) intersections.Add(x);
                else if ((y == e.MinY || y == e.MaxY) && e.Slope != 0)
                {
                    for (int k = 0; k < i; k++)
                    {
                        Edge other = edges[k];
                        if ((y != other.MinY && y != other.MaxY) || other.Slope == 0) continue;
                        if (Round(x) != Round((y - other.Y) * other.Slope + other.X)) continue;
                        int adjacentY = y + (y == e.MaxY ? -1 : 1);
                        float adjacent = (adjacentY - e.Y) * e.Slope + e.X;
                        if (adjacentY < other.MinY || adjacentY > other.MaxY) continue;
                        float adjacentOther = (adjacentY - other.Y) * other.Slope + other.X;
                        if (x > adjacent + 1 && x > adjacentOther + 1) intersections[^1] = Round(Math.Max(adjacent, adjacentOther)) + 1;
                        else if (x < adjacent - 1 && x < adjacentOther - 1) intersections[^1] = Round(Math.Min(adjacent, adjacentOther)) - 1;
                        break;
                    }
                }
            }
            intersections.Sort();
            for (int i = 1; i < intersections.Count; i += 2)
                Line(Round(intersections[i - 1]), y, (int)(intersections[i] >= 0 ? Math.Ceiling(intersections[i] - .5) : -Math.Ceiling(-intersections[i] - .5)));
        }
        static int Round(float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);
        void Line(int left, int y, int right)
        {
            if (y < 0 || y >= Height) return;
            for (int x = Math.Max(0, left); x <= Math.Min(Width - 1, right); x++) Air[Index(x, y)] = air;
        }
    }

    private void Ellipse(double cx, double cy, double rx, double ry, bool air, bool branch = false)
    {
        for (int y = Math.Max(0, (int)(cy - ry - 1)); y <= Math.Min(Height - 1, (int)(cy + ry + 1)); y++)
            for (int x = Math.Max(0, (int)(cx - rx - 1)); x <= Math.Min(Width - 1, (int)(cx + rx + 1)); x++)
            {
                double dx = (x - cx) / Math.Max(rx, .01), dy = (y - cy) / Math.Max(ry, .01);
                if (dx * dx + dy * dy > 1) continue;
                Air[Index(x, y)] = air;
                if (branch) BranchMask[Index(x, y)] = true;
            }
    }

    private void GenerateBranches()
    {
        for (int i = 0; i < Nests.Count; i++)
        {
            Nest nest = Nests[i];
            List<double> directions = new();
            int count = random.Int(3, 5);
            for (int s = 0; s < count; s++)
            {
                double angle = random.Range(0, Tau);
                int tries = 0;
                while (directions.Exists(a => Math.Abs(Math.Atan2(Math.Sin(angle - a), Math.Cos(angle - a))) < .52) && tries < 12)
                { angle = random.Range(0, Tau); tries++; }
                directions.Add(angle);
                double surface = random.Range(.78, .98);
                Branch(nest.X + Math.Cos(angle) * nest.RX * surface, nest.Y + Math.Sin(angle) * nest.RY * surface,
                    angle + random.Range(-.32, .32), random.Int(9, 18), random.Range(.88, 1.32), 3000 + i * 10 + s, random.Range(.18, .27), 0, 2);
            }
        }
        for (int i = 0; i < 28; i++)
            Branch(random.Int(105, 320), random.Int(72, 265), random.Range(0, Tau), random.Int(6, 12), random.Range(.72, 1.05), 6000 + i, random.Range(.08, .15), 0, 1);
    }

    private void Branch(double x, double y, double theta, int steps, double initialWidth, int offset, double forkChance, int depth, int maxDepth)
    {
        Branches++;
        var r = new PythonRandom((long)Seed + offset + depth * 100000);
        double turnVelocity = r.Range(-.012, .012), width = initialWidth;
        bool enteredSolid = false;
        for (int step = 0; step < steps; step++)
        {
            int ix = (int)Math.Round(x), iy = (int)Math.Round(y);
            if (ix < 3 || iy < 3 || ix >= Width - 3 || iy >= Height - 3) break;
            if (!BaseAir[Index(ix, iy)]) enteredSolid = true;
            else if (enteredSolid && step > 5) { RejoinedBranches++; break; }
            turnVelocity = (turnVelocity + r.Range(-.052, .052)) * .86;
            theta += turnVelocity;
            width = Math.Clamp(width + r.Range(-.07, .07), initialWidth * .68, initialWidth * 1.35);
            Ellipse(x, y, width, width * r.Range(.70, .92), true, true);
            if (depth < maxDepth && step >= 5 && r.NextDouble() < forkChance)
            {
                double angle = theta + (r.Int(0, 1) * 2 - 1) * r.Range(.65, 1.28);
                int childSteps = r.Int(Math.Max(5, steps / 3), Math.Max(7, (int)(steps * .55)));
                Branch(x, y, angle, childSteps, Math.Max(.78, width * .80), offset + step * 97 + r.Int(1, 9999), forkChance * .55, depth + 1, maxDepth);
            }
            if (depth == 0 && step >= 4 && r.NextDouble() < .075)
            {
                double angle = theta + (r.Int(0, 1) * 2 - 1) * r.Range(.9, 1.55);
                Branch(x, y, angle, r.Int(4, 7), Math.Max(.68, width * .68), offset + step * 131 + r.Int(1, 9999), 0, maxDepth, maxDepth);
            }
            x += Math.Cos(theta) * r.Range(1, 1.38);
            y += Math.Sin(theta) * r.Range(1, 1.38);
        }
    }

    private void SmoothBranches()
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = Index(x, y), neighbors = 0;
                bool nearBranch = BranchMask[i];
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int adjacent = Index((x + dx + Width) % Width, (y + dy + Height) % Height);
                        if (BeforeSmoothing[adjacent]) neighbors++;
                        nearBranch |= BranchMask[adjacent];
                    }
                if (!BeforeSmoothing[i] && nearBranch && neighbors >= 6) Air[i] = true;
                else if (BeforeSmoothing[i] && BranchMask[i] && neighbors <= 1) Air[i] = false;
            }
    }

    private void GenerateIslands()
    {
        for (int i = 0; i < IslandCandidates.Count; i++)
        {
            Nest island = IslandCandidates[i];
            if (AirRatio(island.X, island.Y, island.RX + 2, island.RY + 2) < .74) continue;
            StampIsland(island, 8000 + i, false);
            if (random.NextDouble() < .65)
            {
                Nest minor = new(island.X + random.Int(-10, 10), island.Y + random.Int(-6, 6), Math.Max(3, island.RX - 2), Math.Max(2, island.RY - 1));
                if (AirRatio(minor.X, minor.Y, minor.RX + 1, minor.RY + 1) >= .82) StampIsland(minor, 9000 + i, true);
            }
        }
        Cell[] columns = [new(212, 43), new(158, 112), new(238, 141), new(312, 176), new(167, 212), new(275, 245), new(115, 246)];
        foreach (Cell cell in columns)
            if (AirRatio(cell.X, cell.Y, 4, 4) >= .85)
            { Ellipse(cell.X, cell.Y, 1.5, 2.5, false); Ellipse(cell.X, cell.Y + 3, 1, 2, false); }
    }

    private void StampIsland(Nest island, int offset, bool minor)
    {
        Islands.Add(island);
        var r = new PythonRandom((long)Seed + offset);
        int count = r.Int(9, 14);
        double[] noise = new double[count];
        for (int i = 0; i < count; i++) noise[i] = r.Range(-1, 1);
        noise = SmoothNoise(noise, 3);
        double phase = r.Range(0, Tau);
        var vertices = new (double X, double Y)[count];
        for (int i = 0; i < count; i++)
        {
            double t = Tau * i / count, sin = Math.Sin(t);
            double radial = 1 + (minor ? .12 : .15) * noise[i] + .10 * Math.Cos(2 * t + phase);
            double y = island.Y + sin * island.RY * radial * (sin < 0 ? .72 : 1);
            if (sin > .35) y += r.Range(.5, 2.8);
            vertices[i] = (island.X + Math.Cos(t) * island.RX * radial, y);
        }
        Polygon(vertices, false);
        int cuts = r.Int(1, minor ? 2 : 3);
        for (int i = 0; i < cuts; i++)
            Ellipse(island.X + r.Range(-island.RX * .6, island.RX * .6), island.Y + r.Range(-island.RY * .4, island.RY * .2), r.Range(1.2, minor ? 2 : 2.6), r.Range(.9, minor ? 1.5 : 2), true);
        int protrusions = r.Int(1, minor ? 1 : 2);
        for (int i = 0; i < protrusions; i++)
        {
            int side = r.Int(0, 2);
            if (side == 0) Ellipse(island.X + r.Range(-1.8, 1.8), island.Y + island.RY * .75, r.Range(.8, 1.6), r.Range(1.4, 3), false);
            else Ellipse(island.X + (side == 1 ? -1 : 1) * island.RX * .85, island.Y + r.Range(-1, 1), r.Range(1, 1.8), r.Range(.7, 1.4), false);
        }
    }
    private double AirRatio(double cx, double cy, double rx, double ry)
    {
        int air = 0, total = 0;
        for (int y = Math.Max(0, (int)(cy - ry)); y < Math.Min(Height, (int)(cy + ry + 1)); y++)
            for (int x = Math.Max(0, (int)(cx - rx)); x < Math.Min(Width, (int)(cx + rx + 1)); x++) { total++; if (IsAir(x, y)) air++; }
        return total == 0 ? 0 : (double)air / total;
    }
    public sealed class PythonRandom
    {
        private readonly uint[] state = new uint[624];
        private int position = 624;
        public PythonRandom(long seed)
        {
            ulong value = (ulong)Math.Abs(seed);
            uint[] key = value > uint.MaxValue ? [(uint)value, (uint)(value >> 32)] : [(uint)value];
            unchecked
            {
                state[0] = 19650218;
                for (int n = 1; n < 624; n++) state[n] = 1812433253U * (state[n - 1] ^ (state[n - 1] >> 30)) + (uint)n;
                int i = 1, j = 0;
                for (int n = Math.Max(624, key.Length); n > 0; n--)
                {
                    state[i] = (state[i] ^ ((state[i - 1] ^ (state[i - 1] >> 30)) * 1664525U)) + key[j] + (uint)j;
                    if (++i >= 624) { state[0] = state[623]; i = 1; }
                    if (++j >= key.Length) j = 0;
                }
                for (int n = 623; n > 0; n--)
                {
                    state[i] = (state[i] ^ ((state[i - 1] ^ (state[i - 1] >> 30)) * 1566083941U)) - (uint)i;
                    if (++i >= 624) { state[0] = state[623]; i = 1; }
                }
                state[0] = 0x80000000U;
            }
        }
        private uint NextUInt()
        {
            if (position == 624)
            {
                for (int i = 0; i < 624; i++)
                {
                    uint y = (state[i] & 0x80000000U) | (state[(i + 1) % 624] & 0x7fffffffU);
                    state[i] = state[(i + 397) % 624] ^ (y >> 1) ^ ((y & 1) != 0 ? 0x9908b0dfU : 0);
                }
                position = 0;
            }
            uint result = state[position++];
            result ^= result >> 11;
            result ^= (result << 7) & 0x9d2c5680U;
            result ^= (result << 15) & 0xefc60000U;
            return result ^ (result >> 18);
        }
        public double NextDouble() => ((NextUInt() >> 5) * 67108864.0 + (NextUInt() >> 6)) / 9007199254740992.0;
        public double Range(double min, double max) => min + (max - min) * NextDouble();
        public int Int(int min, int max)
        {
            uint count = (uint)(max - min + 1), value;
            int bits = 0;
            for (uint n = count; n > 0; n >>= 1) bits++;
            do { value = NextUInt() >> (32 - bits); } while (value >= count);
            return min + (int)value;
        }
    }
}
