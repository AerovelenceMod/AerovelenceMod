using System;
using AerovelenceMod.Content.Items.BossSummons;


using ReLogic.Content;



namespace AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler
{
    public partial class CrystalTumbler
    {
        private float crystalBloom;
        private float fuzzyStrength;
        private float masterHeat;
        private float ambientStrength;
        private float fenceFade;
        private float FenceOpacity => MathHelper.SmoothStep(1f, 0.5f, fenceFade);
        private TumblerState presentationState;
        private readonly Vector2?[] eyeOrigins = new Vector2?[2];

        private static Rectangle BodyFrame(Texture2D texture, int frameIndex)
        {
            Rectangle frame = texture.Frame(1, 2, 0, frameIndex);
            frame.Height -= 2;
            return frame;
        }

        private Vector2 BodyDrawScale(Rectangle frame) => new(NPC.scale, NPC.scale * frame.Width / frame.Height);

        private void UpdatePresentation()
        {
            if (Main.dedServ)
                return;
            fenceFade = Approach(fenceFade, State == TumblerState.KnifeCrystals ? 1f : 0f, 0.05f);
            if (presentationState != State)
            {
                TumblerLightningSystem.Release(NPC);
                presentationState = State;
            }
            bool visible = State != TumblerState.Spawn || StateTimer >= FallStart;
            bool attacking = State is not (TumblerState.Idle or TumblerState.Spawn or TumblerState.Stunned or TumblerState.Despawn);
            float bloomTarget = visible ? MathHelper.Clamp(visualCharge * 0.85f + impactFlash * 0.6f + shieldFlash * 0.65f + (attacking ? 0.12f : 0f), 0f, 1f) : 0f;
            crystalBloom = Approach(crystalBloom, bloomTarget, bloomTarget > crystalBloom ? 0.08f : 0.035f);
            float fuzzyTarget = PhaseTwo && attacking ? 0.35f + visualCharge * 0.65f : 0f;
            fuzzyStrength = Approach(fuzzyStrength, fuzzyTarget, 0.025f);
            masterHeat = Approach(masterHeat, Main.masterMode && NPC.life <= NPC.lifeMax * 0.25f ? 1f : 0f, 0.012f);
            ambientStrength = Approach(ambientStrength, visible && State != TumblerState.Despawn ? 1f : 0f, 0.025f);
            Color ambient = Color.Lerp(new Color(135, 193, 220), new Color(234, 186, 120), PhaseTwo ? 0.55f : 0f);
            Lighting.AddLight(NPC.Center, ambient.ToVector3() * (0.8f + crystalBloom * 0.35f) * ambientStrength);
            for (int i = 0; i < 8; i++)
            {
                Vector2 position = NPC.Center + (i * MathHelper.PiOver4).ToRotationVector2() * 120f;
                Lighting.AddLight(position, ambient.ToVector3() * 0.32f * ambientStrength);
            }
            if (ArenaData.Valid)
            {
                for (int i = 0; i < ArenaData.CrystalPositions.Length; i++)
                    Lighting.AddLight(ArenaData.CrystalPositions[i], new Vector3(0.12f, 0.21f, 0.27f) * ambientStrength);
            }
        }

        private void DrawFuzzyAura(SpriteBatch spriteBatch, Vector2 center, float opacity)
        {
            if (fuzzyStrength < 0.01f || opacity <= 0f)
                return;
            Texture2D fuzzy = ModContent.Request<Texture2D>(Texture + "_Fuzzy", AssetRequestMode.ImmediateLoad).Value;
            ulong seed = Main.TileFrameSeed ^ (ulong)(NPC.whoAmI + 1) * 7919UL;
            float breath = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 7f) * 0.035f;
            float scale = NPC.scale * (1.75f + fuzzyStrength * 0.28f) * breath;
            TumblerVFX.BeginAdditive(spriteBatch);
            for (int pass = 0; pass < 7; pass++)
            {
                Vector2 jitter = new(Utils.RandomInt(ref seed, -10, 11) * 0.4f, Utils.RandomInt(ref seed, -10, 1) * 0.5f);
                for (int y = 0; y < fuzzy.Height; y += 2)
                {
                    float gradient = y / (float)Math.Max(1, fuzzy.Height - 1);
                    Color hot = Color.Lerp(new Color(255, 238, 111), new Color(255, 82, 12), gradient);
                    Color color = Color.Lerp(new Color(99, 186, 245), hot, masterHeat);
                    color *= opacity * fuzzyStrength * 0.085f;
                    color.A = 255;
                    Vector2 stripOrigin = fuzzy.Size() * 0.5f - new Vector2(0f, y);
                    spriteBatch.Draw(fuzzy, center + jitter, new Rectangle(0, y, fuzzy.Width, Math.Min(2, fuzzy.Height - y)), color, NPC.rotation, stripOrigin, scale, SpriteEffects.None, 0f);
                }
            }
            TumblerVFX.EndAdditive(spriteBatch);
        }

