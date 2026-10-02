using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Dusts;

public class CavernPotDust : ModDust
{
    public override void OnSpawn(Dust dust) => UpdateType = DustID.Pot;
}