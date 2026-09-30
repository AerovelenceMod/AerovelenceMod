using System;
using System.Collections.Generic;
using System.Diagnostics;
using AerovelenceMod.Common.Systems.Language;
using ModLanguage = AerovelenceMod.Common.Systems.Language.Language;
using AerovelenceMod.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Gas;

public sealed class GasSystem : ModSystem
{
    private Terraria.Localization.LocalizedText deathMessage;

    public override void SetStaticDefaults()
    {
        deathMessage = LocalizationManager.RegisterTranslation("Mods.AerovelenceMod.Gas.Death", "{0} suffocated in crystal gas.", ModLanguage.Default);
    }

    internal GasFluid Colliding { get; private set; }
    internal GasFluid Free { get; private set; }
    internal GasFluid AdditiveColliding { get; private set; }
    internal GasFluid AdditiveFree { get; private set; }
    internal GasFluid[] VisualFields { get; } = new GasFluid[4];
    private readonly List<GasEmitter> emitters = new();
    private ulong visualEmissionTick;
    private int visualEmissions;
    private int emitterSeed;
    private readonly List<(Vector2 Position, Vector2 Direction, bool Radial)> vacuums = new();
    private int damageTimer;
    internal float RenderBlend => damageTimer % 2 == 0 ? 0.5f : 1f;
    internal bool HasAnyGas
    {
        get
        {
            if ((Colliding?.ChunkCount ?? 0) > 0 || (Free?.ChunkCount ?? 0) > 0 || (AdditiveColliding?.ChunkCount ?? 0) > 0 || (AdditiveFree?.ChunkCount ?? 0) > 0)
                return true;
            for (int i = 0; i < VisualFields.Length; i++)
                if ((VisualFields[i]?.ChunkCount ?? 0) > 0)
                    return true;
            return false;
        }
    }

    public override void OnWorldLoad()
    {
        Colliding = new GasFluid(IsSolid);
        Free = new GasFluid();
        AdditiveColliding = new GasFluid(IsSolid, 64, 4);
        AdditiveFree = new GasFluid(null, 64, 4);
        damageTimer = 0;
        ClearVisuals();
    }

