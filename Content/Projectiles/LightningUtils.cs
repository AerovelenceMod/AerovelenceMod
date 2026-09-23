using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using AerovelenceMod.Common.Systems;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Dusts.GlowDusts;

namespace AerovelenceMod.Content.Projectiles
{
    public static class LightningUtils
    {
        public class LightningData
        {
            internal LightningStrokeRenderer StrokeRenderer;
            public int MaxSegments = 12;
            public float BranchChance = 1f;
            public int MaxBranches = 2;

            public Vector2[] SegmentPositions;
            public float[] SegmentOffsets;
            public List<Branch> Branches;
            public Vector2 TargetPosition;
            public float Alpha = 1f;
            public bool Initialized;
            public float DistanceToTarget;

            public LightningStyle Style = LightningStyle.Default;

            public float DisplacementIntensity = 1.0f;
            public float NoiseFrequency = 0.5f;

            public Projectile Projectile;
            public NPC Npc;
            public Vector2 WorldOrigin;
            public Vector2 WorldTarget;

            public bool HasStaticUpdated = false;
            public int StaticTimer = 0;
            public int StaticMaxTime = 60;

            public Color? CoreColorOverride;
            public Color? MidColorOverride;
            public Color? OuterColorOverride;
            public Color? DistColorOverride;
            public Color? FlashColorOverride;

            public float GlowIntensity = 0.4f;
            public float GlowScale = 0.15f;

            public float StartThickness = 4f;
            public float EndThickness = 0.6f;
            public float BranchThicknessMultiplier = 0.55f;
            public float BranchTipThicknessMultiplier = 0.08f;
            public int GeometryVersion;

            public float ThicknessAt(float progress) => MathHelper.Lerp(
                StartThickness, EndThickness, MathHelper.Clamp(progress, 0f, 1f));

            public LightningData(Projectile projectile, LightningStyle style = LightningStyle.Default)
            {
                Projectile = projectile;
                Style = style;

                switch (Style)
                {
                    case LightningStyle.Jagged:
                        DisplacementIntensity = 0.5f;
                        NoiseFrequency = 10f;
                        break;
                    case LightningStyle.Smooth:
                        DisplacementIntensity = 0.5f;
                        NoiseFrequency = 0.3f;
                        break;
                    case LightningStyle.Chaotic:
                        DisplacementIntensity = 3.5f;
                        NoiseFrequency = 1.8f;
                        break;
                    case LightningStyle.Static:
                        DisplacementIntensity = 8.5f;
                        NoiseFrequency = 7.0f;
                        StaticMaxTime = 60;
                        break;
                    default:
                        DisplacementIntensity = 1.0f;
                        NoiseFrequency = 0.5f;
                        break;
                }
            }

            public LightningData(NPC npc, LightningStyle style = LightningStyle.Default)
            {
                Npc = npc;
                Style = style;

                switch (Style)
                {
                    case LightningStyle.Jagged:
                        DisplacementIntensity = 0.5f;
                        NoiseFrequency = 10f;
                        break;
                    case LightningStyle.Smooth:
                        DisplacementIntensity = 0.5f;
                        NoiseFrequency = 0.3f;
                        break;
                    case LightningStyle.Chaotic:
                        DisplacementIntensity = 3.5f;
                        NoiseFrequency = 1.8f;
                        break;
                    case LightningStyle.Static:
                        DisplacementIntensity = 1.5f;
                        NoiseFrequency = 1.0f;
                        StaticMaxTime = 60;
                        break;
                    default:
                        DisplacementIntensity = 1.0f;
                        NoiseFrequency = 0.5f;
                        break;
                }
            }

            public LightningData(Vector2 worldOrigin, Vector2 worldTarget, LightningStyle style = LightningStyle.Default)
            {
                WorldOrigin = worldOrigin;
                WorldTarget = worldTarget;
                Style = style;

                switch (Style)
                {
                    case LightningStyle.Jagged:
                        DisplacementIntensity = 0.5f;
                        NoiseFrequency = 10f;
                        break;
                    case LightningStyle.Smooth:
                        DisplacementIntensity = 0.5f;
                        NoiseFrequency = 0.3f;
                        break;
                    case LightningStyle.Chaotic:
                        DisplacementIntensity = 3.5f;
                        NoiseFrequency = 1.8f;
                        break;
                    case LightningStyle.Static:
                        DisplacementIntensity = 8.5f;
                        NoiseFrequency = 7.0f;
                        StaticMaxTime = 60;
                        break;
                    default:
                        DisplacementIntensity = 1.0f;
                        NoiseFrequency = 0.5f;
                        break;
                }
            }
        }

