using AerovelenceMod.Common.Systems;
using AerovelenceMod.Common.Systems.Traversal;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ObjectData;

namespace AerovelenceMod.Content.Tiles.Traversal
{
    public sealed class ZiplinePostTile : RopePost
    {
        private Asset<Texture2D> hookTexture;
        private readonly Dictionary<byte, HookPaintTarget> paintedHooks = new();

        public override int Height => 4;
        public override string Texture => "AerovelenceMod/Content/Tiles/Traversal/ZiplinePostTile";

        public override void Load()
        {
            if (Main.dedServ) return;
            hookTexture = ModContent.Request<Texture2D>("AerovelenceMod/Content/Tiles/Traversal/ZiplinePostHook");
            On_TilePaintSystemV2.PrepareAllRequests += PrepareHookPaint;
        }

        public override void Unload()
        {
            if (Main.dedServ) return;
            On_TilePaintSystemV2.PrepareAllRequests -= PrepareHookPaint;
            foreach (HookPaintTarget target in paintedHooks.Values) target.Clear();
            paintedHooks.Clear();
            hookTexture = null;
        }

        private void PrepareHookPaint(On_TilePaintSystemV2.orig_PrepareAllRequests orig, TilePaintSystemV2 self)
        {
            orig(self);
            foreach (HookPaintTarget target in paintedHooks.Values)
                if (!target.IsReady) target.Prepare();
        }

        private sealed class HookPaintTarget(Asset<Texture2D> texture, int tileType, byte paint) : TilePaintSystemV2.ARenderTargetHolder
        {
            public override void Prepare() => PrepareTextureIfNecessary(texture.Value);
            public override void PrepareShader() => PrepareShader(paint, TreePaintSystemData.GetTileSettings(tileType, 0));
        }

        internal static Vector2 HookCenter(Point post, bool ceiling)
            => new(post.X * 16 + 8, post.Y * 16 + (ceiling ? 10 : -42));

        internal static Vector2 ConnectionPoint(Point post, bool ceiling)
            => HookCenter(post, ceiling) + new Vector2(0, ceiling ? 2 : -2);

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            TileID.Sets.CanBeSloped[Type] = true;
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleMultiplier = 2;
            TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
            TileObjectData.newAlternate.Origin = new Point16(0, 0);
            TileObjectData.newAlternate.AnchorBottom = AnchorData.Empty;
            TileObjectData.newAlternate.AnchorTop = new AnchorData(AnchorType.SolidTile, 1, 0);
            TileObjectData.addAlternate(1);
            TileObjectData.addTile(Type);
            RegisterItemDrop(ModContent.ItemType<ZiplinePost>());
            AddMapEntry(new Color(120, 90, 57), this.Localize("Zipline Post"));
        }

        internal static bool IsEndStop(Point point)
            => WorldGen.InWorld(point.X, point.Y) && Main.tile[point.X, point.Y].HasTile
                && Main.tile[point.X, point.Y].TileType == ModContent.TileType<ZiplinePostTile>()
                && Main.tile[point.X, point.Y].TileFrameX >= 36;

        internal static void SetEndStop(Point point, bool stop)
        {
            short frameX = (short)(Main.tile[point.X, point.Y].TileFrameX % 36 + (stop ? 36 : 0));
            for (int row = 0; row < 4; row++) Main.tile[point.X, point.Y - row].TileFrameX = frameX;
        }

        public override bool Slope(int i, int j)
        {
            Hammer(Main.myPlayer, i, j);
            return false;
        }

