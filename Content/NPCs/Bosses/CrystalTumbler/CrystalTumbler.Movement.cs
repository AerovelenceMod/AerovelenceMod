using System;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Items.BossSummons;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler
{
    public partial class CrystalTumbler
    {
        private float rollVelocity;
        private int idleDirection;
        private int storedDirection;
        private int movementCommitTimer;
        private int wallReboundTicks = -1;
        private bool pendingWallDaze;

        private void PrepareMovement()
        {
            if (State != TumblerState.Despawn)
            {
                NPC.noGravity = false;
                NPC.noTileCollide = false;
            }
            RecoverArenaFloor();

            contactDamage = false;
            spinTarget = null;
        }

        private void RollTowardPlayer(float maxSpeed, float acceleration)
        {
            NPC.noGravity = NPC.noTileCollide = false;
            if (movementCommitTimer > 0)
                movementCommitTimer--;
            else if (Math.Abs(Target.Center.X - NPC.Center.X) > 70f || idleDirection == 0)
            {
                idleDirection = Target.Center.X >= NPC.Center.X ? 1 : -1;
                movementCommitTimer = 35;
            }
            float margin = NPC.velocity.X * NPC.velocity.X / (2f * acceleration) + 18f;
            if (NPC.Center.X < PassiveLeft + margin && idleDirection < 0 || NPC.Center.X > PassiveRight - margin && idleDirection > 0)
            {
                idleDirection = NPC.Center.X < ArenaData.ArenaCenter.X ? 1 : -1;
                movementCommitTimer = 50;
            }
            NPC.velocity.X = Approach(NPC.velocity.X, idleDirection * maxSpeed, acceleration);
            contactDamage = true;
        }

        private void GroundRoll(float maxSpeed, float acceleration, float preferredDistance)
        {
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            int edge = TargetEdgeDirection();
            int side = edge != 0 ? -edge : NPC.Center.X < Target.Center.X ? -1 : 1;
            float destination = MathHelper.Clamp(Target.Center.X + side * preferredDistance, PassiveLeft, PassiveRight);
            float offset = destination - NPC.Center.X;
            float brakingSpeed = MathF.Sqrt(2f * acceleration * Math.Max(0f, Math.Abs(offset) - 20f));
            float desiredSpeed = Math.Sign(offset) * Math.Min(maxSpeed, brakingSpeed);
            NPC.velocity.X = Approach(NPC.velocity.X, desiredSpeed, acceleration);
            if (Math.Abs(NPC.velocity.X) > 0.2f)
                idleDirection = Math.Sign(NPC.velocity.X);
            contactDamage = true;
        }

        private void SpinUp(int duration, float dashSpeed)
        {
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.velocity.X = Approach(NPC.velocity.X, 0f, 0.34f);
            float progress = MathHelper.Clamp(StateTimer / (float)duration, 0f, 1f);
            if (storedDirection == 0)
                storedDirection = Target.Center.X >= NPC.Center.X ? 1 : -1;
            spinTarget = storedDirection * dashSpeed / (NPC.width * 0.5f) * Easings.easeInSine(progress);
            visualCharge = Easings.easeInOutSine(progress);
            if (OnGround() && StateTimer % 4 == 0 && progress > 0.12f)
                KickUpDust(progress > 0.65f ? 2 : 1);
            if (OnGround() && StateTimer > 30 && StateTimer < duration - 18 && StateTimer % Math.Max(14, 32 - (int)(progress * 18f)) == 0)
                NPC.velocity.Y = -MathHelper.Lerp(1.2f, 3f, progress);
            if (OnGround() && Math.Abs(rollVelocity) > 0.12f && StateTimer % 8 == 0)
                SpawnProjectile<TumblerSpark>(NPC.Bottom - new Vector2(storedDirection * 26f, 8f), new Vector2(-storedDirection * Main.rand.NextFloat(2f, 5f), Main.rand.NextFloat(-2.5f, -0.5f)), 0, 0f, PhaseTwo ? 1f : 0f);
        }

        private void MoveHorizontal(float destinationX, float maxSpeed, float inertia)
        {
            float desired = MathHelper.Clamp((destinationX - NPC.Center.X) * 0.08f, -maxSpeed, maxSpeed);
            NPC.velocity.X = Approach(NPC.velocity.X, desired, inertia * maxSpeed);
        }

        private static float Approach(float current, float target, float amount)
        {
            if (current < target)
                return Math.Min(current + amount, target);
            return Math.Max(current - amount, target);
        }

        private bool OnGround()
        {
            return NPC.velocity.Y >= 0f && (NPC.collideY || Collision.SolidCollision(NPC.BottomLeft + new Vector2(5f, 1f), NPC.width - 10, 4));
        }

        private bool ReachedWall(int direction)
        {
            float margin = NPC.width * 0.5f + 6f;
            float nextX = NPC.Center.X + NPC.velocity.X;
            bool boundary = direction < 0 ? nextX <= LeftOuter + margin : nextX >= RightOuter - margin;
            return boundary || !NPC.noTileCollide && NPC.collideX && Math.Sign(NPC.oldVelocity.X) == direction;
        }

        private void Rebound(int direction, float speed, float hop)
        {
            bool dashImpact = contactDamage && Math.Abs(NPC.velocity.X) >= 6f && State is (TumblerState.CrystalRun or TumblerState.ShockDash or TumblerState.RailDash or TumblerState.Dash or TumblerState.LoopSlam);
            if (dashImpact)
            {
                pendingWallDaze = IsServer;
                contactDamage = false;
                hop = Math.Max(hop, 4f);
            }
            NPC.velocity.X = -direction * speed;
            if (hop > 0f)
                NPC.velocity.Y = -hop;
            idleDirection = -direction;
            movementCommitTimer = 65;
            impactFlash = 1f;
            SpawnAuraPulse(100f, 24, false);
            SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.65f, Pitch = -0.35f }, NPC.Center);
            ScreenShake(5f);
            KickUpDust(8);
            if (dashImpact)
                ThrowImpactRubble(7, new Vector2(NPC.Center.X + direction * NPC.width * 0.5f, NPC.Center.Y));
            NPC.netUpdate = true;
        }

        private void KeepInsideArena()
        {
            if (!ArenaData.Valid || State is TumblerState.Despawn or TumblerState.Death)
                return;
            RecoverArenaFloor();
            if (NPC.noTileCollide && NPC.velocity.Y >= 0f && NPC.Bottom.Y + NPC.velocity.Y >= FloorY)
            {
                NPC.velocity.Y = Math.Max(0f, FloorY - NPC.Bottom.Y);
                NPC.noGravity = true;
            }
            float margin = NPC.width * 0.5f + 4f;
            float leftEdge = LeftOuter + margin;
            float rightEdge = RightOuter - margin;
            if (NPC.Center.X < leftEdge || NPC.Center.X > rightEdge)
            {
                NPC.Center = new Vector2(MathHelper.Clamp(NPC.Center.X, leftEdge, rightEdge), NPC.Center.Y);
                int direction = NPC.Center.X <= leftEdge ? -1 : 1;
                if (NPC.velocity.X * direction > 0f)
                    Rebound(direction, Math.Max(2f, Math.Abs(NPC.velocity.X) * 0.6f), 0f);
            }
            else if (!NPC.noTileCollide && NPC.collideX && Math.Abs(NPC.oldVelocity.X) > 1f && Math.Sign(NPC.velocity.X) == Math.Sign(NPC.oldVelocity.X))
                Rebound(Math.Sign(NPC.oldVelocity.X), Math.Abs(NPC.oldVelocity.X) * 0.6f, 2f);
        }

        private void RecoverArenaFloor()
        {
            if (!ArenaData.Valid || State is TumblerState.Despawn or TumblerState.Death || NPC.Bottom.Y <= FloorY)
                return;
            NPC.Bottom = new Vector2(NPC.Center.X, FloorY);
            NPC.velocity.Y = Math.Min(0f, NPC.velocity.Y);
            NPC.noGravity = true;
            NPC.collideY = NPC.velocity.Y == 0f;
            if (IsServer)
                NPC.netUpdate = true;
        }

        private void DescendToArenaFloor()
        {
            if (NPC.Bottom.Y < FloorY - 2f)
            {
                NPC.noTileCollide = true;
                if (NPC.velocity.Y >= 0f && NPC.Bottom.Y + Math.Max(1f, NPC.velocity.Y) >= FloorY)
                {
                    NPC.Bottom = new Vector2(NPC.Center.X, FloorY);
                    NPC.velocity.Y = 0f;
                    NPC.noTileCollide = false;
                }
            }
        }

        private void ResolveWallDaze()
        {
            if (pendingWallDaze && IsServer)
            {
                pendingWallDaze = false;
                NPC.noGravity = NPC.noTileCollide = false;
                contactDamage = false;
                spinTarget = null;
                stunReturnTimer = 60;
                ChangeState(TumblerState.Stunned);
                wallReboundTicks = 24;
                NPC.netUpdate = true;
            }
        }
    }
}
