using System;
using System.Collections.Generic;
using AerovelenceMod.Common.Systems;
using AerovelenceMod.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Utilities;

public sealed class LightningStrokeRenderer : IDisposable
{
    private const int PixelSize = 2;
    private static readonly HashSet<LightningStrokeRenderer> active = new();
    private static readonly float[] haloFalloff = CreateHaloFalloff();
    private static readonly List<(LightningStrokeRenderer Renderer, LightningUtils.LightningData Data)> paths = new();
    private static int pathIndex;
    private Texture2D texture;
    private Color[] pixels;
    private Vector2 origin;
    private int geometryHash;
    private ulong lastUsed;

    public void Draw(LightningUtils.LightningData data, float opacity, float widthScale = 1, RenderLayer layer = RenderLayer.OverPlayers)
    {
        if (!Prepare(data, opacity, widthScale, false)) return;
        Texture2D stroke = texture;
        Vector2 position = origin;
        Color tint = Color.White * MathHelper.Clamp(opacity, 0, 1);
        ModContent.GetInstance<NewPixelationSystem>().QueueRenderAction(layer, () =>
        {
            if (!stroke.IsDisposed)
                Main.spriteBatch.Draw(stroke, position - Main.screenPosition, null, tint, 0,
                    Vector2.Zero, PixelSize, SpriteEffects.None, 0);
        });
    }

    public void DrawImmediate(LightningUtils.LightningData data, SpriteBatch spriteBatch, float opacity,
        float widthScale = 1, float screenScale = 1, bool additive = false)
    {
        if (!Prepare(data, opacity, widthScale, additive)) return;
        Color tint = Color.White * MathHelper.Clamp(opacity, 0, 1);
        if (additive) tint.A = 255;
        spriteBatch.Draw(texture, (origin - Main.screenPosition) * screenScale, null, tint, 0,
            Vector2.Zero, PixelSize * screenScale, SpriteEffects.None, 0);
    }

    public static void DrawPath(SpriteBatch spriteBatch, Vector2[] points, Color color, float opacity, float startWidth, float endWidth, float bloom = 1, float screenScale = 1, bool additive = false)
    {
        if (Main.dedServ || points == null || points.Length < 2 || opacity <= 0) return;
        if (pathIndex == paths.Count)
            paths.Add((new LightningStrokeRenderer(), new LightningUtils.LightningData(Vector2.Zero, Vector2.UnitY)));
        var entry = paths[pathIndex++];
        entry.Data.SegmentPositions = points;
        entry.Data.StartThickness = startWidth;
        entry.Data.EndThickness = endWidth;
        entry.Data.CoreColorOverride = Color.Lerp(color, Color.White, .9f);
        entry.Data.OuterColorOverride = color;
        entry.Data.GlowIntensity = .4f * Math.Max(0, bloom);
        entry.Renderer.DrawImmediate(entry.Data, spriteBatch, opacity, screenScale: screenScale, additive: additive);
    }

    private bool Prepare(LightningUtils.LightningData data, float opacity, float widthScale, bool additive)
    {
        if (Main.dedServ || data?.SegmentPositions == null || data.SegmentPositions.Length < 2 || opacity <= 0) return false;
        lastUsed = Main.GameUpdateCount;
        HashCode hash = new();
        hash.Add(widthScale);
        hash.Add(additive);
        hash.Add(data.StartThickness);
        hash.Add(data.EndThickness);
        hash.Add(data.BranchThicknessMultiplier);
        hash.Add(data.BranchTipThicknessMultiplier);
        hash.Add(data.CoreColorOverride);
        hash.Add(data.MidColorOverride);
        hash.Add(data.OuterColorOverride);
        hash.Add(data.GlowIntensity);
        hash.Add(data.GlowScale);
        foreach (Vector2 point in data.SegmentPositions) hash.Add(point);
        if (data.Branches != null)
            foreach (LightningUtils.Branch branch in data.Branches)
            {
                hash.Add(branch.Alpha);
                hash.Add(branch.ParentProgress);
                foreach (Vector2 point in branch.Positions) hash.Add(point);
            }
        int nextHash = hash.ToHashCode();
        if (texture == null || nextHash != geometryHash)
        {
            Rebuild(data, widthScale, additive);
            geometryHash = nextHash;
        }
        return true;
    }

