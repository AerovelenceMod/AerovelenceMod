using System;
using System.Collections.Generic;




namespace AerovelenceMod.Common.Utilities;

public enum SlimeMaterial
{
    Gel,
    Ice,
    Lava
}


public sealed class SlimePixelStyle
{
    //this is sort of done silly but i didnt feel like sampling
    private static readonly Color[] blue =
    [
        new(18, 34, 89),
        new(22, 41, 81),
        new(25, 51, 109),
        new(32, 66, 140),
        new(37, 81, 170),
        new(49, 111, 170)
    ];
    private readonly Color[] colors;
    public SlimeMaterial Material { get; }
    public Color Backlight { get; }
    public Color OutlineLight { get; }
    public bool JoinHighlights { get; }
    public float ShadowWidth { get; }
    public IReadOnlyList<Color> Colors { get; }
    public static SlimePixelStyle Blue { get; } = new(blue);
    public static SlimePixelStyle Baby { get; } = new(
    [
        new(45, 45, 45), new(44, 44, 44), new(57, 57, 57),
        new(73, 73, 73), new(88, 88, 88), new(119, 119, 119)
    ], backlight: new(104, 104, 104), joinHighlights: true, shadowWidth: .43f);
    public static SlimePixelStyle Black { get; } = new(
    [
        new(38, 38, 38), new(36, 36, 36), new(47, 47, 47),
        new(61, 61, 61), new(73, 73, 73), new(91, 91, 91)
    ], backlight: new(77, 77, 77));
    public static SlimePixelStyle Green { get; } = new(
    [
        new(16, 132, 40), new(17, 111, 35), new(17, 150, 44),
        new(21, 193, 55), new(25, 232, 63), new(109, 255, 109)
    ], backlight: new(54, 220, 54), joinHighlights: true);
    public static SlimePixelStyle Jungle { get; } = new(
    [
        new(25, 133, 43), new(27, 110, 33), new(30, 147, 32),
        new(78, 189, 38), new(122, 217, 56), new(168, 223, 88)
    ], backlight: new(91, 232, 106));
    public static SlimePixelStyle Mother { get; } = new(
    [
        new(45, 45, 45), new(44, 44, 44), new(57, 57, 57),
        new(73, 73, 73), new(88, 88, 88), new(104, 104, 104)
    ]);
    public static SlimePixelStyle Pinky { get; } = new(
    [
        new(133, 0, 27), new(122, 7, 69), new(163, 2, 49),
        new(210, 2, 103), new(255, 0, 79), new(244, 88, 164)
    ]);
    public static SlimePixelStyle Purple { get; } = new(
    [
        new(89, 15, 127), new(81, 16, 97), new(108, 16, 153),
        new(139, 21, 168), new(165, 23, 204), new(163, 68, 227)
    ]);
    public static SlimePixelStyle Red { get; } = new(
    [
        new(133, 30, 0), new(122, 7, 27), new(163, 2, 34),
        new(210, 2, 32), new(255, 31, 0), new(255, 96, 74)
    ]);
    public static SlimePixelStyle Yellow { get; } = new(
    [
        new(202, 140, 36), new(180, 115, 7), new(211, 159, 13),
        new(227, 179, 7), new(232, 213, 9), new(244, 255, 0)
    ], outlineLight: new(173, 129, 5));
    public static SlimePixelStyle Dungeon { get; } = new(
    [
        new(54, 43, 91), new(49, 44, 82), new(63, 55, 111),
        new(80, 71, 142), new(95, 85, 172), new(107, 112, 181)
    ]);
    public static SlimePixelStyle Rainbow { get; } = new(
    [
        new(78, 78, 78), new(75, 75, 75), new(97, 97, 97),
        new(125, 125, 125), new(150, 150, 150), new(170, 170, 170)
    ]);
    public static SlimePixelStyle Ice { get; } = new(
    [
        new(51, 99, 145), new(19, 43, 65), new(48, 89, 122),
        new(69, 136, 166), new(124, 200, 218), new(225, 249, 249)
    ], SlimeMaterial.Ice);
    public static SlimePixelStyle Lava { get; } = new(
    [
        new(135, 29, 13), new(103, 18, 0), new(153, 42, 25),
        new(197, 48, 25), new(244, 112, 25), new(255, 224, 111)
    ], SlimeMaterial.Lava);
    public Color this[int shade] => colors[Math.Clamp(shade, 0, colors.Length - 1)];

