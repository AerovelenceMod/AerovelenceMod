using System;
using AerovelenceMod.Common.Systems.Gas;
using AerovelenceMod.Content.Projectiles;


using Terraria.DataStructures;



namespace AerovelenceMod.Common.Utilities;

/// <summary>Identifies the simulation behavior used by gas, TODO right now there's only a couple and I want to make this more extensive</summary>
public enum GasKind : byte
{
    Fog,
    Smoke,
    Sapfog
}

/// <summary>Identifies a paired gas and emitter preset intended for clientside visuals - TODO again like above this will be more extensive</summary>
public enum GasVisualStyle : byte
{
    GroundMist,
    Steam,
    Wispies,
    Embers,
    Nebula
}

/// <summary>Configures the movement, appearance, lifetime, collision, and damage behavior of gas</summary>
public sealed record GasSettings
{
    public GasKind Kind { get; init; }
    public bool TileCollision { get; init; } = true;
    public bool Buoyancy { get; init; } = true;
    public bool Turbulence { get; init; } = true;
    public bool Hostile { get; init; }
    public bool Additive { get; init; }
    public bool Jet { get; init; }
    public float FlowSpeed { get; init; }
    public float FlowRange { get; init; } = 240f;
    public float FlowSpread { get; init; } = 0.12f;
    public float Density { get; init; } = 1f;
    public float Opacity { get; init; } = 1f;
    public float Emissive { get; init; }
    public float Light { get; init; }
    public float Brightness { get; init; } = 1f;
    public float Pressure { get; init; } = 0.16f;
    public float Viscosity { get; init; } = 0.035f;
    public float Lift { get; init; } = 0.012f;
    public float Radius { get; init; } = 42f;
    public int Lifetime { get; init; } = 300;
    public int EmissionTicks { get; init; } = 6;
    public Color FadeColor { get; init; }
    public float ColorFadeRate { get; init; }
    public Color Color { get; init; } = new(155, 205, 235);


    /// <summary>Creates the narrow additive jets mimicking real-life aerosols</summary>
    public static GasSettings ForAerosol() => new()
    {
        Jet = true,
        Additive = true,
        Density = 0.5f,
        Opacity = 1.8f,
        Brightness = 2.25f,
        Pressure = 0.012f,
        Viscosity = 0.012f,
        Lift = 0.002f,
        Radius = 4f,
        Lifetime = 90,
        EmissionTicks = 2,
        Turbulence = true,
        FlowSpeed = 0f,
        FlowRange = 240f,
        FlowSpread = 0.08f,
        Color = new(130, 235, 255)
    };

    /// <summary>Creates a density-free jet of air that pushes existing gas without emitting more gas</summary>
    public static GasSettings ForCompressedAir() => ForAerosol() with
    {
        Density = 0f,
        Pressure = 0f,
        Buoyancy = false,
        FlowSpeed = 28f,
        FlowRange = 320f,
        FlowSpread = 0.12f,
        Radius = 8f
    };

    /// <summary>Creates client-side gas settings matching a visual preset</summary>
    /// <param name="style">The visual preset to create</param>
    public static GasSettings ForVisual(GasVisualStyle style)
    {
        GasSettings basis = new()
        {
            Density = 0.48f,
            Pressure = 0f,
            EmissionTicks = 1,
            Radius = 40f,
            Opacity = 0.4f,
            Lifetime = 300,
            Viscosity = 0.025f
        };
        return style switch
        {
            GasVisualStyle.Steam => basis with { Kind = GasKind.Smoke, Color = new(210, 230, 240), Lift = 0.065f, Radius = 24f, Lifetime = 180, Opacity = 0.55f },
            GasVisualStyle.Wispies => basis with { Additive = true, Color = new(65, 210, 245), Emissive = 1f, Light = 0.9f, Brightness = 1.1f, Lift = 0.018f, Radius = 20f, Lifetime = 210, Opacity = 0.65f },
            GasVisualStyle.Embers => basis with { Kind = GasKind.Smoke, Additive = true, Color = new(255, 105, 30), Emissive = 1f, Light = 1.2f, Brightness = 1.25f, Lift = 0.08f, Radius = 16f, Lifetime = 120, Opacity = 0.8f },
            GasVisualStyle.Nebula => basis with { Additive = true, Color = new(170, 85, 245), Emissive = 1f, Light = 0.55f, Density = 0.65f, Lift = 0f, Radius = 32f, Opacity = 0.6f },
            _ => basis with { Color = new(145, 180, 195), Lift = -0.006f, Lifetime = 480, Opacity = 0.32f, Brightness = 0.9f, Turbulence = false }
        };
    }

