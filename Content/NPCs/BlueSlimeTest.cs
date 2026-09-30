using AerovelenceMod.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.NPCs;

public sealed class BlueSlimeTest : ModNPC
{
    private readonly VanillaSlimeVisual visual = new();
    public override string Texture => $"Terraria/Images/NPC_{NPCID.BlueSlime}";

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.BlueSlime];
        AnimationType = NPCID.BlueSlime;
    }

    public override void SetDefaults()
    {
        NPC.CloneDefaults(NPCID.BlueSlime);
        AIType = NPCID.BlueSlime;
    }

    public override void AI() => visual.Update(NPC);

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        visual.Draw(NPC, spriteBatch, screenPos, drawColor);
        return false;
    }
}
