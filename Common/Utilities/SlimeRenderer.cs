using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
namespace AerovelenceMod.Common.Utilities
{
    public sealed class SlimeRenderer : IDisposable
    {
        private float PixelScale => MathHelper.Clamp(palette.PixelSize, 1, 4);
        private readonly SlimeAppearance palette;
        public SlimeRenderer(SlimeAppearance palette)
        {
            this.palette = palette ?? throw new ArgumentNullException(nameof(palette));
        }
        public SlimeAppearance Appearance => palette;
        private static readonly HashSet<SlimeRenderer> active = new();
        private Vector2 LightDirection => SafeNormalize(palette.LightDirection, -Vector2.UnitY);
        private Texture2D texture;
        private Color[] pixels = Array.Empty<Color>();
        private float[] field = Array.Empty<float>();
        private float[] bodyField = Array.Empty<float>();
        private float[] shadeAcross = Array.Empty<float>();
        private bool[] tendrilMask = Array.Empty<bool>();
        private Vector2 origin;
        private int width, height;
        private ulong lastUsed;
        private ulong lastRendered = ulong.MaxValue;
        private Color lighting;
        private float[] facetCoverage = Array.Empty<float>();
        private readonly HashSet<int> facetMask = new();
        private readonly List<int> facetPrune = new();
        private readonly List<int> facetLine = new();
        private readonly Vector2[] facetPolygon = new Vector2[5];
        public void Draw(SpriteBatch batch, SlimeShape shape, Vector2 position, Color light, float opacity)
        {
            lastUsed = Main.GameUpdateCount;
            if (lastRendered != lastUsed || texture == null)
            {
                Rasterize(shape, light);
                if (texture == null || texture.Width != width || texture.Height != height)
                {
                    texture?.Dispose();
                    texture = new Texture2D(Main.instance.GraphicsDevice, width, height);
                    active.Add(this);
                }
                texture.SetData(pixels);
                lastRendered = lastUsed;
            }
            Vector2 drawPosition = position + origin;
            drawPosition = new Vector2(MathF.Round(drawPosition.X), MathF.Round(drawPosition.Y));
            batch.Draw(texture, drawPosition, null, Color.White * opacity, 0f, Vector2.Zero, PixelScale, SpriteEffects.None, 0f);
        }
        public void Rasterize(SlimeShape shape, Color light)
        {
            lighting = light;
            Vector2 minimum = shape.Center - shape.Size - new Vector2(12f);
            Vector2 maximum = shape.Center + shape.Size + new Vector2(12f);
            for (int i = 0; i < shape.LobeCount; i++)
            {
                SlimeShape.Lobe lobe = shape.Lobes[i];
                minimum = Vector2.Min(minimum, lobe.Center - lobe.Size - new Vector2(12f));
                maximum = Vector2.Max(maximum, lobe.Center + lobe.Size + new Vector2(12f));
            }
            for (int i = 0; i < shape.SurfaceCount; i++)
            {
                minimum = Vector2.Min(minimum, shape.SurfacePoints[i] - new Vector2(14));
                maximum = Vector2.Max(maximum, shape.SurfacePoints[i] + new Vector2(14));
            }
            for (int i = 0; i < shape.TendrilCount; i++)
            {
                SlimeShape.Tendril tendril = shape.Tendrils[i];
                if (tendril.Radius < .2f) continue;
                float margin = Math.Max(tendril.Width(0f), Math.Max(tendril.Width(.5f), tendril.Width(1f))) + tendril.TipExtension + 10f;
                Vector2 tendrilMin = Vector2.Min(tendril.Root, Vector2.Min(tendril.Bend, Vector2.Min(tendril.Tip, tendril.ExtendedTip))) - new Vector2(margin);
                Vector2 tendrilMax = Vector2.Max(tendril.Root, Vector2.Max(tendril.Bend, Vector2.Max(tendril.Tip, tendril.ExtendedTip))) + new Vector2(margin);
                minimum = Vector2.Min(minimum, tendrilMin);
                maximum = Vector2.Max(maximum, tendrilMax);
            }
            origin = new Vector2(MathF.Floor(minimum.X / PixelScale) * PixelScale - 4f, MathF.Floor(minimum.Y / PixelScale) * PixelScale - 4f);
            int neededWidth = Math.Clamp((int)MathF.Ceiling((maximum.X - origin.X + 4f) / PixelScale), 16, 256);
            int neededHeight = Math.Clamp((int)MathF.Ceiling((maximum.Y - origin.Y + 4f) / PixelScale), 16, 256);
            neededWidth = (neededWidth + 15) / 16 * 16;
            neededHeight = (neededHeight + 15) / 16 * 16;
            if (neededWidth > width || neededHeight > height)
            {
                width = Math.Max(width, neededWidth);
                height = Math.Max(height, neededHeight);
                pixels = new Color[width * height];
                field = new float[pixels.Length];
                bodyField = new float[pixels.Length];
                shadeAcross = new float[pixels.Length];
                tendrilMask = new bool[pixels.Length];
                facetCoverage = new float[pixels.Length];
            }
            Array.Clear(pixels);
            Array.Clear(shadeAcross);
            Array.Clear(tendrilMask);
            Array.Fill(field, 1000f);
            Array.Fill(bodyField, 1000f);
            StampBody(shape);
            if (shape.SurfaceCount > 0) StampSurface(shape);
            Array.Copy(field, bodyField, field.Length);
            for (int i = 0; i < shape.TendrilCount; i++)
            {
                SlimeShape.Tendril tendril = shape.Tendrils[i];
                if (tendril.Radius < .2f) continue;
                Vector2 previous = tendril.Root;
                float previousWidth = tendril.Width(0f);
                for (int step = 1; step <= 20; step++)
                {
                    float t = step / 20f;
                    Vector2 point = tendril.Point(t);
                    float currentWidth = tendril.Width(t);
                    StampSegment(previous, point, previousWidth, currentWidth);
                    previous = point;
                    previousWidth = currentWidth;
                }
                if (tendril.Pad > .02f)
                {
                    Vector2 tangent = new Vector2(-tendril.PadNormal.Y, tendril.PadNormal.X) * (tendril.Width(1f) * 1.55f * tendril.Pad);
                    float radius = MathHelper.Lerp(1.5f, tendril.Width(1f) * .62f, tendril.Pad);
                    StampSegment(tendril.Tip - tangent, tendril.Tip + tangent, radius, radius);
                }
            }
            DrawGel(shape);
            if (palette.FacetStyle != null)
                for (int i = 0; i < shape.TendrilCount; i++)
                {
                    SlimeShape.Tendril tendril = shape.Tendrils[i];
                    if (tendril.TipExtension > 2f && tendril.Radius > .2f) DrawFacet(tendril, i, palette.FacetStyle);
                }
            if (shape.SurfaceCount > 0)
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        if (pixels[index].A > 0 && SlimeSurface.IsSolidAt(shape.WorldPosition + PixelPoint(x, y)))
                            pixels[index] = Color.Transparent;
                    }
        }
        private void StampSurface(SlimeShape shape)
        {
            float radius = MathHelper.Lerp(2, 5.5f, shape.SurfaceBlend);
            Vector2 previous = shape.SurfacePoints[0] + shape.SurfaceNormals[0] * (radius - 1);
            StampSegment(previous, previous, radius, radius, true);
            for (int i = 1; i < shape.SurfaceCount; i++)
            {
                Vector2 point = shape.SurfacePoints[i] + shape.SurfaceNormals[i] * (radius - 1);
                StampSegment(previous, point, radius, radius, true);
                previous = point;
            }
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (field[index] <= 0 && SlimeSurface.IsSolidAt(shape.WorldPosition + PixelPoint(x, y))) field[index] = 1000;
                }
        }
        private void StampBody(SlimeShape shape)
        {
            float bodyScale = shape.SurfaceCount > 0 ? 1f - MathHelper.Clamp(shape.SurfaceBlend, 0f, 1f) : 1f;
            if (bodyScale >= .01f) StampBodyPart(shape, shape.Center, Vector2.Max(shape.Size * bodyScale, new Vector2(.1f)), shape.BodyDome, shape.BottomCutoff, shape.RippleStrength, 0f);
            for (int i = 0; i < shape.LobeCount; i++)
            {
                SlimeShape.Lobe lobe = shape.Lobes[i];
                StampBodyPart(shape, lobe.Center, lobe.Size, lobe.BodyDome, lobe.BottomCutoff, lobe.RippleStrength, lobe.Phase);
            }
        }
        private void StampBodyPart(SlimeShape shape, Vector2 center, Vector2 size, float bodyDome, float bottomCutoff, float rippleStrength, float phase)
        {
            GetBounds(center, size + new Vector2(8f), out int left, out int top, out int right, out int bottom);
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    Vector2 offset = PixelPoint(x, y) - center;
                    Vector2 normalized = offset / size;
                    float dome = MathHelper.Clamp(bodyDome, 0f, 1f);
                    float belly = MathHelper.Clamp((normalized.Y + .08f) / 1.08f, 0f, 1f);
                    belly = belly * belly * (3f - 2f * belly);
                    float corner = MathHelper.Clamp((normalized.Y - .52f) / .48f, 0f, 1f);
                    corner = corner * corner * (3f - 2f * corner);
                    float shapedY = MathHelper.Lerp(normalized.Y, normalized.Y * .82f - .14f - belly * .03f, dome);
                    float widthScale = Math.Max(.55f, 1f + dome * (.24f * belly - .1f * corner));
                    Vector2 shaped = new(normalized.X / widthScale, shapedY);
                    float angle = MathF.Atan2(shaped.Y, shaped.X);
                    float ripple = 1f + (MathF.Sin(angle * 3f + shape.Time * 1.3f + phase) * .026f + MathF.Sin(angle * 5f - shape.Time + phase * .7f) * .012f) * Math.Max(0f, rippleStrength);
                    float distance = (shaped.Length() - ripple) * Math.Min(size.X, size.Y);
                    distance = Math.Max(distance, offset.Y - size.Y * MathHelper.Clamp(bottomCutoff, .6f, 1f));
                    int index = y * width + x;
                    if (distance >= field[index]) continue;
                    field[index] = distance;
                    shadeAcross[index] = Vector2.Dot(SafeNormalize(normalized, Vector2.UnitY), LightDirection);
                }
            }
        }
        private void StampSegment(Vector2 start, Vector2 end, float startRadius, float endRadius, bool body = false)
        {
            Vector2 delta = end - start;
            float inverseLength = 1f / Math.Max(.001f, delta.LengthSquared());
            Vector2 tangent = SafeNormalize(delta, Vector2.UnitX);
            Vector2 side = new(-tangent.Y, tangent.X);
            float margin = Math.Max(startRadius, endRadius) + 5f;
            Vector2 extent = new Vector2(Math.Abs(delta.X), Math.Abs(delta.Y)) * .5f + new Vector2(margin);
            GetBounds((start + end) * .5f, extent, out int left, out int top, out int right, out int bottom);
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    Vector2 offset = PixelPoint(x, y) - start;
                    float t = MathHelper.Clamp(Vector2.Dot(offset, delta) * inverseLength, 0f, 1f);
                    Vector2 radial = offset - delta * t;
                    float radius = MathHelper.Lerp(startRadius, endRadius, t);
                    float distance = radial.Length() - radius;
                    int index = y * width + x;
                    if (distance >= field[index])
                        continue;
                    field[index] = distance;
                    tendrilMask[index] = !body;
                    Vector2 normal = radial.LengthSquared() > .01f ? Vector2.Normalize(radial) : side;
                    shadeAcross[index] = Vector2.Dot(normal, LightDirection);
                }
            }
        }
        private void DrawGel(SlimeShape shape)
        {
            for (int y = 2; y < height - 2; y++)
            {
                for (int x = 2; x < width - 2; x++)
                {
                    int index = y * width + x;
                    float distance = field[index];
                    if (distance > 0f)
                        continue;
                    bool tendril = tendrilMask[index];
                    bool checker = palette.DitherShading && ((x + y) & 1) == 0;
                    float depth = -distance;
                    bool edge = IsFieldEdge(field, x, y);
                    bool nearEdge = !edge && (distance > -2.55f || field[index - 1] > -1.1f || field[index + 1] > -1.1f || field[index - width] > -1.1f || field[index + width] > -1.1f || field[index - width - 1] > -1.1f || field[index - width + 1] > -1.1f || field[index + width - 1] > -1.1f || field[index + width + 1] > -1.1f);
                    float shade = shadeAcross[index];
                    Color color;
                    if (edge)
                    {
                        pixels[index] = Lit(palette.Outline, 0f);
                        continue;
                    }
                    if (shade < -.43f)
                    {
                        color = palette.BodyDark;
                        if (depth > 4f && shade < -.67f && checker)
                            color = palette.Outline;
                    }
                    else if (shade < -.18f)
                    {
                        float transition = (shade + .43f) / .25f;
                        color = transition > .5f || checker && transition > .22f ? palette.BodyAA : palette.BodyDark;
                    }
                    else if (shade < .34f)
                    {
                        float transition = (shade + .18f) / .52f;
                        color = transition > .54f || checker && transition > .26f ? palette.BodyMid : palette.BodyAA;
                    }
                    else
                    {
                        color = palette.BodyMid;
                        if (HighlightCell(shape, x, y, tendril, false))
                            color = palette.BodyHighlight;
                        else if (HighlightCell(shape, x, y, tendril, true))
                            color = palette.BodyHighlight;
                    }
                    if (!tendril && nearEdge)
                    {
                        if (color == palette.BodyMid || color == palette.BodyHighlight)
                            color = palette.BodyAA;
                        else if (color == palette.BodyAA && depth > 1.9f)
                            color = palette.BodyDark;
                    }
                    pixels[index] = Lit(color);
                }
            }
            if (palette.BodyBacklight) DrawBodyBacklight(shape);
        }
        private void DrawBodyBacklight(SlimeShape shape)
        {
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index = y * width + x;
                    if (bodyField[index] > 0f || !TryGetBodyLocal(shape, PixelPoint(x, y), out Vector2 local)) continue;
                    if (local.X < .04f || local.X > .74f || local.Y < .02f || local.Y > .56f) continue;
                    float u = MathHelper.Clamp((local.X - .04f) / .7f, 0f, 1f);
                    float v = MathHelper.Clamp((local.Y - .02f) / .54f, 0f, 1f);
                    float sweep = u * .78f + v * .52f;
                    float depth = -bodyField[index];
                    bool edge = IsFieldEdge(bodyField, x, y);
                    if (edge)
                    {
                        if (sweep > .84f && v < .58f) pixels[index] = Lit(palette.BackBright, 0f);
                        else if (sweep > .63f) pixels[index] = Lit(palette.BackSecondary, 0f);
                        else if (sweep > .44f) pixels[index] = Lit(palette.BackDark, 0f);
                        continue;
                    }
                    if (depth <= 1.45f && sweep > .72f && v < .54f) { pixels[index] = Lit(palette.BackHot, .78f); continue; }
                    if (depth <= 2.25f && sweep > .58f && v < .62f) { pixels[index] = Lit(palette.BackAA, .32f); continue; }
                    if (depth <= 3.35f && sweep > .42f) pixels[index] = Lit(palette.BackInner, 0f);
                }
            }
        }
        private bool TryGetBodyLocal(SlimeShape shape, Vector2 point, out Vector2 local)
        {
            local = (point - shape.Center) / Vector2.Max(shape.Size, Vector2.One);
            if (Math.Abs(local.X) <= 1.15f && Math.Abs(local.Y) <= 1.15f) return true;
            for (int i = 0; i < shape.LobeCount; i++)
            {
                SlimeShape.Lobe lobe = shape.Lobes[i];
                local = (point - lobe.Center) / Vector2.Max(lobe.Size, Vector2.One);
                if (Math.Abs(local.X) <= 1.15f && Math.Abs(local.Y) <= 1.15f) return true;
            }
            local = Vector2.Zero;
            return false;
        }
        private bool IsFieldEdge(float[] source, int x, int y)
        {
            if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1)
                return false;
            int index = y * width + x;
            if (source[index] > 0f)
                return false;
            return source[index - 1] > 0f || source[index + 1] > 0f || source[index - width] > 0f || source[index + width] > 0f;
        }
        private bool HighlightCell(SlimeShape shape, int x, int y, bool tendril, bool accent)
        {
            if (tendril || !palette.BodyHighlights) return false;
            if (HighlightBodyCell(shape.Center, shape.Size, x, y, accent)) return true;
            for (int i = 0; i < shape.LobeCount; i++)
            {
                SlimeShape.Lobe lobe = shape.Lobes[i];
                if (HighlightBodyCell(lobe.Center, lobe.Size, x, y, accent)) return true;
            }
            return false;
        }
        private bool HighlightBodyCell(Vector2 center, Vector2 size, int x, int y, bool accent)
        {
            Vector2 primary = center + new Vector2(-size.X * .20f, -size.Y * .60f);
            Vector2 target = accent ? primary + new Vector2(size.X * .34f, size.Y * .14f) : primary;
            Vector2 raster = ToRaster(target);
            int rx = (int)MathF.Floor(raster.X + .5f);
            int ry = (int)MathF.Floor(raster.Y + .5f);
            return accent ? x == rx && y == ry : x >= rx && x <= rx + 1 && y >= ry && y <= ry + 1;
        }
        private void DrawFacet(SlimeShape.Tendril tendril, int variant, SlimeFacetStyle facet)
        {
            if (tendril.TipExtension < 6f) return;
            Vector2 axis = SafeNormalize(tendril.TipDirection, -Vector2.UnitY);
            Vector2 side = new(-axis.Y, axis.X);
            float length = tendril.TipExtension / PixelScale;
            float halfWidth = Math.Clamp(length * (variant % 3 == 1 ? .31f : .27f), 1.8f, 4.2f);
            Vector2 root = ToRaster(tendril.Tip) + new Vector2(.5f);
            Vector2 tipCenter = root + axis * length;
            float cap = MathHelper.Clamp(halfWidth * .22f, .55f, .85f);
            Vector2 tipLeft = tipCenter + side * cap;
            Vector2 tipRight = tipCenter - side * cap;
            Vector2 shoulder = root + axis * (length * .2f);
            Vector2 left = shoulder + side * halfWidth;
            Vector2 right = shoulder - side * halfWidth;
            Vector2 foot = root - axis * Math.Min(1.2f, halfWidth * .28f);
            Vector2 ridge = root + axis * (length * .2f);
            bool lightOnLeft = Vector2.Dot(side, LightDirection) >= 0f;
            Vector2 litShoulder = lightOnLeft ? left : right;
            Vector2 darkShoulder = lightOnLeft ? right : left;
            facetPolygon[0] = tipLeft;
            facetPolygon[1] = left;
            facetPolygon[2] = foot;
            facetPolygon[3] = right;
            facetPolygon[4] = tipRight;
            HashSet<int> mask = BuildFacetMask(facetPolygon);
            if (mask.Count == 0) return;
            FillFacetMask(mask, facet.Dark);
            FillFacetMasked(mask, tipCenter, litShoulder, ridge, facet.Mid, facet.Bright, variant + 1, facet);
            FillFacetMasked(mask, tipCenter, ridge, darkShoulder, facet.Mid, facet.Bright, variant + 5, facet);
            FillFacetMasked(mask, litShoulder, foot, ridge, facet.Dark, facet.Mid, variant + 9, facet);
            FillFacetMasked(mask, ridge, foot, darkShoulder, facet.Dark, facet.Mid, variant + 13, facet);
            if (facet.InteriorLine) DrawFacetInteriorLine(mask, tipCenter, ridge, foot, facet);
            DrawFacetContour(mask);
        }
        private HashSet<int> BuildFacetMask(Vector2[] polygon)
        {
            HashSet<int> mask = facetMask;
            mask.Clear();
            float minX = polygon[0].X;
            float maxX = polygon[0].X;
            float minY = polygon[0].Y;
            float maxY = polygon[0].Y;
            for (int i = 1; i < polygon.Length; i++)
            {
                minX = Math.Min(minX, polygon[i].X);
                maxX = Math.Max(maxX, polygon[i].X);
                minY = Math.Min(minY, polygon[i].Y);
                maxY = Math.Max(maxY, polygon[i].Y);
            }
            int left = Math.Max(0, (int)MathF.Floor(minX) - 1);
            int right = Math.Min(width - 1, (int)MathF.Ceiling(maxX) + 1);
            int top = Math.Max(0, (int)MathF.Floor(minY) - 1);
            int bottom = Math.Min(height - 1, (int)MathF.Ceiling(maxY) + 1);
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    Vector2 point = new(x + .5f, y + .5f);
                    if (!PointInPolygon(point, polygon)) continue;
                    int hits = 0;
                    for (int sy = 0; sy < 4; sy++)
                        for (int sx = 0; sx < 4; sx++)
                            if (PointInPolygon(new Vector2(x + (sx + .5f) * .25f, y + (sy + .5f) * .25f), polygon)) hits++;
                    if (hits < 8) continue;
                    int index = y * width + x;
                    mask.Add(index);
                    facetCoverage[index] = hits / 16f;
                }
            }
            facetPrune.Clear();
            foreach (int index in mask)
            {
                int x = index % width, y = index / width;
                int neighbors = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if ((dx != 0 || dy != 0) && x + dx >= 0 && x + dx < width && y + dy >= 0 && y + dy < height && mask.Contains((y + dy) * width + x + dx)) neighbors++;
                if (neighbors <= 1) facetPrune.Add(index);
            }
            foreach (int index in facetPrune) mask.Remove(index);
            return mask;
        }
        private static bool PointInPolygon(Vector2 point, Vector2[] polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                float denominator = b.Y - a.Y;
                if (Math.Abs(denominator) < .0001f) denominator = denominator < 0f ? -.0001f : .0001f;
                bool crosses = (a.Y > point.Y) != (b.Y > point.Y) && point.X < (b.X - a.X) * (point.Y - a.Y) / denominator + a.X;
                if (crosses) inside = !inside;
            }
            return inside;
        }
        private void FillFacetMask(HashSet<int> mask, Color color)
        {
            foreach (int index in mask) pixels[index] = Lit(color, 0f);
        }
        private void FillFacetMasked(HashSet<int> mask, Vector2 a, Vector2 b, Vector2 c, Color primary, Color secondary, int seed, SlimeFacetStyle facet)
        {
            float area = Cross(b - a, c - a);
            if (Math.Abs(area) < .5f) return;
            float winding = Math.Sign(area);
            foreach (int index in mask)
            {
                int x = index % width;
                int y = index / width;
                Vector2 point = new(x + .5f, y + .5f);
                if (Cross(b - a, point - a) * winding < 0f || Cross(c - b, point - b) * winding < 0f || Cross(a - c, point - c) * winding < 0f) continue;
                float ridge = Cross(b - a, point - a) / area;
                Color color = ridge > .68f + seed % 3 * .04f ? secondary : primary;
                pixels[index] = Lit(color, color == facet.Bright ? .18f : 0f);
            }
        }
        private void DrawFacetInteriorLine(HashSet<int> mask, Vector2 tip, Vector2 ridge, Vector2 foot, SlimeFacetStyle facet)
        {
            Vector2 start = Vector2.Lerp(tip, ridge, .22f);
            Vector2 end = Vector2.Lerp(ridge, foot, .34f);
            List<int> line = RasterLine(start, end, mask, true);
            for (int i = 0; i < line.Count; i++)
            {
                float t = line.Count <= 1 ? 0f : i / (float)(line.Count - 1);
                pixels[line[i]] = Lit(t < .28f || t > .78f ? facet.Mid : facet.Bright, t < .28f || t > .78f ? 0f : .2f);
            }
            if (line.Count == 0) return;
            int accentA = line[Math.Clamp((int)MathF.Round((line.Count - 1) * .28f), 0, line.Count - 1)];
            int highlight = line[Math.Clamp((int)MathF.Round((line.Count - 1) * .40f), 0, line.Count - 1)];
            int accentA2 = line[Math.Clamp((int)MathF.Round((line.Count - 1) * .55f), 0, line.Count - 1)];
            int accentB = line[Math.Clamp((int)MathF.Round((line.Count - 1) * .72f), 0, line.Count - 1)];
            pixels[accentA] = Lit(facet.AccentA, .95f);
            pixels[highlight] = Lit(facet.Highlight, 1f);
            pixels[accentA2] = Lit(facet.AccentA, .95f);
            pixels[accentB] = Lit(facet.AccentB, .65f);
        }
        private void DrawFacetContour(HashSet<int> mask)
        {
            Vector2 center = Vector2.Zero;
            foreach (Vector2 vertex in facetPolygon) center += vertex / facetPolygon.Length;
            foreach (int index in mask)
            {
                int x = index % width, y = index / width;
                bool boundary = x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1 || !mask.Contains(index - 1) || !mask.Contains(index + 1) || !mask.Contains(index - width) || !mask.Contains(index + width);
                if (!boundary) continue;
                Vector2 point = new(x + .5f, y + .5f);
                Vector2 outward = Vector2.UnitY;
                float nearest = float.MaxValue;
                for (int i = 0; i < facetPolygon.Length; i++)
                {
                    Vector2 a = facetPolygon[i];
                    Vector2 b = facetPolygon[(i + 1) % facetPolygon.Length];
                    Vector2 edge = b - a;
                    float t = MathHelper.Clamp(Vector2.Dot(point - a, edge) / Math.Max(.001f, edge.LengthSquared()), 0f, 1f);
                    float distance = Vector2.DistanceSquared(point, a + edge * t);
                    if (distance >= nearest) continue;
                    nearest = distance;
                    outward = SafeNormalize(new Vector2(-edge.Y, edge.X), Vector2.UnitY);
                    if (Vector2.Dot(outward, (a + b) * .5f - center) < 0f) outward = -outward;
                }
                float light = Vector2.Dot(outward, LightDirection);
                Color outline = light > .72f ? palette.BackSecondary : light > .42f ? palette.BackDark : palette.Outline;
                if (light > 0f && facetCoverage[index] < .8f) outline = Color.Lerp(outline, palette.BackDark, MathHelper.Clamp(palette.EdgeSmoothing, 0f, 1f));
                pixels[index] = Lit(outline, 0f);
            }
        }
        private List<int> RasterLine(Vector2 start, Vector2 end, HashSet<int> mask, bool interiorOnly)
        {
            List<int> result = facetLine;
            result.Clear();
            int x0 = (int)MathF.Round(start.X - .5f);
            int y0 = (int)MathF.Round(start.Y - .5f);
            int x1 = (int)MathF.Round(end.X - .5f);
            int y1 = (int)MathF.Round(end.Y - .5f);
            int dx = Math.Abs(x1 - x0);
            int dy = -Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                if (x0 >= 0 && x0 < width && y0 >= 0 && y0 < height)
                {
                    int index = y0 * width + x0;
                    if (mask.Contains(index))
                    {
                        bool boundary = x0 <= 0 || x0 >= width - 1 || y0 <= 0 || y0 >= height - 1 || !mask.Contains(index - 1) || !mask.Contains(index + 1) || !mask.Contains(index - width) || !mask.Contains(index + width);
                        if (!interiorOnly || !boundary)
                            result.Add(index);
                    }
                }
                if (x0 == x1 && y0 == y1)
                    break;
                int twiceError = error * 2;
                if (twiceError >= dy)
                {
                    error += dy;
                    x0 += sx;
                }
                if (twiceError <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
            return result;
        }
        private Vector2 ToRaster(Vector2 point) => (point - origin) / PixelScale - new Vector2(.5f);
        private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
        private static Vector2 SafeNormalize(Vector2 value, Vector2 fallback)
        {
            if (value.LengthSquared() < .0001f)
                return fallback;
            value.Normalize();
            return value;
        }
        private Color Lit(Color color, float emission = 0f) => new(color.ToVector3() * Vector3.Lerp(lighting.ToVector3(), Vector3.One, emission));
        private Vector2 PixelPoint(int x, int y) => origin + new Vector2(x * PixelScale + PixelScale * .5f, y * PixelScale + PixelScale * .5f);
        private void GetBounds(Vector2 center, Vector2 radius, out int left, out int top, out int right, out int bottom)
        {
            Vector2 minimum = (center - radius - origin) / PixelScale;
            Vector2 maximum = (center + radius - origin) / PixelScale;
            left = Math.Max(0, (int)MathF.Floor(minimum.X));
            top = Math.Max(0, (int)MathF.Floor(minimum.Y));
            right = Math.Min(width - 1, (int)MathF.Ceiling(maximum.X));
            bottom = Math.Min(height - 1, (int)MathF.Ceiling(maximum.Y));
        }
        public void Dispose()
        {
            texture?.Dispose();
            texture = null;
            active.Remove(this);
        }
        public static void Clear()
        {
            foreach (SlimeRenderer skin in new List<SlimeRenderer>(active))
                skin.Dispose();
        }
        public static void Collect()
        {
            foreach (SlimeRenderer skin in new List<SlimeRenderer>(active))
                if (Main.GameUpdateCount - skin.lastUsed > 120)
                    skin.Dispose();
        }
    }
}