    internal static bool IsSolid(Vector2 position)
    {
        int x = (int)(position.X / 16f);
        int y = (int)(position.Y / 16f);
        if (!WorldGen.InWorld(x, y, 1))
            return true;
        Tile tile = Main.tile[x, y];
        if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])
            return false;
        float localX = position.X - x * 16;
        float localY = position.Y - y * 16;
        if (tile.IsHalfBlock)
            return localY >= 8f;
        return tile.Slope switch
        {
            SlopeType.SlopeDownRight => localY >= 16f - localX,
            SlopeType.SlopeDownLeft => localY >= localX,
            SlopeType.SlopeUpRight => localY <= localX,
            SlopeType.SlopeUpLeft => localY <= 16f - localX,
            _ => true
        };
    }

    internal static void Inject(Vector2 position, Vector2 velocity, GasSettings settings, int damage, int seed, int age, GasFluid visualField = null)
    {
        GasSystem system = ModContent.GetInstance<GasSystem>();
        GasFluid field = visualField ?? (settings.Additive
            ? settings.TileCollision ? system.AdditiveColliding : system.AdditiveFree
            : settings.TileCollision ? system.Colliding : system.Free);
        if (field is null)
            return;
        Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
        if (settings.FlowSpeed > 0f && settings.Density <= 0f && visualField is null)
        {
            Func<Vector2, Vector2, bool> visible = settings.TileCollision ? Open : null;
            system.Colliding.ApplyAirflow(position, direction, settings.FlowSpeed, settings.FlowRange, settings.Radius, settings.FlowSpread, visible);
            system.Free.ApplyAirflow(position, direction, settings.FlowSpeed, settings.FlowRange, settings.Radius, settings.FlowSpread, visible);
            system.AdditiveColliding.ApplyAirflow(position, direction, settings.FlowSpeed, settings.FlowRange, settings.Radius, settings.FlowSpread, visible);
            system.AdditiveFree.ApplyAirflow(position, direction, settings.FlowSpeed, settings.FlowRange, settings.Radius, settings.FlowSpread, visible);
            foreach (GasFluid visual in system.VisualFields)
                visual?.ApplyAirflow(position, direction, settings.FlowSpeed, settings.FlowRange, settings.Radius, settings.FlowSpread, visible);
        }
        if (settings.Density <= 0f)
            return;
        System.Numerics.Vector4 appearance = new(settings.Opacity, settings.Emissive, settings.Light, settings.Brightness);
        Vector3 fadeColor = settings.FadeColor.ToVector3() * settings.ColorFadeRate;
        System.Numerics.Vector4 colorFade = new(fadeColor.X, fadeColor.Y, fadeColor.Z, settings.ColorFadeRate);
        if (visualField is not null && !settings.Jet)
        {
            field.Inject(position, velocity, settings.Color.ToVector3(), settings.Density * 0.4f, Math.Clamp(settings.Radius * 0.5f, 8f, 32f),
                settings.Buoyancy ? settings.Lift : 0f, settings.Turbulence ? 1f : 0f,
                settings.Viscosity, 3f / settings.Lifetime, settings.Pressure * 0.035f, 0, false, appearance, colorFade);
            return;
        }
        Vector2 transverse = new(-direction.Y, direction.X);
        float pulse = MathF.Sin(seed * 1.37f + age * 0.63f);
        Vector2 impulse = velocity * (settings.Jet ? 1.8f : 1.4f) + transverse * pulse * (settings.Turbulence ? 0.7f : 0f);
        if (settings.Jet)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 point = position + direction * (i * 4f);
                if (settings.TileCollision && !Open(position, point))
                    break;
                int taps = Math.Max(3, (int)MathF.Ceiling(settings.Radius / 4f) * 2 + 1);
                float total = 0f;
                for (int tap = 0; tap < taps; tap++)
                {
                    float offset = tap * 2f / (taps - 1) - 1f;
                    total += MathF.Exp(-offset * offset * 3f);
                }
                for (int tap = 0; tap < taps; tap++)
                {
                    float offset = tap * 2f / (taps - 1) - 1f;
                    Vector2 nozzle = point + transverse * (offset * settings.Radius);
                    if (settings.TileCollision && !Open(position, nozzle))
                        continue;
                    float weight = MathF.Exp(-offset * offset * 3f) / total;
                    field.InjectJet(nozzle, impulse, settings.Color.ToVector3(), settings.Density * 0.11f * weight,
                        settings.Buoyancy ? settings.Lift : 0f, settings.Turbulence ? 1f : 0f,
                        settings.Viscosity * 0.3f, 3f / settings.Lifetime, settings.Pressure * 0.015f * weight, damage, settings.Hostile, appearance, colorFade);
                }
            }
            return;
        }
        float radius = Math.Clamp(settings.Radius * 0.3f, 10f, 18f);
        for (int i = 0; i < 3; i++)
        {
            Vector2 point = position + direction * (i * 7f);
            if (settings.TileCollision && !Open(position, point))
                break;
            field.Inject(point, impulse, settings.Color.ToVector3(), settings.Density * 0.22f, radius,
                settings.Buoyancy ? settings.Lift : 0f, settings.Turbulence ? 1f : 0f,
                settings.Viscosity * 0.3f, 3f / settings.Lifetime, settings.Pressure * 0.035f, damage, settings.Hostile, appearance, colorFade);
        }
    }

    internal static void Vacuum(Vector2 position, Vector2 direction, bool radial = false)
    {
        if (GasUtil.Finite(position) && GasUtil.Finite(direction))
            ModContent.GetInstance<GasSystem>().vacuums.Add((position, direction.SafeNormalize(Vector2.UnitX), radial));
    }

    internal bool EmitVisual(Vector2 position, Vector2 velocity, GasSettings settings)
    {
        if (Colliding is null || settings.Density <= 0f)
            return false;
        Vector2 halfView = new Vector2(Main.screenWidth, Main.screenHeight) / Math.Max(0.25f, Main.GameViewMatrix.Zoom.X) * 0.5f;
        Vector2 delta = position - Main.screenPosition - new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
        Vector2 offset = new(Math.Abs(delta.X), Math.Abs(delta.Y));
        if (offset.X > halfView.X + 384f || offset.Y > halfView.Y + 384f)
            return false;
        if (settings.TileCollision && IsSolid(position))
            return false;
        if (visualEmissionTick != Main.GameUpdateCount)
        {
            visualEmissionTick = Main.GameUpdateCount;
            visualEmissions = 0;
        }
        if (visualEmissions >= 64)
            return false;
        visualEmissions++;
        int index = (settings.Additive ? 2 : 0) + (settings.TileCollision ? 0 : 1);
        GasFluid field = VisualFields[index] ??= new GasFluid(settings.TileCollision ? IsSolid : null, 24, settings.Additive ? 4 : 8);
        Inject(position, velocity, settings, 0, 0, 0, field);
        return true;
    }

    internal GasEmitter CreateEmitter(Vector2 position, GasSettings gas, GasEmitterSettings settings)
    {
        if (Colliding is null || emitters.Count >= 48)
            return null;
        GasEmitter emitter = new(position, gas, settings, emitterSeed++);
        emitters.Add(emitter);
        return emitter;
    }

    internal void ClearVisuals()
    {
        foreach (GasEmitter emitter in emitters)
            emitter.Cancel();
        emitters.Clear();
        Array.Clear(VisualFields);
        visualEmissions = 0;
        visualEmissionTick = 0;
        emitterSeed = 0;
    }

    public override void PostUpdateProjectiles()
    {
        if (Colliding is null)
            return;
        long start = Stopwatch.GetTimestamp();
        if (!GasPerfControl.SimulationEnabled)
        {
            GasPerfControl.UpdateSimulation(start);
            return;
        }
        bool idle = emitters.Count == 0 && vacuums.Count == 0 && !HasAnyGas;
        if (idle)
        {
            GasPerfControl.UpdateSimulation(start);
            return;
        }
        for (int i = emitters.Count - 1; i >= 0; i--)
        {
            emitters[i].Update();
            if (!emitters[i].Active)
                emitters.RemoveAt(i);
        }
        foreach (var vacuum in vacuums)
        {
            Colliding.ApplyVacuum(vacuum.Position, vacuum.Direction, vacuum.Radial, Open);
            Free.ApplyVacuum(vacuum.Position, vacuum.Direction, vacuum.Radial, Open);
            AdditiveColliding.ApplyVacuum(vacuum.Position, vacuum.Direction, vacuum.Radial, Open);
            AdditiveFree.ApplyVacuum(vacuum.Position, vacuum.Direction, vacuum.Radial, Open);
            foreach (GasFluid visual in VisualFields)
                visual?.ApplyVacuum(vacuum.Position, vacuum.Direction, vacuum.Radial, Open);
        }
        vacuums.Clear();
        damageTimer++;
        Advance(Colliding);
        Advance(Free);
        Advance(AdditiveColliding);
        Advance(AdditiveFree);
        foreach (GasFluid visual in VisualFields)
            Advance(visual);
        if (damageTimer % 24 == 0)
            DamageNPCs();
        if (damageTimer % 15 == 0 && !Main.dedServ)
            DamageLocalPlayer();
        GasPerfControl.UpdateSimulation(start);
    }

    private void Advance(GasFluid field)
    {
        if (field is null)
            return;
        if (field.FastUpdates)
            field.Step();
        else if (damageTimer % 2 == 0)
            field.Step(2f);
    }

    private GasDye Sample(Vector2 position) => Colliding.Sample(position) + Free.Sample(position) + AdditiveColliding.Sample(position) + AdditiveFree.Sample(position);

    private GasDye SampleBody(Rectangle hitbox)
    {
        GasDye best = Sample(hitbox.Center.ToVector2());
        GasDye head = Sample(new Vector2(hitbox.Center.X, hitbox.Top + 4));
        GasDye feet = Sample(new Vector2(hitbox.Center.X, hitbox.Bottom - 4));
        return GasDye.Max(best, GasDye.Max(head, feet));
    }

    private void DamageNPCs()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy())
                continue;
            GasDye sample = Colliding.SampleDamage(npc.Hitbox, false);
            GasDye candidate = Free.SampleDamage(npc.Hitbox, false);
            if (candidate.Damage.X > sample.Damage.X) sample = candidate;
            candidate = AdditiveColliding.SampleDamage(npc.Hitbox, false);
            if (candidate.Damage.X > sample.Damage.X) sample = candidate;
            candidate = AdditiveFree.SampleDamage(npc.Hitbox, false);
            if (candidate.Damage.X > sample.Damage.X) sample = candidate;
            if (sample.Damage.X < 0.025f || sample.Damage.Z <= 0f)
                continue;
            int damage = Math.Max(1, (int)(sample.Damage.Z / sample.Damage.X * Math.Min(1f, sample.Damage.X / 0.12f)));
            npc.SimpleStrikeNPC(damage, 0, damageType: DamageClass.Magic);
        }
    }

    private void DamageLocalPlayer()
    {
        Player player = Main.LocalPlayer;
        if (!player.active || player.dead || player.immune)
            return;
        GasDye sample = SampleBody(player.Hitbox);
        if (sample.Damage.Y < 0.2f || sample.Damage.W < 0.2f)
            return;
        int damage = Math.Max(1, (int)(sample.Damage.W / sample.Damage.Y));
        player.Hurt(PlayerDeathReason.ByCustomReason(deathMessage.ToNetworkText(player.name)), damage, 0);
        player.AddBuff(BuffID.Slow, 90);
    }

    internal static bool Open(Vector2 from, Vector2 to) => Collision.CanHitLine(from, 1, 1, to, 1, 1);

    public override void OnWorldUnload()
    {
        Colliding = null;
        Free = null;
        AdditiveColliding = null;
        AdditiveFree = null;
        ClearVisuals();
        vacuums.Clear();
    }
}
