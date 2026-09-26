using System;
using System.Collections.Generic;
using AerovelenceMod.Content.Items.Crafting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora;

public enum CrystalCornStage : byte { Planted, Growing, Grown }

public class CrystalCorn : ModTile
{
    private const int FrameWidth = 18;

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileCut[Type] = true;
        Main.tileNoFail[Type] = true;
        Main.tileObsidianKill[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileID.Sets.IgnoredInHouseScore[Type] = true;
        TileID.Sets.IgnoredByGrowingSaplings[Type] = true;
        TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x2);
        TileObjectData.newTile.Height = 3;
        TileObjectData.newTile.Origin = new Point16(0, 2);
        TileObjectData.newTile.CoordinateHeights = [20, 16, 16];
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newTile.DrawYOffset = 0;
        TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.AlternateTile, 1, 0);
        TileObjectData.newTile.AnchorValidTiles = [TileID.Dirt, TileID.Grass, TileID.HallowedGrass, ModContent.TileType<CrystalDirtTile>(), ModContent.TileType<CrystalGrassTile>()];
        TileObjectData.newTile.AnchorAlternateTiles = [TileID.ClayPot, TileID.PlanterBox];
        TileObjectData.newTile.LavaDeath = true;
        TileObjectData.addTile(Type);
        AddMapEntry(new Color(100, 190, 240), Terraria.Localization.Language.GetText("Mods.AerovelenceMod.Items.CrystalCornItem.DisplayName"));
    }

    public override void SetSpriteEffects(int i, int j, ref SpriteEffects spriteEffects)
    {
        if (i % 2 == 1) spriteEffects = SpriteEffects.FlipHorizontally;
    }

    public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
    {
        width = 18;
        offsetY = tileFrameY == 0 ? -4 : 0;
    }

    public override IEnumerable<Item> GetItemDrops(int i, int j)
    {
        if (GetStage(i, j) != CrystalCornStage.Grown)
        {
            yield return new Item(ModContent.ItemType<CrystalCornSeeds>());
            yield break;
        }
        Player player = Main.player[Player.FindClosest(new Vector2(i * 16, j * 16), 16, 16)];
        bool regrowth = player.active && player.HeldItem.type is ItemID.StaffofRegrowth or ItemID.AcornAxe;
        yield return new Item(ModContent.ItemType<CrystalCornItem>(), regrowth ? Main.rand.Next(1, 3) : 1);
        yield return new Item(ModContent.ItemType<CrystalCornSeeds>(), Main.rand.Next(1, regrowth ? 6 : 4));
    }

    public override bool IsTileSpelunkable(int i, int j) => GetStage(i, j) == CrystalCornStage.Grown;

    public override void RandomUpdate(int i, int j)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Main.tile[i, j].TileFrameY != 0 || !WorldGen.InWorld(i, j + 2, 1)) return;
        CrystalCornStage stage = GetStage(i, j);
        if (stage == CrystalCornStage.Grown) return;
        for (int row = 0; row < 3; row++)
            if (!Main.tile[i, j + row].HasTile || Main.tile[i, j + row].TileType != Type) return;
        for (int row = 0; row < 3; row++)
            Main.tile[i, j + row].TileFrameX = (short)(((int)stage + 1) * FrameWidth);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, i, j, 1, 3);
    }

    private static CrystalCornStage GetStage(int i, int j) => (CrystalCornStage)Math.Clamp(Main.tile[i, j].TileFrameX / FrameWidth, 0, 2);
}

public class CrystalCornSeeds : ModItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<CrystalCorn>());
        Item.width = 12;
        Item.height = 14;
        Item.rare = ItemRarityID.Pink;
        Item.value = Item.buyPrice(silver: 5);
    }
}
