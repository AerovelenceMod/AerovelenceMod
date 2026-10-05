



using System.Linq;
using ReLogic.Content;
using AerovelenceMod.Content.Dusts.OverTile;

namespace AerovelenceMod.Common.Drawing
{
    public class Tiletarget : ModSystem
    {
        public RenderTarget2D dustRenderTarget;
        public RenderTarget2D tileTarget;
        public Effect maskEffect;

        public override void Load()
        {
            if (Main.dedServ)
                return;

            maskEffect = ModContent.Request<Effect>("AerovelenceMod/Effects/Mask/BasicMask", AssetRequestMode.ImmediateLoad).Value;

            Main.QueueMainThreadAction(() => dustRenderTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight));
            Main.QueueMainThreadAction(() => tileTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight));

            On_Main.CheckMonoliths += PrepareTargets;
            On_Main.DrawProjectiles += DrawTarget;
        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            maskEffect = null;

            Main.QueueMainThreadAction(() => dustRenderTarget?.Dispose());

            On_Main.CheckMonoliths -= PrepareTargets;
            On_Main.DrawProjectiles -= DrawTarget;
        }


        private void PrepareTargets(On_Main.orig_CheckMonoliths orig)
        {
            orig();

            if (Main.gameMenu || Main.dedServ)
                return;


            //Add all DrawOverTileDust to the dust target
            RenderTargetBinding[] bindings = Main.graphics.GraphicsDevice.GetRenderTargets();
            Main.graphics.GraphicsDevice.SetRenderTarget(dustRenderTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.EffectMatrix);

            Texture2D tex = CommonTextures.feather_circle128PMA.Value;

            foreach (Dust d in Main.dust.Where(d => ModContent.GetModDust(d.type) is DrawOverTilesDust && d.active))
            {
                (ModContent.GetModDust(d.type) as DrawOverTilesDust).DrawOverTiles(Main.spriteBatch, d);
            }
            Main.spriteBatch.End();

            Main.graphics.GraphicsDevice.SetRenderTargets(bindings);


            //Tile target
            RenderTargetBinding[] bindings2 = Main.graphics.GraphicsDevice.GetRenderTargets();
            Main.graphics.GraphicsDevice.SetRenderTarget(tileTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.EffectMatrix);

            Main.spriteBatch.Draw(Main.instance.tileTarget, Main.sceneTilePos - Main.screenPosition, Color.White);
            
            //Change this if you want to draw over Trees and stuff
            //Main.spriteBatch.Draw(Main.instance.tile2Target, Main.sceneTile2Pos - Main.screenPosition, Color.White);

            Main.spriteBatch.End();

            Main.graphics.GraphicsDevice.SetRenderTargets(bindings);
        }

        private void DrawTarget(On_Main.orig_DrawProjectiles orig, Main self)
        {
            orig(self);

            //Draw the dust render target using the tiletarget as a mask
            maskEffect.Parameters["Mask"].SetValue(tileTarget);

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, maskEffect, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(dustRenderTarget, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);

            Main.spriteBatch.End();
        }
    }
}
