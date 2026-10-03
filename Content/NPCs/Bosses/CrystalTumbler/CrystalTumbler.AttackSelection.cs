using System;
using AerovelenceMod.Common.Systems;
using AerovelenceMod.Content.Items.BossSummons;
using Microsoft.Xna.Framework;
using Terraria;

namespace AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler
{
    public partial class CrystalTumbler
    {
        private static readonly TumblerState[] PhaseOnePattern =
        [
            TumblerState.ShockDash,
            TumblerState.ConductiveFenceField,
            TumblerState.StarAttack,
            TumblerState.RailDash,
            TumblerState.LoopSlam,
            TumblerState.MagnetClash,
            TumblerState.ElectricPulse,
            TumblerState.RippleSlam,
            TumblerState.ElectrifyPlatforms,
            TumblerState.BoltVolley,
            TumblerState.KnifeCrystals
        ];

        private static readonly TumblerState[] PhaseTwoPattern =
        [
            TumblerState.LoopSlam,
            TumblerState.BoltVolley,
            TumblerState.ConductiveFenceField,
            TumblerState.Teleport,
            TumblerState.MagnetClash,
            TumblerState.ElectricPulse,
            TumblerState.KnifeCrystals,
            TumblerState.RailDash,
            TumblerState.ShockDash,
            TumblerState.ElectrifyPlatforms,
            TumblerState.Teleport,
            TumblerState.CrystalCharge,
            TumblerState.ConductiveFenceField,
            TumblerState.RippleSlam,
            TumblerState.LoopSlam,
            TumblerState.Overload,
            TumblerState.Teleport
        ];

        private int distantTimer;
        private int edgeChargeCooldown;
        private float volleyAnchorX;
        private float PassiveLeft => LeftInner + NPC.width * 0.5f;
        private float PassiveRight => RightInner - NPC.width * 0.5f;

        private int TargetEdgeDirection()
        {
            if (Target.Center.X < LeftOuter + 200f)
                return -1;
            return Target.Center.X > RightOuter - 200f ? 1 : 0;
        }

        private bool TryBeginEdgeCharge()
        {
            int edge = TargetEdgeDirection();
            if (!IsServer || edgeChargeCooldown > 0 || edge == 0 || phaseTransitionActive)
                return false;
            ChangeState(TumblerState.CrystalRun);
            storedDirection = edge;
            edgeChargeCooldown = 600;
            distantTimer = 0;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.ModProjectile is not TumblerProjectile { ClearForEdgeCharge: true })
                    continue;
                if (projectile.TryGetGlobalProjectile(out TumblerSharedProjectile shared) && !shared.FromEncounter)
                    continue;
                TumblerProjectileRetirement.Begin(projectile);
            }
            NPC.netUpdate = true;
            return true;
        }

        private float FindVolleyAnchor()
        {
            float best = ArenaData.ArenaCenter.X;
            float bestClearance = -1f;
            for (int i = -1; i <= 1; i++)
            {
                float candidate = MathHelper.Clamp(ArenaData.ArenaCenter.X + i * 340f, PassiveLeft, PassiveRight);
                float clearance = VolleyClearance(candidate);
                if (clearance > bestClearance || clearance == bestClearance && Math.Abs(candidate - NPC.Center.X) < Math.Abs(best - NPC.Center.X))
                {
                    best = candidate;
                    bestClearance = clearance;
                }
            }
            return best;
        }

        private static float VolleyClearance(float sourceX)
        {
            float clearance = float.MaxValue;
            foreach (Player player in Main.ActivePlayers)
            {
                if (!player.dead && ArenaData.WorldBounds.Intersects(player.Hitbox))
                    clearance = Math.Min(clearance, Math.Abs(player.Center.X - sourceX));
            }
            return clearance;
        }

        private static bool HasVolleyClearance(Vector2 source, float distance) => VolleyClearance(source.X) >= distance;

        private void SpawnVolleyOrb(Vector2 position, Vector2 velocity, int damage, bool charged)
        {
            if (!IsServer)
                return;
            position.X = MathHelper.Clamp(position.X, PassiveLeft, PassiveRight);
            if (!HasVolleyClearance(position, 280f))
                position.X = FindVolleyAnchor();
            if (!HasVolleyClearance(position, 280f))
                return;
            if (charged)
                SpawnProjectile<TumblerChargedKnifeBall>(position, velocity, damage);
            else
                SpawnProjectile<TumblerKnifeBall>(position, velocity, damage);
        }

        private void SelectNextAttack()
        {
            if (!IsServer)
                return;
            if (phaseTransitionActive)
            {
                EnterPhaseTransition(clearEncounter: false);
                return;
            }
            if (TryBeginEdgeCharge())
                return;
            TumblerState[] pattern = PhaseTwo ? PhaseTwoPattern : PhaseOnePattern;
            TumblerState next = pattern[PatternIndex % pattern.Length];
            PatternIndex = (PatternIndex + 1) % pattern.Length;
            ChangeState(next);
        }

        private void FinishAttack(int recoveryTicks = 90)
        {
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.dontTakeDamage = false;
            if (phaseTransitionActive)
            {
                EnterPhaseTransition(clearEncounter: false);
            }
            else if (State is TumblerState.LoopSlam or TumblerState.RippleSlam or TumblerState.GroundRaze or TumblerState.CrystalConvergence or TumblerState.Overload)
            {
                stunReturnTimer = recoveryTicks;
                ChangeState(TumblerState.Stunned);
            }
            else
                ChangeState(TumblerState.Idle);
        }

        private void ChangeState(TumblerState state)
        {
            if (!IsServer)
                return;
            if (State == state && StateTimer == 0)
                return;
            State = state;
            StateTimer = 0;
            substate = 0;
            repetitions = 0;
            railProgress = 0f;
            railSpeed = 0f;
            storedDirection = state == TumblerState.LoopSlam
                ? (Main.rand.NextBool() ? 1 : -1)
                : state is TumblerState.CrystalRun or TumblerState.ShockDash or TumblerState.RailDash
                    ? (NPC.Center.X < ArenaData.ArenaCenter.X ? 1 : -1)
                    : (Target.Center.X >= NPC.Center.X ? 1 : -1);
            visualCharge = 0f;
            auraScale = 0f;
            contactDamage = false;
            movementCommitTimer = 0;
            wallReboundTicks = -1;
            if (state == TumblerState.BoltVolley)
                volleyAnchorX = FindVolleyAnchor();
            NPC.netUpdate = true;
        }

        private void PressureDistantPlayer()
        {
            if (State is not (TumblerState.Idle or TumblerState.StarAttack) || Math.Abs(Target.Center.X - NPC.Center.X) < 650f)
            {
                distantTimer = Math.Max(0, distantTimer - 2);
                return;
            }
            if (++distantTimer < 180)
                return;
            distantTimer = 0;
            Vector2 source = ArenaData.ClosestCrystal(Target.Center);
            SpawnProjectile<TumblerAimLine>(source, (Target.Center - source).SafeNormalize(Vector2.UnitY), ProjectileDamage(15), 0f, PhaseTwo ? 1f : 0f);
        }
    }
}