    /// <summary>Creates gas setttings matching a simulation kind</summary>
    /// <param name="kind">The gas kind to create</param>
    public static GasSettings For(GasKind kind) => kind switch
    {
        GasKind.Smoke => new() { Kind = kind, Density = 0.85f, Lift = 0.048f, Radius = 48f, Lifetime = 240, Color = new(145, 130, 165) },
        GasKind.Sapfog => new() { Kind = kind, Density = 1.4f, Pressure = 0.12f, Viscosity = 0.06f, Lift = -0.006f, Radius = 38f, Lifetime = 360, Color = new(55, 155, 255), Emissive = 0.65f, Light = 0.9f },
        _ => new() { Kind = GasKind.Fog }
    };
}

/// <summary>Configures a persistent client-side gas emitter</summary>
public sealed record GasEmitterSettings
{
    public Vector2 Velocity { get; init; } = new(0f, -0.4f);
    public Vector2 Area { get; init; } = new(24f, 8f);
    public int Duration { get; init; } = 600;
    public int Interval { get; init; } = 6;
    public int FadeIn { get; init; } = 30;
    public int FadeOut { get; init; } = 90;
    public float PulseAmount { get; init; } = 0.2f;
    public int PulsePeriod { get; init; } = 180;
    public float Wander { get; init; } = 0.4f;
    public float Swirl { get; init; }
    public float WindInfluence { get; init; } = 0.25f;
    public Color? SecondaryColor { get; init; }

    /// <summary>Creates emitter settings matching a visual gas preset</summary>
    /// <param name="style">The visual preset to create</param>
    public static GasEmitterSettings For(GasVisualStyle style) => style switch
    {
        GasVisualStyle.GroundMist => new() { Area = new(85f, 6f), Velocity = new(0.45f, 0f), Interval = 4, Wander = 0.2f },
        GasVisualStyle.Steam => new() { Area = new(8f, 3f), Velocity = new(0f, -1.2f), Interval = 4, Wander = 0.35f, SecondaryColor = new(160, 190, 215) },
        GasVisualStyle.Wispies => new() { Area = new(24f, 14f), Swirl = 1.1f, SecondaryColor = new(80, 255, 165), Wander = 0.7f },
        GasVisualStyle.Embers => new() { Area = new(20f, 5f), Velocity = new(0f, -1.4f), Interval = 4, PulseAmount = 0.4f, PulsePeriod = 95, SecondaryColor = new(255, 190, 50) },
        GasVisualStyle.Nebula => new() { Area = new(40f, 28f), Velocity = Vector2.Zero, Swirl = 1.8f, Interval = 4, WindInfluence = 0f, SecondaryColor = new(65, 170, 250) },
        _ => new()
    };
}

/// <summary>Controls a persistent client-side gas source created by <see cref="GasUtil.CreateEmitter"/></summary>
public sealed class GasEmitter
{
    private readonly GasSettings gas;
    private readonly GasEmitterSettings settings;
    private readonly float phase;
    private int age;
    private int stopAge;
    /// <summary>Gets or sets the world position used for future emissions</summary>
    public Vector2 Position { get; set; }
    /// <summary>Gets whether the emitter is still producing gas or fading out</summary>
    public bool Active { get; private set; } = true;

    internal GasEmitter(Vector2 position, GasSettings gas, GasEmitterSettings settings, int seed)
    {
        Position = position;
        this.gas = gas;
        this.settings = settings with
        {
            Velocity = GasUtil.Finite(settings.Velocity) ? Vector2.Clamp(settings.Velocity, new(-12f), new(12f)) : Vector2.Zero,
            Area = GasUtil.Finite(settings.Area) ? Vector2.Clamp(settings.Area, Vector2.Zero, new(160f)) : Vector2.Zero,
            Duration = Math.Clamp(settings.Duration, 1, 36000),
            Interval = Math.Clamp(settings.Interval, 1, 60),
            FadeIn = Math.Clamp(settings.FadeIn, 0, 600),
            FadeOut = Math.Clamp(settings.FadeOut, 0, 600),
            PulseAmount = GasUtil.Bounded(settings.PulseAmount, 0.2f, 0f, 1f),
            PulsePeriod = Math.Clamp(settings.PulsePeriod, 12, 3600),
            Wander = GasUtil.Bounded(settings.Wander, 0.4f, 0f, 4f),
            Swirl = GasUtil.Bounded(settings.Swirl, 0f, -4f, 4f),
            WindInfluence = GasUtil.Bounded(settings.WindInfluence, 0.25f, 0f, 3f)
        };
        phase = seed * 2.399963f;
        stopAge = this.settings.Duration;
    }