        private void DrawCrystalMasks(SpriteBatch spriteBatch, Vector2 center, int frameIndex, float opacity)
        {
            Texture2D mask = ModContent.Request<Texture2D>(Texture + "_Glowmask", AssetRequestMode.ImmediateLoad).Value;
            Texture2D bloom = ModContent.Request<Texture2D>(Texture + "_Glowmask_Bloom", AssetRequestMode.ImmediateLoad).Value;
            Rectangle maskFrame = BodyFrame(mask, frameIndex);
            Rectangle bloomFrame = BodyFrame(bloom, frameIndex);
            spriteBatch.Draw(mask, center, maskFrame, Color.White * opacity, NPC.rotation, maskFrame.Size() * 0.5f, BodyDrawScale(maskFrame), SpriteEffects.None, 0f);
            if (crystalBloom < 0.01f)
                return;
            TumblerVFX.BeginAdditive(spriteBatch);
            Color bloomColor = Color.White * (opacity * crystalBloom * 0.6f);
            bloomColor.A = 255;
            spriteBatch.Draw(bloom, center, bloomFrame, bloomColor, NPC.rotation, bloomFrame.Size() * 0.5f, BodyDrawScale(bloomFrame), SpriteEffects.None, 0f);
            TumblerVFX.EndAdditive(spriteBatch);
        }

        private void DrawFixedEye(SpriteBatch spriteBatch, Vector2 center, int frameIndex, float opacity, bool backLayer)
        {
            Texture2D eye = ModContent.Request<Texture2D>(Texture + "_Eye", AssetRequestMode.ImmediateLoad).Value;
            Rectangle frame = eye.Frame(1, 2, 0, frameIndex);
            if (!eyeOrigins[frameIndex].HasValue)
            {
                Color[] pixels = new Color[frame.Width * frame.Height];
                eye.GetData(0, frame, pixels, 0, pixels.Length);
                int left = frame.Width, right = -1, top = frame.Height, bottom = -1;
                for (int y = 0; y < frame.Height; y++)
                    for (int x = 0; x < frame.Width; x++)
                        if (pixels[y * frame.Width + x].A > 0)
                        {
                            left = Math.Min(left, x);
                            right = Math.Max(right, x);
                            top = Math.Min(top, y);
                            bottom = Math.Max(bottom, y);
                        }
                eyeOrigins[frameIndex] = right >= left ? new Vector2((left + right + 1f) * 0.5f, (top + bottom + 1f) * 0.5f) : frame.Size() * 0.5f;
            }
            Vector2 origin = eyeOrigins[frameIndex].Value;
            Vector2 bodyFrameSize = new(frame.Width, frame.Height - 2f);
            Vector2 bodyScale = new(1f, bodyFrameSize.X / bodyFrameSize.Y);
            Vector2 anchorOffset = (origin - bodyFrameSize * 0.5f) * bodyScale * NPC.scale;
            Vector2 anchoredCenter = center + anchorOffset.RotatedBy(NPC.rotation);
            if (backLayer)
                DrawEyeBackLayer(spriteBatch, eye, anchoredCenter, frame, origin, opacity);
            else
                DrawEyeFlame(spriteBatch, eye, anchoredCenter, frame, origin, opacity);
        }

        private void DrawEyeBackLayer(SpriteBatch spriteBatch, Texture2D eye, Vector2 center, Rectangle frame, Vector2 origin, float opacity)
        {
            Color bright = frame.Y > 0 ? new Color(255, 190, 90) : Color.SkyBlue;
            Color deep = frame.Y > 0 ? new Color(255, 105, 20) : Color.DeepSkyBlue;
            for (int i = 0; i < 8; i++)
            {
                Color color = i == 0 ? bright with { A = 0 } : deep with { A = 0 };
                spriteBatch.Draw(eye, center + Main.rand.NextVector2Circular(3f, 3f), frame, color * opacity, 0f, origin, NPC.scale * 1.1f, SpriteEffects.None, 0f);
            }
        }

