using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;

namespace AerovelenceMod.Common.Utilities;

public sealed class SlimeSpikes
{
    public const int Count = 4;
    public const int RegrowTicks = 42;
    private readonly ulong[] firedAt = new ulong[Count];
    private readonly byte[] receivedAge = new byte[Count];
    private byte firedMask;

    public static Vector2 Direction(int slot) => MathHelper.Lerp(-2.65f, -.49f, Math.Clamp(slot, 0, Count - 1) / 3f).ToRotationVector2();

    public float Growth(int slot, ulong tick)
    {
        if ((firedMask & 1 << slot) == 0) return 1f;
        float age = receivedAge[slot] + (tick >= firedAt[slot] ? tick - firedAt[slot] : 0f);
        return MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(age / RegrowTicks, 0f, 1f));
    }

    public int Fire(Vector2 velocity, ulong tick)
    {
        Vector2 direction = velocity.LengthSquared() > .001f ? Vector2.Normalize(velocity) : -Vector2.UnitY;
        int selected = 0;
        float best = float.NegativeInfinity;
        for (int slot = 0; slot < Count; slot++)
        {
            float score = Vector2.Dot(Direction(slot), direction) + Growth(slot, tick) * 3f;
            if (score <= best) continue;
            best = score;
            selected = slot;
        }
        FireSlot(selected, tick);
        return selected;
    }

    public void FireSlot(int slot, ulong tick)
    {
        if (slot < 0 || slot >= Count) return;
        firedMask |= (byte)(1 << slot);
        firedAt[slot] = tick;
        receivedAge[slot] = 0;
    }

    public void Apply(SlimeShape shape, ulong tick)
    {
        for (int slot = 0; slot < Count; slot++)
        {
            Vector2 direction = Direction(slot);
            Vector2 boundary = SurfacePoint(shape, direction);
            Vector2 root = shape.GetGelPoint(boundary * .9f);
            Vector2 tip = shape.GetGelPoint(boundary * 1.04f);
            Vector2 outward = tip - root;
            outward = outward.LengthSquared() > .001f ? Vector2.Normalize(outward) : direction;
            float growth = Growth(slot, tick);
            float length = MathHelper.Clamp(Math.Min(shape.Size.X, shape.Size.Y) * .78f, 7f, 14f);
            shape.SetSpike(slot, root, root + outward * (length + Vector2.Distance(root, tip)) * growth,
                MathHelper.Clamp(shape.Size.X * .2f, 2.6f, 4f) * MathF.Sqrt(growth));
        }
    }

    public static Vector2 SurfacePoint(SlimeShape shape, Vector2 direction)
    {
        direction = direction.LengthSquared() > .001f ? Vector2.Normalize(direction) : -Vector2.UnitY;
        Vector2 flow = shape.ToBodySpace(shape.Flow) / Vector2.Max(shape.Size, Vector2.One);
        flow = flow.LengthSquared() > .001f ? Vector2.Normalize(flow) : Vector2.UnitY;
        float low = 0f, high = 2f;
        for (int step = 0; step < 16; step++)
        {
            float middle = (low + high) * .5f;
            if (SlimeGelGeometry.Distance(direction * middle, flow, shape.AirborneBlend, shape.Time, suspensionTension: shape.SuspensionTension) > 0f) high = middle;
            else low = middle;
        }
        return direction * ((low + high) * .5f);
    }

    public void Write(BinaryWriter writer, ulong tick)
    {
        for (int slot = 0; slot < Count; slot++)
        {
            ulong age = (firedMask & 1 << slot) == 0 ? RegrowTicks : receivedAge[slot] + (tick >= firedAt[slot] ? tick - firedAt[slot] : 0);
            writer.Write((byte)Math.Min((ulong)RegrowTicks, age));
        }
    }

    public void Read(BinaryReader reader, ulong tick)
    {
        firedMask = 0;
        for (int slot = 0; slot < Count; slot++)
        {
            int age = Math.Min(RegrowTicks, (int)reader.ReadByte());
            if (age == RegrowTicks) continue;
            firedMask |= (byte)(1 << slot);
            firedAt[slot] = tick;
            receivedAge[slot] = (byte)age;
        }
    }
}