        public class Branch
        {
            public Vector2[] Positions { get; set; }
            public float[] Offsets { get; set; }
            public float Alpha { get; set; }
            public int LifeTime { get; set; }
            public float ParentProgress { get; set; }
        }

        public enum LightningStyle
        {
            Default,
            Jagged,
            Smooth,
            Chaotic,
            Static
        }

        public static void InitializeProjectiles(LightningData data)
        {
            data.SegmentPositions = new Vector2[data.MaxSegments];
            data.SegmentOffsets = new float[data.MaxSegments];
            data.Branches = new List<Branch>();

            Vector2 direction = data.TargetPosition - data.Projectile.Center;
            data.DistanceToTarget = direction.Length();
            float segmentLength = data.DistanceToTarget / (data.MaxSegments - 1);
            direction.Normalize();

            for (int i = 0; i < data.MaxSegments; i++)
            {
                data.SegmentPositions[i] = data.Projectile.Center + direction * (segmentLength * i);
                data.SegmentOffsets[i] = 0f;
            }
        }

        public static void InitializeNPCs(LightningData data)
        {
            data.SegmentPositions = new Vector2[data.MaxSegments];
            data.SegmentOffsets = new float[data.MaxSegments];
            data.Branches = new List<Branch>();

            Vector2 direction = data.TargetPosition - data.Npc.Center;
            data.DistanceToTarget = direction.Length();
            float segmentLength = data.DistanceToTarget / (data.MaxSegments - 1);
            direction.Normalize();

            for (int i = 0; i < data.MaxSegments; i++)
            {
                data.SegmentPositions[i] = data.Npc.Center + direction * (segmentLength * i);
                data.SegmentOffsets[i] = 0f;
            }
        }

        public static void InitializeBetweenPoints(LightningData data, Vector2 startPos, Vector2 endPos, LightningStyle style = LightningStyle.Default)
        {
            data.Style = style;

            if (!data.Initialized)
            {
                data.Branches = new List<Branch>();
                data.SegmentPositions = new Vector2[data.MaxSegments];
                data.SegmentOffsets = new float[data.MaxSegments];
                data.Initialized = true;
            }

            Vector2 direction = endPos - startPos;
            data.DistanceToTarget = direction.Length();
            float segmentLength = data.DistanceToTarget / (data.MaxSegments - 1);
            direction.Normalize();

            for (int i = 0; i < data.MaxSegments; i++)
            {
                data.SegmentPositions[i] = startPos + direction * (segmentLength * i);
            }
        }


        public static void UpdateSegments(LightningData data)
        {
            data.GeometryVersion++;
            if (data.Style == LightningStyle.Static)
            {
                data.StaticTimer++;
                float fade = 1f - (data.StaticTimer / (float)data.StaticMaxTime);
                data.Alpha = MathHelper.Clamp(fade, 0f, 1f);

                if (!data.HasStaticUpdated)
                {
                    DoChaoticPass(data);
                    for (int i = 0; i < 25; i++)
                    {
                        CreateBranch(data);
                    }

                    data.HasStaticUpdated = true;
                }
                return;
            }

            DoChaoticPass(data);

            if (Main.rand.NextFloat() < data.BranchChance && data.Branches.Count < data.MaxBranches)
            {
                CreateBranch(data);
            }
        }

