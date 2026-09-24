using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using AerovelenceMod.Common.Utilities;
using Terraria.Graphics.Shaders;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using AerovelenceMod.Content.Dusts.GlowDusts;
using System;
using AerovelenceMod.Content.Dusts;

namespace AerovelenceMod.Content.Items.Weapons.Misc.Magic.WandOfExploding
{
    public class ManaLeech : ModBuff
    {
        public int timer = 0;
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;  // Is it a debuff?
            Main.buffNoSave[Type] = true; // Causes this buff not to persist when exiting and rejoining the world

        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            if (npc.type != NPCID.TargetDummy) //Im not letting this happen lol
            {
                npc.GetGlobalNPC<ManaLeechModNPC>().ManaLeechDebuff = true;
                timer++;
            }
        }
    }

    public class ManaLeechModNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        public bool ManaLeechDebuff = false;
        public float ManaLeechTime = 0f;

        public override void ResetEffects(NPC npc)
        {
            if (!npc.HasBuff(ModContent.BuffType<ManaLeech>()))
            {
                ManaLeechDebuff = false;
                ManaLeechTime = 0;
                
            } else
            {

            }
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (ManaLeechDebuff)
            {
                if (ManaLeechTime % 14 == 0)
                {
                    Projectile.NewProjectile(null, npc.Center, Main.rand.NextVector2CircularEdge(7,7), ModContent.ProjectileType<ManaLeechStar>(), 
                        0, 0, Main.myPlayer);
                }
                if (ManaLeechTime % 1 == 0)
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, ModContent.DustType<GlowPixelRise>(), Scale: 0.5f, newColor: Color.DodgerBlue);
                    Main.dust[dust].velocity.Y = Math.Abs(Main.dust[dust].velocity.Y) * -1;
                    Main.dust[dust].velocity.X *= 0.5f;
                    Main.dust[dust].alpha = 2;
                    Main.dust[dust].noLight = true;
                    //Main.dust[dust].rotation = Main.rand.NextFloat(6.28f);


                    if (ManaLeechTime % 8 == 0)
                    {
                        int dust2 = Dust.NewDust(npc.position, npc.width, npc.height, ModContent.DustType<GlowPixelRise>(), Scale: 0.5f, newColor: Color.DodgerBlue);
                        Main.dust[dust2].velocity.Y = Math.Abs(Main.dust[dust2].velocity.Y) * -1;
                        Main.dust[dust2].velocity.X *= 0.85f;
                        Main.dust[dust2].alpha = 2;
                        Main.dust[dust2].noLight = true;
                        //Main.dust[dust2].rotation = Main.rand.NextFloat(6.28f);
                    }

                }

                if (ManaLeechTime % 4 == 0)
                {
                    int dust3 = Dust.NewDust(npc.position, npc.width, npc.height, ModContent.DustType<GlowPixelCross>(), newColor: Color.DodgerBlue);
                    Main.dust[dust3].scale *= 0.35f;
                }

                if (ManaLeechTime % 17 == 0)
                {
                    int dust3 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.PortalBoltTrail, newColor: Color.DeepSkyBlue);
                    Main.dust[dust3].velocity.Y = Math.Abs(Main.dust[dust3].velocity.Y) * -2;
                }

                ManaLeechTime++;
            }
        }
    }

	public class ManaLeechStar : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_0";

		private int timer;
		public float scale = 1f;

		public override void SetDefaults()
		{
			Projectile.scale = 1;
			Projectile.width = 2;
			Projectile.height = 2;

			Projectile.friendly = false;
			Projectile.hostile = false;

			Projectile.timeLeft = 300;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;

		}
		public override bool? CanCutTiles() => false;

		public override bool? CanDamage() => false;

        public override void AI()
		{
			//TODO: Very not multiplayer compatible
			Player target = Main.player[Main.myPlayer];

			if (timer > 20)
			{
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.DirectionTo(target.Center) * (13f + (timer * 0.02f)), .3f);

				if (Projectile.Center.Distance(target.Center) < 30)
                {
					SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = 0.7f, Volume = 0.2f }, target.position);
					target.statMana += 3;
					target.ManaEffect(3);
					Projectile.Kill();
                }
			}
			else
			{
				Projectile.velocity *= 0.96f;
			}

			scale = Math.Clamp(MathHelper.Lerp(scale, 1.25f, 0.08f), 0f, 1f);

			if (Projectile.velocity.X > 0)
				Projectile.rotation += 0.3f;
			else
				Projectile.rotation -= 0.3f;
			timer++;

		}


		public override bool PreDraw(ref Color lightColor)
		{
			Texture2D Star = (Texture2D)ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/Twinkle");

			Color betweenBlueA = Color.Lerp(Color.DodgerBlue, Color.DeepSkyBlue, 0.5f);
            Color betweenBlueB = Color.Lerp(Color.Blue, Color.DodgerBlue, 0.5f);


            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color[] cols = { Color.White * 1f, betweenBlueA * 0.75f, betweenBlueB * 0.525f };
            float[] scales = { 1.15f, 1.6f, 2.5f };

            float orbAlpha = 2f;
			float orbScale = 0.2f * Projectile.scale * scale;
            Vector2 orbOrigin = Star.Size() / 2f;

            float sineScale1 = 1f + (float)Math.Sin(Main.timeForVisualEffects * 0.07f) * 0.15f;
            float sineScale2 = 1f + (float)Math.Cos(Main.timeForVisualEffects * 0.13f) * 0.1f;

            Main.EntitySpriteDraw(Star, drawPos, null, cols[0] with { A = 0 } * orbAlpha, Projectile.rotation, orbOrigin, orbScale * scales[0], SpriteEffects.None);
            Main.EntitySpriteDraw(Star, drawPos, null, cols[1] with { A = 0 } * orbAlpha, Projectile.rotation, orbOrigin, orbScale * scales[1] * sineScale1, SpriteEffects.None);
            Main.EntitySpriteDraw(Star, drawPos, null, cols[2] with { A = 0 } * orbAlpha, Projectile.rotation, orbOrigin, orbScale * scales[2] * sineScale2, SpriteEffects.None);

            return false;
		}
	}
}