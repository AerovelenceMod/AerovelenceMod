using System;
using System.Collections.Generic;



using Terraria.GameContent;


namespace AerovelenceMod.Common.Utilities;

public sealed class VanillaSlimeVisual : IDisposable
{
    private sealed record SpritePalette(Vector2 Size, Color[] Shades);
    private static readonly Dictionary<int, SpritePalette> sprites = new();
    private static readonly List<(VanillaSlimeVisual Visual, NPC Npc)> pending = new(256);
    private readonly SlimeShape shape;
    private readonly SlimeRenderer skin;
    private readonly bool customPalette;
    public VanillaSlimeVisual() : this(null, null) { }
    public VanillaSlimeVisual(SlimeRenderer renderer, SlimeShape shape = null)
    {
        this.shape = shape ?? new(1, 3) { BodyDome = .72f, BottomCutoff = .98f, RippleStrength = .35f };
        customPalette = renderer != null;
        skin = renderer ?? new(new SlimeAppearance
        {
            PixelSize = 2,
            LightDirection = new Vector2(-.58f, -.82f),
            EdgeSmoothing = .45f,
            BodyBacklight = true,
            BodyHighlights = true,
            UniformOutline = true
        });
    }
    private Vector2 bodySize;
    private Vector2 bodyOffset;
    private Vector2 bodyOffsetVelocity;
    private Vector2 wobble;
    private Vector2 wobbleVelocity;
    private float compression;
    private float compressionVelocity;
    private Vector2 previousVelocity;
    private Vector2 motion;
    private Vector2 motionVelocity;
    private float travelStretch = 1f;
    private float travelStretchVelocity;
    private float airborneBlend;
    private float airborneBlendVelocity;
    private float suspensionTension, suspensionTensionVelocity;
    private ulong lastDripTick;
    private ulong queuedAt = ulong.MaxValue;
    private bool disposed;
    private Vector2 spriteSize;
    private Color previousColor;
    private int previousAlpha = -1;
    private int previousType = -1;
    private int previousNetId = int.MinValue;
    private bool wasGrounded;
    private bool wasWall;
    private int previousLife;
    private float opacity = 1f;
    private ulong updatedAt = ulong.MaxValue;
    public Vector2? BirthOrigin { get; set; }
    public float BirthProgress { get; set; } = 1f;
    public bool MotherBuds { get; set; }
    public bool Rainbow { get; set; }
    public Vector2? BodyDimensions { get; set; }
    public float GroundOffset { get; set; } = 1f;
    public float? JumpAnticipation { get; set; }
    public Action<NPC> ShapeModifier { get; set; }
    public SlimeShape Shape => shape;
    public float Opacity => opacity;
    public bool EmitParticles { get; set; } = true;
    public SlimeSpikes Spikes { get; set; }
    public bool Suspended { get; set; }
    public Vector2? SuspensionCenter { get; set; }
    public float SuspensionRotation { get; set; }

    public void QueueUpdate(NPC npc)
    {
        if (Main.dedServ || disposed || queuedAt == Main.GameUpdateCount) return;
        queuedAt = Main.GameUpdateCount;
        pending.Add((this, npc));
    }

    public static void UpdatePending()
    {
        foreach (var entry in pending)
            if (entry.Npc.active && !entry.Visual.disposed) entry.Visual.Update(entry.Npc);
        pending.Clear();
    }

    public static void ClearPending() => pending.Clear();

    public void Update(NPC npc) => Update(npc, Main.GameUpdateCount);