        private static void DoChaoticPass(LightningData data)
        {
            float time = Main.GameUpdateCount;
            float globalIntensity = (float)(
                Math.Sign(Math.Sin(time * 0.1f)) * 0.2f
                + Math.Sign(Math.Cos(time * 0.15f)) * 0.1f
                + 0.3f
            );
            if (data.Style == LightningStyle.Static)
            {
                int segmentGroups = 10;
                int segmentsPerGroup = data.MaxSegments / segmentGroups;

                for (int i = 1; i < data.MaxSegments - 1; i++)
                {
                    int groupIndex = i / segmentsPerGroup;
                    float groupProgress = (i % segmentsPerGroup) / (float)segmentsPerGroup;
                    float zigzagFactor = (groupIndex % 2 == 0) ?
                        MathHelper.SmoothStep(0, 1, groupProgress) :
                        MathHelper.SmoothStep(1, 0, groupProgress);
                    float xOffset = zigzagFactor * 2 - 1;
                    xOffset *= 25f;
                    float randomVariation = Main.rand.NextFloat(-5f, 5f);
                    Vector2 normal = (data.SegmentPositions[i + 1] - data.SegmentPositions[i - 1])
                        .RotatedBy(MathHelper.PiOver2)
                        .SafeNormalize(Vector2.Zero);
                    Vector2 horizontalOffset = new Vector2(xOffset, 0);
                    Vector2 randomOffset = normal * randomVariation;
                    data.SegmentPositions[i] += horizontalOffset + randomOffset * 0.5f;
                    data.SegmentOffsets[i] = xOffset + randomVariation;
                }
                if (Main.rand.NextBool(2))
                {
                    int segment = Main.rand.Next(data.MaxSegments / 4, (data.MaxSegments * 3) / 4);
                    float displacementAmount = Main.rand.NextFloat(-70f, 70f);
                    float horizontalBias = Main.rand.NextFloat(15f, 25f) * (Main.rand.NextBool() ? 1 : -1);

                    Vector2 normal = (data.SegmentPositions[segment + 1] - data.SegmentPositions[segment - 1])
                        .RotatedBy(MathHelper.PiOver2)
                        .SafeNormalize(Vector2.Zero);

                    data.SegmentPositions[segment] += normal * displacementAmount + new Vector2(horizontalBias, 0);
                }
            }
            else
            {
                for (int i = 1; i < data.MaxSegments - 1; i++)
                {
                    float centerEmphasis = (float)Math.Exp(
                        -(
                            Math.Pow(i - data.MaxSegments / 2f, 2)
                            / (2 * Math.Pow(data.MaxSegments / 4f, 2))
                        )
                    ) * 0.7f;
                    float noiseFactor = data.NoiseFrequency * (Main.rand.NextFloat() - 0.5f) * 2f;
                    float roughnessMultiplier = 1.5f;
                    float noise = (
                        (float)Math.Sign(Math.Sin(time * 0.8f + i * noiseFactor)) * 1.2f
                        + (float)Math.Sign(Math.Cos(time * 0.5f + i * 0.7f)) * 1.0f
                        + ((float)Math.Sin(time * 1.2f + i * 0.2f) > 0 ? 1 : -1) * globalIntensity * 1.8f
                    ) * centerEmphasis * roughnessMultiplier;
                    float finalAmplitude = Math.Min(8f, data.DistanceToTarget * 0.08f);
                    data.SegmentOffsets[i] = noise * finalAmplitude * data.DisplacementIntensity;
                    Vector2 normal = (data.SegmentPositions[i + 1] - data.SegmentPositions[i - 1])
                        .RotatedBy(MathHelper.PiOver2)
                        .SafeNormalize(Vector2.Zero);
                    float tangentOffset = (float)Math.Sign(Math.Sin(time * 0.6f + i * 0.8f)) * 0.7f * centerEmphasis;
                    float suddenMultiplier = Main.rand.NextBool(20) ? 1.5f : 1f;
                    data.SegmentPositions[i] += (normal * data.SegmentOffsets[i] + tangentOffset * Vector2.UnitX) * suddenMultiplier;
                }
                if (Main.rand.NextBool(6))
                {
                    int segment = Main.rand.Next(data.MaxSegments / 4, (data.MaxSegments * 3) / 4);
                    float displacementAmount = Main.rand.NextFloat(-6f, 6f);
                    Vector2 normal = (data.SegmentPositions[segment + 1] - data.SegmentPositions[segment - 1])
                        .RotatedBy(MathHelper.PiOver2)
                        .SafeNormalize(Vector2.Zero);
                    data.SegmentPositions[segment] += normal * displacementAmount;
                }
            }
        }

        private static void CreateBranch(LightningData data)
        {
            int startSegment = Main.rand.Next(1, data.MaxSegments - 2);
            int branchSegments = Main.rand.Next(3, 6);

            if (data.Style == LightningStyle.Static)
            {
                branchSegments = Main.rand.Next(3, 10);
            }

            Branch branch = new()
            {
                Positions = new Vector2[branchSegments],
                Offsets = new float[branchSegments],
                Alpha = 0.7f,
                ParentProgress = startSegment / (float)(data.MaxSegments - 1),
                LifeTime = Main.rand.Next(10, 20)
            };

            Vector2 branchDirection = (
                data.SegmentPositions[startSegment + 1] - data.SegmentPositions[startSegment]
            ).RotatedBy(Main.rand.NextFloat(-0.7f, 0.7f)).SafeNormalize(Vector2.Zero);

            float segmentLength = 8f;

            for (int i = 0; i < branch.Positions.Length; i++)
            {
                branch.Positions[i] = data.SegmentPositions[startSegment] + branchDirection * (i * segmentLength);

                if (i > 0 && i < branch.Positions.Length - 1)
                {
                    float displacementIntensity = data.DisplacementIntensity * (0.5f + 0.5f * (i / (float)branch.Positions.Length));
                    float randomOffset = Main.rand.NextFloat(-displacementIntensity, displacementIntensity);
                    Vector2 normal = branchDirection.RotatedBy(MathHelper.PiOver2);
                    branch.Positions[i] += normal * randomOffset * 5f;
                }

                branch.Offsets[i] = 0f;
            }

            data.Branches.Add(branch);
        }


