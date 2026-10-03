using System;
using AerovelenceMod.Content.Items.BossSummons;
using Microsoft.Xna.Framework;
using Terraria;

namespace AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler
{
    public partial class CrystalTumbler
    {
        private static int PylonWarningTime => Main.masterMode ? 135 : Main.expertMode ? 165 : 195;
        private static int PylonWaveTime => PylonWarningTime + 100;
        private static float PylonSafeWidth => Main.masterMode ? 170f : Main.expertMode ? 260f : 320f;

        private void ConductiveFloorRoll()
        {
            RollTowardPlayer(PhaseTwo ? 7f : 6f, 0.16f);
            DescendToArenaFloor();
        }

        private void SpawnFenceUpperBolts()
        {
            float left = LeftOuter + 60f;
            float width = RightOuter - 60f - left;
            for (int lane = 0; lane < 3; lane++)
            {
                float y = FloorY - 93f - lane * 100f;
                if (y < ArenaData.WorldBounds.Top + 100f)
                    continue;
                SpawnProjectile<TumblerLightningBolt>(new Vector2(left, y), new Vector2(width, 0f), ProjectileDamage(17), 0f, TumblerLightningBolt.FenceWarning(PylonWarningTime), PhaseTwo ? 1f : 0f, -75f);
            }
        }

        private void ConductiveFenceField()
        {
            EnsureConductiveCrystals();
            if (StateTimer <= 1)
                EnsureMagneticPlatforms();
            if (StateTimer < TumblerConductiveSequence.ChargeEnd)
            {
                GroundRoll(1.8f, 0.08f, 180f);
                contactDamage = false;
            }
            else
                ConductiveFloorRoll();
            visualCharge = MathHelper.Clamp(StateTimer / 90f, 0f, 1f);
            if (StateTimer == 180)
                SpawnConductiveLink(false);
            if (StateTimer == 270)
                SpawnProjectile<TumblerBossAura>(NPC.Center, Vector2.Zero, ProjectileDamage(22), 0f, NPC.whoAmI, 180f, 64f);
            for (int shot = 0; shot < TumblerConductiveSequence.ShotCount; shot++)
            {
                if (StateTimer != TumblerConductiveSequence.WarningTime(shot))
                    continue;
                foreach (NPC crystal in Main.ActiveNPCs)
                {
                    if (crystal.ModNPC is not TumblerConductiveCrystal || Math.Sign(crystal.ai[0]) != TumblerConductiveSequence.Side(shot))
                        continue;
                    Vector2 tip = crystal.Top + new Vector2(0f, 6f);
                    SpawnProjectile<TumblerAimLine>(tip, (Target.Center - tip).SafeNormalize(Vector2.UnitY), ProjectileDamage(15), 0f, PhaseTwo ? 1f : 0f);
                }
            }
            if (StateTimer >= 270 && StateTimer < 450)
            {
                contactDamage = true;
                auraScale = 0.8f;
            }
            if (StateTimer >= 450)
                FinishAttack();
        }
    }
}
