using Terraria.Graphics.Shaders;
using ReLogic.Content;
using AerovelenceMod.Content.Dusts.GlowDusts;
using System;
using AerovelenceMod.Content.Dusts;

namespace AerovelenceMod.Content.Items.Weapons.Aurora
{
    public class AuroraFire : ModBuff
    {
        public int timer = 0;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;  // Is it a debuff?
            //Main.buffNoTimeDisplay[Type] = false;
            Main.buffNoSave[Type] = true; // Causes this buff not to persist when exiting and rejoining the world

        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            npc.GetGlobalNPC<AuroraFireModNPC>().AuroraFireDebuff = true;
            timer++;
        }
    }

    public class AuroraFireModNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool AuroraFireDebuff = false;
        public float AuroraFireTime = 0f;

        public override void ResetEffects(NPC npc)
        {
            if (!npc.HasBuff(ModContent.BuffType<AuroraFire>()))
            {
                AuroraFireDebuff = false;
                AuroraFireTime = 0;
            }
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (AuroraFireDebuff)
            {
                ArmorShaderData dustShader = Main.dedServ ? null : new ArmorShaderData(new Ref<Effect>(Mod.Assets.Request<Effect>("Effects/GlowDustShader", AssetRequestMode.ImmediateLoad).Value), "ArmorBasic");
                ArmorShaderData dustShader2 = Main.dedServ ? null : new ArmorShaderData(new Ref<Effect>(Mod.Assets.Request<Effect>("Effects/GlowDustShader", AssetRequestMode.ImmediateLoad).Value), "ArmorBasic");

                float randomR = 20f;// Main.rand.NextFloat(0, 256);
                float randomG = 55f; //Main.rand.NextFloat(0, 256);
                float randomB = 200f;// Main.rand.NextFloat(0, 256);
                Color randomColor = new Color(randomR, randomG, randomB);

                Color c = new Color(

                (byte)Main.rand.Next(0, 255),

                (byte)Main.rand.Next(0, 255),

                (byte)Main.rand.Next(0, 255));

                if (AuroraFireTime % 5 == 0)
                {
                    int p = GlowDustHelper.DrawGlowDust(npc.position, npc.width, npc.height, ModContent.DustType<GlowCircleFlare>(), c, 0.5f, 0.5f, 0f, dustShader);
                    Main.dust[p].velocity *= 0.5f;
                    Main.dust[p].noLight = true;
                    Main.dust[p].alpha = 2;

                }
                else if (AuroraFireTime % 7 == 0) //else if is intentional
                {

                    int p = GlowDustHelper.DrawGlowDust(npc.position, npc.width, npc.height, ModContent.DustType<GlowSpark>(), c, 0.1f, 1f, 0f, dustShader2);
                    Main.dust[p].noLight = true;
                    //Main.dust[p].velocity.X *= 0.2f;
                }

                npc.lifeRegen -= 20;

                AuroraFireTime++;
            }
        }
    }
}