        private void DrawEyeFlame(SpriteBatch spriteBatch, Texture2D eye, Vector2 center, Rectangle frame, Vector2 origin, float opacity)
        {
            float charge = MathHelper.Clamp(visualCharge + shieldFlash * 0.5f, 0f, 1f);
            float breath = 1f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.035f;
            Color color = frame.Y > 0 ? new Color(255, 154, 38) : new Color(45, 155, 255);
            Color outer = color * (opacity * (0.22f + charge * 0.1f));
            Color inner = Color.Lerp(color, Color.White, 0.55f) * (opacity * (0.3f + charge * 0.12f));
            Color core = Color.Lerp(color, Color.White, 0.9f) * (opacity * (0.8f + charge * 0.2f));
            outer.A = inner.A = core.A = 255;
            Texture2D bloom = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64", AssetRequestMode.ImmediateLoad).Value;
            float bloomScale = NPC.scale * breath * (0.25f + charge * 0.035f);
            TumblerVFX.BeginAdditive(spriteBatch);
            spriteBatch.Draw(bloom, center, null, outer, 0f, bloom.Size() * 0.5f, bloomScale * 1.35f, SpriteEffects.None, 0f);
            spriteBatch.Draw(bloom, center, null, inner, 0f, bloom.Size() * 0.5f, bloomScale * 0.68f, SpriteEffects.None, 0f);
            spriteBatch.Draw(eye, center, frame, outer, 0f, origin, NPC.scale * breath * (1.35f + charge * 0.08f), SpriteEffects.None, 0f);
            spriteBatch.Draw(eye, center, frame, core, 0f, origin, NPC.scale, SpriteEffects.None, 0f);
            TumblerVFX.EndAdditive(spriteBatch);
        }

        private void UpdateRotation()
        {
            float targetRoll = spinTarget ?? NPC.velocity.X / (NPC.width * 0.5f);
            if (!spinTarget.HasValue && !OnGround() && State != TumblerState.Despawn)
                targetRoll = rollVelocity * 0.995f;
            rollVelocity = Approach(rollVelocity, targetRoll, spinTarget.HasValue ? 0.012f : 0.025f);
            NPC.rotation += rollVelocity;
        }

