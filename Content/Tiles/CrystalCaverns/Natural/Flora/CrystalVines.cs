using AerovelenceMod.Common.Bases;
using AerovelenceMod.Common.Systems;
using ReLogic.Content;
using System;
using Terraria.GameContent.RGB;
using static AerovelenceMod.Common.Systems.GlowVineSystem;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora
{
    public class CrystalVines : BaseGlowVine
    {
        private Asset<Texture2D> glowTexture;

        public override void SetStaticDefaultsBase()
        {
            Main.tileCut[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = false;
            TileID.Sets.IsVine[Type] = true;
            TileID.Sets.VineThreads[Type] = true;

            AddMapEntry(new Color(100, 125, 255));

            HitSound = SoundID.Grass;
            DustType = DustID.Skyware;

            glowTexture = (Main.dedServ ? null : ModContent.Request<Texture2D>(Texture));
        }

        public override bool TileFrame(int i, int j, ref bool resetFrame, ref bool noBreak)
        {
            Tile tileAbove = Framing.GetTileSafely(i, j - 1);
            int type = -1;
            if (tileAbove.HasTile && !tileAbove.BottomSlope)
            {
                type = tileAbove.TileType;
            }

            if (type == ModContent.TileType<CrystalGrassTile>() || type == Type)
            {
                return true;
            }

            WorldGen.KillTile(i, j);
            return true;
        }

        public override void RandomUpdate(int i, int j)
        {
            Tile tileBelow = Framing.GetTileSafely(i, j + 1);
            if (WorldGen.genRand.NextBool(10) && !tileBelow.HasTile && tileBelow.LiquidType != LiquidID.Lava)
            {
                bool placeVine = false;
                int yTest = j;
                while (yTest > j - 10)
                {
                    Tile testTile = Framing.GetTileSafely(i, yTest);
                    if (testTile.BottomSlope)
                    {
                        break;
                    }
                    else if (!testTile.HasTile || testTile.TileType != ModContent.TileType<CrystalGrassTile>())
                    {
                        yTest--;
                        continue;
                    }
                    placeVine = true;
                    break;
                }
                if (placeVine)
                {
                    tileBelow.TileType = Type;
                    tileBelow.HasTile = true;
                    WorldGen.SquareTileFrame(i, j + 1, true);
                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        NetMessage.SendTileSquare(-1, i, j + 1, 1, TileChangeType.None);
                    }
                }
            }
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0f;
            g = 0.050f;
            b = 0.200f;
        }

        public override GlowVineData GetGlowVineData(int x, int y)
        {
            return new GlowVineData(
                glowTexture.Value,
                Color.White * MathHelper.Lerp(0.1f, 0.7f, (float)Math.Pow(Math.Sin(NoiseHelper.GetDynamicNoise(new Vector2(x * 0.02f, y * 0.02f), Main.GlobalTimeWrappedHourly * 0.25f)), 2)));
        }
    }
}