    private void Rebuild(LightningUtils.LightningData data, float widthScale, bool additive)
    {
        Vector2 minimum = data.SegmentPositions[0];
        Vector2 maximum = minimum;
        foreach (Vector2 point in data.SegmentPositions) Include(point);
        if (data.Branches != null)
            foreach (LightningUtils.Branch branch in data.Branches)
                foreach (Vector2 point in branch.Positions) Include(point);
        float glowSpread = MathHelper.Clamp(data.GlowScale / .15f, .25f, 3);
        float glowStrength = Math.Max(0, data.GlowIntensity / .4f);
        float padding = (Math.Max(data.StartThickness, data.EndThickness) * widthScale * 2 + 22) * Math.Max(1, glowSpread);
        origin = new Vector2(MathF.Floor((minimum.X - padding) / PixelSize), MathF.Floor((minimum.Y - padding) / PixelSize)) * PixelSize;
        int width = Math.Clamp((int)MathF.Ceiling((maximum.X + padding - origin.X) / PixelSize), 1, 2048);
        int height = Math.Clamp((int)MathF.Ceiling((maximum.Y + padding - origin.Y) / PixelSize), 1, 2048);
        width = (width + 31) / 32 * 32;
        height = (height + 31) / 32 * 32;
        if (texture == null || texture.Width != width || texture.Height != height)
        {
            texture?.Dispose();
            texture = new Texture2D(Main.graphics.GraphicsDevice, width, height);
            pixels = new Color[width * height];
            active.Add(this);
        }
        else Array.Clear(pixels);

        Color core = data.CoreColorOverride ?? Color.Yellow;
        Color glow = data.OuterColorOverride ?? new Color(100, 180, 255);
        if (data.MidColorOverride.HasValue) glow = Color.Lerp(glow, data.MidColorOverride.Value, .3f);
        Rasterize(pixels, width, height, origin, PixelSize, data.SegmentPositions,
            data.StartThickness * widthScale, data.EndThickness * widthScale, core, glow, 1, glowStrength, glowSpread);
        if (data.Branches != null)
            foreach (LightningUtils.Branch branch in data.Branches)
            {
                float root = data.ThicknessAt(branch.ParentProgress) * data.BranchThicknessMultiplier * widthScale;
                Rasterize(pixels, width, height, origin, PixelSize, branch.Positions, root,
                    Math.Max(.6f, root * data.BranchTipThicknessMultiplier), core, glow, branch.Alpha, glowStrength, glowSpread);
            }
        if (additive)
            for (int i = 0; i < pixels.Length; i++)
                if (pixels[i].PackedValue != 0) pixels[i].A = 255;
        texture.SetData(pixels);

        void Include(Vector2 point)
        {
            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }
    }

