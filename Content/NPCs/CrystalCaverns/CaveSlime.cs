using AerovelenceMod.Content.Biomes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;

namespace AerovelenceMod.Content.NPCs.CrystalCaverns
{
    public class CaveSlime : ModNPC
    {
        private enum CaveSlimeState
        {
            Idle,
            MorphIn,
            Rolling,
            MorphOut
        }

        private CaveSlimeState State
        {
            get => (CaveSlimeState)(int)NPC.ai[0];
            set => NPC.ai[0] = (int)value;
        }

        private ref float StateTimer => ref NPC.ai[1];
        private ref float RollDirection => ref NPC.ai[2];
        private ref float MorphVisual => ref NPC.localAI[0];
        private ref float Wiggle => ref NPC.localAI[1];

        private const int MorphInDuration = 22;
        private const int RollingDuration = 110;
        private const int MorphOutDuration = 18;
        private const int RollingCooldownMax = 7 * 60;

        private int rollingCooldown = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 3;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new()
            {
                Position = new Vector2(0f, 8f),
                PortraitPositionXOverride = 0f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lifeMax = 70;
            NPC.damage = 15;
            NPC.defense = 2;
            NPC.knockBackResist = 0.2f;
            NPC.width = 46;
            NPC.height = 44;
            NPC.value = Item.buyPrice(0, 0, 7, 0);
            NPC.lavaImmune = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath44;
            SpawnModBiomes = new int[] { ModContent.GetInstance<CrystalCavernsBiome>().Type };
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
                new FlavorTextBestiaryInfoElement("These slimes seem to have gotten themselves covered up in crystals. They like to roll around like the tumblers.")
            });
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.InModBiome(ModContent.GetInstance<CrystalCavernsBiome>()))
                return SpawnCondition.Cavern.Chance + SpawnCondition.Underground.Chance;

            return 0f;
        }

        public override void AI()
        {
            if (rollingCooldown > 0)
                rollingCooldown--;

            NPC.TargetClosest(true);
            Player target = Main.player[NPC.target];
            Wiggle += 0.08f;

            switch (State)
            {
                case CaveSlimeState.Idle:
                    DoIdle(target);
                    break;
                case CaveSlimeState.MorphIn:
                    DoMorphIn(target);
                    break;
                case CaveSlimeState.Rolling:
                    DoRolling(target);
                    break;
                case CaveSlimeState.MorphOut:
                    DoMorphOut(target);
                    break;
            }

            ApplyGravityAndCollisions();
            UpdateMorphVisual();
        }

        private void DoIdle(Player target)
        {
            StateTimer++;
            NPC.rotation = 0f;

            if (NPC.collideY)
            {
                float move = target.Center.X > NPC.Center.X ? 1f : -1f;
                NPC.velocity.X *= 0.92f;

                if (Math.Abs(target.Center.X - NPC.Center.X) > 28f)
                    NPC.velocity.X += move * 0.055f;

                NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X, -1.6f, 1.6f);

                if (StateTimer % 72f == 0f)
                {
                    NPC.velocity.Y = -4.2f;
                    NPC.netUpdate = true;
                }
            }

            float distanceToPlayer = Vector2.Distance(NPC.Center, target.Center);
            if (rollingCooldown <= 0 && distanceToPlayer < 250f && Collision.CanHitLine(NPC.position, NPC.width, NPC.height, target.position, target.width, target.height))
            {
                BeginMorphIn(target);
            }
        }

        private void BeginMorphIn(Player target)
        {
            State = CaveSlimeState.MorphIn;
            StateTimer = 0f;
            RollDirection = target.Center.X >= NPC.Center.X ? 1f : -1f;
            NPC.direction = (int)RollDirection;
            NPC.spriteDirection = NPC.direction;
            NPC.velocity.X *= 0.45f;
            NPC.netUpdate = true;
        }

        private void DoMorphIn(Player target)
        {
            StateTimer++;
            NPC.direction = (int)RollDirection;
            NPC.spriteDirection = NPC.direction;
            NPC.velocity.X *= 0.9f;

            if (NPC.collideY && StateTimer == 5f)
                NPC.velocity.Y = -2.2f;

            if (StateTimer % 4f == 0f)
                SpawnMorphDust(2);

            if (StateTimer >= MorphInDuration)
            {
                State = CaveSlimeState.Rolling;
                StateTimer = 0f;
                NPC.velocity = new Vector2(RollDirection * 4.8f, -1.2f);
                NPC.netUpdate = true;
            }
        }

        private void DoRolling(Player target)
        {
            StateTimer++;
            NPC.direction = NPC.velocity.X >= 0f ? 1 : -1;
            NPC.spriteDirection = NPC.direction;

            float desiredSpeed = 5.2f;
            float accel = 0.09f;

            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, RollDirection * desiredSpeed, accel);

            if (NPC.collideX)
            {
                RollDirection *= -1f;
                NPC.velocity.X = RollDirection * 4.4f;
                NPC.netUpdate = true;
            }

            NPC.rotation += NPC.velocity.X * 0.085f;

            if (NPC.collideY && Math.Abs(NPC.velocity.X) > 2f && Main.rand.NextBool(6))
                SpawnMorphDust(1);

            if (target.Center.X > NPC.Center.X + 30f)
                RollDirection = 1f;
            else if (target.Center.X < NPC.Center.X - 30f)
                RollDirection = -1f;

            if (StateTimer >= RollingDuration)
            {
                State = CaveSlimeState.MorphOut;
                StateTimer = 0f;
                rollingCooldown = RollingCooldownMax;
                NPC.velocity.X *= 0.45f;
                NPC.netUpdate = true;
            }
        }

        private void DoMorphOut(Player target)
        {
            StateTimer++;
            NPC.rotation *= 0.84f;
            NPC.velocity.X *= 0.88f;

            if (StateTimer % 5f == 0f)
                SpawnMorphDust(1);

            if (StateTimer >= MorphOutDuration)
            {
                State = CaveSlimeState.Idle;
                StateTimer = 0f;
                NPC.rotation = 0f;
                NPC.netUpdate = true;
            }
        }

        private void ApplyGravityAndCollisions()
        {
            if (!NPC.noGravity)
            {
                NPC.velocity.Y += 0.28f;
                if (NPC.velocity.Y > 10f)
                    NPC.velocity.Y = 10f;
            }

            NPC.velocity = Collision.TileCollision(NPC.position, NPC.velocity, NPC.width, NPC.height, true, true);
        }

        private void UpdateMorphVisual()
        {
            float target = State switch
            {
                CaveSlimeState.MorphIn => MathHelper.Clamp(StateTimer / MorphInDuration, 0f, 1f),
                CaveSlimeState.Rolling => 1f,
                CaveSlimeState.MorphOut => 1f - MathHelper.Clamp(StateTimer / MorphOutDuration, 0f, 1f),
                _ => 0f
            };

            MorphVisual = MathHelper.Lerp(MorphVisual, target, 0.28f);
        }

        private void SpawnMorphDust(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), Main.rand.NextFloat(-1.5f, 0.4f));
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 193, velocity.X, velocity.Y, 0, Color.LightBlue, Main.rand.NextFloat(0.65f, 0.95f));
                Main.dust[dust].noGravity = true;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (State == CaveSlimeState.Rolling)
            {
                NPC.frame.Y = 2 * frameHeight;
                return;
            }

            if (State == CaveSlimeState.MorphIn || State == CaveSlimeState.MorphOut)
            {
                NPC.frame.Y = 0;
                return;
            }

            NPC.frameCounter += 0.14;
            if (NPC.frameCounter >= 8)
                NPC.frameCounter = 0;

            NPC.frame.Y = (int)NPC.frameCounter < 4 ? 0 : frameHeight;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;
            int frameHeight = texture.Height / Main.npcFrameCount[Type];

            Rectangle slimeFrameA = new Rectangle(0, 0, texture.Width, frameHeight);
            Rectangle slimeFrameB = new Rectangle(0, frameHeight, texture.Width, frameHeight);
            Rectangle orbFrame = new Rectangle(0, frameHeight * 2, texture.Width, frameHeight);

            Rectangle currentSlimeFrame = NPC.frame.Y == frameHeight ? slimeFrameB : slimeFrameA;

            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f);
            SpriteEffects effects = NPC.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Vector2 drawPos = NPC.Center - screenPos + new Vector2(0f, NPC.gfxOffY);

            float morph = MathHelper.Clamp(MorphVisual, 0f, 1f);
            float eased = EaseInOut(morph);

            float slimeAlpha = 1f - eased;
            float orbAlpha = eased;

            Vector2 slimeScale = new Vector2(
                1f + 0.22f * eased + MathF.Sin(Wiggle) * 0.02f * (1f - eased),
                1f - 0.22f * eased - Math.Abs(MathF.Sin(Wiggle * 0.8f)) * 0.02f * (1f - eased)
            );

            Vector2 orbScale = new Vector2(
                0.7f + 0.3f * eased,
                1.3f - 0.3f * eased
            );

            float crystalCollapse = eased;
            Vector2 offsetPulse = new Vector2(MathF.Sin(Wiggle * 1.3f), MathF.Cos(Wiggle * 1.1f)) * (1.2f * eased);

            if (slimeAlpha > 0.01f)
            {
                spriteBatch.Draw(
                    texture,
                    drawPos,
                    currentSlimeFrame,
                    drawColor * slimeAlpha,
                    0f,
                    origin,
                    slimeScale,
                    effects,
                    0f
                );
            }

            if (orbAlpha > 0.01f)
            {
                spriteBatch.Draw(
                    texture,
                    drawPos + offsetPulse,
                    orbFrame,
                    drawColor * orbAlpha,
                    NPC.rotation,
                    origin,
                    orbScale,
                    effects,
                    0f
                );
            }

            if (morph > 0.02f && morph < 0.98f)
            {
                DrawTransitionCrystals(spriteBatch, drawPos, drawColor, effects, crystalCollapse);
            }

            return false;
        }

        private void DrawTransitionCrystals(SpriteBatch spriteBatch, Vector2 drawPos, Color drawColor, SpriteEffects effects, float t)
        {
            Texture2D texture = TextureAssets.Npc[Type].Value;
            int frameHeight = texture.Height / Main.npcFrameCount[Type];
            Rectangle orbFrame = new Rectangle(0, frameHeight * 2, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f);

            Vector2[] points =
            {
                new Vector2(-14f, -8f),
                new Vector2(14f, -10f),
                new Vector2(-16f, 8f),
                new Vector2(18f, 7f),
                new Vector2(0f, -15f)
            };

            for (int i = 0; i < points.Length; i++)
            {
                Vector2 start = points[i] * (1.25f - t * 0.75f);
                Vector2 end = Vector2.Zero;
                Vector2 pos = Vector2.Lerp(start, end, EaseInOut(t));
                pos += new Vector2(MathF.Sin(Wiggle * 1.4f + i), MathF.Cos(Wiggle * 1.2f + i)) * 1.1f * (1f - t);
                if (effects == SpriteEffects.FlipHorizontally)
                    pos.X *= -1f;

                float alpha = 0.32f * (1f - Math.Abs(t - 0.5f) * 2f);
                float scale = 0.22f + (1f - t) * 0.08f;

                spriteBatch.Draw(
                    texture,
                    drawPos + pos,
                    orbFrame,
                    Color.LightBlue * alpha,
                    NPC.rotation * 0.35f,
                    origin,
                    scale,
                    effects,
                    0f
                );
            }
        }

        private static float EaseInOut(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0 || NPC.life >= 0)
            {
                int d = 193;
                for (int k = 0; k < 12; k++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, d, 2.5f * hit.HitDirection, -2.5f, 0, Color.LightBlue, 0.7f);
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, d, 2.5f * hit.HitDirection, -2.5f, 0, Color.LightBlue, 0.7f);
                }
            }
        }
    }
}
