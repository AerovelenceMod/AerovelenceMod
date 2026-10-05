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
        private bool phaseTransitionActive;
        private int shieldHits;

        private int OvershieldHitCount => Main.masterMode ? 25 : Main.expertMode ? 20 : 15;

        internal bool OvershieldActive => State == TumblerState.PhaseTransition && shieldHits > 0;

        private void UpdatePhaseTransition()
        {
            if (IsServer && !PhaseTwo && State is not TumblerState.Spawn and not TumblerState.PhaseTransition and not TumblerState.Despawn and not TumblerState.Death && NPC.life <= NPC.lifeMax * 0.5f)
                EnterPhaseTransition(clearEncounter: true);
        }

        private void EnterPhaseTransition(bool clearEncounter)
        {
            if (clearEncounter)
                phaseTransitionActive = true;
            shieldHits = OvershieldHitCount;
            if (clearEncounter)
            {
                ArenaData.ClearEncounterEntities(false, true, true);
                TumblerMagneticPlatform.Release(NPC);
            }
            ChangeState(TumblerState.PhaseTransition);
        }

        private void PhaseTransition()
        {
            NPC.dontTakeDamage = false;
            shieldHits = Math.Max(shieldHits, 0);
            auraScale = 0.75f + 0.08f * MathF.Sin(StateTimer * 0.08f);
            GroundRoll(4f, 0.11f, 230f);
            if (StateTimer == 1)
                SpawnProjectile<TumblerShieldStorm>(NPC.Center, Vector2.Zero, ProjectileDamage(23), 0f, NPC.whoAmI);
            if (StateTimer % 180 == 60)
                FireShardFan(3, 7.5f);
            if (shieldHits <= 0 && IsServer)
            {
                PhaseTwo = true;
                phaseTransitionActive = false;
                shieldFlash = 1f;
                SoundEngine.PlaySound(SoundID.Shatter with { Pitch = -0.15f }, NPC.Center);
                ScreenShake(10f);
                PatternIndex = 0;
                stunReturnTimer = 180;
                ChangeState(TumblerState.Stunned);
            }
        }
    }
}
