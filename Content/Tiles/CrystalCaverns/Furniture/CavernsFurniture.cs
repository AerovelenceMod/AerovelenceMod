using AerovelenceMod.Common.Utilities;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ObjectInteractions;
using Terraria.GameContent;
using Microsoft.Xna.Framework;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Glimmerwood;
using Terraria.Localization;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Furniture
{
    //Torch
    #region Torch
    public class CrystalTorchTile : ModTile
    {
        private Asset<Texture2D> flameTexture;
        public override void SetStaticDefaults()
        {
            flameTexture = ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/CrystalTorchTile_Flame");
            CommonTileHelper.SetupTorch(this, new Color(123, 123, 123), ModContent.ItemType<CrystalTorchItem>(), DustID.BlueCrystalShard, true, true, false);
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, DustID.GemSapphire, 0f, 0f, 1, new Color(190, 255, 60), 1f);
            return false;
        }

        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<CrystalTorchItem>();
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            Tile tile = Main.tile[i, j];

            if (tile.TileFrameX < 66)
            {
                r = 0f;
                g = b = 0.9f;
            }
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY)
        {
            offsetY = 0;
            if (WorldGen.SolidTile(i, j - 1))
            {
                offsetY = 2;
                if (WorldGen.SolidTile(i - 1, j + 1) || WorldGen.SolidTile(i + 1, j + 1))
                    offsetY = 4;
            }
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => CommonTileHelper.HandleFlameDraw(Main.tile[i, j], i, j, spriteBatch, flameTexture);

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (Main.tile[i, j].TileFrameX < 66)
                CommonTileHelper.HandleFlameDust(Main.rand.NextBool() ? 61 : 64, 5, i, j);
        }

        public override bool RightClick(int i, int j)
        {
            Tile tile = Main.tile[i, j];
            if (tile != null && tile.HasTile)
            {
                WorldGen.KillTile(i, j);  // Simplified version, no need for the extra parameters
                if (!tile.HasTile && Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.TileManipulation, -1, -1, null, 0, i, j);
                }
                return true;
            }
            return false;
        }
    }

    public class CrystalTorchItem : ModItem
    {
        public override void SetStaticDefaults()
        {
            Item.ResearchUnlockCount = 100;
            ItemID.Sets.Torches[Item.type] = true;
            ItemID.Sets.SingleUseInGamepad[Type] = true;
            ItemID.Sets.ShimmerTransformToItem[Type] = ItemID.ShimmerTorch;
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 18;
            Item.maxStack = 9999;
            Item.holdStyle = 1;
            Item.noWet = true;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.useAnimation = 15;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.consumable = true;
            Item.createTile = ModContent.TileType<CrystalTorchTile>();
            Item.flame = true;
            Item.value = 500;
        }

        public override void HoldItem(Player player)
        {
            bool killTorch = Collision.DrownCollision(player.position, player.width, player.height, player.gravDir) || Item.wet;
            Vector2 position = player.RotatedRelativePoint(new Vector2(player.itemLocation.X + 12f * player.direction + player.velocity.X, player.itemLocation.Y - 14f + player.velocity.Y), true);
            if (!killTorch)
                Lighting.AddLight(position, 0.9f, 1.2f, 0.3f);
        }

        public override void PostUpdate()
        {
            if (!Item.wet)
                Lighting.AddLight((int)((Item.position.X + Item.width / 2) / 16f), (int)((Item.position.Y + Item.height / 2) / 16f), 0.5f, 0.75f, 1.2f);
        }

        public override void AddRecipes()
        {
            CreateRecipe(3).
            AddIngredient(ItemID.Torch, 3).
            AddIngredient<CavernCrystalItem>().
            Register();
        }
    }
    #endregion

    //Campfire
    #region Campfire
	public class CrystalCampfireTile : ModTile
	{
		private Asset<Texture2D> flameTexture;

		public override void SetStaticDefaults() {
			// Properties
			Main.tileLighted[Type] = true;
			Main.tileFrameImportant[Type] = true;
			Main.tileWaterDeath[Type] = true;
			Main.tileLavaDeath[Type] = true;
			TileID.Sets.HasOutlines[Type] = true;
			TileID.Sets.InteractibleByNPCs[Type] = true;
			TileID.Sets.Campfire[Type] = true;

			DustType = -1; // No dust when mined.
			AdjTiles = [TileID.Campfire];

			// Placement
			TileObjectData.newTile.CopyFrom(TileObjectData.GetTileData(TileID.Campfire, 0));
			/*  This is what is copied from the Campfire tile
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x2);
			TileObjectData.newTile.StyleWrapLimit = 16;
			TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
			TileObjectData.newTile.LavaPlacement = LiquidPlacement.NotAllowed;
			TileObjectData.newTile.WaterDeath = true;
			TileObjectData.newTile.LavaDeath = true;
			TileObjectData.newTile.DrawYOffset = 2;
			*/
			TileObjectData.newTile.StyleLineSkip = 9; // This needs to be added to work for modded tiles.
			TileObjectData.addTile(Type);

			// Etc
			AddMapEntry(new Color(254, 121, 2), Language.GetText("ItemName.Campfire"));

			// Assets
			flameTexture = ModContent.Request<Texture2D>(Texture + "_Flame");
		}

		public override void NearbyEffects(int i, int j, bool closer) {
			// HasCampfire is a gameplay effect, so we don't run the code if closer is true.
			if (closer) {
				return;
			}

			if (Main.tile[i, j].TileFrameY < 36) {
				Main.SceneMetrics.HasCampfire = true;
			}
		}

		public override void MouseOver(int i, int j) {
			Player player = Main.LocalPlayer;
			player.noThrow = 2;
			player.cursorItemIconEnabled = true;

			int style = TileObjectData.GetTileStyle(Main.tile[i, j]);
			player.cursorItemIconID = TileLoader.GetItemDropFromTypeAndStyle(Type, style);
		}

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) {
			return true;
		}

		public override bool RightClick(int i, int j) {
			SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16, j * 16));
			ToggleTile(i, j);
			return true;
		}

		public override void HitWire(int i, int j) {
			ToggleTile(i, j);
		}

		// ToggleTile is a method that contains code shared by HitWire and RightClick, since they both toggle the state of the tile.
		// Note that TileFrameY doesn't necessarily match up with the image that is drawn, AnimateTile and AnimateIndividualTile contribute to the drawing decisions.
		public void ToggleTile(int i, int j) {
			Tile tile = Main.tile[i, j];
			int topX = i - tile.TileFrameX % 54 / 18;
			int topY = j - tile.TileFrameY % 36 / 18;

			short frameAdjustment = (short)(tile.TileFrameY >= 36 ? -36 : 36);

			for (int x = topX; x < topX + 3; x++) {
				for (int y = topY; y < topY + 2; y++) {
					Main.tile[x, y].TileFrameY += frameAdjustment;

					if (Wiring.running) {
						Wiring.SkipWire(x, y);
					}
				}
			}

			if (Main.netMode != NetmodeID.SinglePlayer) {
				NetMessage.SendTileSquare(-1, topX, topY, 3, 2);
			}
		}

		public override void AnimateTile(ref int frame, ref int frameCounter) {
			if (++frameCounter >= 4) {
				frameCounter = 0;
				// We animate through the 1st 8 frames. The 9th frame is manually drawn if in the "off" state so it is not included in the animation logic here.
				frame = ++frame % 8;
			}
		}
		public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset) {
			var tile = Main.tile[i, j];
			if (tile.TileFrameY < 36) {
				frameYOffset = Main.tileFrame[type] * 36;
			}
			else {
				// When in the "off" state, TileFrameY of the top tile is 36.
				// Since we want to draw the 9th animation frame when "off", we need to offset the TileFrameY value by 252. (Because 8 * 36 == 288 and 36 + 252 == 288)
				frameYOffset = 252;
			}
		}

		public override void EmitParticles(int i, int j, Tile tileCache, short tileFrameX, short tileFrameY, Color tileLight, bool visible) {
			// Unlike a typical tile, campfire tiles intentionally still spawn dust even when the tile is invisible. This means we do NOT check visible as other examples do.

			Tile tile = Main.tile[i, j];
			// Only emit dust from the top tiles, and only if toggled on. This logic limits dust spawning under different conditions.
			if (tile.TileFrameY == 0 && Main.rand.NextBool(3)) {
				Dust dust = Dust.NewDustDirect(new Vector2(i * 16 + 2, j * 16 - 4), 4, 8, DustID.Smoke, 0f, 0f, 100);
				if (tile.TileFrameX == 0)
					dust.position.X += Main.rand.Next(8);

				if (tile.TileFrameX == 36)
					dust.position.X -= Main.rand.Next(8);

				dust.alpha += Main.rand.Next(100);
				dust.velocity *= 0.2f;
				dust.velocity.Y -= 0.5f + Main.rand.Next(10) * 0.1f;
				dust.fadeIn = 0.5f + Main.rand.Next(10) * 0.1f;
			}
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			Tile tile = Main.tile[i, j];
			if (tile.TileFrameY < 36) {
				float pulse = Main.rand.Next(28, 42) * 0.005f;
				pulse += (270 - Main.mouseTextColor) / 700f;
				r = 0.1f + pulse;
				g = 0.9f + pulse;
				b = 0.3f + pulse;
			}
		}

		public override void PostDraw(int i, int j, SpriteBatch spriteBatch) {
			var tile = Main.tile[i, j];

			if (!TileDrawing.IsVisible(tile)) {
				return;
			}

			if (tile.TileFrameY < 36) {
				Color color = new Color(255, 255, 255, 0);

				Vector2 zero = Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange);

				int width = 16;
				int offsetY = 0;
				int height = 16;
				short frameX = tile.TileFrameX;
				short frameY = tile.TileFrameY;
				int addFrX = 0;
				int addFrY = 0;

				TileLoader.SetDrawPositions(i, j, ref width, ref offsetY, ref height, ref frameX, ref frameY); // calculates the draw offsets
				TileLoader.SetAnimationFrame(Type, i, j, ref addFrX, ref addFrY); // calculates the animation offsets

				Rectangle drawRectangle = new Rectangle(tile.TileFrameX, tile.TileFrameY + addFrY, 16, 16);

				// The flame is manually drawn separate from the tile texture so that it can be drawn at full brightness.
				spriteBatch.Draw(flameTexture.Value, new Vector2(i * 16 - (int)Main.screenPosition.X, j * 16 - (int)Main.screenPosition.Y + offsetY) + zero, drawRectangle, color, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
			}
		}
	}

    public class CrystalCampfireItem : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<CrystalCampfireTile>(), 0);
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddRecipeGroup(RecipeGroupID.Wood, 10)
				.AddIngredient<CrystalTorchItem>(5)
				.Register();
		}
	}
    #endregion

    //Platform
    #region Platform
    [LegacyName("GlimmerwoodPlatform")]
    public class GlimmerwoodPlatformTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupPlatform(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodPlatformItem>(), DustID.BlueCrystalShard, false, false);
        public override void PostSetDefaults() => Main.tileNoSunLight[Type] = false;
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
    }

    public class GlimmerwoodPlatformItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodPlatformTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Candle
    #region Workbench
    public class GlimmerwoodWorkbenchTile : ModTile { public override void SetStaticDefaults() => CommonTileHelper.SetupWorkbench(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodWorkbenchItem>(), DustID.BlueCrystalShard, true, true, true); }

    public class GlimmerwoodWorkbenchItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 32, 16, 150, ModContent.TileType<GlimmerwoodWorkbenchTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 10).Register();
    }
    #endregion

    //Candle
    #region Candle
    public class GlimmerwoodCandleTile : ModTile
    {
        private bool isOn = true;
        public override void SetStaticDefaults() => CommonTileHelper.SetupCandle(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodCandleItem>(), DustID.BlueCrystalShard, true, true, true);

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (isOn)
            {
                r = 0f;
                g = 0.75f;
                b = 1f;
            }
            else
                r = g = b = 0f;
        }
        public override void HitWire(int i, int j) => CommonTileHelper.HandleHitWire(i, j, tileWidth: 3, tileHeight: 3);

        public override bool RightClick(int i, int j)
        {
            isOn = !isOn;
            return true;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => CommonTileHelper.HandlePostDraw(ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/GlimmerwoodCandleTile_Flame"), i, j, spriteBatch, isOn, flameWidth: 54, flameHeight: 54, frameSize: 54);

        public class GlimmerwoodCandleItem : ModItem
        {
            public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodCandleTile>());
            public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
        }
    }

    #endregion

    //Lantern
    #region Lantern
    public class GlimmerwoodLanternTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupLantern(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodLanternItem>(), DustID.BlueCrystalShard, true, true, true);

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0f;
            g = 0.75f;
            b = 1f;
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => CommonTileHelper.PlatformHangOffset(i, j, ref offsetY);
        public override void HitWire(int i, int j) => CommonTileHelper.HandleHitWire(i, j, tileWidth: 1, tileHeight: 2);
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => CommonTileHelper.HandlePostDraw(ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/GlimmerwoodLanternTile_Flame"), i, j, spriteBatch, true, flameWidth: 18, flameHeight: 32, frameSize: 32);

        public class GlimmerwoodLanternItem : ModItem
        {
            public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 10, 20, 150, ModContent.TileType<GlimmerwoodLanternTile>());
            public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 6).AddTile(TileID.WorkBenches).Register();
        }
    }
    #endregion

    //Lamp
    #region Lamp
    public class GlimmerwoodLampTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupLamp(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodLampItem>(), DustID.BlueCrystalShard, true, true, true);

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            r = 0f;
            g = 0.75f;
            b = 1f;
        }

        public override void HitWire(int i, int j) => CommonTileHelper.HandleHitWire(i, j, tileWidth: 1, tileHeight: 3);

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => CommonTileHelper.HandlePostDraw(ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/GlimmerwoodLampTile_Flame"), i, j, spriteBatch, true, flameWidth: 18, flameHeight: 48, frameSize: 48);

        public class GlimmerwoodLampItem : ModItem
        {
            public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 10, 26, 150, ModContent.TileType<GlimmerwoodLampTile>());

            public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 10).AddTile(TileID.WorkBenches).Register();
        }
    }
    #endregion

    //Candelabra
    #region Candelabra
    public class GlimmerwoodCandelabraTile : ModTile
    {
        private bool isOn = true;

        public override void SetStaticDefaults() => CommonTileHelper.SetupCandelabra(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodCandelabraItem>(), DustID.BlueCrystalShard, true, true, true);

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (isOn)
            {
                r = 0f;
                g = 0.75f;
                b = 1f;
            }
            else
                r = g = b = 0f;
        }

        public override void HitWire(int i, int j) => CommonTileHelper.HandleHitWire(i, j, tileWidth: 2, tileHeight: 3);

        public override bool RightClick(int i, int j)
        {
            isOn = !isOn;
            return true;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch) => CommonTileHelper.HandlePostDraw(ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/GlimmerwoodCandelabraTile_Flame"), i, j, spriteBatch, isOn, flameWidth: 36, flameHeight: 48, frameSize: 48);

        public class GlimmerwoodCandelabraItem : ModItem
        {
            public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 16, 32, 150, ModContent.TileType<GlimmerwoodCandelabraTile>());

            public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 15).AddTile(TileID.WorkBenches).Register();
        }
    }
    #endregion

    //Chandelier
    #region Chandelier
    public class GlimmerwoodChandelierTile : ModTile
    {
        private Asset<Texture2D> flameTexture;
        public override void SetStaticDefaults()
        {
            flameTexture = ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/CrystalCaverns/Furniture/GlimmerwoodChandelierTile_Flame");
            CommonTileHelper.SetupChandelier(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodChandelierItem>(), DustID.BlueCrystalShard, true, true, false);
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
            if (Main.tile[i, j].TileFrameX < 18)
            {
                r = 0f;
                g = 0.75f;
                b = 1f;
            }
            else
                r = g = b = 0f;
        }
        public override void HitWire(int i, int j) => CommonTileHelper.HandleHitWire(i, j, tileWidth: 3, tileHeight: 3, isToilet: true);
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            bool isOn = tile.TileFrameX < 54;
            CommonTileHelper.HandlePostDraw(flameTexture, i, j, spriteBatch,isOn, flameWidth: 54, flameHeight: 54, frameSize: 54);
        }
    }

    public class GlimmerwoodChandelierItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodChandelierTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Chair
    #region Chair
    public class GlimmerwoodChairTile : ModTile
    {
        public const int NextStyleHeight = 40;
        public override void SetStaticDefaults() => CommonTileHelper.SetupChair(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodChairItem>(), DustID.BlueCrystalShard, true, true, true);
        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info) => CommonTileHelper.ModifySittingTargetInfo(i, j, ref info, NextStyleHeight);
        public override bool RightClick(int i, int j) { CommonTileHelper.HandleChairRightClick(this, i, j); return true; }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;

            if (!player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance)) return;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<GlimmerwoodChairItem>();
            if (Main.tile[i, j].TileFrameX / 18 < 1)
                player.cursorItemIconReversed = true;
        }
    }

    public class GlimmerwoodChairItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodChairTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Stool
    #region Stool
    public class GlimmerwoodStoolTile : ModTile
    {
        public const int NextStyleHeight = 40;
        public override void SetStaticDefaults() => CommonTileHelper.SetupChair(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodStoolItem>(), DustID.BlueCrystalShard, true, true, true);
        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info) => CommonTileHelper.ModifySittingTargetInfo(i, j, ref info, NextStyleHeight);
        public override bool RightClick(int i, int j) { CommonTileHelper.HandleChairRightClick(this, i, j); return true; }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            if (!player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance)) return;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<GlimmerwoodStoolItem>();
            if (Main.tile[i, j].TileFrameX / 18 < 1)
                player.cursorItemIconReversed = true;
        }
    }

    public class GlimmerwoodStoolItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodStoolTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Toilet
    #region Toilet
    public class GlimmerwoodToiletTile : ModTile
    {
        public const int NextStyleHeight = 40;
        public override void SetStaticDefaults() => CommonTileHelper.SetupToilet(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodToiletItem>(), DustID.BlueCrystalShard, true, true, true);
        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info) => CommonTileHelper.ModifySittingTargetInfo(i, j, ref info, NextStyleHeight);
        public override bool RightClick(int i, int j) { CommonTileHelper.HandleChairRightClick(this, i, j); return true; }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;

            if (!player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance)) return;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<GlimmerwoodToiletItem>();
            if (Main.tile[i, j].TileFrameX / 18 < 1)
                player.cursorItemIconReversed = true;
        }
    }

    public class GlimmerwoodToiletItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodToiletTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Sofa
    #region Sofa
    public class GlimmerwoodSofaTile : ModTile
    {
        public const int NextStyleHeight = 40;
        public override void SetStaticDefaults() => CommonTileHelper.SetupSofa(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodSofaItem>(), DustID.BlueCrystalShard, true, true, true, false);
        public override void ModifySittingTargetInfo(int i, int j, ref TileRestingInfo info) => CommonTileHelper.ModifySittingTargetInfo(i, j, ref info, NextStyleHeight);
        public override bool RightClick(int i, int j) { CommonTileHelper.HandleChairRightClick(this, i, j); return true; }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;

            if (!player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance)) return;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<GlimmerwoodChairItem>();
            if (Main.tile[i, j].TileFrameX / 18 < 1)
                player.cursorItemIconReversed = true;
        }
    }

    public class GlimmerwoodSofaItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodSofaTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Chest
    #region Chests
    public class GlimmerwoodChestTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupChest(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodChestItem>(), DustID.BlueCrystalShard,false);
        public override bool RightClick(int i, int j) { return CommonTileHelper.HandleRightClick(this, i, j, Main.LocalPlayer, ItemID.GoldenKey); }
        public override void MouseOver(int i, int j) => CommonTileHelper.HandleMouseOver(this, i, j, ModContent.ItemType<GlimmerwoodChestItem>(), ItemID.GoldenKey);
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
    }

    public class GlimmerwoodChestItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodChestTile>());
        public override void AddRecipes() { CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register(); }
    }

    public class CavernChestTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupChest(this, new Color(123, 123, 123), ModContent.ItemType<CavernChestItem>(), DustID.BlueCrystalShard, false);
        public override bool RightClick(int i, int j) { return CommonTileHelper.HandleRightClick(this, i, j, Main.LocalPlayer, ItemID.GoldenKey); }
        public override void MouseOver(int i, int j) => CommonTileHelper.HandleMouseOver(this, i, j, ModContent.ItemType<CavernChestItem>(), ItemID.GoldenKey);
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
    }

    public class CavernChestItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<CavernChestTile>());
        public override void AddRecipes() { CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register(); }
    }

    public class CitadelChestTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupChest(this, new Color(123, 123, 123), ModContent.ItemType<CitadelChestItem>(), DustID.BlueCrystalShard, false);
        public override bool RightClick(int i, int j) { return CommonTileHelper.HandleRightClick(this, i, j, Main.LocalPlayer, ModContent.ItemType<CitadelChestKey>()); }
        public override void MouseOver(int i, int j) => CommonTileHelper.HandleMouseOver(this, i, j, ModContent.ItemType<CitadelChestItem>(), ModContent.ItemType<CitadelChestKey>());
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
    }

    public class CitadelChestItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<CitadelChestTile>());
        public override void AddRecipes() { CreateRecipe().AddIngredient(ModContent.ItemType<CitadelChestItem>(), 8).AddTile(TileID.WorkBenches).Register(); }
    }

    public class CitadelChestKey : ModItem
    {
        public override void SetDefaults() => Item.CloneDefaults(ItemID.GoldenKey);
    }
    #endregion

    //Dresser
    #region Dresser
    public class GlimmerwoodDresserTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupDresser(this, new Color(200, 200, 200), ModContent.ItemType<GlimmerwoodDresserItem>(), DustID.BlueCrystalShard, true, true, true);
        public override LocalizedText DefaultContainerName(int frameX, int frameY) => CreateMapEntryName();
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
        public override void ModifySmartInteractCoords(ref int width, ref int height, ref int frameWidth, ref int frameHeight, ref int extraY)
        {
            width = 3;
            height = 1;
            extraY = 0;
        }
        public override bool RightClick(int i, int j)
        {
            CommonTileHelper.HandleDresserRightClick(i, j);
            return true;
        }
        public override void MouseOver(int i, int j) => CommonTileHelper.HandleMouseOverNearAndFarSharedLogic(Main.LocalPlayer, i, j, ModContent.ItemType<GlimmerwoodDresserItem>());
        public override void MouseOverFar(int i, int j)
        {
            Player player = Main.LocalPlayer;
            CommonTileHelper.HandleMouseOverNearAndFarSharedLogic(player, i, j, ModContent.ItemType<GlimmerwoodDresserItem>());
            if (player.cursorItemIconText == "")
            {
                player.cursorItemIconEnabled = false;
                player.cursorItemIconID = 0;
            }
        }
        public override void NumDust(int i, int j, bool fail, ref int num) => num = fail ? 1 : 3;
        public override void KillMultiTile(int i, int j, int frameX, int frameY) => Chest.DestroyChest(i, j);
    }

    public class GlimmerwoodDresserItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodDresserTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }

    #endregion

    //Piano
    #region Piano

    public class GlimmerwoodPipeOrganTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupDecorativeMultiTile(this, new Color(123, 123, 123), 3, 2, ModContent.ItemType<GlimmerwoodPipeOrganItem>());
    }

    public class GlimmerwoodPipeOrganItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodPipeOrganTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Clock
    #region Clock
    public class GlimmerwoodClockTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupClock(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodClockItem>(), DustID.BlueCrystalShard, true, true, true);
        public override bool RightClick(int x, int y) { return CommonTileHelper.HandleClockRightClick(x, y); }
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
        public override void NumDust(int i, int j, bool fail, ref int num) { num = fail ? 1 : 3; }
    }

    public class GlimmerwoodClockItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodClockTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddRecipeGroup(RecipeGroupID.IronBar, 6).AddIngredient(ItemID.Glass, 6).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Bed
    #region Bed
    public class GlimmerwoodBedTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupBed(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodBedItem>(), DustID.BlueCrystalShard, true, true, true);
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
        public override void ModifySmartInteractCoords(ref int width, ref int height, ref int frameWidth, ref int frameHeight, ref int extraY)
        {
            width = 4;
            height = 2;
        }

        public override void ModifySleepingTargetInfo(int i, int j, ref TileRestingInfo info) => CommonTileHelper.ModifyBedSleepingTargetInfo(i, j, ref info);

        public override void NumDust(int i, int j, bool fail, ref int num) => num = 1;

        public override bool RightClick(int i, int j) => CommonTileHelper.HandleBedRightClick(i, j, ModContent.ItemType<GlimmerwoodBedItem>());

        public override void MouseOver(int i, int j) => CommonTileHelper.HandleBedMouseOver(i, j, ModContent.ItemType<GlimmerwoodBedItem>());
    }

    public class GlimmerwoodBedItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 32, 22, 150, ModContent.TileType<GlimmerwoodBedTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 15).AddIngredient(ItemID.Silk, 5).AddTile(ModContent.TileType<CrystallineFabricator>()).Register();
    }
    #endregion

    //Door
    #region Door
    public class GlimmerwoodDoorTileOpen : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupOpenDoor(this, ModContent.TileType<GlimmerwoodDoorTileClosed>(), new Color(200, 200, 200), ModContent.ItemType<GlimmerwoodDoorItem>(), DustID.BlueCrystalShard, true, true, false);
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<GlimmerwoodDoorItem>();
        }
    }

    public class GlimmerwoodDoorTileClosed : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupClosedDoor(this, ModContent.TileType<GlimmerwoodDoorTileOpen>(), new Color(200, 200, 200), ModContent.ItemType<GlimmerwoodDoorItem>(), DustID.BlueCrystalShard, true, true, false);
        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<GlimmerwoodDoorItem>();
        }
    }

    public class GlimmerwoodDoorItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodDoorTileClosed>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Sink
    #region Sink
    public class GlimmerwoodSinkTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupDecorativeMultiTile(this, new Color(123, 123, 123), 2, 2, ModContent.ItemType<GlimmerwoodSinkItem>());
    }

    public class GlimmerwoodSinkItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodSinkTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Bookcase
    #region Bookcase
    public class GlimmerwoodBookcaseTile : ModTile { public override void SetStaticDefaults() => CommonTileHelper.SetupBookcase(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodBookcaseItem>(), DustID.BlueCrystalShard, true, true, true); }
    public class GlimmerwoodBookcaseItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 32, 22, 150, ModContent.TileType<GlimmerwoodBookcaseTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 15).AddIngredient(ItemID.Silk, 5).AddTile(ModContent.TileType<CrystallineFabricator>()).Register();
    }
    #endregion

    //Table
    #region Table
    public class GlimmerwoodTableTile : ModTile
    {
        public override void SetStaticDefaults() => CommonTileHelper.SetupTable(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodTableItem>(), DustID.BlueCrystalShard, true, true, true);
    }

    public class GlimmerwoodTableItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodTableTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion

    //Bathtub
    #region Bathtub
    public class GlimmerwoodBathtubTile : ModTile { public override void SetStaticDefaults() => CommonTileHelper.SetupBathtub(this, new Color(123, 123, 123), ModContent.ItemType<GlimmerwoodBathtubItem>(), DustID.BlueCrystalShard, true, true, true); }
    public class GlimmerwoodBathtubItem : ModItem
    {
        public override void SetDefaults() => CommonItemHelper.SetupPlaceableItem(this, 28, 14, 150, ModContent.TileType<GlimmerwoodBathtubTile>());
        public override void AddRecipes() => CreateRecipe().AddIngredient(ModContent.ItemType<GlimmerwoodItem>(), 8).AddTile(TileID.WorkBenches).Register();
    }
    #endregion
}