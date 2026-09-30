using System;
using Microsoft.Xna.Framework;
using Terraria;
namespace AerovelenceMod.Common.Utilities
{
    public sealed class SlimeShape
    {
        public sealed class Tendril
        {
            public Vector2 Root, Bend, Tip;
            public Vector2 BendVelocity, TipVelocity;
            public float Radius, TipExtension, Pad;
            public Vector2 TipDirection = -Vector2.UnitY;
            public Vector2 PadNormal = Vector2.UnitY;
            public bool Pinned;
            public Vector2 ExtendedTip => Tip + TipDirection * TipExtension;
            public Vector2 Point(float t) => Vector2.Lerp(Vector2.Lerp(Root, Bend, t), Vector2.Lerp(Bend, Tip, t), t);
            public float Width(float t)
            {
                t = MathHelper.Clamp(t, 0f, 1f);
                float core = MathHelper.Clamp(Radius * .24f, 1.7f, 2.8f);
                float root = MathF.Pow(1f - t, 3.2f) * MathHelper.Clamp(Radius * .34f, .8f, 3.1f);
                float tipStrength = MathHelper.Clamp(TipExtension / 14f + Pad * .65f, 0f, 1f);
                float tip = MathF.Pow(t, 4f) * MathHelper.Clamp(Radius * .28f, .65f, 2.6f) * tipStrength;
                float pulse = MathF.Sin(t * MathHelper.Pi) * MathHelper.Clamp(Radius * .035f, 0f, .28f);
                return core + root + tip + pulse;
            }
        }
        public sealed class Lobe
        {
            public Vector2 Center, Size;
            public float BodyDome, BottomCutoff, RippleStrength, Phase;
        }
        public readonly Tendril[] Tendrils;
        public readonly Lobe[] Lobes;
        public int TendrilCount;
        public int LobeCount;
        public bool Initialized;
        public Vector2 Center, Size;
        public float Time;
        public Vector2[] SurfacePoints { get; } = new Vector2[17];
        public Vector2[] SurfaceNormals { get; } = new Vector2[17];
        public int SurfaceCount;
        public float SurfaceBlend;
        public float BodyDome;
        public float BottomCutoff = .9f;
        public float RippleStrength = 1f;
        public Vector2 WorldPosition => worldPosition;
        private Vector2 previousPosition;
        private Vector2 displacement;
        private Vector2 worldPosition;
        public SlimeShape(int tendrilCapacity = 0, int lobeCapacity = 4)
        {
            Tendrils = new Tendril[Math.Clamp(tendrilCapacity, 0, 32)];
            for (int i = 0; i < Tendrils.Length; i++) Tendrils[i] = new Tendril();
            Lobes = new Lobe[Math.Clamp(lobeCapacity, 0, 16)];
            for (int i = 0; i < Lobes.Length; i++) Lobes[i] = new Lobe();
        }
        public void Begin(Vector2 center, Vector2 size, float time, Vector2 position)
        {
            Center = center;
            Size = size;
            Time = time;
            TendrilCount = 0;
            LobeCount = 0;
            SurfaceCount = 0;
            SurfaceBlend = 0f;
            displacement = Initialized ? position - previousPosition : Vector2.Zero;
            if (displacement.LengthSquared() > 160 * 160) Initialized = false;
            previousPosition = position;
            worldPosition = position;
        }
        public bool AddLobe(Vector2 center, Vector2 size, float bodyDome = 0f, float bottomCutoff = 1f, float rippleStrength = 1f, float phase = 0f)
        {
            if (LobeCount >= Lobes.Length) return false;
            Lobe lobe = Lobes[LobeCount++];
            lobe.Center = center;
            lobe.Size = Vector2.Max(size, new Vector2(.1f));
            lobe.BodyDome = bodyDome;
            lobe.BottomCutoff = bottomCutoff;
            lobe.RippleStrength = rippleStrength;
            lobe.Phase = phase;
            return true;
        }
        public void AlignPinnedTips(Vector2 position)
        {
            Vector2 offset = worldPosition - position;
            for (int i = 0; i < TendrilCount; i++)
            {
                Tendril tendril = Tendrils[i];
                if (!tendril.Pinned) continue;
                tendril.Tip += offset;
                tendril.Bend += offset * .5f;
            }
            worldPosition = position;
        }
        public void SetTendril(int index, Vector2 root, Vector2 bend, Vector2 tip, float radius, float tipExtension, bool pad, Vector2 padNormal, bool pinned)
        {
            Tendril tendril = Tendrils[index];
            TendrilCount = Math.Max(TendrilCount, index + 1);
            tendril.Root = root;
            if (!Initialized || tendril.Radius < .1f)
            {
                tendril.Bend = bend;
                tendril.Tip = tip;
                tendril.BendVelocity = tendril.TipVelocity = Vector2.Zero;
            }
            else
            {
                tendril.Bend -= displacement * .6f;
                tendril.Tip -= displacement * .8f;
                Spring(ref tendril.Bend, ref tendril.BendVelocity, bend, .19f, .7f);
                Spring(ref tendril.Tip, ref tendril.TipVelocity, tip, pad ? .38f : .25f, .64f);
            }
            tendril.Pinned = pinned;
            if (pinned) { tendril.Tip = tip; tendril.TipVelocity = Vector2.Zero; }
            tendril.PadNormal = padNormal;
            tendril.Radius = MathHelper.Lerp(tendril.Radius, radius, Initialized ? .35f : 1f);
            tendril.TipExtension = MathHelper.Lerp(tendril.TipExtension, tipExtension, Initialized ? .28f : 1f);
            tendril.Pad = MathHelper.Lerp(tendril.Pad, pad ? 1f : 0f, .25f);
            Vector2 direction = tendril.Tip - tendril.Bend;
            if (direction.LengthSquared() > .01f)
            {
                direction.Normalize();
                float angle = MathHelper.WrapAngle(MathF.Atan2(direction.Y, direction.X) - MathF.Atan2(tendril.TipDirection.Y, tendril.TipDirection.X));
                float rotation = MathF.Atan2(tendril.TipDirection.Y, tendril.TipDirection.X) + angle * (Initialized ? .3f : 1f);
                tendril.TipDirection = new Vector2(MathF.Cos(rotation), MathF.Sin(rotation));
            }
        }
        public Vector2 GetBodyPoint(Vector2 normalizedPoint) => Center + normalizedPoint * Size;
        public Vector2 GetWorldBodyPoint(Vector2 normalizedPoint) => worldPosition + GetBodyPoint(normalizedPoint);
        public Vector2 GetLobePoint(int index, Vector2 normalizedPoint) => Lobes[index].Center + normalizedPoint * Lobes[index].Size;
        public Vector2 GetWorldLobePoint(int index, Vector2 normalizedPoint) => worldPosition + GetLobePoint(index, normalizedPoint);
        public Vector2 GetTendrilPoint(int index, float t) => Tendrils[index].Point(MathHelper.Clamp(t, 0f, 1f));
        public Vector2 GetWorldTendrilPoint(int index, float t) => worldPosition + GetTendrilPoint(index, t);
        public Vector2 GetTendrilTip(int index, bool extended = false) => extended ? Tendrils[index].ExtendedTip : Tendrils[index].Tip;
        public Vector2 GetWorldTendrilTip(int index, bool extended = false) => worldPosition + GetTendrilTip(index, extended);
        public static void Spring(ref Vector2 position, ref Vector2 velocity, Vector2 target, float stiffness, float damping)
        {
            velocity = (velocity + (target - position) * stiffness) * damping;
            position += velocity;
        }
        public bool TendrilIntersects(int index, Vector2 position, Rectangle target, float extensionWidth = 5f)
        {
            if (!Initialized || index < 0 || index >= TendrilCount) return false;
            Tendril tendril = Tendrils[index];
            if (tendril.Radius < .2f) return false;
            Vector2 previous = position + tendril.Root;
            for (int i = 1; i <= 16; i++)
            {
                float t = i / 16f;
                Vector2 point = position + tendril.Point(t);
                float distance = 0f;
                if (Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), previous, point, tendril.Width(t) * 2f, ref distance)) return true;
                previous = point;
            }
            float extensionHit = 0f;
            return tendril.TipExtension > 1f && extensionWidth > 0f && Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), previous, position + tendril.ExtendedTip, extensionWidth, ref extensionHit);
        }
    }

}
