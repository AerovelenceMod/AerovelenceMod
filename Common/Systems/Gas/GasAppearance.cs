using System;
using Microsoft.Xna.Framework;
using FieldValue = System.Numerics.Vector4;

namespace AerovelenceMod.Common.Systems.Gas;

internal static class GasAppearance
{
    public static float Envelope(int age, int duration, int fadeIn, int fadeOut)
    {
        if (age < 0 || age >= duration)
            return 0f;
        float start = fadeIn <= 0 ? 1f : Math.Clamp(age / (float)fadeIn, 0f, 1f);
        float end = fadeOut <= 0 ? 1f : Math.Clamp((duration - age) / (float)fadeOut, 0f, 1f);
        return MathHelper.SmoothStep(0f, 1f, start) * MathHelper.SmoothStep(0f, 1f, end);
    }

    public static FieldValue Cubic(FieldValue a, FieldValue b, FieldValue c, FieldValue d, float t)
    {
        return Cubic(a, b, c, d, CubicWeights(t));
    }

    public static FieldValue CubicWeights(float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return new FieldValue(-7f * t3 + 15f * t2 - 9f * t + 1f,
            21f * t3 - 36f * t2 + 16f,
            -21f * t3 + 27f * t2 + 9f * t + 1f,
            7f * t3 - 6f * t2);
    }

    public static FieldValue Cubic(FieldValue a, FieldValue b, FieldValue c, FieldValue d, FieldValue weights)
        => (a * weights.X + b * weights.Y + c * weights.Z + d * weights.W) / 18f;

    public static Color Shade(FieldValue value, Vector3 ambient) => Shade(value, ambient, new FieldValue(value.W, 0f, 0f, value.W));

    public static Color Shade(FieldValue value, Vector3 ambient, FieldValue appearance)
    {
        if (value.W <= 0.003f || appearance.X <= 0f)
            return Color.Transparent;
        float opacity = 1f - MathF.Exp(-appearance.X * 0.78f);
        Vector3 pigment = Vector3.Clamp(new Vector3(value.X, value.Y, value.Z) / value.W, Vector3.Zero, Vector3.One);
        Vector3 lighting = Vector3.Clamp(ambient, Vector3.Zero, Vector3.One);
        lighting = Vector3.Lerp(lighting, Vector3.One, Math.Clamp(appearance.Y / value.W, 0f, 1f));
        Vector3 rgb = pigment * lighting * opacity * 0.97f * Math.Clamp(appearance.W / value.W, 0f, 3f);
        return new Color(new Vector4(Vector3.Clamp(rgb, Vector3.Zero, Vector3.One), opacity * 0.76f));
    }
    public static Color ShadeAdditive(FieldValue value) => ShadeAdditive(value, new FieldValue(value.W, 0f, 0f, value.W));

    public static Color ShadeAdditive(FieldValue value, FieldValue appearance)
        => ShadeAdditive(value, Vector3.One, appearance);

    public static Color ShadeAdditive(FieldValue value, Vector3 ambient, FieldValue appearance)
    {
        if (value.W <= 0.003f || appearance.X <= 0f)
            return Color.Transparent;
        float opacity = 1f - MathF.Exp(-appearance.X * 0.95f);
        Vector3 pigment = Vector3.Clamp(new Vector3(value.X, value.Y, value.Z) / value.W, Vector3.Zero, Vector3.One);
        Vector3 rgb = Vector3.Lerp(pigment, Vector3.One, opacity * 0.3f) * (0.72f + opacity * 0.28f) * Math.Clamp(appearance.W / value.W, 0f, 3f);
        rgb *= Vector3.Lerp(Vector3.Clamp(ambient, Vector3.Zero, Vector3.One), Vector3.One, Math.Clamp(appearance.Y / value.W, 0f, 1f));
        return new Color(new Vector4(Vector3.Clamp(rgb * (opacity * 0.72f), Vector3.Zero, Vector3.One), 0f));
    }

    public static Vector3 Light(FieldValue value, FieldValue appearance)
    {
        if (value.W <= 0.003f || appearance.Z <= 0f)
            return Vector3.Zero;
        Vector3 pigment = Vector3.Clamp(new Vector3(value.X, value.Y, value.Z) / value.W, Vector3.Zero, Vector3.One);
        float strength = (1f - MathF.Exp(-appearance.Z)) * Math.Clamp(appearance.W / value.W, 0f, 3f);
        return Vector3.Clamp(pigment * strength, Vector3.Zero, Vector3.One);
    }
}
