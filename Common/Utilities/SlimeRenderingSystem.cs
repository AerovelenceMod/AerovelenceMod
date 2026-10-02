using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace AerovelenceMod.Common.Utilities
{
    public sealed class SlimeRenderingSystem : ModSystem
    {
        public override void PostDrawInterface(SpriteBatch spriteBatch) => SlimeRenderer.Collect();
        public override void PostUpdateNPCs() => VanillaSlimeVisual.UpdatePending();
        public override void PostUpdateEverything() => SlimeParticles.Update();
        public override void PostDrawTiles()
        {
            if (Main.dedServ || SlimeParticles.Active.Count == 0) return;
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            SlimeParticles.Draw(Main.spriteBatch);
            Main.spriteBatch.End();
        }
        public override void OnWorldUnload()
        {
            SlimeParticles.Clear();
            VanillaSlimeVisual.ClearPending();
            Main.QueueMainThreadAction(SlimeRenderer.Clear);
        }
        public override void Unload() => Main.QueueMainThreadAction(() =>
        {
            SlimeRenderer.Clear();
            VanillaSlimeVisual.ClearCache();
            SlimeParticles.Unload();
        });
    }
}