        internal static void Hammer(int playerId, int i, int j)
        {
            if (playerId < 0 || playerId >= Main.maxPlayers || !WorldGen.InWorld(i, j, 10)) return;
            Player player = Main.player[playerId];
            Tile tile = Main.tile[i, j];
            if (!player.active || player.dead || player.HeldItem.hammer <= 0 || !tile.HasTile
                || tile.TileType != ModContent.TileType<ZiplinePostTile>()
                || !player.IsInTileInteractionRange(i, j, TileReachCheckSettings.Simple)) return;
            Point point = ModContent.GetInstance<ZiplinePostTile>().Bottom(i, j);
            if (!RopeSpanSystem.ValidPost(point, true) || RopeSpanSystem.ConnectionCount(point) > 1) return;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = RopeSpanSystem.NewPacket(11);
                packet.Write((short)i);
                packet.Write((short)j);
                packet.Send();
            }
            else SetEndStop(point, !IsEndStop(point));
            if (!Main.dedServ && Main.netMode != NetmodeID.Server)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, new Vector2(i * 16, j * 16));
            if (Main.netMode == NetmodeID.Server) NetMessage.SendTileSquare(-1, point.X, point.Y - 3, 1, 4);
        }

        public override bool RightClick(int i, int j)
        {
            if (!Main.LocalPlayer.releaseUseTile) return false;
            if (RopeSpanSystem.IsRope(Main.LocalPlayer.HeldItem))
                RopeSpanSystem.ClickPost(Bottom(i, j), true);
            else
                Main.LocalPlayer.GetModPlayer<RopeSpanPlayer>().TryRideAt(Main.SmartCursorIsUsed ? Main.MouseWorld : new Vector2(i * 16 + 8, j * 16 + 8));
            return true;
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
            => Main.SmartCursorIsUsed || RopeSpanSystem.IsRope(Main.LocalPlayer.HeldItem);

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            int row = tile.TileFrameY / 18 % Height;
            bool ceiling = tile.TileFrameX % 36 == 18;
            if (ceiling) row = Height - 1 - row;
            int column = tile.TileFrameX / 36;
            if (row == 1) column = 0;
            return DrawPost(i, j, spriteBatch, row, ceiling, column: column,
                flipHorizontal: tile.TileFrameX >= 36 && StopFacesRight(Bottom(i, j)));
        }

        internal static bool StopFacesRight(Point post)
        {
            RopeSpan span = RopeSpanSystem.AtTile(post);
            return span != null && (span.Left == post ? span.Right : span.Left).X > post.X;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Tile tile = Main.tile[i, j];
            bool ceiling = tile.TileFrameX % 36 == 18;
            if (tile.TileFrameY / 18 % Height == (ceiling ? Height - 1 : 0))
                DrawHook(i, j, spriteBatch, ceiling);
        }

        internal void DrawConnectionHook(Point post, SpriteBatch spriteBatch)
        {
            bool ceiling = RopeSpanSystem.CeilingPost(post);
            DrawHook(post.X, ceiling ? post.Y : post.Y - Height + 1, spriteBatch, ceiling, screenOffset: Vector2.Zero);
        }

        private void DrawHook(int i, int j, SpriteBatch spriteBatch, bool ceiling, bool preview = false, bool validPlacement = true, Vector2? screenOffset = null)
        {
            Tile tile = Main.tile[i, j];
            if (!preview && tile.IsTileInvisible && !Main.ShouldShowInvisibleWalls()) return;
            Texture2D texture = hookTexture.Value;
            if (!preview && tile.TileColor != PaintID.None)
            {
                if (!paintedHooks.TryGetValue(tile.TileColor, out HookPaintTarget target))
                    paintedHooks[tile.TileColor] = target = new HookPaintTarget(hookTexture, Type, tile.TileColor);
                if (target.IsReady) texture = target.Target;
            }
            Point post = new(i, ceiling ? j : j + Height - 1);
            Vector2 offset = screenOffset ?? (Main.drawToScreen ? Vector2.Zero : new Vector2(Main.offScreenRange));
            Color color = new(255, validPlacement ? 255 : 127, validPlacement ? 255 : 127, preview ? 127 : 255);
            spriteBatch.Draw(texture, HookCenter(post, ceiling) - Main.screenPosition + offset, null,
                tile.IsTileFullbright ? color : Lighting.GetColor(i, j).MultiplyRGBA(color), 0, hookTexture.Size() * 0.5f,
                1, ceiling ? SpriteEffects.FlipVertically : SpriteEffects.None, 0);
        }

        public override bool PreDrawPlacementPreview(int i, int j, SpriteBatch spriteBatch, ref Rectangle frame, ref Vector2 position, ref Color color, bool validPlacement, ref SpriteEffects spriteEffects)
        {
            int row = frame.Y / 18 % Height;
            bool ceiling = frame.X == 18;
            if (ceiling) row = Height - 1 - row;
            DrawPost(i, j, spriteBatch, row, ceiling, true, validPlacement);
            if (row == 0) DrawHook(i, j, spriteBatch, ceiling, true, validPlacement);
            return false;
        }
    }

    public sealed class ZiplinePost : TranslatableModItem
    {
        public override string Texture => "AerovelenceMod/Content/Tiles/Traversal/ZiplinePostItem";

        public override void SetStaticDefaults()
        {
            this.AddName(Language.Default, "Zipline Post")
                .AddTooltip(Language.Default, "Right-click two posts with ropes or chains to connect"
                    + "\nConsumes 2 ropes or chains per section"
                    + "\nHammer an end post to change it to a stopper");
        }

        public override void SetDefaults()
        {
            CommonItemHelper.SetupPlaceableItem(this, 16, 32, Item.buyPrice(copper: 20), ModContent.TileType<ZiplinePostTile>());
        }

        public override void AddRecipes() => CreateRecipe()
            .AddIngredient(ItemID.WoodenBeam, 6)
            .AddRecipeGroup(RecipeGroupID.IronBar)
            .AddTile(TileID.Anvils)
            .Register();
    }
}
