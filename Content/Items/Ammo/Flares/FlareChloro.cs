using AerovelenceMod.Common.Bases;
using AerovelenceMod.Content.Dusts;
using AerovelenceMod.Content.Dusts.GlowDusts;
using ReLogic.Content;
using System;
using Terraria.Audio;
using Terraria.Graphics.Shaders;

namespace AerovelenceMod.Content.Items.Ammo.Flares
{
    public class FlareChloro : ModBuff
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
            npc.GetGlobalNPC<FlareChloroModNPC>().DebuffActive = true;
            timer++;
        }
    }

    public class FlareChloroModNPC : BaseFlareDebuffNPC
    {
        public override bool InstancePerEntity => true;

        public override void ResetEffects(NPC npc)
        {
            if (!DebuffActive)
            {
                DebuffIndex = ModContent.BuffType<FlareChloro>();
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
                tickDamage = 3;
                sound = new SoundStyle("Terraria/Sounds/Custom/dd2_betsy_fireball_shot_1") with { Pitch = -.53f, PitchVariance = 0.3f, Volume = 0.5f, MaxInstances = -1 };
                colorA = Color.LawnGreen;
                colorB = Color.Green;
                DebuffIndex = ModContent.BuffType<FlareChloro>();
                BaseUpdateLifeRegen(npc, ref damage);
            }
        }
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (DebuffActive)
            {
                DebuffIndex = ModContent.BuffType<FlareChloro>();
                timeBetweenHits = 30;
                tickDamage = 3;
                sound = new SoundStyle("Terraria/Sounds/Custom/dd2_betsy_fireball_shot_1") with { Pitch = -.53f, PitchVariance = 0.3f, Volume = 0.5f, MaxInstances = -1 };
                colorA = Color.LawnGreen;
                colorB = Color.Green;
                tagDamage = 3;
                tagCrit = 4;
            }

        }

    }
}