    private SlimePixelStyle(Color[] colors, SlimeMaterial material = SlimeMaterial.Gel,
        Color? backlight = null, Color? outlineLight = null, bool joinHighlights = false, float shadowWidth = .49f)
    {
        this.colors = colors;
        Material = material;
        Backlight = backlight ?? colors[5];
        OutlineLight = outlineLight ?? colors[2];
        JoinHighlights = joinHighlights;
        ShadowWidth = shadowWidth;
        List<Color> unique = new(colors);
        if (!unique.Contains(Backlight)) unique.Add(Backlight);
        if (!unique.Contains(OutlineLight)) unique.Add(OutlineLight);
        Colors = unique.AsReadOnly();
    }

    public static SlimePixelStyle ForVanilla(int netId, Color fallback, bool rainbow = false) => rainbow ? Rainbow : netId switch
    {
        NPCID.BabySlime => Baby,
        NPCID.BlackSlime => Black,
        NPCID.BlueSlime => Blue,
        NPCID.GreenSlime => Green,
        NPCID.JungleSlime => Jungle,
        NPCID.MotherSlime => Mother,
        NPCID.Pinky => Pinky,
        NPCID.PurpleSlime => Purple,
        NPCID.RedSlime => Red,
        NPCID.YellowSlime => Yellow,
        NPCID.DungeonSlime => Dungeon,
        NPCID.RainbowSlime => Rainbow,
        NPCID.LavaSlime => Lava,
        NPCID.IceSlime => Ice,
        NPCID.SpikedIceSlime => Ice,
        NPCID.SlimeSpiked => Blue,
        _ => Recolor(fallback)
    };

    public static SlimePixelStyle Recolor(Color target)
    {
        Vector3 source = ToHsl(blue[4]);
        Vector3 tint = ToHsl(target);
        Vector3 targetRgb = target.ToVector3();
        float brightness = Math.Max(targetRgb.X, Math.Max(targetRgb.Y, targetRgb.Z));
        Color[] result = new Color[blue.Length];
        for (int i = 0; i < result.Length; i++)
        {
            Vector3 shade = ToHsl(blue[i]);
            float hue = Wrap(tint.X + shade.X - source.X);
            float saturation = MathHelper.Clamp(shade.Y * tint.Y / source.Y, 0f, 1f);
            Vector3 referenceRgb = blue[i].ToVector3();
            float value = Math.Max(referenceRgb.X, Math.Max(referenceRgb.Y, referenceRgb.Z)) * brightness / (170f / 255f);
            float lightness = saturation < .001f ? shade.Z * brightness / source.Z : value / (1f + saturation);
            lightness = MathHelper.Clamp(lightness, 0f, 1f);
            result[i] = Main.hslToRgb(hue, saturation, lightness);
        }
        return new SlimePixelStyle(result);
    }

    private static Vector3 ToHsl(Color color)
    {
        Vector3 rgb = color.ToVector3();
        float minimum = Math.Min(rgb.X, Math.Min(rgb.Y, rgb.Z));
        float maximum = Math.Max(rgb.X, Math.Max(rgb.Y, rgb.Z));
        float range = maximum - minimum;
        float lightness = (maximum + minimum) * .5f;
        if (range < .0001f) return new Vector3(0f, 0f, lightness);
        float hue = maximum == rgb.X ? (rgb.Y - rgb.Z) / range : maximum == rgb.Y ? (rgb.Z - rgb.X) / range + 2f : (rgb.X - rgb.Y) / range + 4f;
        return new Vector3(Wrap(hue / 6f), range / (1f - Math.Abs(2f * lightness - 1f)), lightness);
    }

    private static float Wrap(float value) => value - MathF.Floor(value);
}
