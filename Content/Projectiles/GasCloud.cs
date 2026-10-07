using System.IO;
using AerovelenceMod.Common.Systems.Gas;

namespace AerovelenceMod.Content.Projectiles;

public sealed class GasCloud : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private GasSettings settings = GasSettings.For(GasKind.Fog);
    private bool ready;
    private int age;

    public override void SetStaticDefaults() => this.AddName(Language.Default, "Gas Cloud");

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 18;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.hide = true;
        Projectile.netImportant = true;
    }

    public void Configure(GasSettings value)
    {
        settings = GasUtil.Sanitize(value);
        Projectile.hostile = settings.Hostile;
        ready = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void AI()
    {
        if (!ready)
            return;
        if (age < settings.EmissionTicks)
            GasSystem.Inject(Projectile.Center, Projectile.velocity, settings, Projectile.damage, Projectile.identity + Projectile.owner * 13, age);
        age++;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(ready);
        if (!ready) return;
        writer.Write(age);
        writer.Write((byte)settings.Kind);
        writer.Write(settings.TileCollision);
        writer.Write(settings.Buoyancy);
        writer.Write(settings.Turbulence);
        writer.Write(settings.Hostile);
        writer.Write(settings.Additive);
        writer.Write(settings.Jet);
        writer.Write(settings.FlowSpeed);
        writer.Write(settings.FlowRange);
        writer.Write(settings.FlowSpread);
        writer.Write(settings.Density);
        writer.Write(settings.Opacity);
        writer.Write(settings.Emissive);
        writer.Write(settings.Light);
        writer.Write(settings.Brightness);
        writer.Write(settings.Pressure);
        writer.Write(settings.Viscosity);
        writer.Write(settings.Lift);
        writer.Write(settings.Radius);
        writer.Write(settings.Lifetime);
        writer.Write(settings.EmissionTicks);
        writer.Write(settings.Color.PackedValue);
        writer.Write(settings.FadeColor.PackedValue);
        writer.Write(settings.ColorFadeRate);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        ready = reader.ReadBoolean();
        if (!ready) return;
        age = System.Math.Max(0, reader.ReadInt32());
        settings = GasUtil.Sanitize(new GasSettings
        {
            Kind = (GasKind)reader.ReadByte(),
            TileCollision = reader.ReadBoolean(),
            Buoyancy = reader.ReadBoolean(),
            Turbulence = reader.ReadBoolean(),
            Hostile = reader.ReadBoolean(),
            Additive = reader.ReadBoolean(),
            Jet = reader.ReadBoolean(),
            FlowSpeed = reader.ReadSingle(),
            FlowRange = reader.ReadSingle(),
            FlowSpread = reader.ReadSingle(),
            Density = reader.ReadSingle(),
            Opacity = reader.ReadSingle(),
            Emissive = reader.ReadSingle(),
            Light = reader.ReadSingle(),
            Brightness = reader.ReadSingle(),
            Pressure = reader.ReadSingle(),
            Viscosity = reader.ReadSingle(),
            Lift = reader.ReadSingle(),
            Radius = reader.ReadSingle(),
            Lifetime = reader.ReadInt32(),
            EmissionTicks = reader.ReadInt32(),
            Color = new Color { PackedValue = reader.ReadUInt32() },
            FadeColor = new Color { PackedValue = reader.ReadUInt32() },
            ColorFadeRate = reader.ReadSingle()
        });
        Projectile.hostile = settings.Hostile;
        ready = true;
    }
}
