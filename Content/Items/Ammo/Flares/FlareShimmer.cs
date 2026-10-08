using AerovelenceMod.Common.Bases;
using AerovelenceMod.Content.Dusts;
using AerovelenceMod.Content.Dusts.GlowDusts;
using ReLogic.Content;
using System;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace AerovelenceMod.Content.Items.Ammo.Flares
{
    public class FlareShimmer : ModBuff
    {
        public int timer = 0;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;  // Is it a debuff?
            Main.buffNoSave[Type] = true; // Causes this buff not to persist when exiting and rejoining the world
            BuffID.Sets.IsATagBuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            //While the npc has this debuff, activate the FlareFireModNPC class
            npc.GetGlobalNPC<FlareShimmerModNPC>().DebuffActive = true;
            timer++;
        }
    }

    public class FlareShimmerModNPC : BaseFlareDebuffNPC
    {
        public override bool InstancePerEntity => true;

        public override void ResetEffects(NPC npc)
        {
            if (!DebuffActive)
            {
                DebuffIndex = ModContent.BuffType<FlareShimmer>();
                DebuffTime = 0;
                BaseResetEffects(npc);
            }
            BaseResetEffects(npc);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (DebuffActive)
            {
                timeBetweenHits = 30;
                tickDamage = 10;
                sound = new SoundStyle("Terraria/Sounds/Custom/dd2_betsy_fireball_shot_1") with { Pitch = -.53f, PitchVariance = 0.3f, Volume = 0.5f, MaxInstances = -1 };
                colorA = new Color(191, 128, 255);
                colorB = new Color(191, 128, 255);
                DebuffIndex = ModContent.BuffType<FlareShimmer>();
                BaseUpdateLifeRegen(npc, ref damage);
            }
        }
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (DebuffActive)
            {
                DebuffIndex = ModContent.BuffType<FlareShimmer>();
                timeBetweenHits = 30;
                tickDamage = 10;
                sound = new SoundStyle("Terraria/Sounds/Custom/dd2_betsy_fireball_shot_1") with { Pitch = -.53f, PitchVariance = 0.3f, Volume = 0.5f, MaxInstances = -1 };
                colorA = new Color(191, 128, 255);
                colorB = new Color(191, 128, 255);
                tagDamage = 3;
                tagCrit = 5;
            }
        }
    }
}