        public static void UpdateBranches(LightningData data)
        {
            data.GeometryVersion++;
            for (int i = data.Branches.Count - 1; i >= 0; i--)
            {
                Branch branch = data.Branches[i];
                branch.LifeTime--;
                if (branch.LifeTime <= 0)
                {
                    data.Branches.RemoveAt(i);
                    continue;
                }
                branch.Alpha *= 0.95f;
                float time = Main.GameUpdateCount;
                for (int j = 1; j < branch.Positions.Length - 1; j++)
                {
                    float noise = (float)(
                        Math.Sin(time * 0.7f + j * 0.3f) * 1.5f
                        + Math.Cos(time * 0.4f + j * 0.6f) * 1.0f
                    );
                    branch.Offsets[j] = noise;
                    Vector2 normal = (branch.Positions[j + 1] - branch.Positions[j - 1])
                        .RotatedBy(MathHelper.PiOver2)
                        .SafeNormalize(Vector2.Zero);
                    branch.Positions[j] += normal * (branch.Offsets[j] - branch.Offsets[j]);
                }
            }
        }

        public static void SpawnDust(LightningData data)
        {
            PixellationSystem.QueuePixelationAction(() =>
            {
                for (int i = 0; i < 0.2; i++)
                {
                    Vector2 randomSegment = data.SegmentPositions[Main.rand.Next(0, data.MaxSegments)];
                    Vector2 dir = (data.SegmentPositions[data.MaxSegments - 1] - data.SegmentPositions[0])
                        .SafeNormalize(Vector2.Zero);

                    Color dustColor = Color.Lerp(
                        new Color(0, 236, 255),
                        new Color(0, 255, 191),
                        Main.rand.NextFloat()
                    );

                    Dust a = Dust.NewDustPerfect(
                        (randomSegment + dir * 2f) / 2,
                        ModContent.DustType<GlowStrong>(),
                        dir.RotatedByRandom(0.5f) * Main.rand.NextFloat(1f, 3f),
                        0,
                        newColor: dustColor,
                        Scale: Main.rand.NextFloat(0.5f, 2f)
                    );
                    a.alpha = 2;
                }

                foreach (Branch branch in data.Branches)
                {
                    if (Main.rand.NextBool(3))
                    {
                        for (int i = 0; i < branch.Positions.Length - 1; i++)
                        {
                            Vector2 dustPos = Vector2.Lerp(
                                branch.Positions[i],
                                branch.Positions[i + 1],
                                Main.rand.NextFloat()
                            );

                            Color dustColor = Color.Lerp(
                                Color.Aqua,
                                Color.LightBlue,
                                Main.rand.NextFloat()
                            );

                            Dust dust = Dust.NewDustPerfect(
                                dustPos,
                                DustID.Electric,
                                Vector2.Zero,
                                0,
                                dustColor * branch.Alpha * data.Alpha,
                                Main.rand.NextFloat(0.6f, 0.9f) * branch.Alpha
                            );
                            dust.noGravity = true;
                            dust.fadeIn = 0f;
                        }
                    }
                }
            }, PixellationSystem.RenderType.Additive);
        }

        public static void DrawLightning(LightningData data, SpriteBatch spriteBatch)
        {
            if (Main.dedServ || data?.SegmentPositions == null || data.Alpha <= 0) return;
            data.StrokeRenderer ??= new LightningStrokeRenderer();
            data.StrokeRenderer.Draw(data, data.Alpha, layer: RenderLayer.UnderProjectiles);
        }

        public static void DrawTaperedLightning(LightningData data, SpriteBatch spriteBatch)
        {
            if (Main.dedServ || data?.SegmentPositions == null || data.Alpha <= 0) return;
            data.StrokeRenderer ??= new LightningStrokeRenderer();
            data.StrokeRenderer.DrawImmediate(data, spriteBatch, data.Alpha);
        }
    }
}
