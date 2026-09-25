using Microsoft.Xna.Framework;
using Terraria;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns;

public readonly record struct CitadelSiteProfile(
    int CeilingDistance,
    int FloorDistance,
    int LeftDistance,
    int RightDistance,
    int OpenDirection,
    bool CeilingBound,
    bool FloorBound,
    bool LeftBound,
    bool RightBound)
{
    public int OpenWidth => LeftDistance + RightDistance;
    public int OpenHeight => CeilingDistance + FloorDistance;
    public bool Tall => OpenHeight >= OpenWidth + 8;
    public bool Wide => OpenWidth >= OpenHeight + 10;
    public bool Hanging => CeilingBound && !FloorBound && CeilingDistance <= 10;
    public bool Grounded => FloorBound && FloorDistance <= 10;
    public bool WalledLeft => LeftBound && LeftDistance <= 12;
    public bool WalledRight => RightBound && RightDistance <= 12;

    public static CitadelSiteProfile Analyze(Point center, int horizontalRange = 38, int verticalRange = 30)
    {
        int ceiling = Scan(center, 0, -1, verticalRange);
        int floor = Scan(center, 0, 1, verticalRange);
        int left = Scan(center, -1, 0, horizontalRange);
        int right = Scan(center, 1, 0, horizontalRange);

        bool ceilingBound = ceiling < verticalRange;
        bool floorBound = floor < verticalRange;
        bool leftBound = left < horizontalRange;
        bool rightBound = right < horizontalRange;

        int openDirection;
        if (leftBound && !rightBound) openDirection = 1;
        else if (rightBound && !leftBound) openDirection = -1;
        else if (right - left >= 6) openDirection = 1;
        else if (left - right >= 6) openDirection = -1;
        else openDirection = 0;

        return new CitadelSiteProfile(ceiling, floor, left, right, openDirection, ceilingBound, floorBound, leftBound, rightBound);
    }

    private static int Scan(Point center, int dx, int dy, int range)
    {
        for (int distance = 1; distance <= range; distance++)
        {
            int x = center.X + dx * distance;
            int y = center.Y + dy * distance;
            if (!WorldGen.InWorld(x, y, 10)) return distance;
            if (WorldGen.SolidTile(x, y)) return distance;
        }

        return range;
    }
}