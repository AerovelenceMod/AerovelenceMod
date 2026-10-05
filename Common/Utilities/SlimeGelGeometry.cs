using System;


namespace AerovelenceMod.Common.Utilities;

public static class SlimeGelGeometry
{
    public static float Distance(Vector2 point, Vector2 flowDirection, float airborne, float time, float phase = 0f, float suspensionTension = 0f)
    {
        if (suspensionTension > 0f)
        {
            float tension = MathHelper.Clamp(suspensionTension, 0f, 1f);
            float underside = .86f - (.36f + tension * .18f) * MathF.Exp(-point.X * point.X * 6f);
            Vector2 bean = new(point.X, (point.Y + .04f) / .95f);
            return SmoothMax(bean.Length() - 1f, point.Y - underside, .12f);
        }
        Vector2 dome = new(point.X, (point.Y - .26f) / 1.26f);
        float resting = SmoothMax(dome.Length() - 1f, point.Y - 1f, .22f);
        if (airborne <= 0f) return resting;
        Vector2 side = new(-flowDirection.Y, flowDirection.X);
        float along = Vector2.Dot(point, flowDirection);
        float across = Vector2.Dot(point, side);
        float width = Math.Max(.65f, 1f + along * .18f - along * along * .055f);
        Vector2 fluid = new(across / width, along);
        float angle = MathF.Atan2(fluid.Y, fluid.X);
        float ripple = MathF.Sin(angle * 3f + time * .9f + phase) * .035f
            + MathF.Sin(angle * 5f - time * .63f + phase) * .018f;
        return MathHelper.Lerp(resting, fluid.Length() - 1f - ripple, MathHelper.Clamp(airborne, 0f, 1f));
    }

    public static float SmoothMin(float a, float b, float radius)
    {
        float blend = MathHelper.Clamp(.5f + .5f * (b - a) / radius, 0f, 1f);
        return MathHelper.Lerp(b, a, blend) - radius * blend * (1f - blend);
    }

    private static float SmoothMax(float a, float b, float radius) => -SmoothMin(-a, -b, radius);
}
