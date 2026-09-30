using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
namespace AerovelenceMod.Common.Utilities
{
    public sealed class SlimeRenderingSystem : ModSystem
    {
        public override void PostDrawInterface(SpriteBatch spriteBatch) => SlimeRenderer.Collect();
        public override void OnWorldUnload() => Main.QueueMainThreadAction(SlimeRenderer.Clear);
        public override void Unload() => Main.QueueMainThreadAction(SlimeRenderer.Clear);
    }
}