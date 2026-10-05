using System;


namespace AerovelenceMod.Common.Utilities;

public static class SlimeSpikeGeometry
{
    public static float Distance(Vector2 point, Vector2 root, Vector2 tip, float halfWidth)
    {
        Vector2 delta = tip - root;
        if (delta.LengthSquared() < .01f || halfWidth < .01f) return 1000f;
        Vector2 side = Vector2.Normalize(new Vector2(-delta.Y, delta.X)) * halfWidth;
        Vector2 a = root - side, b = root + side;
        float distance = Math.Min(SegmetDistance(point, a, b), Math.Min(SegmetDistance(point, b, tip), SegmetDistance(point, tip, a)));
        float first = Cross(b - a, point - a), second = Cross(tip - b, point - b), third = Cross(a - tip, point - tip);
        bool inside = first >= 0f && second >= 0f && third >= 0f || first <= 0f && second <= 0f && third <= 0f;
        return inside ? -distance : distance;
    }

    public static Color Shade(Vector2 point, Vector2 root, Vector2 tip, float halfWidth, SlimePixelStyle style)
    {
        Vector2 axis = Vector2.Normalize(tip - root);
        Vector2 side = new(-axis.Y, axis.X);
        float across = Vector2.Dot(point - root, side) / Math.Max(.01f, halfWidth);
        float facing = Vector2.Dot(side, new Vector2(-.58f, -.82f)) * across;
        return style[facing > .07f ? 5 : facing > -.12f ? 4 : 2];
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    private static float SegmetDistance(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 delta = b - a;
        float t = MathHelper.Clamp(Vector2.Dot(point - a, delta) / Math.Max(.001f, delta.LengthSquared()), 0f, 1f);
        return Vector2.Distance(point, a + delta * t);
    }
}
