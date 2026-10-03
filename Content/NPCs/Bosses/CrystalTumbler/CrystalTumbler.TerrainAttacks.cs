using System;
using AerovelenceMod.Content.Items.BossSummons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler
{
    public partial class CrystalTumbler
    {
        private const float RailLaunchSpeed = 11.5f;
        private float railProgress;
        private float railSpeed;
        internal float CascadeProgress => railProgress;
        internal bool CascadeFinished => State != TumblerState.RailDash || substate >= 3;
        internal float LoopRailProgress => railProgress;
        internal bool LoopRailFinished => State != TumblerState.LoopSlam || substate >= 4;

        private void RailDash()
        {
            if (substate == 0)
            {
                float startX = storedDirection > 0 ? LeftInner + 90f : RightInner - 90f;
                MoveHorizontal(startX, 8f, 0.14f);
                DescendToArenaFloor();
                if (Math.Abs(NPC.Center.X - startX) > 8f || Math.Abs(NPC.Bottom.Y - FloorY) > 2f || !OnGround())
                    return;
                rampStart = new Vector2(startX, FloorY - NPC.height * 0.5f);
                SpawnProjectile<TumblerCascadeRail>(rampStart, Vector2.Zero, 0, 0f, NPC.whoAmI, storedDirection);
                substate = 1;
                StateTimer = 0;
                NPC.netUpdate = true;
            }
            if (substate == 1)
            {
                SpinUp(80, RailLaunchSpeed);
                if (StateTimer < 80 || !OnGround())
                    return;
                substate = 2;
                railSpeed = RailLaunchSpeed;
                StateTimer = 0;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.65f }, NPC.Center);
            }
            if (substate == 2)
            {
                NPC.noGravity = NPC.noTileCollide = true;
                contactDamage = true;
                float previousProgress = railProgress;
                Vector2 destination = TumblerRailMotion.Advance(t => TumblerCascadeRail.Point(rampStart, storedDirection, t), ref railProgress, ref railSpeed);
                destination.Y = Math.Min(destination.Y, FloorY - NPC.height * 0.5f);
                NPC.velocity = destination - NPC.Center;
                spinTarget = storedDirection * railSpeed / 52f;
                visualCharge = 0.8f;
                if (previousProgress < 0.5f && railProgress >= 0.5f)
                {
                    KickUpDust(12);
                    SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.6f, Pitch = 0.35f }, NPC.Center);
                }
                if (railProgress >= 1f)
                {
                    substate = 3;
                    StateTimer = 0;
                    NPC.netUpdate = true;
                }
                return;
            }
            if (substate == 3)
            {
                NPC.noGravity = NPC.noTileCollide = false;
                RollTowardPlayer(3.5f, 0.12f);
                if (StateTimer == 1)
                {
                    KickUpDust(12);
                    ThrowImpactRubble(8);
                }
                if (StateTimer >= 25)
                    FinishAttack();
            }
        }

        private void RippleSlam()
        {
            if (substate == 0)
            {
                MoveHorizontal(ArenaData.ArenaCenter.X, 7f, 0.12f);
                if (Math.Abs(NPC.Center.X - ArenaData.ArenaCenter.X) > 8f || !OnGround())
                    return;
                rampStart = new Vector2(NPC.Center.X, FloorY - 52f);
                substate = 1;
                StateTimer = 0;
                NPC.netUpdate = true;
            }
            if (substate == 1)
            {
                SpinUp(75, 14f);
                if (StateTimer < 75 || !OnGround())
                    return;
                substate = 2;
                StateTimer = 0;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
            }
            if (substate == 2)
            {
                NPC.noGravity = NPC.noTileCollide = true;
                contactDamage = StateTimer >= 30;
                float progress = MathHelper.Clamp((StateTimer + 1f) / 60f, 0f, 1f);
                float height = Math.Min(230f, FloorY - ArenaData.WorldBounds.Top - 170f);
                Vector2 destination = rampStart - new Vector2(0f, MathF.Sin(progress * MathHelper.Pi) * height);
                NPC.velocity = destination - NPC.Center;
                spinTarget = storedDirection * 0.2f;
                if (StateTimer < 59)
                    return;
                NPC.Center = rampStart;
                NPC.velocity = Vector2.Zero;
                impactFlash = 1f;
                KickUpDust(24);
                ThrowImpactRubble(18);
                ScreenShake(13f);
                SpawnAuraPulse(200f, 35, false);
                SpawnProjectile<TumblerFloorRipple>(new Vector2(rampStart.X, FloorY), Vector2.Zero, 0, 0f, NPC.whoAmI);
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.95f, Pitch = -0.6f }, NPC.Center);
                FinishAttack();
            }
        }

        private void LoopSlam()
        {
            if (substate == 0)
            {
                float startX = TumblerLoopRail.StartX(storedDirection);
                MoveHorizontal(startX, 9f, 0.16f);
                if (Math.Abs(NPC.Center.X - startX) < 6f && Math.Abs(NPC.velocity.X) < 2f && OnGround())
                {
                    NPC.velocity.X *= 0.5f;
                    rampStart = new Vector2(NPC.Center.X, FloorY - NPC.height * 0.5f);
                    SpawnProjectile<TumblerLoopRail>(rampStart, Vector2.Zero, 0, 0f, NPC.whoAmI, 0f, storedDirection);
                    substate = 1;
                    StateTimer = 0;
                    NPC.netUpdate = true;
                }
                return;
            }
            if (substate == 1)
            {
                SpinUp(90, RailLaunchSpeed);
                if (StateTimer < 90 || !OnGround())
                    return;
                substate = 2;
                railSpeed = RailLaunchSpeed;
                WarnLoopSlam();
                StateTimer = 0;
                NPC.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.6f, Pitch = -0.1f }, NPC.Center);
            }
            if (substate == 2)
            {
                NPC.noGravity = NPC.noTileCollide = true;
                contactDamage = true;
                Vector2 destination = TumblerRailMotion.Advance(t => TumblerLoopRail.Point(rampStart, storedDirection, t), ref railProgress, ref railSpeed, railProgress < 0.2f ? 0.035f : 0f);
                NPC.velocity = destination - NPC.Center;
                spinTarget = storedDirection * railSpeed / 52f;
                visualCharge = 0.9f;
                if (railProgress >= 1f)
                {
                    substate = 4;
                    StateTimer = 0;
                    NPC.netUpdate = true;
                }
                return;
            }
            if (substate == 4)
            {
                NPC.noGravity = NPC.noTileCollide = true;
                contactDamage = true;
                NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.9f, 21f);
                if (NPC.Bottom.Y + NPC.velocity.Y < FloorY)
                    return;
                NPC.Bottom = new Vector2(NPC.Center.X, FloorY);
                NPC.velocity = Vector2.Zero;
                impactFlash = 1f;
                KickUpDust(18);
                ThrowImpactRubble(14);
                ScreenShake(11f);
                SpawnAuraPulse(160f, 30, false);
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.8f, Pitch = -0.4f }, NPC.Center);
                EnsureMagneticPlatforms();
                EnsureConductiveCrystals();
                TumblerLightningBolt.ReleaseSlam(NPC);
                FinishAttack(Main.expertMode ? 150 : 90);
            }
        }

        private void WarnLoopSlam()
        {
            int rings = Main.expertMode ? 5 : 3;
            float center = ArenaData.ArenaCenter.X;
            for (int ring = 0; ring < rings; ring++)
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = center + side * (80f + ring * (RightInner - center - 100f) / (rings - 1));
                    Vector2 source = new(x, ArenaData.WorldBounds.Top + 120f);
                    SpawnProjectile<TumblerLightningBolt>(source, new Vector2(0f, FloorY - source.Y), ProjectileDamage(19), 0f, 90f, PhaseTwo ? 1f : 0f, NPC.whoAmI + 1);
                }
        }

        private void GroundRaze()
        {
            GroundRoll(1.8f, 0.1f, 220f);
            visualCharge = MathHelper.Clamp(StateTimer / 120f, 0f, 1f);
            if (StateTimer == 1)
            {
                int side = Target.Center.X < ArenaData.ArenaCenter.X ? -1 : 1;
                SpawnProjectile<TumblerRazeBeam>(new Vector2(ArenaData.ArenaCenter.X, FloorY), Vector2.Zero, ProjectileDamage(21), 0f, NPC.whoAmI, side);
            }
            if (StateTimer >= 390)
                FinishAttack();
        }

        private void CrystalConvergence()
        {
            GroundRoll(0.9f, 0.06f, 180f);
            visualCharge = MathHelper.Clamp(StateTimer / 160f, 0f, 1f);
            if (StateTimer == 1)
                SpawnProjectile<TumblerConvergenceOrb>(TumblerConvergenceOrb.Anchor, Vector2.Zero, ProjectileDamage(22), 0f, NPC.whoAmI);
            if (StateTimer >= 550)
                FinishAttack();
        }
    }
}
