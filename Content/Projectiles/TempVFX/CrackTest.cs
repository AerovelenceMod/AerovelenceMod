using System;
using AerovelenceMod.Content.Items;
using AerovelenceMod.Common.Systems;
using Terraria.Audio;
using Terraria.DataStructures;

namespace AerovelenceMod.Content.Projectiles.TempVFX
{
    public sealed class CrackTest : ModProjectile
    {
        private static readonly CrackShading skyShading = new(new Color(214, 230, 236) * .65f, new Color(17, 17, 34) * .7f)
        { SampleHighlight = CrackUtil.SampleEnvironmentHighlight };
        private static readonly CrackShading groundShading = new(new Color(170, 143, 134) * .45f, new Color(12, 9, 20) * .65f)
        { SampleHighlight = CrackUtil.SampleEnvironmentHighlight };
        private static readonly Rectangle imageBounds = new(-112, -130, 224, 336);
        private CrackUtil.CrackData crack;
        private float opening;
        private int impacts = 1;
        private int shownImpacts;
        private bool Sky => Projectile.ai[0] == 1f;
        public override string Texture => "Terraria/Images/Projectile_0";
        public override Terraria.Localization.LocalizedText DisplayName => ModContent.GetInstance<DebugItem>().DisplayName;
        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;
        public override bool? CanCutTiles() => false;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }

        internal static void Place(Player player, IEntitySource source, Vector2 target, bool sky)
        {
            if (player.whoAmI != Main.myPlayer) return;
            if (!sky)
            {
                Point tile = target.ToTileCoordinates();
                bool found = false;
                for (int y = tile.Y; y < Math.Min(Main.maxTilesY - 10, tile.Y + 80); y++)
                {
                    if (!WorldGen.InWorld(tile.X, y, 10) || !WorldGen.SolidTile(tile.X, y)) continue;
                    target.Y = y * 16f + 6f;
                    found = true;
                    break;
                }
                if (!found) return;
            }
            target = new Vector2(MathF.Round(target.X / 2f), MathF.Round(target.Y / 2f)) * 2f;
            int type = ModContent.ProjectileType<CrackTest>();
            Projectile closest = null;
            float distance = sky ? 64f * 64f : 24f * 24f;
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != type || (projectile.ai[0] == 1f) != sky) continue;
                float next = Vector2.DistanceSquared(projectile.Center, target);
                if (next >= distance) continue;
                closest = projectile;
                distance = next;
            }
            if (closest == null)
                Projectile.NewProjectile(source, target, Vector2.Zero, type, 0, 0f, player.whoAmI, sky ? 1f : 0f, 1f);
            else
            {
                if (sky) closest.ai[1] = Math.Min(12f, closest.ai[1] + 1f);
                closest.ai[2] = 0f;
                closest.netUpdate = true;
            }
            SoundEngine.PlaySound(SoundID.Shatter with { Volume = .3f, Pitch = .4f }, target);
        }

        public override void AI()
        {
            Projectile.timeLeft = 2;
            if (++Projectile.ai[2] >= (Sky ? 1800f : 600f))
            {
                Projectile.Kill();
                return;
            }
            if (Main.dedServ) return;
            crack ??= CrackUtil.Create(Vector2.Zero, Sky ? new Rectangle(-240, -240, 480, 480) : new Rectangle(-80, -48, 160, 112),
                Projectile.identity, Sky ? new CrackSettings { Length = 52f, Branches = 7, Thickness = 6f, Taper = 1.4f, OpeningBounds = imageBounds }
                : new CrackSettings { Length = 72f, Branches = 6, Thickness = 7f, Taper = 1.8f, DecayRate = .15f });
            if (Sky)
            {
                if (Projectile.ai[2] == 1f || shownImpacts < Projectile.ai[1])
                {
                    shownImpacts = (int)Projectile.ai[1];
                    if (Projectile.ai[2] < 30f)
                    {
                        float distance = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
                        float power = (3f + Projectile.ai[1] * .5f) * MathHelper.Clamp(1f - distance / 1200f, 0f, 1f);
                        AeroPlayer camera = Main.LocalPlayer.GetModPlayer<AeroPlayer>();
                        camera.ScreenShakePower = Math.Max(camera.ScreenShakePower, power);
                    }
                }
                while (impacts < Projectile.ai[1])
                {
                    crack.Grow(14f);
                    impacts++;
                }
                float previousOpening = MathF.Round(opening / CrackUtil.PixelSize) * CrackUtil.PixelSize;
                opening = MathHelper.Lerp(opening, Math.Min(96f, Projectile.ai[1] * 8f), .15f);
                crack.OpeningRadius = opening;
                float nextOpening = MathF.Round(opening / CrackUtil.PixelSize) * CrackUtil.PixelSize;
                if (Projectile.ai[2] < 60f && nextOpening > previousOpening)
                {
                    int index = 0;
                    foreach (CrackFragment fragment in crack.GetOpeningFragments())
                    {
                        if (fragment.BreakRadius >= 24f && fragment.BreakRadius > previousOpening && fragment.BreakRadius <= nextOpening)
                            CrackShardSystem.Break(Projectile.Center, fragment, Projectile.identity * 397 ^ index);
                        index++;
                    }
                }
            }
            else
                crack.Retraction = MathHelper.Clamp((Projectile.ai[2] - 180f) / 60f * crack.Settings.DecayRate, 0f, 1f);
        }

        public override void OnKill(int timeLeft) => crack?.Dispose();

        private bool GroundMask(Vector2 point) => Collision.SolidCollision(Projectile.Center + point - Vector2.One, 2, 2);

        public override bool PreDraw(ref Color lightColor)
        {
            if (crack == null) return false;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            if (Sky)
            {
                Texture2D picture = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Obama").Value;
                CrackUtil.Draw(crack, Main.spriteBatch, position, new Color(28, 18, 40), applyLighting: false,
                    shading: skyShading, fillTexture: picture, fillBounds: imageBounds);
            }
            else
                CrackUtil.Draw(crack, Main.spriteBatch, position, new Color(22, 17, 28) * .9f,
                    mask: GroundMask, shading: groundShading);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