    internal static void Rasterize(Color[] pixels, int width, int height, Vector2 origin, float pixelSize, Vector2[] points, float startWidth, float endWidth, Color core, Color glow, float opacity, float glowStrength = 1, float glowSpread = 1)
    {
        float totalLength = 0;
        for (int i = 1; i < points.Length; i++) totalLength += Vector2.Distance(points[i - 1], points[i]);
        if (totalLength < .01f) return;
        Vector3 coreLight = core.ToVector3() * opacity;
        Vector3 glowLight = glow.ToVector3() * opacity;
        float traveled = 0;
        for (int i = 1; i < points.Length; i++)
        {
            Vector2 start = points[i - 1];
            Vector2 delta = points[i] - start;
            float lengthSquared = delta.LengthSquared();
            if (lengthSquared < .0001f) continue;
            float length = MathF.Sqrt(lengthSquared);
            float inverseLengthSquared = 1 / lengthSquared;
            float radiusStart = MathHelper.Lerp(startWidth, endWidth, traveled / totalLength) * .5f;
            float radiusEnd = MathHelper.Lerp(startWidth, endWidth, (traveled + length) / totalLength) * .5f;
            float maximumRadius = Math.Max(radiusStart, radiusEnd);
            float extent = maximumRadius + (2.5f + maximumRadius * .5f) * glowSpread * 4 + pixelSize;
            Vector2 minimum = (Vector2.Min(start, points[i]) - new Vector2(extent) - origin) / pixelSize;
            Vector2 maximum = (Vector2.Max(start, points[i]) + new Vector2(extent) - origin) / pixelSize;
            int left = Math.Max(0, (int)MathF.Floor(minimum.X));
            int right = Math.Min(width - 1, (int)MathF.Ceiling(maximum.X));
            int top = Math.Max(0, (int)MathF.Floor(minimum.Y));
            int bottom = Math.Min(height - 1, (int)MathF.Ceiling(maximum.Y));
            for (int y = top; y <= bottom; y++)
            {
                float relativeY = origin.Y + (y + .5f) * pixelSize - start.Y;
                for (int x = left; x <= right; x++)
                {
                    float relativeX = origin.X + (x + .5f) * pixelSize - start.X;
                    float along = MathHelper.Clamp((relativeX * delta.X + relativeY * delta.Y) * inverseLengthSquared, 0, 1);
                    float radius = MathHelper.Lerp(radiusStart, radiusEnd, along);
                    float offsetX = relativeX - delta.X * along;
                    float offsetY = relativeY - delta.Y * along;
                    float distanceSquared = offsetX * offsetX + offsetY * offsetY;
                    float falloff = (2.5f + radius * .5f) * glowSpread;
                    float cutoff = radius + falloff * 4;
                    if (distanceSquared > cutoff * cutoff) continue;
                    float distance = MathF.Sqrt(distanceSquared);
                    float coverage = MathHelper.Clamp((radius - distance) / pixelSize + .5f, 0, 1);
                    float outside = Math.Max(0, distance - radius);
                    float halo = haloFalloff[Math.Min(128, (int)(outside / falloff * 32))];
                    Vector3 light = coreLight * coverage + glowLight * halo * (1 - coverage) * glowStrength;
                    Color color = new(light) { A = 0 };
                    int index = y * width + x;
                    Color previous = pixels[index];
                    pixels[index] = new Color(Math.Max(previous.R, color.R), Math.Max(previous.G, color.G), Math.Max(previous.B, color.B), 0);
                }
            }
            traveled += length;
        }
    }

    private static float[] CreateHaloFalloff()
    {
        float[] values = new float[129];
        for (int i = 0; i < values.Length; i++) values[i] = .36f * MathF.Exp(-.5f * i * i / (32 * 32));
        return values;
    }

    public void Dispose()
    {
        texture?.Dispose();
        texture = null;
        pixels = null;
        active.Remove(this);
    }

    internal static void Clear()
    {
        foreach (LightningStrokeRenderer renderer in new List<LightningStrokeRenderer>(active)) renderer.Dispose();
        paths.Clear();
        pathIndex = 0;
    }

    internal static void EndFrame()
    {
        pathIndex = 0;
        foreach (LightningStrokeRenderer renderer in new List<LightningStrokeRenderer>(active))
            if (Main.GameUpdateCount - renderer.lastUsed > 120) renderer.Dispose();
    }
}

public sealed class LightningStrokeSystem : ModSystem
{
    public override void PostDrawInterface(SpriteBatch spriteBatch) => LightningStrokeRenderer.EndFrame();
    public override void OnWorldUnload() => Main.QueueMainThreadAction(LightningStrokeRenderer.Clear);
    public override void Unload() => Main.QueueMainThreadAction(LightningStrokeRenderer.Clear);
}