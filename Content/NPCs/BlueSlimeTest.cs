using AerovelenceMod.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
namespace AerovelenceMod.Content.NPCs
{
    public class BlueSlimeTest : ModNPC
    {
        private readonly SlimeShape shape = new(0) { BodyDome = .72f, BottomCutoff = .98f };
        private readonly SlimeRenderer skin = new(new SlimeAppearance
        {
            PixelSize = 2,
            LightDirection = new Vector2(-.58f, -.82f),
            EdgeSmoothing = .45f,
            BodyBacklight = true,
            BodyHighlights = true,
            DitherShading = false,
            Outline = new Color(0, 13, 42),
            BackDark = new Color(0, 43, 143),
            BackSecondary = new Color(0, 69, 211),
            BackBright = new Color(24, 111, 255),
            BackInner = new Color(11, 89, 238),
            BackAA = new Color(75, 151, 255),
            BackHot = new Color(151, 207, 255),
            BodyDark = new Color(0, 48, 158),
            BodyAA = new Color(5, 78, 220),
            BodyMid = new Color(49, 116, 255),
            BodyHighlight = new Color(135, 193, 255)
        });
        private Vector2 bodySize;
        private Vector2 bodySizeVelocity;
        private Vector2 bodyOffset;
        private Vector2 bodyOffsetVelocity;
        private Vector2 previousVelocity;
        private int jumpTimer;
        private bool wasGrounded;
        private bool paletteLoaded;
        private float landingImpulse;
        private float VisualTime => (float)Main.GameUpdateCount * .055f + NPC.whoAmI * .67f;
        public override string Texture => $"Terraria/Images/NPC_{NPCID.BlueSlime}";
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.BlueSlime];
            AnimationType = NPCID.BlueSlime;
        }
        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.BlueSlime);
            NPC.aiStyle = -1;
        }
        public override void AI()
        {
            NPC.TargetClosest(false);
            bool grounded = NPC.collideY && Math.Abs(NPC.velocity.Y) < .4f;
            if (grounded)
            {
                NPC.velocity.X *= .82f;
                jumpTimer++;
                if (jumpTimer >= 54)
                {
                    Player target = Main.player[NPC.target];
                    NPC.direction = target.active && !target.dead && target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.spriteDirection = NPC.direction;
                    NPC.velocity.X = NPC.direction * 2.15f;
                    NPC.velocity.Y = -5.6f;
                    jumpTimer = 0;
                    NPC.netUpdate = true;
                }
            }
            else
            {
                jumpTimer = 0;
                if (!NPC.collideX) NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, NPC.direction * 1.65f, .012f);
            }
            UpdateShape(grounded);
        }
        private void UpdateShape(bool grounded)
        {
            bool landed = grounded && !wasGrounded && previousVelocity.Y > 1f;
            bool launched = !grounded && wasGrounded && NPC.velocity.Y < -1f;
            if (landed)
            {
                landingImpulse = MathHelper.Clamp(previousVelocity.Y / 7f, 0f, 1f);
                bodySizeVelocity.X += landingImpulse * 5.8f;
                bodySizeVelocity.Y -= landingImpulse * 4.4f;
                bodyOffsetVelocity.Y += landingImpulse * 1.8f;
            }
            if (launched)
            {
                float force = MathHelper.Clamp(-NPC.velocity.Y / 6f, 0f, 1f);
                bodySizeVelocity.X -= force * 3.2f;
                bodySizeVelocity.Y += force * 4.7f;
                bodyOffsetVelocity.Y += force * 1.4f;
            }
            landingImpulse *= .8f;
            float breathe = MathF.Sin(VisualTime * 1.65f);
            float airStretch = MathHelper.Clamp(Math.Abs(NPC.velocity.Y) * .055f, 0f, .42f);
            float horizontalStretch = MathHelper.Clamp(Math.Abs(NPC.velocity.X) * .025f, 0f, .12f);
            float anticipation = grounded ? MathHelper.Clamp((jumpTimer - 38f) / 16f, 0f, 1f) : 0f;
            float frameyAnticipation = anticipation <= 0f ? 0f : anticipation < .34f ? .32f : anticipation < .68f ? .68f : 1f;
            Vector2 baseSize = new(NPC.width * .58f, NPC.height * .6f);
            Vector2 size = new(baseSize.X * (1f - airStretch * .27f + horizontalStretch) + landingImpulse * 5.8f + frameyAnticipation * 5.2f, baseSize.Y * (1f + airStretch * .38f - horizontalStretch * .3f) - landingImpulse * 4f - frameyAnticipation * 4.2f + breathe * .3f);
            float groundedBottom = NPC.height * .5f + 2f;
            Vector2 offset = new(-NPC.velocity.X * .58f, groundedBottom - size.Y * shape.BottomCutoff - NPC.velocity.Y * .12f);
            if (!shape.Initialized)
            {
                bodySize = size;
                bodyOffset = offset;
            }
            float stiffness = frameyAnticipation > 0f ? .4f : .2f;
            float damping = frameyAnticipation > 0f ? .5f : .68f;
            SlimeShape.Spring(ref bodySize, ref bodySizeVelocity, size, stiffness, damping);
            SlimeShape.Spring(ref bodyOffset, ref bodyOffsetVelocity, offset, stiffness * .8f, damping);
            bodySize = Vector2.Max(bodySize, new Vector2(4f));
            shape.Begin(bodyOffset, bodySize, VisualTime, NPC.Center);
            shape.SurfaceCount = 0;
            shape.SurfaceBlend = 0f;
            shape.Initialized = true;
            previousVelocity = NPC.velocity;
            wasGrounded = grounded;
        }
        private void EnsureVanillaPalette()
        {
            if (paletteLoaded) return;
            paletteLoaded = true;
            Color vanilla = NPC.color;
            if (vanilla.A == 0 || vanilla.B < 100) vanilla = new Color(0, 80, 255, 255);
            vanilla.A = 255;
            SlimeAppearance palette = skin.Appearance;
            palette.Outline = Multiply(vanilla, .16f);
            palette.BodyDark = Multiply(vanilla, .55f);
            palette.BodyAA = Multiply(vanilla, .78f);
            palette.BodyMid = Color.Lerp(vanilla, Color.White, .2f);
            palette.BodyHighlight = Color.Lerp(vanilla, Color.White, .55f);
            palette.BackDark = Multiply(vanilla, .48f);
            palette.BackSecondary = Multiply(vanilla, .72f);
            palette.BackInner = Multiply(vanilla, .88f);
            palette.BackBright = Color.Lerp(vanilla, Color.White, .14f);
            palette.BackAA = Color.Lerp(vanilla, Color.White, .38f);
            palette.BackHot = Color.Lerp(vanilla, Color.White, .67f);
        }
        private static Color Multiply(Color color, float amount)
        {
            Vector3 value = color.ToVector3() * amount;
            return new Color(MathHelper.Clamp(value.X, 0f, 1f), MathHelper.Clamp(value.Y, 0f, 1f), MathHelper.Clamp(value.Z, 0f, 1f));
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            EnsureVanillaPalette();
            if (!shape.Initialized) UpdateShape(NPC.collideY && Math.Abs(NPC.velocity.Y) < .4f);
            Color slimeLight = Color.Lerp(drawColor, Color.White, .72f);
            skin.Draw(spriteBatch, shape, NPC.Center - screenPos, slimeLight, .6f);
            return false;
        }
    }
}