    public void Update(NPC npc, ulong tick)
    {
        if (!Main.dedServ && !Main.gameMenu) EnsurePalette(npc);
        if (updatedAt == tick && shape.Initialized) return;
        updatedAt = tick;
        Vector2 groundPoint = npc.Bottom;
        bool grounded = !Suspended && SlimeSurface.TryGroundContact(npc, out groundPoint, out _);
        float time = (float)tick * .055f + npc.whoAmI * .67f;
        float mass = MathHelper.Clamp(npc.scale, .65f, 1.6f);
        float idle = Suspended ? .7f : grounded ? MathHelper.Clamp(1f - Math.Abs(npc.velocity.X) / .8f, 0f, 1f) : 0f;
        bool initialized = shape.Initialized;
        float airborneTarget = grounded ? 0f : MathHelper.Clamp(.62f + npc.velocity.Length() * .032f, .62f, 1f);
        if (!initialized) airborneBlend = airborneTarget;
        airborneBlendVelocity = (airborneBlendVelocity + (airborneTarget - airborneBlend) * (.2f / mass)) * .72f;
        airborneBlend = MathHelper.Clamp(airborneBlend + airborneBlendVelocity, 0f, 1f);
        Vector2 acceleration = initialized ? npc.velocity - previousVelocity : Vector2.Zero;
        float support = Suspended ? MathHelper.Clamp(.68f + Math.Abs(acceleration.Y) * .12f + npc.velocity.Length() * .04f, .55f, 1f) : 0f;
        if (!initialized) suspensionTension = support;
        suspensionTensionVelocity = (suspensionTensionVelocity + (support - suspensionTension) * .22f) * .74f;
        suspensionTension = MathHelper.Clamp(suspensionTension + suspensionTensionVelocity, 0f, 1f);
        if (!initialized) motion = npc.velocity;
        SlimeShape.Spring(ref motion, ref motionVelocity, npc.velocity, .24f / mass, .76f);
        float horizontalSpeed = Math.Abs(npc.velocity.X);
        float horizontalShare = horizontalSpeed / Math.Max(.01f, horizontalSpeed + Math.Abs(npc.velocity.Y) * .7f);
        float targetTravelStretch = 1f + MathHelper.Clamp(horizontalSpeed * .11f * horizontalShare, 0f, .4f);
        if (Suspended) targetTravelStretch = MathHelper.Lerp(1f, targetTravelStretch, .35f);
        travelStretchVelocity = (travelStretchVelocity + (targetTravelStretch - travelStretch) * (.18f / mass)) * .79f;
        travelStretch = MathHelper.Clamp(travelStretch + travelStretchVelocity, 1f, 1.45f);
        Vector2 travelDirection = grounded || Suspended ? Vector2.UnitX : motion.LengthSquared() > .01f ? Vector2.Normalize(motion) : Vector2.UnitY;
        if (initialized && grounded && !wasGrounded && previousVelocity.Y > 1f)
        {
            float impact = MathHelper.Clamp(previousVelocity.Y * previousVelocity.Y / 90f, .12f, 2.4f);
            compressionVelocity += impact * .2f;
            wobbleVelocity += new Vector2(-previousVelocity.X * .015f, impact * .065f);
        }
        if (initialized && !grounded && wasGrounded && npc.velocity.Y < -1f)
        {
            float launch = MathHelper.Clamp(-npc.velocity.Y / 8f, .25f, 1f);
            compressionVelocity -= launch * .13f;
            wobbleVelocity += new Vector2(-npc.velocity.X * .02f, -launch * .06f);
        }
        if (initialized && npc.collideX && !wasWall)
        {
            wobbleVelocity.X += MathHelper.Clamp(npc.oldVelocity.X * .04f, -.2f, .2f);
            compressionVelocity += Math.Min(Math.Abs(npc.oldVelocity.X) * .012f, .08f);
        }
        if (initialized && npc.life < previousLife)
            wobbleVelocity += new Vector2(-npc.direction * .07f, .04f);
        wobbleVelocity -= new Vector2(acceleration.X * .016f, acceleration.Y * .008f);
        Vector2 idleWobble = new(MathF.Sin(time * .73f) * .07f, MathF.Sin(time * 1.07f + 1.4f) * .055f);
        SlimeShape.Spring(ref wobble, ref wobbleVelocity, idleWobble * idle, .11f / mass, .86f);
        wobble = Vector2.Clamp(wobble, new Vector2(-.35f, -.25f), new Vector2(.35f, .25f));
        Vector2 footprint = BodyDimensions ?? (spriteSize == Vector2.Zero ? new Vector2(npc.width * 1.25f, npc.height * 1.2f) : spriteSize * npc.scale);
        Vector2 basis = footprint * .5f;
        float threshold = npc.ai[0] < -1500f ? -2000f : npc.ai[0] < -500f ? -1000f : 0f;
        float anticipation = grounded && npc.ai[0] < threshold ? MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((npc.ai[0] - threshold + 24f) / 24f, 0f, 1f)) : 0f;
        if (JumpAnticipation is float preparation) anticipation = grounded ? MathHelper.Clamp(preparation, 0f, 1f) : 0f;
        float flightStretch = npc.velocity.Y > 0f ? MathHelper.Clamp(npc.velocity.Y * npc.velocity.Y * .0016f, 0f, .36f)
            : MathHelper.Clamp(-npc.velocity.Y * .017f, 0f, .17f);
        float breathing = idle * (MathF.Sin(time * .81f) * .06f + MathF.Sin(time * 1.39f + 1.8f) * .025f);
        float targetCompression = grounded ? anticipation * .18f + breathing : -flightStretch;
        compressionVelocity = (compressionVelocity + (targetCompression - compression) * (.15f / mass)) * .82f;
        compression = MathHelper.Clamp(compression + compressionVelocity, -.42f, .5f);
        float stretch = 1f - compression;
        Vector2 size = basis * new Vector2(1f / stretch, stretch);
        float bottom = grounded ? groundPoint.Y - npc.Center.Y + GroundOffset : npc.height * .5f + GroundOffset;
        Vector2 offset = new(-npc.velocity.X * .65f, grounded ? bottom - size.Y / travelStretch : bottom - basis.Y - npc.velocity.Y * .15f);
        if (Suspended)
            offset = (SuspensionCenter ?? npc.Center - Vector2.UnitY * 5f * npc.scale) - npc.Center
                + new Vector2(-npc.velocity.X * .25f, -npc.velocity.Y * .1f);
        if (!shape.Initialized) { bodySize = size; bodyOffset = offset; }
        bodySize = Vector2.Max(size, new Vector2(2f));
        SlimeShape.Spring(ref bodyOffset, ref bodyOffsetVelocity, offset, .13f / mass, .8f);
        if (grounded)
        {
            bodyOffset.Y = bottom - bodySize.Y / travelStretch;
            bodyOffsetVelocity.Y = 0f;
        }
        shape.Begin(bodyOffset, bodySize, time, npc.Center);
        float horizontalWeight = Math.Abs(motion.X) / (1f + motion.Length());
        shape.Shear = MathHelper.Clamp(motion.X * .14f * horizontalWeight, -.5f, .5f);
        shape.Wobble = wobble;
        shape.Flow = npc.velocity;
        shape.LightPhase = npc.netID * .37f + npc.whoAmI * .61f;
        shape.TravelDirection = travelDirection;
        shape.TravelStretch = travelStretch;
        shape.AirborneBlend = grounded ? 0f : airborneBlend;
        shape.SuspensionTension = Suspended ? suspensionTension : 0f;
        shape.BodyRotation = Suspended ? SuspensionRotation : 0f;
        Spikes?.Apply(shape, tick);
        if (MotherBuds)
        {
            float division = MathHelper.Clamp(1f - npc.life / (float)Math.Max(1, npc.lifeMax), 0f, 1f);
            for (int i = 0; i < 3; i++)
            {
                float angle = MathHelper.Lerp(-2.6f, -.55f, i / 2f);
                Vector2 direction = angle.ToRotationVector2();
                float bud = division * (.28f + MathF.Sin(time + i * 2f) * .025f);
                if (bud > .035f)
                    shape.AddLobe(bodyOffset + shape.TransformBodyOffset(direction * bodySize * (.65f + division * .35f)), bodySize * bud, .72f, .98f, .2f, i);
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
        if (EmitParticles && !Main.dedServ && !Main.gameMenu)
        {
            bool launched = initialized && !grounded && wasGrounded && npc.velocity.Y < -1f;
            if (launched || !grounded && npc.velocity.LengthSquared() > 6f && tick - lastDripTick >= 12)
            {
                EnsurePalette(npc);
                int count = launched ? Math.Clamp((int)(bodySize.X * .17f), 2, 7) : Math.Clamp((int)(bodySize.X * .055f), 1, 3);
                SlimeParticles.Shed(shape, skin.Appearance.PixelStyle, opacity, Rainbow, count);
                lastDripTick = tick;
            }
        }
        shape.Initialized = true;
        previousVelocity = npc.velocity;
        wasGrounded = grounded;
        wasWall = npc.collideX;
        previousLife = npc.life;
        ShapeModifier?.Invoke(npc);
    }

    public void Draw(NPC npc, SpriteBatch batch, Vector2 screenPosition, Color light)
    {
        EnsurePalette(npc);
        Update(npc);
        skin.Draw(batch, shape, npc.Center - screenPosition + Vector2.UnitY * npc.gfxOffY, light, opacity);
    }

    public void DeathSplatter(NPC npc, int hitDirection)
    {
        if (Main.dedServ || Main.gameMenu) return;
        EnsurePalette(npc);
        Update(npc);
        SlimeParticles.Burst(shape, skin.Appearance.PixelStyle, opacity, Rainbow, hitDirection);
    }

    private void EnsurePalette(NPC npc)
    {
        if (customPalette)
        {
            opacity = npc.Opacity;
            return;
        }
        if (previousType == npc.type && previousNetId == npc.netID && (Rainbow || previousColor == npc.color) && previousAlpha == npc.alpha) return;
        int sourceType = npc.type == NPCID.SpikedIceSlime ? NPCID.IceSlime : npc.type == NPCID.SlimeSpiked ? NPCID.BlueSlime : npc.type;
        if (!sprites.TryGetValue(sourceType, out SpritePalette sprite))
        {
            Main.instance.LoadNPC(sourceType);
            Texture2D texture = TextureAssets.Npc[sourceType].Value;
            Color[] data = new Color[texture.Width * texture.Height];
            texture.GetData(data);
            CacheSprite(sourceType, data, texture.Width, texture.Height, Main.npcFrameCount[sourceType]);
            sprite = sprites[sourceType];
        }
        bool resized = spriteSize != sprite.Size || previousType != npc.type;
        spriteSize = sprite.Size;
        Color baseColor = npc.GetAlpha(Color.White);
        Color tint = npc.GetColor(Color.White);
        float baseAlpha = baseColor.A / 255f;
        float tintAlpha = (Rainbow ? 100f : tint.A) / 255f;
        float sourceOpacity = npc.type == NPCID.LavaSlime ? MathHelper.Clamp(1f - npc.alpha / 255f, 0f, 1f)
            : MathHelper.Clamp(tintAlpha + baseAlpha * (1f - tintAlpha), 0f, 1f);
        opacity = sourceOpacity;
        Color Shade(int index)
        {
            Vector3 pixel = sprite.Shades[index].ToVector3();
            Vector3 value = pixel * (baseColor.ToVector3() * (1f - tintAlpha) + tint.ToVector3());
            return new Color(Vector3.Clamp(value / Math.Max(.01f, sourceOpacity), Vector3.Zero, Vector3.One));
        }
        SlimeAppearance palette = skin.Appearance;
        Color theme = Rainbow ? new Color(150, 150, 150) : Shade(3);
        palette.PixelStyle = !Rainbow && npc.color == new Color(0, 80, 255, 100) ? SlimePixelStyle.Blue
            : SlimePixelStyle.ForVanilla(npc.netID, theme, Rainbow);
        palette.Outline = palette.PixelStyle[1];
        palette.BodyDark = palette.PixelStyle[0];
        palette.BodyAA = palette.PixelStyle.OutlineLight;
        palette.BodyMid = palette.PixelStyle[4];
        palette.BodyHighlight = palette.PixelStyle[5];
        palette.BackDark = palette.PixelStyle[1];
        palette.BackSecondary = palette.PixelStyle[2];
        palette.BackInner = palette.PixelStyle[3];
        palette.BackBright = palette.PixelStyle[4];
        palette.BackAA = palette.PixelStyle[4];
        palette.BackHot = palette.PixelStyle.Backlight;
        palette.ColorTransform = Rainbow ? RainbowColor : null;
        previousType = npc.type;
        previousNetId = npc.netID;
        previousColor = npc.color;
        previousAlpha = npc.alpha;
        if (resized) shape.Initialized = false;
    }

    public static void CacheSprite(int type, Color[] data, int width, int height, int frames)
    {
        if (width <= 0 || height <= 0 || frames <= 0 || height < frames || data.Length != width * height)
            throw new ArgumentException("Invalid slime sprite dimensions");
        int frameHeight = height / frames;
        int left = width, right = 0, top = frameHeight, bottom = 0;
        List<Color> shades = new();
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Color pixel = data[y * width + x];
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
        sprites[type] = new SpritePalette(new Vector2(Math.Max(4, right - left + 1), Math.Max(4, bottom - top + 1)), samples);
    }
    public static Color RainbowColor(Vector2 point, float time, Color shade)
    {
        float hue = (time * .035f + point.X * .009f + point.Y * .014f) % 1f;
        if (hue < 0f) hue += 1f;
        hue = MathF.Floor(hue * 24f) / 24f;
        float brightness = MathHelper.Clamp(shade.ToVector3().Length() / MathF.Sqrt(3f), .06f, .8f);
        float hueShift = MathHelper.Lerp(.025f, -.04f, MathHelper.Clamp((brightness - .16f) / .3f, 0f, 1f));
        float saturation = MathHelper.Lerp(.58f, .86f, MathHelper.Clamp(brightness * 2f, 0f, 1f));
        Color color = Main.hslToRgb((hue + hueShift + 1f) % 1f, saturation, brightness);
        return color with { A = shade.A };
    }

    public void Dispose()
    {
        disposed = true;
        skin.Dispose();
    }
    public static void ClearCache()
    {
        sprites.Clear();
        pending.Clear();
    }
}
