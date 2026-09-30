using System;
using Microsoft.Xna.Framework;
namespace AerovelenceMod.Common.Utilities
{
    public sealed class SlimeFacetStyle
    {
        public bool InteriorLine { get; set; }
        public Color Highlight { get; set; }
        public Color AccentA { get; set; }
        public Color AccentB { get; set; }
        public Color Dark { get; set; }
        public Color Mid { get; set; }
        public Color Bright { get; set; }
    }
    public sealed class SlimeAppearance
    {
        public float PixelSize { get; set; } = 2;
        public Vector2 LightDirection { get; set; } = -Vector2.One;
        public float EdgeSmoothing { get; set; }
        public bool BodyBacklight { get; set; }
        public bool BodyHighlights { get; set; }
        public bool DitherShading { get; set; }
        public Color Outline { get; set; }
        public Color BackDark { get; set; }
        public Color BackSecondary { get; set; }
        public Color BackBright { get; set; }
        public Color BackInner { get; set; }
        public Color BackAA { get; set; }
        public Color BackHot { get; set; }
        public Color BodyDark { get; set; }
        public Color BodyAA { get; set; }

        public Color BodyMid { get; set; }
        public Color BodyHighlight { get; set; }
        public SlimeFacetStyle FacetStyle { get; set; }
        public Func<Vector2, float, Color, Color> ColorTransform { get; set; }
    }

}
