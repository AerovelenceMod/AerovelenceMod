using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace AerovelenceMod.Common.Utilities;

public sealed class VanillaSlimeVisual : IDisposable
{
    private sealed record SpritePalette(Vector2 Size, Color[] Shades);
    private static readonly Dictionary<int, SpritePalette> sprites = new();
    private readonly SlimeShape shape = new(1, 3) { BodyDome = .72f, BottomCutoff = .98f, RippleStrength = .35f };
    private readonly SlimeRenderer skin = new(new SlimeAppearance
    {
        PixelSize = 2,
        LightDirection = new Vector2(-.58f, -.82f),
        EdgeSmoothing = .45f,
        BodyBacklight = true,
        BodyHighlights = true
    });
    private Vector2 bodySize;
    private Vector2 bodySizeVelocity;
    private Vector2 bodyOffset;
    private Vector2 bodyOffsetVelocity;
    private Vector2 previousVelocity;
    private Vector2 spriteSize;
    private Color previousColor;
    private int previousAlpha = -1;
    private int previousType = -1;
    private bool wasGrounded;
    private float landingImpulse;
    private float opacity = 1f;
    private ulong updatedAt = ulong.MaxValue;
    public Vector2? BirthOrigin { get; set; }
    public float BirthProgress { get; set; } = 1f;
    public bool MotherBuds { get; set; }
    public bool Rainbow { get; set; }
    public SlimeShape Shape => shape;

    public void Update(NPC npc)
    {
        if (updatedAt == Main.GameUpdateCount && shape.Initialized) return;
        updatedAt = Main.GameUpdateCount;
        bool grounded = SlimeSurface.TryGroundContact(npc, out Vector2 groundPoint, out _);
        float time = (float)Main.GameUpdateCount * .055f + npc.whoAmI * .67f;
        if (grounded && !wasGrounded && previousVelocity.Y > 1f)
        {
            landingImpulse = MathHelper.Clamp(previousVelocity.Y / 8f, 0f, 1f);
            bodySizeVelocity += new Vector2(2.5f, -2f) * npc.scale * landingImpulse;
        }
        if (!grounded && wasGrounded && npc.velocity.Y < -1f)
            bodySizeVelocity += new Vector2(-1.4f, 2f) * npc.scale;
        landingImpulse *= .8f;
        Vector2 footprint = spriteSize == Vector2.Zero ? new Vector2(npc.width * 1.25f, npc.height * 1.2f) : spriteSize * npc.scale;
        Vector2 basis = footprint / new Vector2(2.2f, 2.04f);
        float flight = grounded ? 0f : MathHelper.Clamp(Math.Abs(npc.velocity.Y) * .025f, 0f, .2f);
        float anticipation = grounded ? MathHelper.Clamp((npc.ai[0] + 16f) / 16f, 0f, 1f) : 0f;
        Vector2 size = basis * new Vector2(1f - flight * .3f + anticipation * .08f, 1f + flight * .35f - anticipation * .08f);
        size += new Vector2(landingImpulse * 1.5f, -landingImpulse + MathF.Sin(time * 1.65f) * .15f) * npc.scale;
        float bottom = grounded ? groundPoint.Y - npc.Center.Y + 1f : npc.height * .5f + 1f;
        Vector2 offset = new(-npc.velocity.X * .2f, bottom - size.Y * shape.BottomCutoff);
        if (!shape.Initialized) { bodySize = size; bodyOffset = offset; }
        SlimeShape.Spring(ref bodySize, ref bodySizeVelocity, size, .25f, .62f);
        SlimeShape.Spring(ref bodyOffset, ref bodyOffsetVelocity, offset, .22f, .64f);
        bodySize = Vector2.Max(bodySize, new Vector2(2f));
        if (grounded)
        {
            bodyOffset.Y = bottom - bodySize.Y * shape.BottomCutoff;
            bodyOffsetVelocity.Y = 0f;
        }
        shape.Begin(bodyOffset, bodySize, time, npc.Center);
        if (MotherBuds)
        {
            float division = MathHelper.Clamp(1f - npc.life / (float)Math.Max(1, npc.lifeMax), 0f, 1f);
            for (int i = 0; i < 3; i++)
            {
                float angle = MathHelper.Lerp(-2.6f, -.55f, i / 2f);
                Vector2 direction = angle.ToRotationVector2();
                float bud = division * (.28f + MathF.Sin(time + i * 2f) * .025f);
                if (bud > .035f)
                    shape.AddLobe(bodyOffset + direction * bodySize * (.65f + division * .35f), bodySize * bud, .72f, .98f, .2f, i);
            }
        }
        if (BirthOrigin is Vector2 origin && BirthProgress < 1f)
        {
            float remaining = 1f - MathHelper.Clamp(BirthProgress, 0f, 1f);
            Vector2 tip = origin - npc.Center;
            if (tip.LengthSquared() < 160f * 160f)
            {
                Vector2 root = bodyOffset;
                Vector2 bend = Vector2.Lerp(root, tip, .5f) + Vector2.UnitY * (MathF.Sin(BirthProgress * MathHelper.Pi) * 8f);
                shape.SetTendril(0, root, bend, tip, remaining * 3f, 0f, false, Vector2.UnitY, false);
            }
        }
        shape.Initialized = true;
        previousVelocity = npc.velocity;
        wasGrounded = grounded;
    }

