using System;

namespace AerovelenceMod.Common.Drawing
{
    public class Rainbow
    {
        public Color FetchRainbow(float timer, float offset = 0)
        {
            float sin1 = (float)Math.Sin(MathHelper.ToRadians(timer + offset));
            float sin2 = (float)Math.Sin(MathHelper.ToRadians(timer + 120 + offset));
            float sin3 = (float)Math.Sin(MathHelper.ToRadians(timer + 240 + offset));
            int middle = 180;
            int length = 75;
            float r = middle + length * sin1;
            float g = middle + length * sin2;
            float b = middle + length * sin3;
            Color color = new Color((int)r, (int)g, (int)b);
            return color;
        }
    }
}
