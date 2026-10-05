using System;



namespace AerovelenceMod.Common.Utilities;

public sealed class SlimeGelParticle
{
    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public float Radius { get; private set; }
    public int Age { get; private set; }
    public int Lifetime { get; private set; }
    public int Bounces { get; private set; }
    public bool Resting { get; private set; }
    public bool Rainbow { get; private set; }
    public float LightEmission => style?.Material == SlimeMaterial.Lava ? .8f : .32f;
    public bool Active => Age < Lifetime && Radius >= .5f;
    private SlimePixelStyle style;
    private float initialRadius;
    private float opacity;
    private float phase;
    private float compression;
    private float compressionVelocity;
    private Vector2 colorPoint;
    private Vector2 groundPoint;
    private Vector2 groundNormal;
    private Vector2 RenderCenter => SlimeRenderer.SnapToGrid(Position - Vector2.One) + Vector2.One;
    private float RenderRadius => Math.Max(1f, MathF.Round(Radius));
    private Vector2 Axis => !Resting && Velocity.LengthSquared() > .1f ? Vector2.Normalize(Velocity) : Vector2.UnitY;
    private float Stretch => (1f + MathHelper.Clamp(Velocity.Length() * .07f, 0f, .65f)) * (1f - compression);

    public void Reset(Vector2 position, Vector2 velocity, float radius, int lifetime, SlimePixelStyle palette, float alpha, bool rainbow, Vector2 origin, float time)
    {
        Position = position;
        Velocity = velocity;
        Radius = initialRadius = MathHelper.Clamp(radius, .6f, 5f);
        Lifetime = Math.Max(12, lifetime);
        style = palette;
        opacity = MathHelper.Clamp(alpha, 0f, 1f);
        Rainbow = rainbow;
        colorPoint = position - origin;
        phase = time;
        Age = Bounces = 0;
        Resting = false;
        compression = compressionVelocity = 0f;
    }

    public void Update()
    {
        if (!Active) return;
        Age++;
        Radius = initialRadius * MathF.Sqrt(Math.Max(0f, 1f - Age / (float)Lifetime));
        compressionVelocity = (compressionVelocity - compression * .2f) * .74f;
        compression = MathHelper.Clamp(compression + compressionVelocity, 0f, .48f);
        if (Resting)
        {
            Position = groundPoint + groundNormal * (Radius * .65f + .25f);
            return;
        }
        Velocity = new Vector2(Velocity.X * .985f, Math.Min(Velocity.Y + .22f, 12f));
        float distance = Velocity.Length();
        float collisionRadius = Math.Max(.6f, Radius * .65f);
        bool contact = SlimeSurface.TryContact(Position, Velocity, distance + collisionRadius, out Vector2 point, out Vector2 normal);
        if (TryPlatform(Position, Velocity, collisionRadius, out Vector2 platform)
            && (!contact || Vector2.DistanceSquared(Position, platform) < Vector2.DistanceSquared(Position, point)))
        {
            contact = true;
            point = platform;
            normal = -Vector2.UnitY;
        }
        if (!contact)
        {
            Position += Velocity;
            return;
        }
        Position = point + normal * (collisionRadius + .25f);
        float incoming = Vector2.Dot(Velocity, normal);
        if (incoming >= 0f) return;
        Vector2 tangent = Velocity - normal * incoming;
        Velocity = tangent * .67f - normal * incoming * .32f;
        compressionVelocity += MathHelper.Clamp(-incoming * .07f, .03f, .4f);
        Bounces++;
        if (normal.Y < -.25f && (-incoming < .85f || Velocity.LengthSquared() < .15f))
        {
            Resting = true;
            Velocity = Vector2.Zero;
            groundPoint = point;
            groundNormal = normal;
        }
    }

    private static bool TryPlatform(Vector2 position, Vector2 velocity, float radius, out Vector2 point)
    {
        point = position;
        if (velocity.Y <= 0f) return false;
        float startY = position.Y + radius;
        float endY = startY + velocity.Y;
        int first = (int)MathF.Ceiling(startY / 16f), last = (int)MathF.Floor(endY / 16f);
        for (int y = first; y <= last; y++)
        {
            float t = (y * 16f - startY) / velocity.Y;
            float x = position.X + velocity.X * t;
            int tileX = (int)MathF.Floor(x / 16f);
            if (!WorldGen.InWorld(tileX, y, 1)) continue;
            Tile tile = Main.tile[tileX, y];
            if (!tile.HasUnactuatedTile || !Main.tileSolidTop[tile.TileType] || tile.TileFrameY != 0) continue;
            point = new Vector2(x, y * 16f);
            return true;
        }
        return false;
    }

    public Rectangle PixelBounds()
    {
        Vector2 axis = Axis;
        Vector2 side = new(-axis.Y, axis.X);
        Vector2 extent = (new Vector2(Math.Abs(axis.X), Math.Abs(axis.Y)) * Stretch
            + new Vector2(Math.Abs(side.X), Math.Abs(side.Y)) / Stretch) * RenderRadius;
        Vector2 center = RenderCenter;
        int left = (int)MathF.Floor((center.X - extent.X - 1f) / 2f) * 2;
        int top = (int)MathF.Floor((center.Y - extent.Y - 1f) / 2f) * 2;
        int right = (int)MathF.Ceiling((center.X + extent.X + 1f) / 2f) * 2;
        int bottom = (int)MathF.Ceiling((center.Y + extent.Y + 1f) / 2f) * 2;
        return new Rectangle(left, top, right - left, bottom - top);
    }

    public Color Pixel(Vector2 point)
    {
        if (!Active || style == null) return Color.Transparent;
        Vector2 offset = (point - RenderCenter) / RenderRadius;
        Vector2 axis = Axis;
        Vector2 side = new(-axis.Y, axis.X);
        Vector2 local = axis * (Vector2.Dot(offset, axis) / Stretch) + side * (Vector2.Dot(offset, side) * Stretch);
        float distance = local.Length();
        if (distance > 1.05f) return Color.Transparent;
        float light = Vector2.Dot(local, new Vector2(-.58f, -.82f));
        int shade = distance > .72f ? light > .05f ? 2 : 1 : light > .18f ? 5 : light > -.25f ? 4 : 2;
        Color color = style[shade];
        if (Rainbow)
        {
            float brightness = MathHelper.Clamp(color.ToVector3().Length() / MathF.Sqrt(3f), .06f, .8f);
            float hue = ((phase + Age * .035f) * .035f + colorPoint.X * .009f + colorPoint.Y * .014f + 2f) % 1f;
            float shift = MathHelper.Lerp(.012f, -.008f, MathHelper.Clamp(brightness * 2f, 0f, 1f));
            color = Main.hslToRgb((hue + shift + 1f) % 1f, .76f, brightness);
        }
        float fade = MathHelper.Clamp((Lifetime - Age) / 18f, 0f, 1f);
        return color with { A = (byte)(255f * opacity * fade) };
    }
}
