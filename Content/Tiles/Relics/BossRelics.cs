using System;
using System.Collections.Generic;
using Terraria.Enums;
using Terraria.DataStructures;
using Terraria.Localization;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.Relics
{
    public class CrystalTumblerRelicTile : ModTile
    {
        public const int FrameWidth = 18 * 3;
        public const int FrameHeight = 18 * 4;
        public const int HorizontalFrames = 1;
        public const int VerticalFrames = 1;
        public List<Point> Coordinates = [];

        public override void SetStaticDefaults()
        {
            CommonTileHelper.SetupBossRelic(this, ModContent.ItemType<CrystalTumblerRelicItem>());
            Coordinates = [];
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Point p = new(i, j);
            Coordinates.Remove(p);
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            return false;
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            tileFrameX %= FrameWidth;
            tileFrameY %= FrameHeight * 2;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            // Ensure only the top-left tile registers for special drawing
            if (drawData.tileFrameX % FrameWidth == 0 && drawData.tileFrameY % FrameHeight == 0)
            {
                Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
            }
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            CommonTileHelper.DrawRelics(this, ModContent.Request<Texture2D>(Texture + "_Floating").Value, FrameWidth, FrameHeight, HorizontalFrames, VerticalFrames, i, j, spriteBatch);
        }
    }

    public class CrystalTumblerRelicItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.KingSlimeMasterTrophy);
            Item.createTile = ModContent.TileType<CrystalTumblerRelicTile>();
        }
    }

    public class CyvercryRelicTile : ModTile
    {
        public const int FrameWidth = 18 * 3;
        public const int FrameHeight = 18 * 4;
        public const int HorizontalFrames = 1;
        public const int VerticalFrames = 1;
        public List<Point> Coordinates = [];

        public override void SetStaticDefaults()
        {
            CommonTileHelper.SetupBossRelic(this, ModContent.ItemType<CyvercryRelicItem>());
            Coordinates = [];
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Point p = new(i, j);
            Coordinates.Remove(p);
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            return false;
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            tileFrameX %= FrameWidth;
            tileFrameY %= FrameHeight * 2;
        }

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            // Ensure only the top-left tile registers for special drawing
            if (drawData.tileFrameX % FrameWidth == 0 && drawData.tileFrameY % FrameHeight == 0)
            {
                Main.instance.TilesRenderer.AddSpecialLegacyPoint(i, j);
            }
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            CommonTileHelper.DrawRelics(this, ModContent.Request<Texture2D>(Texture + "_Floating").Value, FrameWidth, FrameHeight, HorizontalFrames, VerticalFrames, i, j, spriteBatch);
        }
    }

    public class CyvercryRelicItem : ModItem
    {
        public override void SetDefaults()
        {
            Item.CloneDefaults(ItemID.KingSlimeMasterTrophy);
            Item.createTile = ModContent.TileType<CyvercryRelicTile>();
        }
    }
}
