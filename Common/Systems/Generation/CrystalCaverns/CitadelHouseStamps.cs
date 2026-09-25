using System.Collections.Generic;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns;

public sealed record CitadelHouseStamp(string Id, int PreviewX, int PreviewY, string[] Rows)
{
    public int Width => Rows[0].Length;
    public int Height => Rows.Length;
    public char Cell(int x, int y) => x < 0 || y < 0 || x >= Width || y >= Height ? '.' : Rows[y][x];
    public bool CompleteDoor(int x, int y) => Cell(x, y) == 'D' && Cell(x, y - 1) == 'B' &&
        Cell(x, y + 1) == 'D' && Cell(x, y + 2) == 'D' && Cell(x, y + 3) == 'B';
    public IEnumerable<(int X, int Y)> FoundationColumns()
    {
        for (int x = 0; x < Width; x++)
            for (int y = Height - 1; y >= Height / 2; y--)
            {
                if (Cell(x, y) != '.' && !"Bs".Contains(Cell(x, y))) break;
                if ("Bs".Contains(Cell(x, y)) && ("Bs".Contains(Cell(x - 1, y)) || "Bs".Contains(Cell(x + 1, y))))
                { yield return (x, y); break; }
            }
    }
    public int StairDirection(int x, int y)
    {
        return Cell(x, y) switch
        {
            '>' => 1,
            '<' => -1,
            _ => 0
        };
    }
    public bool StairTop(int x, int y)
    {
        int direction = StairDirection(x, y);
        return direction != 0 && Cell(x - direction, y) == 'P' && StairDirection(x - direction, y - 1) != direction;
    }
}

public sealed record CitadelHouseSite(string Id, int PreviewX, int PreviewY, int Width, int Height);

public static class CitadelHouseSites
{
    public static IReadOnlyList<CitadelHouseSite> Ruins { get; } =
    [
        new("Ruined cloister", 0, 0, 25, 12),
        new("Broken gate", 0, 0, 19, 12),
        new("Ruined hall", 0, 0, 29, 12)
    ];

    public static IEnumerable<(CitadelHouseSite Stamp, double X, double Y)> ExtraSites(SilkenCitadelLayout layout)
    {
        var random = new SilkenCitadelLayout.PythonRandom((long)layout.Seed + 73129);
        var available = new List<SilkenCitadelLayout.Nest>();
        foreach (var nest in layout.Nests)
            if (nest.Y >= 112 && nest.Y <= 278) available.Add(nest);
        var selected = new List<(double X, double Y)>();
        foreach (var stamp in Houses) selected.Add((stamp.PreviewX + stamp.Width / 2.0, stamp.PreviewY + stamp.Height / 2.0));
        while (available.Count > 0)
        {
            int index = random.Int(0, available.Count - 1);
            var nest = available[index]; available.RemoveAt(index);
            if (selected.Exists(p => System.Math.Pow(p.X - nest.X, 2) + System.Math.Pow(p.Y - nest.Y, 2) < 29 * 29)) continue;
            selected.Add((nest.X, nest.Y));
            yield return (Houses[random.Int(0, Houses.Count - 1)], nest.X, nest.Y);
        }
    }

    public static IReadOnlyList<CitadelHouseSite> Houses { get; } =
    [
        new("H01", 198, 135, 25, 22),
        new("H02", 135, 140, 27, 22),
        new("H03", 169, 146, 23, 24),
        new("H04", 233, 147, 29, 27),
        new("H05", 264, 165, 19, 21),
        new("H06", 175, 167, 27, 24),
        new("H07", 130, 172, 19, 21),
        new("H08", 290, 176, 25, 23),
        new("H09", 222, 180, 21, 22),
        new("H10", 160, 204, 27, 24),
        new("H11", 208, 207, 21, 23),
        new("H12", 262, 218, 27, 24),
    ];
}

public static class CitadelBridgeShape
{
    public static bool[] Create(int width, int height, int deck = 3)
    {
        bool[] cells = new bool[width * height];
        int bays = System.Math.Max(1, width / 24);
        double bayWidth = (width - 1.0) / bays;
        for (int x = 0; x < width; x++)
        {
            double local = x == width - 1 ? 1 : (x % bayWidth) / bayWidth;
            double u = 2 * local - 1;
            int underside = deck + (int)System.Math.Round((height - deck - 2) * (1 - System.Math.Sqrt(System.Math.Max(0, 1 - u * u))));
            if (local < .07 || local > .93) underside = height - 1;
            for (int y = 0; y <= System.Math.Min(height - 1, underside); y++) cells[x + y * width] = true;
        }
        return cells;
    }
}