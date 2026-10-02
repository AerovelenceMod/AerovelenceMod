using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Utilities;

namespace AerovelenceMod.Common.Utilities;

public static class SlimeParticles
{
    public const int Maximum = 384;
    public const int SpriteSize = 16;
    private const int Columns = 16;
    private const int AtlasWidth = Columns * SpriteSize;
    private const int AtlasHeight = (Maximum / Columns) * SpriteSize;
    private static Texture2D atlas;
    private static readonly Color[] pixels = new Color[AtlasWidth * AtlasHeight];
    private static readonly Rectangle[] sources = new Rectangle[Maximum];
    private static readonly Vector2[] positions = new Vector2[Maximum];
    private static readonly List<SlimeGelParticle> particles = new(Maximum);
    private static readonly Stack<SlimeGelParticle> recycled = new();
    private static readonly UnifiedRandom random = new();
    public static IReadOnlyList<SlimeGelParticle> Active => particles;

    public static void Shed(SlimeShape shape, SlimePixelStyle style, float opacity, bool rainbow, int count)
    {
        if (Main.dedServ || Main.gameMenu || style == null) return;
        Vector2 direction = shape.Flow.LengthSquared() > .1f ? Vector2.Normalize(shape.Flow) : -Vector2.UnitY;
        Vector2 side = new(-direction.Y, direction.X);
        for (int i = 0; i < count; i++)
        {
            Vector2 local = -direction * random.NextFloat(.5f, .82f) + side * random.NextFloat(-.28f, .28f);
            Vector2 position = shape.GetWorldBodyPoint(local);
            Vector2 velocity = shape.Flow * random.NextFloat(.12f, .35f) - direction * random.NextFloat(.35f, 1.3f)
                + side * random.NextFloat(-.9f, .9f);
            float radius = random.NextFloat(1.8f, MathHelper.Clamp(shape.Size.X * .095f, 2.6f, 3.6f));
            Add(position, velocity, radius, random.Next(42, 76), style, MathHelper.Clamp(opacity + .3f, .78f, .94f), rainbow, shape.WorldPosition, shape.Time);
        }
    }

    public static void Burst(SlimeShape shape, SlimePixelStyle style, float opacity, bool rainbow, int hitDirection)
    {
        if (Main.dedServ || Main.gameMenu || style == null) return;
        float area = shape.Size.X * shape.Size.Y * 4f;
        int count = Math.Clamp((int)(area * .016f), 6, 48);
        float largest = MathHelper.Clamp(MathF.Sqrt(area) * .095f, 2.8f, 5f);
        for (int i = 0; i < count; i++)
        {
            Vector2 direction = random.NextFloat(MathHelper.TwoPi).ToRotationVector2();
            Vector2 local = direction * MathF.Sqrt(random.NextFloat()) * .8f;
            Vector2 position = shape.GetWorldBodyPoint(local);
            Vector2 velocity = direction * random.NextFloat(1.4f, 5.6f) + shape.Flow * .35f
                + new Vector2(Math.Sign(hitDirection) * random.NextFloat(.5f, 2f), -random.NextFloat(.8f, 2.8f));
            Add(position, velocity, random.NextFloat(1.8f, largest), random.Next(55, 106), style,
                MathHelper.Clamp(opacity + .3f, .8f, .96f), rainbow, shape.WorldPosition, shape.Time);
        }
    }

    private static void Add(Vector2 position, Vector2 velocity, float radius, int lifetime, SlimePixelStyle style, float opacity, bool rainbow, Vector2 origin, float time)
    {
        SlimeGelParticle particle;
        if (particles.Count >= Maximum)
        {
            particle = particles[0];
            particles.RemoveAt(0);
        }
        else particle = recycled.Count > 0 ? recycled.Pop() : new SlimeGelParticle();
        particle.Reset(position, velocity, radius, lifetime, style, opacity, rainbow, origin, time);
        particles.Add(particle);
    }

    public static void Update()
    {
        if (Main.dedServ || Main.gamePaused) return;
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            SlimeGelParticle particle = particles[i];
            particle.Update();
            if (particle.Active) continue;
            particles.RemoveAt(i);
            recycled.Push(particle);
        }
    }

    public static void Draw(SpriteBatch batch)
    {
        if (Main.dedServ || Main.gameMenu) return;
        Array.Clear(pixels);
        int count = 0;
        foreach (SlimeGelParticle particle in particles)
        {
            Rectangle bounds = particle.PixelBounds();
            if (bounds.Right < Main.screenPosition.X - 16f || bounds.Left > Main.screenPosition.X + Main.screenWidth + 16f
                || bounds.Bottom < Main.screenPosition.Y - 16f || bounds.Top > Main.screenPosition.Y + Main.screenHeight + 16f) continue;
            int left = count % Columns * SpriteSize, top = count / Columns * SpriteSize;
            Color light = Lighting.GetColor((int)(particle.Position.X / 16f), (int)(particle.Position.Y / 16f));
            Rasterize(particle, light, pixels, AtlasWidth, top * AtlasWidth + left);
            sources[count] = new Rectangle(left, top, bounds.Width / 2, bounds.Height / 2);
            positions[count] = SlimeRenderer.SnapToGrid(new Vector2(bounds.X, bounds.Y) - Main.screenPosition);
            count++;
        }
        if (count == 0) return;
        atlas ??= new Texture2D(Main.instance.GraphicsDevice, AtlasWidth, AtlasHeight);
        atlas.SetData(pixels);
        for (int i = 0; i < count; i++)
            batch.Draw(atlas, positions[i], sources[i], Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
    }

    public static void Rasterize(SlimeGelParticle particle, Color light, Color[] target, int stride, int offset = 0)
    {
        Rectangle bounds = particle.PixelBounds();
        Vector3 lighting = Vector3.Lerp(light.ToVector3(), Vector3.One, particle.LightEmission);
        for (int y = 0; y < bounds.Height / 2; y++)
            for (int x = 0; x < bounds.Width / 2; x++)
            {
                Vector2 point = new(bounds.X + x * 2 + 1, bounds.Y + y * 2 + 1);
                Color color = particle.Pixel(point);
                target[offset + y * stride + x] = color.A == 0 || SlimeSurface.IsSolidAt(point)
                    ? Color.Transparent : new Color(color.ToVector3() * lighting) * (color.A / 255f);
            }
    }

    public static void Clear()
    {
        particles.Clear();
        recycled.Clear();
    }
    public static void Unload()
    {
        Clear();
        atlas?.Dispose();
        atlas = null;
    }
}
