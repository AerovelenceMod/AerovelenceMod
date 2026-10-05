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
    }
}