        private void DrawAttackEffects(SpriteBatch spriteBatch, Vector2 screenPos, Texture2D texture, Rectangle frame, Vector2 origin)
        {
            if (State == TumblerState.RippleSlam && substate is 1 or 2)
            {
                Vector2 ground = new(rampStart.X, FloorY);
                float charge = substate == 1 ? MathHelper.Clamp(StateTimer / 75f, 0f, 1f) : 1f;
                TumblerVFX.DrawCharge(spriteBatch, ground - screenPos, PhaseColor, charge, 90f, StateTimer * 0.045f);
                TumblerVFX.DrawTelegraph(spriteBatch, ground - screenPos, ground - screenPos - new Vector2(0f, 290f), PhaseColor, 0.4f + charge * 0.45f, 100f);
                for (int side = -1; side <= 1; side += 2)
                    TumblerVFX.DrawTelegraph(spriteBatch, ground - screenPos, ground - screenPos + new Vector2(side * 520f, 0f), PhaseColor, charge * 0.65f, 100f);
            }
            if (State == TumblerState.Overload && StateTimer >= 120 && StateTimer < 205)
            {
                int count = StateTimer < 180 ? 10 : 14;
                float start = StateTimer < 180 ? 120f : 180f;
                float end = StateTimer < 180 ? 180f : 205f;
                float rotation = StateTimer < 180 ? 0f : 0.12f;
                float charge = MathHelper.Clamp((StateTimer - start) / (end - start), 0f, 1f);
                float radius = Math.Min(720f, Math.Max(RightOuter - LeftOuter, FloorY - ArenaData.WorldBounds.Top));
                Vector2 center = NPC.Center - screenPos;
                for (int i = 0; i < count; i++)
                {
                    Vector2 endPoint = center + (MathHelper.TwoPi * i / count + rotation).ToRotationVector2() * radius;
                    TumblerVFX.DrawTelegraph(spriteBatch, center, endPoint, PhaseColor, 0.18f + charge * 0.62f, 58f);
                }
                TumblerVFX.DrawCharge(spriteBatch, center, PhaseColor, charge, 36f - charge * 10f, StateTimer * 0.035f);
            }
            if (State == TumblerState.Stunned)
            {
                float fade = MathHelper.Clamp((stunReturnTimer - StateTimer) / 25f, 0f, 1f);
                Texture2D star = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/CrispStarPMA").Value;
                for (int i = 0; i < 4; i++)
                {
                    float angle = StateTimer * 0.065f + i * MathHelper.PiOver2;
                    Vector2 position = NPC.Top - screenPos + new Vector2(MathF.Cos(angle) * 40f, -22f + MathF.Sin(angle) * 10f);
                    spriteBatch.Draw(star, position, null, TumblerVFX.Glow(Color.Lerp(PhaseColor, Color.White, 0.7f), fade), angle, star.Size() * 0.5f, 17f / star.Width, SpriteEffects.None, 0f);
                }
            }
            if (State == TumblerState.Teleport && StateTimer >= 50 && StateTimer < 108)
            {
                Vector2 start = StateTimer < 70 ? NPC.Center : teleportOrigin;
                float progress = MathHelper.Clamp((StateTimer - 50f) / 36f, 0f, 1f);
                float fade = MathHelper.Clamp((108f - StateTimer) / 28f, 0f, 1f);
                for (int i = 0; i < 12; i++)
                {
                    float position = i / 11f;
                    float brightness = Math.Max(0f, 1f - Math.Abs(position - progress) * 4f) * fade;
                    Vector2 point = Vector2.Lerp(start, teleportDestination, position) - screenPos;
                    spriteBatch.Draw(texture, point, frame, TumblerVFX.Glow(PhaseColor, brightness * 0.45f), NPC.rotation - position * 2f, origin, BodyDrawScale(frame) * (0.8f + brightness * 0.2f), SpriteEffects.None, 0f);
                }
                TumblerVFX.DrawElectricLine(spriteBatch, start - screenPos, teleportDestination - screenPos, PhaseColor, fade * 0.32f, 32, NPC.whoAmI);
            }
            if (State == TumblerState.LoopSlam && substate is 2 or 4)
            {
                Vector2 tip = TumblerLoopRail.Point(rampStart, storedDirection, 1f);
                Vector2 ground = new(tip.X, FloorY);
                float strength = substate == 4 ? 0.8f : 0.35f;
                TumblerVFX.DrawTelegraph(spriteBatch, ground - screenPos, tip - screenPos, PhaseColor, strength, 60f);
                TumblerVFX.DrawCharge(spriteBatch, ground - screenPos, PhaseColor, substate == 4 ? 1f : railProgress, 45f, StateTimer * 0.035f);
            }
            if (State == TumblerState.PhaseTransition)
            {
                Vector2 position = NPC.Top - screenPos - new Vector2(0f, 40f);
                Utils.DrawBorderString(spriteBatch, shieldHits + "!", position, Color.Lerp(PhaseColor, Color.White, shieldFlash), 1.2f + shieldFlash * 0.22f, 0.5f, 0.5f);
            }
        }

        internal static void DrawBackgroundOverlay(Texture2D texture)
        {
            bool bossIsActive = NPC.AnyNPCs(ModContent.NPCType<CrystalTumbler>());
            if (!bossIsActive || texture == null)
                return;

            try
            {
                ModContent.GetInstance<AerovelenceMod>()?.Logger.Warn("Tumbler active");

                var currentState = Main.spriteBatch.GraphicsDevice.BlendState;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.Additive,
                    SamplerState.LinearClamp,
                    DepthStencilState.DepthRead,
                    RasterizerState.CullNone,
                    null,
                    Main.GameViewMatrix.TransformationMatrix
                );

                Main.spriteBatch.Draw(
                    texture,
                    new Rectangle(0, 0, Main.screenWidth, Main.screenHeight),
                    Color.White * 0.8f
                );

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(
                    SpriteSortMode.Immediate,
                    currentState,
                    SamplerState.LinearClamp,
                    DepthStencilState.Default,
                    RasterizerState.CullNone,
                    null,
                    Main.GameViewMatrix.TransformationMatrix
                );
            }
            catch (Exception ex)
            {
                ModContent.GetInstance<AerovelenceMod>()?.Logger.Warn("Error drawing overlay: " + ex.Message);
            }
        }
    }
}
