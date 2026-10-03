using AerovelenceMod.Common.Globals.Worlds;
using Terraria.GameContent.UI;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.EmoteBubbles
{
    public class TumblerEmote : ModEmoteBubble
    {
        public override void SetStaticDefaults()
        {
            AddToCategory(EmoteID.Category.Dangers);
        }

        public override bool IsUnlocked()
        {
            return DownedWorld.DownedCrystalTumbler;
        }
    }
}