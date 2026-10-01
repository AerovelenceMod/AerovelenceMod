using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace AerovelenceMod.Content.Dusts.OverTile
{
	public abstract class DrawOverTilesDust : ModDust
	{
        public virtual void DrawOverTiles(SpriteBatch spriteBatch, Dust dust) { }
    }
}