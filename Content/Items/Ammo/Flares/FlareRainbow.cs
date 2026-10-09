using AerovelenceMod.Common.Bases;
using AerovelenceMod.Common.Drawing;
using AerovelenceMod.Content.Dusts;
using AerovelenceMod.Content.Dusts.GlowDusts;
using ReLogic.Content;
using System;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace AerovelenceMod.Content.Items.Ammo.Flares
{
    public class FlareRainbow : ModBuff
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
            npc.GetGlobalNPC<FlareRainbowModNPC>().DebuffActive = true;
            timer++;
        }
    }

    public class FlareRainbowModNPC : BaseFlareDebuffNPC
    {
        public override bool InstancePerEntity => true;

        public override void ResetEffects(NPC npc)
        {
            if (!DebuffActive)
            {
                DebuffIndex = ModContent.BuffType<FlareRainbow>();
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
                colorA = ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects);
                colorB = ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects);
                DebuffIndex = ModContent.BuffType<FlareRainbow>();
                BaseUpdateLifeRegen(npc, ref damage);
            }
        }
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (DebuffActive)
            {
                DebuffIndex = ModContent.BuffType<FlareRainbow>();
                timeBetweenHits = 30;
                tickDamage = 10;
                sound = new SoundStyle("Terraria/Sounds/Custom/dd2_betsy_fireball_shot_1") with { Pitch = -.53f, PitchVariance = 0.3f, Volume = 0.5f, MaxInstances = -1 };
                colorA = ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects);
                colorB = ModContent.GetInstance<Rainbow>().FetchRainbow((float)Main.timeForVisualEffects);
                tagDamage = 3;
                tagCrit = 5;
            }
        }
    }
}
