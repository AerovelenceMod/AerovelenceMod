using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Dusts.OverTile;
using ReLogic.Content;
namespace AerovelenceMod.Common.Drawing
{
    public class Tiletarget : ModSystem
    {
        public RenderTarget2D dustRenderTarget;
        public RenderTarget2D tileTarget;
        public Effect maskEffect;
        private const int MaskTextureSlot = 1;
        private bool hasDust;
        public override void Load()
        {
            if (Main.dedServ)
                return;
            maskEffect = ModContent.Request<Effect>("AerovelenceMod/Effects/Mask/BasicMask", AssetRequestMode.ImmediateLoad).Value;
            On_Main.CheckMonoliths += PrepareTargets;
            On_Main.DrawProjectiles += DrawTarget;
        }
        public override void Unload()
        {
            if (Main.dedServ)
                return;
            On_Main.CheckMonoliths -= PrepareTargets;
            On_Main.DrawProjectiles -= DrawTarget;
            RenderTarget2D oldDustTarget = dustRenderTarget;
            RenderTarget2D oldTileTarget = tileTarget;
            Effect oldEffect = maskEffect;
            dustRenderTarget = null;
            tileTarget = null;
            maskEffect = null;
            hasDust = false;
            Main.QueueMainThreadAction(() =>
            {
                if (oldEffect != null && !oldEffect.IsDisposed)
                    oldEffect.Parameters["Mask"].SetValue((Texture2D)null);
                oldDustTarget?.Dispose();
                oldTileTarget?.Dispose();
            });
        }
        private void PrepareTargets(On_Main.orig_CheckMonoliths orig)
        {
            orig();
            hasDust = false;
            if (Main.gameMenu || Main.dedServ || Main.instance.tileTarget == null || Main.instance.tileTarget.IsDisposed)
                return;
            foreach (Dust dust in Main.dust)
            {
                if (dust.active && ModContent.GetModDust(dust.type) is DrawOverTilesDust)
                {
                    hasDust = true;
                    break;
                }
            }
            if (!hasDust)
                return;
            DrawUtils.EnsureRenderTarget(ref dustRenderTarget, Main.screenWidth, Main.screenHeight);
            DrawUtils.EnsureRenderTarget(ref tileTarget, Main.screenWidth, Main.screenHeight);
            GraphicsDevice device = Main.instance.GraphicsDevice;
            RenderTargetBinding[] bindings = device.GetRenderTargets();
            Viewport viewport = device.Viewport;
            Rectangle scissor = device.ScissorRectangle;
            try
            {
                //Add all DrawOverTileDust to the dust target
                device.SetRenderTarget(dustRenderTarget);
                device.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.EffectMatrix);
                foreach (Dust dust in Main.dust)
                {
                    if (dust.active && ModContent.GetModDust(dust.type) is DrawOverTilesDust drawing)
                        drawing.DrawOverTiles(Main.spriteBatch, dust);
                }
                Main.spriteBatch.End();
                //Tile target
                device.SetRenderTarget(tileTarget);
                device.Clear(Color.Transparent);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.EffectMatrix);
                Main.spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition, Color.White);
                //Change this if you want to draw over Trees and stuff
                //Main.spriteBatch.Draw(Main.instance.tile2Target, Main.sceneTile2Pos - Main.screenPosition, Color.White);
                Main.spriteBatch.End();
            }
            finally
            {
                device.SetRenderTargets(bindings);
                device.Viewport = viewport;
                device.ScissorRectangle = scissor;
            }
        }
        private void DrawTarget(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);
            if (!hasDust || dustRenderTarget == null || dustRenderTarget.IsDisposed || tileTarget == null || tileTarget.IsDisposed)
                return;
            GraphicsDevice device = self.GraphicsDevice;
            Texture previousMask = device.Textures[MaskTextureSlot];
            SamplerState previousSampler = device.SamplerStates[MaskTextureSlot];
            try
            {
                //Draw the dust render target using the tiletarget as a mask
                maskEffect.Parameters["Mask"].SetValue(tileTarget);
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, maskEffect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(dustRenderTarget, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
                Main.spriteBatch.End();
            }
            finally
            {
                device.Textures[MaskTextureSlot] = previousMask;
                device.SamplerStates[MaskTextureSlot] = previousSampler;
            }
        }
    }
}