    /// <summary>Stops new emissions and lets the emitter finish its configured fadeout</summary>
    public void Stop() => stopAge = Math.Min(stopAge, age + settings.FadeOut);

    internal void Cancel() => Active = false;

    internal void Update()
    {
        if (!Active || age >= stopAge || !GasUtil.Finite(Position))
        {
            Active = false;
            return;
        }
        if (age % settings.Interval == 0)
        {
            float pulse = 0.5f + 0.5f * MathF.Sin(age * MathHelper.TwoPi / settings.PulsePeriod + phase);
            float fade = GasAppearance.Envelope(age, stopAge, settings.FadeIn, settings.FadeOut);
            float amount = fade * (1f - settings.PulseAmount * pulse);
            float angle = phase + age / settings.Interval * 2.399963f;
            float radius = MathF.Sqrt((age / settings.Interval * 0.618034f + 0.5f) % 1f);
            Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
            Vector2 point = Position + radial * settings.Area * radius;
            float sway = MathF.Sin(age * 0.027f + phase);
            Vector2 drift = settings.Velocity + new Vector2(sway * settings.Wander + Main.windSpeedCurrent * settings.WindInfluence, MathF.Cos(age * 0.019f + phase) * settings.Wander * 0.3f);
            drift += new Vector2(-radial.Y, radial.X) * (settings.Swirl * radius);
            Color color = settings.SecondaryColor is Color secondary ? Color.Lerp(gas.Color, secondary, pulse) : gas.Color;
            if (amount > 0.001f)
                GasUtil.EmitVisual(point, drift, gas with { Color = color, Density = gas.Density * amount });
        }
        age++;
    }
}

/// <summary>Creates simulated gas, visual-only gas, persistent emitters, airflow, and vacuum forces</summary>
public static class GasUtil
{
    /// <summary>The maximum number of gas controller projectiles allowed at once simiarly like vanilla dust caps</summary>
    public const int MaxClouds = 240;

    /// <summary>The maximum number of non hostile gas controller projectiles allowed at once</summary>
    public const int MaxFriendlyClouds = 180;

