using AerovelenceMod.Content.Items.Crafting;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Metadata;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora
{
    public enum CrystalCornStage : byte
    {
        Planted,
        Growing1,
        Growing2,
        Grown
    }

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
			TileID.Sets.ReplaceTileBreakUp[Type] = true;
			TileID.Sets.IgnoredInHouseScore[Type] = true;
			TileID.Sets.IgnoredByGrowingSaplings[Type] = true;
			TileMaterials.SetForTileId(Type, TileMaterials._materialsByName["Plant"]);

            TileObjectData.newTile.CopyFrom(TileObjectData.StyleAlch);
            TileObjectData.newTile.Height = 3;
            TileObjectData.newTile.CoordinateHeights = [16, 16, 16];
            TileObjectData.newTile.AnchorValidTiles =
            [
                TileID.Grass,
                TileID.HallowedGrass,
                ModContent.TileType<CrystalGrassTile>()
            ];
            TileObjectData.newTile.AnchorAlternateTiles =
            [
                TileID.ClayPot,
                TileID.PlanterBox
            ];
            TileObjectData.addTile(Type);
        }

		public override bool CanPlace(int i, int j) {
			Tile tile = Framing.GetTileSafely(i, j); // Safe way of getting a tile instance

			if (tile.HasTile) {
				int tileType = tile.TileType;
				if (tileType == Type) {
					CrystalCornStage stage = GetStage(i, j); // The current stage of the herb

					// Can only place on the same herb again if it's grown already
					return stage == CrystalCornStage.Grown;
				}
				else {
					// Support for vanilla herbs/grasses:
					if (Main.tileCut[tileType] || TileID.Sets.BreakableWhenPlacing[tileType] || tileType == TileID.WaterDrip || tileType == TileID.LavaDrip || tileType == TileID.HoneyDrip || tileType == TileID.SandDrip) {
						bool foliageGrass = tileType == TileID.Plants || tileType == TileID.Plants2;
						bool moddedFoliage = tileType >= TileID.Count && (Main.tileCut[tileType] || TileID.Sets.BreakableWhenPlacing[tileType]);
						bool harvestableVanillaHerb = Main.tileAlch[tileType] && WorldGen.IsHarvestableHerbWithSeed(tileType, tile.TileFrameX / 18);

						if (foliageGrass || moddedFoliage || harvestableVanillaHerb) {
							WorldGen.KillTile(i, j);
							if (!tile.HasTile && Main.netMode == NetmodeID.MultiplayerClient) {
								NetMessage.SendData(MessageID.TileManipulation, number: 0, number2: i, number3: j);
							}

							return true;
						}
					}

					return false;
				}
			}

			return true;
		}

        public override void SetSpriteEffects(int i, int j, ref SpriteEffects spriteEffects)
        {
            if (i % 2 == 1)
                spriteEffects = SpriteEffects.FlipHorizontally;
        }

		public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) {
			offsetY = -2; // This is -1 for tiles using StyleAlch, but vanilla sets to -2 for herbs, which causes a slight visual offset between the placement preview and the placed tile. 
		}

		public override bool CanDrop(int i, int j) {
			CrystalCornStage stage = GetStage(i, j);

			if (stage == CrystalCornStage.Planted) {
				// Do not drop anything when just planted
				return false;
			}
			return true;
		}

        public override IEnumerable<Item> GetItemDrops(int i, int j)
        {
            CrystalCornStage stage = GetStage(i, j);

			Vector2 worldPosition = new Vector2(i, j).ToWorldCoordinates();
			Player nearestPlayer = Main.player[Player.FindClosest(worldPosition, 16, 16)];

			int herbItemType = ModContent.ItemType<CrystalCornItem>();
			int herbItemStack = 1;

			int seedItemType = ModContent.ItemType<CrystalCornSeeds>();
			int seedItemStack = 1;

			if (nearestPlayer.active && (nearestPlayer.HeldItem.type == ItemID.StaffofRegrowth || nearestPlayer.HeldItem.type == ItemID.AcornAxe)) {
				// Increased yields with Staff of Regrowth, even when not fully grown
				herbItemStack = Main.rand.Next(1, 3);
				seedItemStack = Main.rand.Next(1, 6);
			}
			else if (stage == CrystalCornStage.Grown) {
				// Default yields, only when fully grown
				herbItemStack = 1;
				seedItemStack = Main.rand.Next(1, 4);
			}

			if (herbItemType > 0 && herbItemStack > 0) {
				yield return new Item(herbItemType, herbItemStack);
			}

			if (seedItemType > 0 && seedItemStack > 0) {
				yield return new Item(seedItemType, seedItemStack);
			}
        }

		public override bool IsTileSpelunkable(int i, int j) {
			CrystalCornStage stage = GetStage(i, j);

			// Only glow if the herb is grown
			return stage == CrystalCornStage.Grown;
		}

        public override void RandomUpdate(int i, int j)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            CrystalCornStage stage = GetStage(i, j);
            if (stage != CrystalCornStage.Grown)
            {
                tile.TileFrameX += FrameWidth;

                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendTileSquare(-1, i, j, 3, TileChangeType.None);
                }
            }
        }

        private CrystalCornStage GetStage(int i, int j)
        {
            Tile tile = Framing.GetTileSafely(i, j);
            return (CrystalCornStage)(tile.TileFrameX / FrameWidth);
        }
    }

	public class CrystalCornSeeds : ModItem
	{
		public override void SetDefaults()
		{
			Item.autoReuse = true;
			Item.useTurn = true;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.useAnimation = 15;
			Item.rare = ItemRarityID.Pink;
			Item.useTime = 10;
			Item.maxStack = 99;
			Item.consumable = true;
			Item.placeStyle = 0;
			Item.width = 12;
			Item.height = 14;
			Item.value = Item.buyPrice(0, 0, 5, 0);
			Item.createTile = ModContent.TileType<CrystalCorn>();
		}
	}
}