    public void Draw(NPC npc, SpriteBatch batch, Vector2 screenPosition, Color light)
    {
        EnsurePalette(npc);
        Update(npc);
        skin.Draw(batch, shape, npc.Center - screenPosition + Vector2.UnitY * npc.gfxOffY, light, opacity);
    }

    private void EnsurePalette(NPC npc)
    {
        if (previousType == npc.type && (Rainbow || previousColor == npc.color) && previousAlpha == npc.alpha) return;
        if (!sprites.TryGetValue(npc.type, out SpritePalette sprite))
        {
            Main.instance.LoadNPC(npc.type);
            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            int frameHeight = texture.Height / Math.Max(1, Main.npcFrameCount[npc.type]);
            int left = texture.Width, right = 0, top = frameHeight, bottom = 0;
            List<Color> shades = new();
            for (int y = 0; y < texture.Height; y++)
                for (int x = 0; x < texture.Width; x++)
                {
                    Color pixel = data[y * texture.Width + x];
                    if (pixel.A < 200) continue;
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    top = Math.Min(top, y % frameHeight); bottom = Math.Max(bottom, y % frameHeight);
                    shades.Add(pixel);
                }
            shades.Sort((a, b) => (a.R + a.G + a.B).CompareTo(b.R + b.G + b.B));
            if (shades.Count == 0) shades.Add(Color.White);
            Color[] samples = new Color[6];
            float[] percentiles = [.02f, .18f, .36f, .58f, .8f, .96f];
            for (int i = 0; i < samples.Length; i++) samples[i] = shades[(int)((shades.Count - 1) * percentiles[i])];
            sprite = new SpritePalette(new Vector2(Math.Max(4, right - left + 1), Math.Max(4, bottom - top + 1)), samples);
            sprites[npc.type] = sprite;
        }
        spriteSize = sprite.Size;
        Color baseColor = npc.GetAlpha(Color.White);
        Color tint = Rainbow ? Color.Transparent : npc.GetColor(Color.White);
        float baseAlpha = baseColor.A / 255f;
        float tintAlpha = tint.A / 255f;
        opacity = MathHelper.Clamp(tintAlpha + baseAlpha * (1f - tintAlpha), 0f, 1f);
        Color Shade(int index)
        {
            Vector3 pixel = sprite.Shades[index].ToVector3();
            Vector3 value = pixel * (baseColor.ToVector3() * (1f - tintAlpha) + tint.ToVector3());
            return new Color(Vector3.Clamp(value / Math.Max(.01f, opacity), Vector3.Zero, Vector3.One));
        }
        SlimeAppearance palette = skin.Appearance;
        palette.Outline = Shade(0);
        palette.BodyDark = Shade(1);
        palette.BodyAA = Shade(2);
        palette.BodyMid = Shade(3);
        palette.BodyHighlight = Shade(5);
        palette.BackDark = Shade(1);
        palette.BackSecondary = Shade(2);
        palette.BackInner = Shade(3);
        palette.BackBright = Shade(4);
        palette.BackAA = Shade(4);
        palette.BackHot = Shade(5);
        palette.ColorTransform = Rainbow ? RainbowColor : null;
        previousType = npc.type;
        previousColor = npc.color;
        previousAlpha = npc.alpha;
        shape.Initialized = false;
    }

    public static Color RainbowColor(Vector2 point, float time, Color shade)
    {
        float hue = (time * .035f + point.X * .009f + point.Y * .014f) % 1f;
        if (hue < 0f) hue += 1f;
        float brightness = MathHelper.Clamp(shade.ToVector3().Length() / MathF.Sqrt(3f), .06f, 1f);
        Color color = Main.hslToRgb(hue, .9f, .55f);
        return new Color(color.ToVector3() * brightness) { A = shade.A };
    }

    public void Dispose() => skin.Dispose();
    public static void ClearCache() => sprites.Clear();
}
