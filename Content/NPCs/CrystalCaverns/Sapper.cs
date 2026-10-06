using AerovelenceMod.Content.Biomes;


using AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler;


using System;
using System.Collections.Generic;

using Terraria.DataStructures;


using Terraria.ModLoader.Utilities;
using Terraria.WorldBuilding;

namespace AerovelenceMod.Content.NPCs.CrystalCaverns
{
    public class Sapper : TranslatableModNPC
    {
        private const int VINE_WIDTH = 22;
        private const int VINE_HEIGHT = 32;
        private const int FRAME_SPACING = 2;
        private const int MAX_SEGMENTS = 6;
        private const int MIN_SEGMENTS = 3;
        private const float VERLET_ITERATIONS = 3f;
        private const float SEGMENT_DISTANCE = 24f;
        private const float SWAY_STRENGTH = 0.02f;
        private const float PLAYER_ATTRACTION = 0.03f;
        private const float LOOK_SPEED = 0.05f;
        private const int ATTACK_COOLDOWN = 90;

        private List<VerletSegment> segments;
        private Player target;
        private int vineVariant;
        private int attackTimer;
        private bool initialized = false;

        private int segmentCount;

        [Obsolete]
        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Sapper", "A flower-like creature that sways with the currents of the Crystal Caverns. Its benign appearance belies its aggressive nature.");
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0)
            {
                Position = new Vector2(0f, 8f),
                PortraitPositionXOverride = 0f
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, value);
        }

        public override void SetDefaults()
        {
            NPC.width = 40;
            NPC.height = 40;
            NPC.damage = 15;
            NPC.defense = 8;
            NPC.lifeMax = 100;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 100f;
            NPC.knockBackResist = 0.5f;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.rotation = 0f;
            NPC.behindTiles = true;
            // Banner = Item.NPCtoBanner(NPCID.Sapper);
            // BannerItem = Item.BannerToItem(Banner);

            SpawnModBiomes = new int[] { ModContent.GetInstance<CrystalCavernsBiome>().Type };
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.InModBiome(ModContent.GetInstance<CrystalCavernsBiome>()))
            {
                return SpawnCondition.Cavern.Chance * 0.5f + SpawnCondition.Underground.Chance * 0.5f;
            }
            return 0f;
        }


        public override void AI()
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }
            FindTarget();
            UpdateVerletSegments();
            HandleSapperBehavior();
            if (segments != null && segments.Count > 0)
            {
                VerletSegment topSegment = segments[^1];
                VerletSegment secondSegment = segments.Count > 1 ? segments[^2] : null;
                Vector2 offset = new Vector2(0, 8).RotatedBy(NPC.rotation);
                NPC.position = topSegment.currentPosition - new Vector2(NPC.width / 2, NPC.height / 2) + offset;
                if (secondSegment != null)
                {
                    Vector2 vineDirection = topSegment.currentPosition - secondSegment.currentPosition;
                    NPC.rotation = (float)Math.Atan2(vineDirection.Y, vineDirection.X) - MathHelper.PiOver2;
                    if (target != null)
                    {
                        Vector2 direction = target.Center - NPC.Center;
                        float targetRotation = (float)Math.Atan2(direction.Y, direction.X) - MathHelper.PiOver2;
                        float angleDifference = MathHelper.WrapAngle(targetRotation - NPC.rotation);
                        float maxAdjustment = MathHelper.ToRadians(15);
                        float clampedDifference = MathHelper.Clamp(angleDifference, -maxAdjustment, maxAdjustment);
                        NPC.rotation += clampedDifference * 0.2f;
                    }
                }
            }
        }

        private void Initialize()
        {
            Point spawnPoint = Point.Zero;
            WorldUtils.Find(NPC.Center.ToTileCoordinates() + new Point(0, 40), Searches.Chain(new Searches.Up(50), new Conditions.IsSolid()), out spawnPoint);
            if (spawnPoint != Point.Zero)
            {
                NPC.Center = spawnPoint.ToWorldCoordinates();
            }

            segmentCount = Main.rand.Next(MIN_SEGMENTS, MAX_SEGMENTS + 1);
            segments = [];
            Vector2 basePos = NPC.Center;
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 segPos = basePos - new Vector2(0, SEGMENT_DISTANCE * i);
                segments.Add(new VerletSegment(segPos));
            }
            segments[0].isFixed = true;
            vineVariant = Main.rand.Next(3);
            attackTimer = ATTACK_COOLDOWN;
        }

        private void FindTarget()
        {
            float closestDistance = 267f;
            target = null;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    float distance = Vector2.Distance(player.Center, NPC.Center);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        target = player;
                    }
                }
            }
        }

        private void UpdateVerletSegments()
        {
            if (segments == null) return;
            float swayFactor = (float)Math.Sin(Main.GameUpdateCount * 0.01f) * SWAY_STRENGTH;
            foreach (var segment in segments)
            {
                if (!segment.isFixed)
                {
                    Vector2 tempPosition = segment.currentPosition;
                    segment.currentPosition += (segment.currentPosition - segment.oldPosition) * 0.97f;
                    segment.currentPosition.Y += 0.05f;
                    segment.currentPosition.X += swayFactor;
                    if (target != null)
                    {
                        Vector2 playerGroundPos = new(target.Center.X, target.position.Y + target.height);
                        Vector2 toPlayer = playerGroundPos - segment.currentPosition;
                        float distanceToPlayer = toPlayer.Length();
                        if (distanceToPlayer < 267f)
                        {
                            int indexFromTop = segments.IndexOf(segment);
                            float influenceFactor = (segments.Count - indexFromTop) / (float)segments.Count;
                            toPlayer.Normalize();
                            segment.currentPosition += toPlayer * PLAYER_ATTRACTION * 5f * influenceFactor;
                            segment.currentPosition.X -= swayFactor * 0.8f;
                        }
                    }
                    segment.oldPosition = tempPosition;
                }
            }
            for (int iteration = 0; iteration < VERLET_ITERATIONS; iteration++)
            {
                for (int i = 0; i < segments.Count - 1; i++)
                {
                    VerletSegment segmentA = segments[i];
                    VerletSegment segmentB = segments[i + 1];
                    Vector2 delta = segmentB.currentPosition - segmentA.currentPosition;
                    float distance = delta.Length();
                    float difference = SEGMENT_DISTANCE - distance;
                    float percent = difference / distance * 0.5f;
                    Vector2 offset = delta * percent;
                    if (!segmentA.isFixed)
                        segmentA.currentPosition -= offset;
                    if (!segmentB.isFixed)
                        segmentB.currentPosition += offset;
                }
            }
        }

        private void HandleSapperBehavior()
        {
            attackTimer--;
            if (target != null && attackTimer > 0 && attackTimer < 24 && !Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(20f, 20f);
                Dust dust = Dust.NewDustPerfect(NPC.Center + offset, DustID.GemSapphire, -offset * 0.07f, 120, new Color(55, 155, 255), 0.7f);
                dust.noGravity = true;
            }
            if (target != null && attackTimer <= 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    ShootGasClouds();
                attackTimer = ATTACK_COOLDOWN + Main.rand.Next(-10, 11);
            }
        }

        private void ShootGasClouds()
        {
            Vector2 baseDirection = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY);
            GasSettings settings = GasSettings.For(GasKind.Sapfog) with { Hostile = true };
            for (int i = 0; i < 7; i++)
            {
                float spreadAngle = MathHelper.ToRadians(-32 + i * 10.67f);
                Vector2 spreadDirection = baseDirection.RotatedBy(spreadAngle);
                GasUtil.Emit(NPC.GetSource_FromAI(), NPC.Center + baseDirection * 10f, spreadDirection * Main.rand.NextFloat(4f, 6f), settings, Math.Max(5, NPC.damage / 3));
            }
        }

        public override void DrawBehind(int index)
        {
            // Case 2: Behind tiles, but in front of non solid tiles
            Main.instance.DrawCacheProjsBehindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (segments == null || segments.Count == 0) return true;

            Texture2D vineTexture = ModContent.Request<Texture2D>("AerovelenceMod/Content/NPCs/CrystalCaverns/Sapper_Vines").Value;
            int vineFrameHeight = VINE_HEIGHT + FRAME_SPACING;
            int vineYFrame = vineVariant * vineFrameHeight;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                Vector2 positionA = segments[i].currentPosition - screenPos;
                Vector2 positionB = segments[i + 1].currentPosition - screenPos;
                Vector2 direction = positionB - positionA;
                float distance = direction.Length();
                float rotation = (float)Math.Atan2(direction.Y, direction.X) - MathHelper.PiOver2;
                float stretchFactor = distance / VINE_HEIGHT;
                Rectangle sourceRectangle = new(0, vineYFrame, VINE_WIDTH, VINE_HEIGHT);
                Vector2 origin = new(VINE_WIDTH / 2, 0);
                spriteBatch.Draw(vineTexture, positionA, sourceRectangle, drawColor, rotation, origin, new Vector2(1f, stretchFactor), SpriteEffects.None, 0f);
                if (i == segments.Count - 2)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        float bendProgress = c / 2f;
                        Vector2 bendOffset = Vector2.Lerp(new Vector2(0, 2).RotatedBy(rotation), new Vector2(0, 4), bendProgress);
                        Vector2 bendPosition = positionB - bendOffset;
                        float bendRotation = rotation - (0.1f * (1 - bendProgress));
                        Rectangle bendRect = new(0, vineYFrame, VINE_WIDTH, 4);
                        Vector2 bendOrigin = new(VINE_WIDTH / 2, 0);
                        float bendScale = 1f - (bendProgress * 0.3f);
                        spriteBatch.Draw(vineTexture, bendPosition, bendRect, drawColor * (1f - (bendProgress * 0.3f)), bendRotation, bendOrigin, new Vector2(bendScale, bendScale), SpriteEffects.None, 0f);
                    }
                }
            }
            return true;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 20; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PurpleCrystalShard,
                        hit.HitDirection * 2f, -2f, 0, default, 1f);
                }
            }
            else
            {
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.PurpleCrystalShard,
                        hit.HitDirection, -1f, 0, default, 0.8f);
                }
            }
        }
    }

    public class VerletSegment
    {
        public Vector2 currentPosition;
        public Vector2 oldPosition;
        public bool isFixed;

        public VerletSegment(Vector2 position)
        {
            currentPosition = position;
            oldPosition = position;
            isFixed = false;
        }
    }
}
