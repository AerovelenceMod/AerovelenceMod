
using System;

using Terraria.GameContent.RGB;



namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural
{
    [LegacyName("ArenaCavernCrystal")]
    public class ArenaCavernCrystalTile : ModTile
    {
        public override string Texture => ModContent.GetInstance<CavernCrystalTile>().Texture;
        public override void SetStaticDefaults()
        {
            MineResist = 2.5f;
            Main.tileSolid[Type] = true;
            Main.tileMerge[Type][ModContent.TileType<CrystalGrassTile>()] = true;
            Main.tileMerge[Type][ModContent.TileType<CavernCrystalTile>()] = true;
            Main.tileMerge[Type][ModContent.TileType<CavernStoneTile>()] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlendAll[Type] = true;
            Main.tileMergeDirt[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLighted[Type] = true;
            AddMapEntry(new Color(115, 230, 250));
            DustType = DustID.BlueFairy;
            HitSound = SoundID.Tink;
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            float lightFactor = MathHelper.Lerp(0.3f, 2f, ((float)Math.Pow(Math.Sin(NoiseHelper.GetDynamicNoise(new Vector2(i * 0.02f, j * 0.02f), Main.GlobalTimeWrappedHourly * 0.2f)), 2)));
            r = 0.0f * lightFactor;
            g = 0.6f * lightFactor;
            b = 0.9f * lightFactor;
        }

        public override bool CanExplode(int i, int j)
        {
            return false;
        }
    }
}
