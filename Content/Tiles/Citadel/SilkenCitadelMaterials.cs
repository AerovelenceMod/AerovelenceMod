using System;
using AerovelenceMod.Common.Utilities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Tiles.Citadel;

public sealed class SilkenAmberTile : ModTile
{
    public override string Texture => "Terraria/Images/Tiles_" + TileID.Hive;
    public override void SetStaticDefaults()
    {
        this.SimpleFramedTile(ItemID.Silk, SoundID.Dig, DustID.Silk, 0, mergeDirt: true);
        Main.tileBlockLight[Type] = false;
        MineResist = .5f;
        AddMapEntry(new Color(191, 172, 128));
    }
    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        float pulse = .8f + .2f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 1.2 + i * .13);
        r = .36f * pulse; g = .24f * pulse; b = .13f * pulse;
    }
}

public sealed class CitadelBrickWallUnsafe : ModWall
{
    public override string Texture => "AerovelenceMod/Content/Walls/CrystalCaverns/Natural/CitadelBrickWall";
    public override void SetStaticDefaults() => this.SimpleWall(ItemID.None, SoundID.Dig, DustID.Stone, new Color(24, 34, 53));
}

public sealed class SilkenNestWall : ModWall
{
    public override string Texture => "Terraria/Images/Wall_" + WallID.SpiderUnsafe;
    public override void SetStaticDefaults() => this.SimpleWall(ItemID.None, SoundID.Grass, DustID.Silk, new Color(35, 32, 48));
}

public sealed class SilkenVeilWall : ModWall
{
    public override string Texture => "Terraria/Images/Wall_" + WallID.SpiderUnsafe;
    public override void SetStaticDefaults() => this.SimpleWall(ItemID.None, SoundID.Grass, DustID.Silk, new Color(97, 91, 110));
}

public sealed class CitadelWindowWall : ModWall
{
    public override string Texture => "Terraria/Images/Wall_" + WallID.BlueStainedGlass;
    public override void SetStaticDefaults()
    {
        Main.wallLight[Type] = true;
        this.SimpleWall(ItemID.None, SoundID.Shatter, DustID.BlueCrystalShard, new Color(57, 122, 173));
    }
    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) { r = .07f; g = .18f; b = .28f; }
}

public sealed class CitadelRoseWindowWall : ModWall
{
    public override string Texture => "Terraria/Images/Wall_" + WallID.BlueStainedGlass;
    public override void SetStaticDefaults()
    {
        Main.wallLight[Type] = true;
        this.SimpleWall(ItemID.None, SoundID.Shatter, DustID.BlueCrystalShard, new Color(117, 186, 222));
    }
    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) { r = .11f; g = .23f; b = .30f; }
}