    /// <summary>Creates a networked gas controller projectile that injects gas into the simulation</summary>
    /// <param name="source">The entity source responsible for the gas</param>
    /// <param name="position">The world position where gas begins</param>
    /// <param name="velocity">The initial gas direction and velocity</param>
    /// <param name="settings">The gas behavior and appearance settings</param>
    /// <param name="damage">The damage carried by hostile or friendly gas</param>
    /// <param name="owner">The owning player, or you can use -1 to use the local player</param>
    /// <returns>The spawned projectile index, or you can use -1 when the request cannot be emitted</returns>
    public static int Emit(IEntitySource source, Vector2 position, Vector2 velocity, GasSettings settings, int damage = 0, int owner = -1)
    {
        if (settings is null || !Finite(position) || !Finite(velocity))
            return -1;
        if (settings.Hostile && Main.netMode == NetmodeID.MultiplayerClient)
            return -1;
        owner = settings.Hostile ? 255 : owner < 0 ? Main.myPlayer : owner;
        if (!settings.Hostile && owner != Main.myPlayer)
            return -1;

        int total = 0;
        int friendly = 0;
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile.ModProjectile is not GasCloud)
                continue;
            total++;
            if (!projectile.hostile)
                friendly++;
        }
        if (total >= MaxClouds || (!settings.Hostile && friendly >= MaxFriendlyClouds))
            return -1;

        settings = Sanitize(settings);
        if (settings.TileCollision && Collision.SolidCollision(position - new Vector2(6), 12, 12))
            return -1;

        int index = Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<GasCloud>(), Math.Max(0, damage), 0f, owner);
        if (index >= Main.maxProjectiles)
            return -1;
        ((GasCloud)Main.projectile[index].ModProjectile).Configure(settings);
        Main.projectile[index].netUpdate = true;
        return index;
    }

    /// <summary>Applies a directional or radial vacuum force during the current simulation update</summary>
    /// <param name="position">The world position of the vacuum nozzle or center</param>
    /// <param name="direction">The direction gas is pulled when the vacuum is not radial</param>
    /// <param name="radial">Whether gas should be pulled inward from every direction</param>
    public static void Vacuum(Vector2 position, Vector2 direction, bool radial = false) => GasSystem.Vacuum(position, direction, radial);

    /// <summary>Injects non-damaging, non-networked gas into a client-side visual field</summary>
    /// <param name="position">The world position where gas begins</param>
    /// <param name="velocity">The initial visual gas velocity</param>
    /// <param name="settings">The gas appearance and movement settings</param>
    /// <returns>Whether the visual gas was accepted</returns>
    public static bool EmitVisual(Vector2 position, Vector2 velocity, GasSettings settings)
    {
        if (Main.dedServ || Main.gameMenu || settings is null || !Finite(position) || !Finite(velocity))
            return false;
        return ModContent.GetInstance<GasSystem>().EmitVisual(position, velocity, Sanitize(settings) with { Hostile = false, FlowSpeed = 0f });
    }

    /// <summary>Creates a persistent client-side source of visual gas</summary>
    /// <param name="position">The initial world position of the emitter</param>
    /// <param name="gas">The visual gas appearance and movement settings</param>
    /// <param name="settings">The emitter pattern settings, or null for the defaults</param>
    /// <returns>The controllable emitter, or null when emitters are unavialable</returns>
    public static GasEmitter CreateEmitter(Vector2 position, GasSettings gas, GasEmitterSettings settings = null)
    {
        if (Main.dedServ || Main.gameMenu || gas is null || !Finite(position))
            return null;
        return ModContent.GetInstance<GasSystem>().CreateEmitter(position, Sanitize(gas) with { Hostile = false, FlowSpeed = 0f }, settings ?? new GasEmitterSettings());
    }

    /// <summary>Stops every persistent emitter and clears all client-side visual gas fields</summary>
    public static void ClearVisuals() => ModContent.GetInstance<GasSystem>().ClearVisuals();

    /// <summary>Creates a density-free air jet that pushes existing gas</summary>
    /// <param name="source">The entity source responsible for the airflow</param>
    /// <param name="position">The world position where airflow begins</param>
    /// <param name="velocity">The airflow direction and speed</param>
    /// <param name="range">The maximum distance affected by the airflow</param>
    /// <param name="radius">The width of the airflow at its origin</param>
    /// <param name="spread">The amount the airflow widens over its range</param>
    /// <param name="tileCollision">Whether solid tiles block the airflow</param>
    /// <param name="owner">The owning player, or -1 for the local player</param>
    /// <returns>The spawned controller projectile index, or you can use -1 when the request cannot be emitted</returns>
    public static int Blow(IEntitySource source, Vector2 position, Vector2 velocity, float range = 320f, float radius = 8f, float spread = 0.12f, bool tileCollision = true, int owner = -1)
        => Emit(source, position, velocity, GasSettings.ForCompressedAir() with
        {
            FlowSpeed = velocity.Length(),
            FlowRange = range,
            FlowSpread = spread,
            Radius = radius,
            TileCollision = tileCollision,
            EmissionTicks = 1
        }, owner: owner);

    internal static bool Finite(Vector2 value) => float.IsFinite(value.X) && float.IsFinite(value.Y);

    internal static float Bounded(float value, float fallback, float min, float max) => float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;

    internal static GasSettings Sanitize(GasSettings settings) => settings with
    {
        Kind = Enum.IsDefined(settings.Kind) ? settings.Kind : GasKind.Fog,
        Density = Bounded(settings.Density, 1f, 0f, 3f),
        Opacity = Bounded(settings.Opacity, 1f, 0f, 2f),
        Emissive = Bounded(settings.Emissive, 0f, 0f, 1f),
        Light = Bounded(settings.Light, 0f, 0f, 2f),
        Brightness = Bounded(settings.Brightness, 1f, 0f, 3f),
        FlowSpeed = Bounded(settings.FlowSpeed, 0f, 0f, 40f),
        FlowRange = Bounded(settings.FlowRange, 240f, 16f, 480f),
        FlowSpread = Bounded(settings.FlowSpread, 0.12f, 0f, 0.6f),
        Pressure = Bounded(settings.Pressure, 0.16f, 0f, 0.5f),
        Viscosity = Bounded(settings.Viscosity, 0.035f, 0f, 0.15f),
        Lift = Bounded(settings.Lift, 0.012f, -0.1f, 0.1f),
        Radius = Bounded(settings.Radius, 42f, settings.Jet ? 1f : 16f, 64f),
        Lifetime = Math.Clamp(settings.Lifetime, 30, 600),
        EmissionTicks = Math.Clamp(settings.EmissionTicks, 1, 12),
        ColorFadeRate = Bounded(settings.ColorFadeRate, 0f, 0f, 0.2f),
        FadeColor = new Color(settings.FadeColor.R, settings.FadeColor.G, settings.FadeColor.B),
        Color = new Color(settings.Color.R, settings.Color.G, settings.Color.B)
    };
}
