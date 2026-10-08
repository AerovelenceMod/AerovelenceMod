using AerovelenceMod.Content.Biomes;



using System;
using System.Collections.Generic;
using System.IO;

using Terraria.GameContent.Bestiary;


using Terraria.ModLoader.Utilities;
namespace AerovelenceMod.Content.NPCs.CrystalCaverns
{
    public class CrystalSlime : ModNPC
    {
        private enum ActionState
        {
            Idle,
            Ceiling,
            Radial,
            FloorSplat,
            Stretch
        }
        private readonly SlimeShape shape = new(9) { BottomCutoff = 1f };
        private readonly VanillaSlimeVisual movement;
        private const int AttackCooldown = 300;
        private const int RadialReachTime = 28;
        private readonly SlimeRenderer skin = new(new SlimeAppearance
        {
            PixelSize = 2,
            LightDirection = new Vector2(-.58f, -.82f),
            EdgeSmoothing = .65f,
            BodyBacklight = true,
            BodyHighlights = true,
            DitherShading = false,
            UniformOutline = true,
            PixelStyle = new SlimePixelStyle(
            [
                new(19, 13, 44), new(0, 0, 0), new(36, 32, 52),
                new(69, 66, 88), new(90, 92, 110), new(134, 146, 166)
            ], SlimePixelStyle.Ice.Material, backlight: new(82, 145, 228), outlineLight: new(36, 32, 52),
                joinHighlights: SlimePixelStyle.Ice.JoinHighlights, shadowWidth: SlimePixelStyle.Ice.ShadowWidth),
            Outline = new(0, 0, 0),
            OutlineLight = new(36, 32, 52),
            OutlineShadow = new(19, 13, 44),
            BacklightOutline = new(23, 20, 90),
            BackDark = new(14, 12, 58),
            BackSecondary = new(23, 20, 90),
            BackBright = new(65, 30, 96),
            BackInner = new(51, 45, 159),
            BackAA = new(82, 145, 228),
            BackHot = new(81, 232, 255),
            BodyDark = new(36, 32, 52),
            BodyAA = new(69, 66, 88),
            BodyMid = new(90, 92, 110),
            BodyHighlight = new(134, 146, 166),
            FacetStyle = new SlimeFacetStyle
            {
                SmoothShading = true,
                InteriorLine = true,
                Outline = new(19, 13, 44),
                OutlineLight = new(23, 20, 90),
                OutlineMid = new(36, 0, 126),
                OutlineHighlight = new(65, 30, 96),
                Highlight = Color.White,
                AccentA = new(244, 255, 0),
                AccentB = new(209, 245, 196),
                Dark = new(51, 45, 159),
                Mid = new(82, 145, 228),
                Bright = new(81, 232, 255)
            }
        });
        public CrystalSlime()
        {
            movement = new(skin, shape)
            {
                BodyDimensions = new Vector2(34, 24),
                GroundOffset = 2f,
                EmitParticles = false,
                ShapeModifier = ApplyCrystalShape
            };
        }
        private Vector2 previousVelocity;
        private Vector2 bodySize = new(17, 12);
        private Vector2 bodySizeVelocity;
        private Vector2 bodyOffset = new(0, 4);
        private Vector2 bodyOffsetVelocity;
        private Vector2 anchorA;
        private Vector2 anchorB;
        private Vector2 anchorNormalA = Vector2.UnitY;
        private Vector2 anchorNormalB = -Vector2.UnitY;
        private Vector2 nextGrip;
        private float gripTimer = -1;
        private int leapTicks;
        private int hopTimer;
        private int hopIndex;
        private int splatImpactTicks;
        private bool airborneBlob;
        private float blobAmount;
        private bool splatSpikes;
        private int surfaceCount;
        private readonly Vector2[] surfacePoints = new Vector2[17];
        private readonly Vector2[] surfaceNormals = new Vector2[17];
        private readonly Vector2[] radialAnchors = new Vector2[4];
        private ref float State => ref NPC.ai[0];
        private ref float Timer => ref NPC.ai[1];
        private ref float Phase => ref NPC.ai[2];
        private ref float LastAttack => ref NPC.ai[3];
        private ActionState CurrentState => (ActionState)(int)State;
        private float VisualTime => (float)Main.GameUpdateCount * 0.055f + NPC.whoAmI * 0.67f;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 2;
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
            NPC.width = 38;
            NPC.height = 32;
            NPC.value = Item.buyPrice(0, 0, 7, 0);
            NPC.lavaImmune = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = null;
            SpawnModBiomes = new int[] { ModContent.GetInstance<CrystalCavernsSurfaceBiome>().Type };
        }
        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement>
            {
                new FlavorTextBestiaryInfoElement("Like any slime, these creatures have adapted well to their habitat. This one, particularly well...")
            });
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.InModBiome(ModContent.GetInstance<CrystalCavernsSurfaceBiome>()))
                return SpawnCondition.OverworldDaySlime.Chance + SpawnCondition.OverworldNightMonster.Chance * 0.5f;
            return 0f;
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            WriteVector(writer, anchorA);
            WriteVector(writer, anchorB);
            WriteVector(writer, anchorNormalA);
            WriteVector(writer, anchorNormalB);
            WriteVector(writer, nextGrip);
            writer.Write(gripTimer);
            writer.Write(leapTicks);
            writer.Write(hopTimer);
            writer.Write((byte)hopIndex);
            writer.Write((byte)splatImpactTicks);
            writer.Write(airborneBlob);
            writer.Write(splatSpikes);
            writer.Write((byte)surfaceCount);
            for (int i = 0; i < surfaceCount; i++)
            {
                WriteVector(writer, surfacePoints[i]);
                WriteVector(writer, surfaceNormals[i]);
            }
            for (int i = 0; i < radialAnchors.Length; i++)
                WriteVector(writer, radialAnchors[i]);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            anchorA = ReadVector(reader);
            anchorB = ReadVector(reader);
            anchorNormalA = ReadVector(reader);
            anchorNormalB = ReadVector(reader);
            nextGrip = ReadVector(reader);
            gripTimer = reader.ReadSingle();
            leapTicks = reader.ReadInt32();
            hopTimer = reader.ReadInt32();
            hopIndex = reader.ReadByte();
            splatImpactTicks = reader.ReadByte();
            airborneBlob = reader.ReadBoolean();
            splatSpikes = reader.ReadBoolean();
            int receivedSurfaces = reader.ReadByte();
            surfaceCount = Math.Min(receivedSurfaces, surfacePoints.Length);
            for (int i = 0; i < receivedSurfaces; i++)
            {
                Vector2 point = ReadVector(reader);
                Vector2 normal = ReadVector(reader);
                if (i >= surfaceCount) continue;
                surfacePoints[i] = point;
                surfaceNormals[i] = normal;
            }
            for (int i = 0; i < radialAnchors.Length; i++)
                radialAnchors[i] = ReadVector(reader);
        }
        public override void AI()
        {
            NPC.TargetClosest(false);
            if (splatImpactTicks > 0) splatImpactTicks--;
            Player target = Main.player[NPC.target];
            Lighting.AddLight(NPC.Center, 0.055f, 0.12f, 0.22f);
            switch (CurrentState)
            {
                case ActionState.Idle:
                    DoIdle(target);
                    break;
                case ActionState.Ceiling:
                    DoCeiling(target);
                    break;
                case ActionState.Radial:
                    DoRadial(target);
                    break;
                case ActionState.FloorSplat:
                    DoFloorSplat(target);
                    break;
                case ActionState.Stretch:
                    DoStretch(target);
                    break;
            }
            NPC.knockBackResist = CurrentState == ActionState.FloorSplat && Phase == 2 ? 0 : airborneBlob ? .95f : .2f;
            previousVelocity = NPC.velocity;
            ConfigureMovement();
            if (Main.dedServ) UpdateShape();
            else movement.QueueUpdate(NPC);
        }
        private void DoIdle(Player target)
        {
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.damage = 15;
            Timer++;
            bool grounded = SlimeSurface.IsGrounded(NPC);
            if ((airborneBlob || splatImpactTicks > 0) && TryImpactSplat(false)) return;
            if (!target.active || target.dead)
            {
                NPC.velocity.X *= .96f;
                return;
            }
            NPC.direction = target.Center.X >= NPC.Center.X ? 1 : -1;
            NPC.spriteDirection = NPC.direction;
            if (grounded && NPC.velocity.Y == 0f)
            {
                if (airborneBlob)
                {
                    airborneBlob = false;
                    hopTimer = 0;
                    NPC.netUpdate = Main.netMode != NetmodeID.MultiplayerClient;
                }
                NPC.velocity.X *= .8f;
                if (Math.Abs(NPC.velocity.X) < .1f) NPC.velocity.X = 0f;
                hopTimer++;
                int wait = hopIndex == 2 ? 58 : 38;
                if (hopTimer >= wait && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float distance = Math.Abs(target.Center.X - NPC.Center.X);
                    bool largeHop = hopIndex == 2;
                    NPC.velocity = new Vector2(NPC.direction * MathHelper.Clamp(distance / 95, 1.5f, largeHop ? 4 : 3), largeHop ? -8 : -5.4f);
                    airborneBlob = true;
                    hopIndex = (hopIndex + 1) % 3;
                    hopTimer = 0;
                    NPC.netUpdate = true;
                }
                if (!airborneBlob && Timer >= AttackCooldown && hopTimer >= 12 && Vector2.DistanceSquared(NPC.Center, target.Center) < 760 * 760 && Main.netMode != NetmodeID.MultiplayerClient)
                    ChooseAttack();
            }
            else
            {
                airborneBlob = !grounded;
                if (!NPC.collideX) NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * 2.1f, .018f);
            }
        }
        private void ChooseAttack()
        {
            int next = Main.rand.Next(1, 5);
            if (next == (int)LastAttack)
                next = next % 4 + 1;
            LastAttack = next;
            airborneBlob = false;
            surfaceCount = 0;
            State = next;
            Timer = 0f;
            Phase = 0f;
            anchorA = NPC.Center;
            anchorB = NPC.Center;
            for (int i = 0; i < radialAnchors.Length; i++)
                radialAnchors[i] = NPC.Center;
            NPC.netUpdate = true;
        }
        private void ReturnToIdle(float launchY = -2.8f)
        {
            State = (float)ActionState.Idle;
            Timer = -Main.rand.Next(60);
            Phase = 0f;
            airborneBlob = true;
            hopTimer = 0;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.velocity.Y = launchY;
            NPC.netUpdate = true;
        }
        private void DoCeiling(Player target)
        {
            NPC.damage = 15;
            Timer++;
            NPC.noTileCollide = false;
            if (Phase == 0)
            {
                NPC.noGravity = false;
                NPC.velocity.X *= .8f;
                if (Timer >= 26 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!TryFindRoof(target, false, out anchorA)) { ReturnToIdle(); return; }
                    anchorNormalA = Vector2.UnitY;
                    nextGrip = Vector2.Zero;
                    gripTimer = -1;
                    NPC.velocity = LeapVelocity(anchorA, out leapTicks);
                    NPC.noGravity = true;
                    Phase = 1;
                    Timer = 0;
                    NPC.netUpdate = true;
                }
                return;
            }
            if (Phase == 1)
            {
                NPC.noGravity = true;
                NPC.velocity.Y += .18f;
                Vector2 landing = anchorA + Vector2.UnitY * (NPC.height * .5f + 3);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!AnchorExists(anchorA, anchorNormalA)) { DropFromCeiling(target); return; }
                    bool canGrip = Timer >= 3 && Vector2.DistanceSquared(NPC.Center, landing) <= 24 * 24 && !Collision.SolidCollision(landing - NPC.Size * .5f, NPC.width, NPC.height) && ClearLine(NPC.Center, landing);
                    if (canGrip)
                    {
                        NPC.Center = landing;
                        Phase = 2;
                        Timer = 0;
                        NPC.velocity = new Vector2(MathHelper.Clamp(NPC.velocity.X * .2f, -1, 1), 0);
                        NPC.netUpdate = true;
                        SpawnHazards(3);
                        BurstDust(anchorA + Vector2.UnitY * 4, 8, 1.7f);
                    }
                    else if (Timer > leapTicks + 12 || Timer > 2 && NPC.collideX) DropFromCeiling(target);
                }
                return;
            }
            if (Phase == 3)
            {
                NPC.noGravity = false;
                if (Main.netMode != NetmodeID.MultiplayerClient && (Timer >= 24 || Timer > 5 && NPC.collideY)) ReturnToIdle(NPC.velocity.Y);
                return;
            }
            NPC.noGravity = true;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!target.active || target.dead || !AnchorExists(anchorA, anchorNormalA) || Timer >= 300 || Timer > 120 && gripTimer < 0 && Math.Abs(target.Center.X - NPC.Center.X) < 28 && target.Center.Y > NPC.Center.Y)
                {
                    DropFromCeiling(target);
                    return;
                }
                if (nextGrip != Vector2.Zero && !AnchorExists(nextGrip, Vector2.UnitY))
                {
                    nextGrip = Vector2.Zero;
                    gripTimer = -1;
                    NPC.netUpdate = true;
                }
                if (gripTimer < 0 && Timer >= 32 && (int)Timer % 42 == 0 && TryFindRoof(target, true, out Vector2 grip))
                {
                    nextGrip = grip;
                    gripTimer = 0;
                    NPC.netUpdate = true;
                }
            }
            Vector2 support = anchorA;
            if (gripTimer >= 0)
            {
                gripTimer = Math.Min(gripTimer + 1, 34);
                support = Vector2.Lerp(anchorA, nextGrip, EaseInOut((gripTimer - 16) / 16));
                if (gripTimer >= 32 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    anchorA = nextGrip;
                    nextGrip = Vector2.Zero;
                    gripTimer = -1;
                    NPC.netUpdate = true;
                }
            }
            NPC.velocity.Y += .23f;
            NPC.velocity.X += MathHelper.Clamp((target.Center.X - NPC.Center.X) * .0025f, -.13f, .13f);
            NPC.velocity *= .985f;
            float length = MathHelper.Lerp(24, 64, EaseInOut(Timer / 24));
            Vector2 offset = NPC.Center + NPC.velocity - support;
            if (offset.LengthSquared() > length * length)
                NPC.velocity = support + SafeNormalize(offset, Vector2.UnitY) * length - NPC.Center;
            NPC.velocity = Collision.TileCollision(NPC.position, NPC.velocity, NPC.width, NPC.height, true, true);
            if ((int)Timer % 24 == 0 && Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
        }
        private void DoRadial(Player target)
        {
            NPC.damage = 15;
            Timer++;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.velocity.X *= 0.76f;
            if (NPC.collideY)
                NPC.velocity.Y = 0f;
            else
                NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.28f, 8f);
            if (Phase == 0f && Timer >= 36f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float baseAngle = Main.rand.NextFloat(-.18f, .18f);
                for (int i = 0; i < 4; i++)
                {
                    float angle = baseAngle + MathHelper.Lerp(-2.8f, -.34f, i / 3f) + Main.rand.NextFloat(-.1f, .1f);
                    Vector2 direction = angle.ToRotationVector2();
                    radialAnchors[i] = TraceAnchor(NPC.Center, direction, Main.rand.NextFloat(56f, 152f));
                }
                Phase = 1f;
                Timer = 0f;
                NPC.netUpdate = true;
            }
            if (Phase == 1f && Timer >= RadialReachTime && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnHazards(4);
                Phase = 2f;
                Timer = 0f;
                NPC.netUpdate = true;
            }
            if (Phase == 2f && Timer >= 72f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Phase = 3f;
                Timer = 0f;
                NPC.netUpdate = true;
            }
            if (Phase == 3f && Timer >= 24f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                BurstDust(NPC.Center, 12, 2.5f);
                ReturnToIdle(-3.1f);
            }
        }
        private void DoFloorSplat(Player target)
        {
            NPC.damage = 15;
            Timer++;
            NPC.noTileCollide = false;
            if (Phase < 2)
            {
                NPC.noGravity = false;
                if (Timer == 1 && Phase == 0)
                {
                    NPC.direction = target.Center.X >= NPC.Center.X ? 1 : -1;
                    NPC.velocity = new Vector2(NPC.direction * 2.7f, -8.2f);
                    airborneBlob = true;
                }
                if (Timer > 3 && TryImpactSplat(true)) return;
                if (Phase == 0 && Timer >= 20 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Phase = 1;
                    Timer = 0;
                    NPC.netUpdate = true;
                }
                if (Phase == 1)
                {
                    NPC.velocity.X *= .98f;
                    NPC.velocity.Y = Math.Min(NPC.velocity.Y + .25f, 13);
                    if (Timer > 100 && Main.netMode != NetmodeID.MultiplayerClient) ReturnToIdle(NPC.velocity.Y);
                }
                return;
            }
            NPC.noGravity = true;
            NPC.velocity = Vector2.Zero;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (!SlimeSurface.IsSolidAt(anchorA - anchorNormalA * 2))
                {
                    ReleaseTheSplat(false);
                    return;
                }
                if ((int)Timer % 12 == 0)
                {
                    BuildSplatPatch();
                    NPC.netUpdate = true;
                }
                if (splatSpikes && Timer == 10) SpawnHazards(9);
                if (Timer >= (splatSpikes ? 96 : 42)) ReleaseTheSplat(true);
            }
        }
        private bool TryImpactSplat(bool attack)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !attack && splatImpactTicks <= 0) return false;
            Vector2 impact = NPC.oldVelocity;
            if (Math.Abs(impact.X) < Math.Abs(previousVelocity.X)) impact.X = previousVelocity.X;
            if (Math.Abs(impact.Y) < Math.Abs(previousVelocity.Y)) impact.Y = previousVelocity.Y;
            Vector2 normal;
            if (NPC.collideX && Math.Abs(impact.X) > (attack ? 2 : 5))
                normal = new Vector2(-Math.Sign(impact.X), 0);
            else if ((NPC.collideY || SlimeSurface.IsGrounded(NPC)) && impact.Y > (attack ? 1 : 6.5f))
                normal = -Vector2.UnitY;
            else if (NPC.collideY && impact.Y < (attack ? -2 : -5) && CurrentState != ActionState.Ceiling)
                normal = Vector2.UnitY;
            else return false;
            Vector2 edge = NPC.Center - normal * (new Vector2(NPC.width, NPC.height) * .5f);
            if (!SlimeSurface.TryContact(edge + normal * 6, -normal, 20, out Vector2 contact, out Vector2 surfaceNormal)) return false;
            State = (float)ActionState.FloorSplat;
            Phase = 2;
            Timer = 0;
            anchorA = contact;
            anchorNormalA = surfaceNormal;
            splatSpikes = attack;
            splatImpactTicks = 0;
            airborneBlob = false;
            NPC.velocity = Vector2.Zero;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            BuildSplatPatch();
            NPC.netUpdate = true;
            BurstDust(contact + surfaceNormal * 3, 14, MathHelper.Clamp(impact.Length() * .35f, 1.5f, 4));
            return true;
        }
        private void BuildSplatPatch()
        {
            const int center = 8;
            surfacePoints[center] = anchorA;
            surfaceNormals[center] = anchorNormalA;
            int first = center, last = center;
            Vector2 tangent = new(-anchorNormalA.Y, anchorNormalA.X);
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Vector2 previous = anchorA;
                for (int step = 1; step <= center; step++)
                {
                    Vector2 probe = previous + tangent * (direction * 8) + anchorNormalA * 10;
                    if (!SlimeSurface.TryContact(probe, -anchorNormalA, 22, out Vector2 point, out Vector2 normal)) break;
                    if (Vector2.Dot(normal, anchorNormalA) < .5f || Math.Abs(Vector2.Dot(point - previous, anchorNormalA)) > 8) break;
                    int index = center + direction * step;
                    surfacePoints[index] = point;
                    surfaceNormals[index] = normal;
                    first = Math.Min(first, index);
                    last = Math.Max(last, index);
                    previous = point;
                }
            }
            surfaceCount = last - first + 1;
            Array.Copy(surfacePoints, first, surfacePoints, 0, surfaceCount);
            Array.Copy(surfaceNormals, first, surfaceNormals, 0, surfaceCount);
        }
        private void ReleaseTheSplat(bool pushOff)
        {
            Vector2 release = pushOff ? anchorNormalA * 3.5f + new Vector2(NPC.direction * 1.5f, -1) : Vector2.Zero;
            ReturnToIdle(release.Y);
            NPC.velocity = release;
            airborneBlob = true;
            hopTimer = 0;
            surfaceCount = 0;
            NPC.netUpdate = true;
            //release the splat. this sounds really funny youre welcome
        }
        private void DoStretch(Player target)
        {
            NPC.damage = 15;
            Timer++;
            if (Phase == 0f)
            {
                NPC.noGravity = false;
                NPC.noTileCollide = false;
                NPC.velocity.X *= 0.74f;
                if (NPC.collideY)
                    NPC.velocity.Y = 0f;
                else
                    NPC.velocity.Y = Math.Min(NPC.velocity.Y + 0.25f, 7f);
                if (Timer >= 24f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (!TryFindSpan(target)) { ReturnToIdle(); return; }
                    Phase = 1f;
                    Timer = 0f;
                    NPC.noGravity = true;
                    NPC.noTileCollide = false;
                    NPC.netUpdate = true;
                }
                return;
            }
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.velocity = Vector2.Zero;
            if (Phase < 3 && Main.netMode != NetmodeID.MultiplayerClient && (!AnchorExists(anchorA, anchorNormalA) || !AnchorExists(anchorB, anchorNormalB) || !ClearBodyPath(NPC.Center, (anchorA + anchorB) * .5f)))
            {
                Phase = 3;
                Timer = 0;
                NPC.netUpdate = true;
            }
            if (Phase == 1f)
            {
                Vector2 midpoint = (anchorA + anchorB) * 0.5f;
                float t = EaseInOut(MathHelper.Clamp(Timer / 24f, 0f, 1f));
                NPC.Center = Vector2.Lerp(NPC.Center, midpoint, 0.08f + t * 0.1f);
                if (Timer >= 24f && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.Center = midpoint;
                    Phase = 2f;
                    Timer = 0f;
                    NPC.netUpdate = true;
                    SpawnHazards(2);
                    BurstDust(anchorA, 8, 2.1f);
                    BurstDust(anchorB, 8, 2.1f);
                }
                return;
            }
            if (Phase == 2)
            {
                Vector2 sway = (anchorA + anchorB) * .5f + new Vector2(MathF.Sin(VisualTime * 1.4f) * 2, MathF.Cos(VisualTime) * 2);
                if (ClearBodyPath(NPC.Center, sway)) NPC.Center = sway;
            }
            if (Phase == 2f && Timer >= 94f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Phase = 3f;
                Timer = 0f;
                NPC.netUpdate = true;
            }
            if (Phase == 3f && Timer >= 24f && Main.netMode != NetmodeID.MultiplayerClient)
            {
                BurstDust(NPC.Center, 15, 3f);
                ReturnToIdle(-3.5f);
            }
        }
        private void SpawnHazards(int count)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            for (int i = 0; i < count; i++)
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<CrystalSlimeHazard>(), NPC.damage, 0f, Main.myPlayer, NPC.whoAmI, i);
        }
        public bool GetHazardSegment(int slot, out Vector2 start, out Vector2 end, out float width)
        {
            start = end = NPC.Center;
            width = 0;
            if (!shape.Initialized || slot < 0 || slot >= shape.TendrilCount || Phase != 2) return false;
            shape.AlignPinnedTips(NPC.Center);
            bool active = CurrentState switch
            {
                ActionState.Ceiling => slot < 3,
                ActionState.Radial => slot < 4,
                ActionState.FloorSplat => splatSpikes && slot < 9 && GetFloorSplatAmount() > .25f,
                ActionState.Stretch => slot < 2,
                _ => false
            };
            if (!active) return false;
            SlimeShape.Tendril tendril = shape.Tendrils[slot];
            if (tendril.Radius < 1) return false;
            start = NPC.Center + tendril.Root;
            end = NPC.Center + tendril.Tip;
            width = tendril.Width(.5f) * 2;
            return true;
        }
        internal bool HazardCollides(int slot, Rectangle target) => GetHazardSegment(slot, out _, out _, out _) && shape.TendrilIntersects(slot, NPC.Center, target);
        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            Vector2 extent = shape.BodyExtent(bodySize);
            return target.Hitbox.Intersects(new Rectangle((int)(NPC.Center.X + bodyOffset.X - extent.X), (int)(NPC.Center.Y + bodyOffset.Y - extent.Y), (int)(extent.X * 2f), (int)(extent.Y * 2f)));
        }
        public override void FindFrame(int frameHeight) { NPC.frame = new Rectangle(0, 0, 38, frameHeight); }
        private void ConfigureMovement()
        {
            int wait = hopIndex == 2 ? 58 : 38;
            movement.JumpAnticipation = CurrentState == ActionState.Idle ? EaseInOut((hopTimer - wait + 24f) / 24f) : 0f;
        }
        private void UpdateShape()
        {
            ConfigureMovement();
            movement.Update(NPC);
        }
        private void ApplyCrystalShape(NPC npc)
        {
            float preparation = CurrentState != ActionState.Idle && Phase == 0 ? EaseInOut(Timer / 26f) : 0;
            Vector2 size = shape.Size + new Vector2(preparation * 3f, -preparation * 2f);
            Vector2 offset = shape.Center;
            float tuck = CurrentState == ActionState.Idle ? airborneBlob ? 1 : EaseInOut((hopTimer - (hopIndex == 2 ? 48 : 28)) / 10f) : CurrentState == ActionState.FloorSplat && Phase < 2 ? 1 : 0;
            blobAmount = MathHelper.Lerp(blobAmount, tuck, tuck > blobAmount ? .42f : .2f);
            float ceilingBlob = CurrentState == ActionState.Ceiling ? Phase == 0 ? EaseInOut(Timer / 26) : Phase == 1 ? 1 : 0 : blobAmount * .2f;
            if (ceilingBlob > 0 && CurrentState == ActionState.Ceiling)
            {
                size = Vector2.Lerp(size, new Vector2(12, 12), ceilingBlob);
                offset = Vector2.Lerp(offset, Vector2.Zero, ceilingBlob);
            }
            if (CurrentState == ActionState.Ceiling && Phase >= 2)
            {
                size = new Vector2(14, 16);
                offset = new Vector2(-NPC.velocity.X * .55f, 3);
            }
            float splat = CurrentState == ActionState.FloorSplat ? MathHelper.Clamp(GetFloorSplatAmount(), 0, 1.08f) : 0;
            if (splat > 0)
            {
                size = Vector2.Lerp(size, new Vector2(12, 9), Math.Min(1, splat));
                offset = Vector2.Lerp(offset, anchorA + anchorNormalA * 6 - NPC.Center, Math.Min(1, splat));
            }
            float stretch = CurrentState == ActionState.Stretch && Phase >= 1
                ? Phase == 1 ? EaseInOut(Timer / 24f) : Phase == 3 ? 1 - EaseInOut(Timer / 24f) : 1 : 0;
            Vector2 spanAxis = SafeNormalize(anchorB - anchorA, Vector2.UnitY);
            Vector2 spanSide = new(-spanAxis.Y, spanAxis.X);
            if (stretch > 0)
            {
                size = Vector2.Lerp(size, new Vector2(10 + Math.Abs(spanAxis.X) * 4, 10 + Math.Abs(spanAxis.Y) * 4), stretch);
                offset = Vector2.Lerp(offset, Vector2.Zero, stretch);
            }
            if (CurrentState == ActionState.Idle)
            {
                bodySize = size;
                bodyOffset = offset;
                bodySizeVelocity = bodyOffsetVelocity = Vector2.Zero;
            }
            else
            {
                SlimeShape.Spring(ref bodySize, ref bodySizeVelocity, size, .2f, .64f);
                SlimeShape.Spring(ref bodyOffset, ref bodyOffsetVelocity, offset, .16f, .65f);
                bodySize = Vector2.Max(bodySize, new Vector2(5));
            }
            shape.Center = bodyOffset;
            shape.Size = bodySize;
            float attached = MathHelper.Clamp(Math.Max(splat, stretch), 0f, 1f);
            shape.TravelStretch = MathHelper.Lerp(shape.TravelStretch, 1f, attached);
            shape.AirborneBlend *= 1f - Math.Min(1f, attached);
            shape.Shear *= 1f - Math.Min(1f, attached);
            shape.Wobble *= 1f - Math.Min(1f, attached);
            shape.SurfaceCount = splat > 0 ? surfaceCount : 0;
            shape.SurfaceBlend = MathHelper.Clamp(splat, 0, 1);
            for (int i = 0; i < shape.SurfaceCount; i++)
            {
                shape.SurfacePoints[i] = Vector2.Lerp(anchorA, surfacePoints[i], MathHelper.Clamp(splat, 0, 1)) - NPC.Center
                    + (surfaceNormals[i].Y < -.25f ? Vector2.UnitY * 2f : Vector2.Zero);
                shape.SurfaceNormals[i] = surfaceNormals[i];
            }
            for (int i = 0; i < 9; i++)
            {
                float angle = MathHelper.Lerp(-2.85f, -.3f, Math.Min(i, 3) / 3f);
                Vector2 outward = angle.ToRotationVector2();
                Vector2 root = shape.GetGelPoint(outward * .65f);
                Vector2 tip = shape.GetGelPoint(outward) + SafeNormalize(shape.TransformBodyOffset(outward), outward) * (i % 2 == 0 ? 2 : 5);
                Vector2 bend = Vector2.Lerp(root, tip, .5f) + new Vector2(-NPC.velocity.X * .9f, MathF.Sin(VisualTime * 1.6f + i) * 2);
                float radius = i < 4 ? 5.2f : 0;
                float crystal = i < 4 ? i % 2 == 0 ? 16 : 23 : 0;
                bool pad = false;
                bool pinned = false;
                Vector2 padNormal = Vector2.UnitY;
                if (ceilingBlob > 0)
                {
                    tip = Vector2.Lerp(tip, bodyOffset, ceilingBlob);
                    bend = Vector2.Lerp(root, tip, .5f);
                    radius *= 1 - ceilingBlob;
                    crystal *= 1 - ceilingBlob;
                }
                if (CurrentState == ActionState.Radial && Phase >= 1 && i < 4)
                {
                    float reach = MathHelper.Clamp(GetRadialAmount(), 0, 1.08f);
                    tip = Vector2.Lerp(tip, radialAnchors[i] - NPC.Center, reach);
                    Vector2 delta = tip - root;
                    bend = Vector2.Lerp(root, tip, .55f) + new Vector2(delta.X * .13f, Math.Abs(delta.X) * .22f + MathF.Sin(VisualTime * 2 + i) * 4) * reach;
                    radius = 8.5f;
                    crystal = MathHelper.Lerp(crystal, 22, reach);
                }
                if (CurrentState == ActionState.Ceiling && Phase >= 2)
                {
                    float reach = Phase == 2 ? EaseInOut(Timer / 20) : 1 - EaseInOut(Timer / 22);
                    if (i < 3)
                    {
                        root = bodyOffset + new Vector2((i - 1) * 9, 5);
                        tip = Vector2.Lerp(tip, new Vector2((i - 1) * 19 - NPC.velocity.X * 1.5f, 31 + (i % 2) * 8), reach);
                        bend = Vector2.Lerp(root, tip, .5f) + new Vector2(-NPC.velocity.X * .7f + MathF.Sin(VisualTime * 1.7f + i) * 3, 0);
                        crystal = MathHelper.Lerp(crystal, 19, reach);
                    }
                    else if (i == 3 || i == 4)
                    {
                        bool secondary = i == 4;
                        float attachment = Phase == 2 ? 1 : reach;
                        if (secondary) attachment *= nextGrip != Vector2.Zero ? EaseInOut(gripTimer / 16) : 0;
                        else if (gripTimer > 24) attachment *= 1 - EaseInOut((gripTimer - 24) / 8);
                        Vector2 grip = secondary ? nextGrip : anchorA;
                        root = bodyOffset + new Vector2(secondary ? 5 : -5, -9);
                        tip = Vector2.Lerp(root, grip - NPC.Center + Vector2.UnitY * 2, attachment);
                        bend = Vector2.Lerp(root, tip, .5f) + new Vector2(-NPC.velocity.X * 2, 5) * attachment;
                        radius = 6.5f * attachment;
                        crystal = 0;
                        pad = true;
                        pinned = Phase == 2 && attachment >= .99f;
                    }
                }
                if (splat > 0 && surfaceCount > 0)
                {
                    float surfaceIndex = i / 8f * (surfaceCount - 1);
                    int first = (int)surfaceIndex;
                    int second = Math.Min(surfaceCount - 1, first + 1);
                    float along = surfaceIndex - first;
                    Vector2 point = Vector2.Lerp(surfacePoints[first], surfacePoints[second], along);
                    Vector2 normal = SafeNormalize(Vector2.Lerp(surfaceNormals[first], surfaceNormals[second], along), anchorNormalA);
                    root = Vector2.Lerp(anchorA, point, Math.Min(1, splat)) - NPC.Center + normal * 4;
                    tip = root + normal * ((i % 2 == 0 ? 10 : 5) * splat);
                    bend = Vector2.Lerp(root, tip, .5f);
                    radius = splatSpikes ? 4.5f * splat : 0;
                    crystal = splatSpikes ? (i % 2 == 0 ? 25 : 17) * splat : 0;
                }
                if (stretch > 0)
                {
                    if (i < 2)
                    {
                        root = bodyOffset + spanAxis * (i == 0 ? -5 : 5);
                        padNormal = i == 0 ? anchorNormalA : anchorNormalB;
                        tip = Vector2.Lerp(tip, (i == 0 ? anchorA : anchorB) - NPC.Center + padNormal * 2, stretch);
                        bend = Vector2.Lerp(root, tip, .5f) + spanSide * (MathF.Sin(VisualTime * 2 + i * MathHelper.Pi) * 7 * stretch);
                        radius = MathHelper.Lerp(radius, 8, stretch);
                        crystal *= 1 - stretch;
                        pad = true;
                        pinned = stretch >= .99f;
                    }
                    else if (i < 6)
                    {
                        float side = i % 2 == 0 ? -1 : 1;
                        float y = i < 4 ? -6 : 8;
                        root = Vector2.Lerp(root, bodyOffset + spanSide * (side * 5) + spanAxis * y, stretch);
                        tip = Vector2.Lerp(tip, bodyOffset + spanSide * (side * 17) + spanAxis * (y + (i < 4 ? -5 : 7)), stretch);
                        bend = Vector2.Lerp(root, tip, .5f) + new Vector2(-side * 3 * stretch, MathF.Sin(VisualTime + i) * 2);
                        radius = MathHelper.Lerp(radius, 4.8f, stretch);
                        crystal = MathHelper.Lerp(crystal, 17, stretch);
                    }
                }
                shape.SetTendril(i, root, bend, tip, radius, crystal, pad, padNormal, pinned);
            }
            shape.Initialized = true;
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UpdateShape();
            shape.AlignPinnedTips(NPC.Center);
            skin.Draw(spriteBatch, shape, NPC.Center - screenPos, drawColor, NPC.Opacity);
            return false;
        }
        private float GetRadialAmount()
        {
            if (Phase == 1f)
                return EaseOutBack(MathHelper.Clamp(Timer / RadialReachTime, 0f, 1f));
            if (Phase == 2f)
                return 1f;
            if (Phase == 3f)
                return 1f - EaseInOut(MathHelper.Clamp(Timer / 24f, 0f, 1f));
            return 0f;
        }
        private float GetFloorSplatAmount()
        {
            if (Phase != 2f)
                return 0f;
            if (Timer < 14f)
                return EaseOutBack(MathHelper.Clamp(Timer / 14f, 0f, 1f));
            float retractAt = splatSpikes ? 72 : 26;
            float retractDuration = splatSpikes ? 24 : 16;
            if (Timer > retractAt)
                return 1 - EaseInOut((Timer - retractAt) / retractDuration);
            return 1f;
        }
        private readonly record struct SurfaceAnchor(Vector2 Position, Vector2 Normal);
        private List<SurfaceAnchor> FindSurfaces(Vector2 origin, float range, bool roofOnly)
        {
            List<SurfaceAnchor> surfaces = new();
            int radius = (int)(range / 16);
            Point center = origin.ToTileCoordinates();
            for (int y = Math.Max(10, center.Y - radius); y <= Math.Min(Main.maxTilesY - 11, center.Y + radius); y++)
                for (int x = Math.Max(10, center.X - radius); x <= Math.Min(Main.maxTilesX - 11, center.X + radius); x++)
                {
                    if (!WorldGen.SolidTile(x, y)) continue;
                    for (int face = 0; face < (roofOnly ? 1 : 4); face++)
                    {
                        Vector2 normal = face switch { 0 => Vector2.UnitY, 1 => -Vector2.UnitY, 2 => Vector2.UnitX, _ => -Vector2.UnitX };
                        if (WorldGen.SolidTile(x + (int)normal.X, y + (int)normal.Y)) continue;
                        Vector2 point = new Vector2(x * 16 + 8, y * 16 + 8) + normal * 8;
                        if (Vector2.DistanceSquared(origin, point) > range * range || roofOnly && point.Y > origin.Y - 32) continue;
                        if (!ClearLine(origin, point + normal * 4)) continue;
                        surfaces.Add(new SurfaceAnchor(point, normal));
                    }
                }
            return surfaces;
        }
        private static bool ClearLine(Vector2 start, Vector2 end) => Collision.CanHitLine(start, 1, 1, end, 1, 1);
        private bool ClearBodyPath(Vector2 start, Vector2 end)
        {
            int steps = Math.Max(1, (int)(Vector2.Distance(start, end) / 8));
            for (int i = 0; i <= steps; i++)
                if (Collision.SolidCollision(Vector2.Lerp(start, end, i / (float)steps) - NPC.Size * .5f, NPC.width, NPC.height)) return false;
            return true;
        }
        private static bool AnchorExists(Vector2 point, Vector2 normal)
        {
            Point tile = (point - normal * 3).ToTileCoordinates();
            return WorldGen.InWorld(tile.X, tile.Y, 10) && WorldGen.SolidTile(tile.X, tile.Y);
        }
        private bool TryFindSpan(Player target)
        {
            List<SurfaceAnchor> surfaces = FindSurfaces(NPC.Center, 240, false);
            float bestScore = float.MinValue;
            bool found = false;
            for (int i = 0; i < surfaces.Count; i++)
                for (int j = i + 1; j < surfaces.Count; j++)
                {
                    SurfaceAnchor first = surfaces[i], second = surfaces[j];
                    Vector2 span = second.Position - first.Position;
                    float distance = span.Length();
                    if (distance < 80 || distance > 320) continue;
                    Vector2 axis = span / distance;
                    if (Vector2.Dot(first.Normal, axis) < .2f || Vector2.Dot(second.Normal, -axis) < .2f) continue;
                    Vector2 midpoint = (first.Position + second.Position) * .5f;
                    float score = distance * .16f - Vector2.Distance(midpoint, NPC.Center) * .55f - Vector2.Distance(midpoint, target.Center) * .08f + Main.rand.NextFloat(18);
                    if (score <= bestScore || !ClearLine(first.Position + first.Normal * 4, second.Position + second.Normal * 4) || !ClearBodyPath(NPC.Center, midpoint)) continue;
                    bestScore = score;
                    anchorA = first.Position;
                    anchorB = second.Position;
                    anchorNormalA = first.Normal;
                    anchorNormalB = second.Normal;
                    found = true;
                }
            return found;
        }
        private bool TryFindRoof(Player target, bool stepping, out Vector2 grip)
        {
            grip = Vector2.Zero;
            float bestScore = float.MinValue;
            foreach (SurfaceAnchor surface in FindSurfaces(NPC.Center, stepping ? 180 : 224, true))
            {
                Vector2 point = surface.Position;
                if (stepping)
                {
                    float toward = Math.Sign(target.Center.X - NPC.Center.X);
                    float advance = (point.X - anchorA.X) * toward;
                    if (advance < 24 || advance > 100 || Math.Abs(point.Y - anchorA.Y) > 48) continue;
                    if (!ClearLine(anchorA + Vector2.UnitY * 6, point + Vector2.UnitY * 6)) continue;
                    if (!ClearBodyPath(NPC.Center, point + Vector2.UnitY * 60)) continue;
                }
                else if (Math.Abs(point.X - NPC.Center.X) > 120 || !ClearLeap(point, out _)) continue;
                float score = -Math.Abs(point.X - target.Center.X) * .65f - Vector2.Distance(point, NPC.Center) * .35f;
                if (score <= bestScore) continue;
                bestScore = score;
                grip = point;
            }
            return grip != Vector2.Zero;
        }
        private Vector2 LeapVelocity(Vector2 grip, out int ticks)
        {
            Vector2 destination = grip + Vector2.UnitY * (NPC.height * .5f + 3);
            ticks = Math.Clamp((int)(Vector2.Distance(NPC.Center, destination) / 8), 16, 30);
            return (destination - NPC.Center) / ticks - Vector2.UnitY * (.18f * (ticks - 1) * .5f);
        }
        private bool ClearLeap(Vector2 grip, out int ticks)
        {
            Vector2 velocity = LeapVelocity(grip, out ticks);
            Vector2 position = NPC.Center;
            for (int i = 0; i < ticks; i++)
            {
                Vector2 next = position + velocity;
                if (!ClearBodyPath(position, next)) return false;
                position = next;
                velocity.Y += .18f;
            }
            return true;
        }
        private void DropFromCeiling(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Phase = 3;
            Timer = 0;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.velocity = new Vector2(MathHelper.Clamp((target.Center.X - NPC.Center.X) * .045f, -4, 4), Math.Max(3, NPC.velocity.Y));
            NPC.netUpdate = true;
        }
        private Vector2 TraceAnchor(Vector2 origin, Vector2 direction, float maxDistance)
        {
            direction = SafeNormalize(direction, Vector2.UnitX);
            Vector2 last = origin + direction * maxDistance;
            for (float distance = 20f; distance <= maxDistance; distance += 8f)
            {
                Vector2 point = origin + direction * distance;
                int x = (int)(point.X / 16f);
                int y = (int)(point.Y / 16f);
                if (x < 10 || y < 10 || x >= Main.maxTilesX - 10 || y >= Main.maxTilesY - 10)
                    return origin + direction * Math.Max(20f, distance - 8f);
                if (WorldGen.SolidTile(x, y))
                    return origin + direction * Math.Max(20f, distance - 6f);
                last = point;
            }
            return last;
        }
        private void BurstDust(Vector2 position, int amount, float speed)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < amount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(speed, speed) * Main.rand.NextFloat(0.45f, 1f);
                int dust = Dust.NewDust(position - new Vector2(4f), 8, 8, 193, velocity.X, velocity.Y, 0, Color.LightBlue, Main.rand.NextFloat(0.55f, 0.9f));
                Main.dust[dust].noGravity = true;
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 && hit.Knockback > 0 && (CurrentState == ActionState.Idle || CurrentState == ActionState.FloorSplat && Phase < 2))
            {
                splatImpactTicks = 45;
                NPC.netUpdate = Main.netMode != NetmodeID.MultiplayerClient;
            }
            bodyOffsetVelocity += new Vector2(hit.HitDirection * 2, -1);
            bodySizeVelocity += new Vector2(2.5f, -2);
            if (Main.dedServ) return;
            int amount = NPC.life <= 0 ? 22 : 7;
            for (int i = 0; i < amount; i++)
            {
                Vector2 velocity = new Vector2(2.5f * hit.HitDirection, -2.5f) + Main.rand.NextVector2Circular(1.8f, 1.8f);
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 193, velocity.X, velocity.Y, 0, Color.LightBlue, Main.rand.NextFloat(0.55f, 0.85f));
                Main.dust[dust].noGravity = Main.rand.NextBool(2);
            }
        }
        private static float EaseInOut(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
        private static float EaseOutBack(float t)
        {
            t = MathHelper.Clamp(t, 0f, 1f);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = t - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
        private static Vector2 SafeNormalize(Vector2 vector, Vector2 fallback)
        {
            if (vector.LengthSquared() < 0.0001f)
                return fallback;
            vector.Normalize();
            return vector;
        }
        private static void WriteVector(BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
        }
        private static Vector2 ReadVector(BinaryReader reader) { return new Vector2(reader.ReadSingle(), reader.ReadSingle()); }
    }
    public class CrystalSlimeHazard : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_1";
        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            CooldownSlot = ImmunityCooldownID.Bosses;
        }
        public override void AI()
        {
            int npcIndex = (int)Projectile.ai[0];
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }
            NPC npc = Main.npc[npcIndex];
            if (!npc.active || npc.ModNPC is not CrystalSlime slime)
            {
                Projectile.Kill();
                return;
            }
            if (!slime.GetHazardSegment((int)Projectile.ai[1], out Vector2 start, out Vector2 end, out _))
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = (start + end) * 0.5f;
            Projectile.rotation = (end - start).ToRotation();
            Projectile.timeLeft = 2;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            int npcIndex = (int)Projectile.ai[0];
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return false;
            NPC npc = Main.npc[npcIndex];
            if (!npc.active || npc.ModNPC is not CrystalSlime slime)
                return false;
            if (!slime.GetHazardSegment((int)Projectile.ai[1], out Vector2 start, out Vector2 end, out float width))
                return false;
            return slime.HazardCollides((int)Projectile.ai[1], targetHitbox);
        }
        public override bool PreDraw(ref Color lightColor) { return false; }
    }
}
