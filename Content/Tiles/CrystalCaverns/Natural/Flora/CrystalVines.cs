using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.RGB;
using static Terraria.GameContent.Drawing.TileDrawing;

namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural.Flora
{
    public class CrystalVines : ModTile
    {
        private Asset<Texture2D> glowTexture;

        private int _specialsCount;
        private List<Point> _vineRootsPositions;
        private WindGrid _windGrid;
        private double _vineWindCounter;

        public override void Load()
        {
            On_Main.DoUpdateInWorld += Main_DoUpdateInWorld;

        }

        public override void Unload()
        {
            base.Unload();
        }

        private void Main_DoUpdateInWorld(On_Main.orig_DoUpdateInWorld orig, Main self, Stopwatch sw)
        {
            orig(self, sw);

            Update();
        }

        public override void SetStaticDefaults()
        {
            Main.tileCut[Type] = true;
            Main.tileBlockLight[Type] = true;
            Main.tileLavaDeath[Type] = true;
            Main.tileNoFail[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLighted[Type] = false;
            TileID.Sets.IsVine[Type] = true;
            TileID.Sets.VineThreads[Type] = true;

            _specialsCount = 0;
            _vineRootsPositions = [];
            _windGrid = new WindGrid();

            AddMapEntry(new Color(100, 125, 255));

            HitSound = SoundID.Grass;
            DustType = DustID.Skyware;

            glowTexture = (Main.dedServ ? null : ModContent.Request<Texture2D>(Texture));
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Tile tile = Framing.GetTileSafely(i, j + 1);
            if (tile.HasTile && tile.TileType == Type)
            {
                WorldGen.KillTile(i, j + 1);
            }
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

        public override bool PreDraw(int i, int j, SpriteBatch spriteBatch)
        {
            //_specialsCount = 0;

            int y = j;
            for (int num = j - 1; num > 0; num--)
            {
                Tile tile = Main.tile[i, num];
                if (WorldGen.BottomEdgeCanBeAttachedTo(i, num) || !tile.HasTile)
                {
                    y = num + 1;
                    break;
                }
            }

            Point item = new Point(i, y);
            if (!_vineRootsPositions.Contains(item))
            {
                _specialsCount++;
                _vineRootsPositions.Add(item);
            }

            return false;
        }

        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            DrawVines();
        }

        private void Update()
        { 
            double num = Math.Abs(Main.WindForVisuals);
            num = Utils.GetLerpValue(0.08f, 1.2f, (float)num, clamped: true);
            _vineWindCounter += 1.0 / 120.0 + 1.0 / 120.0 * num * 0.4000000059604645;
            EnsureWindGridSize();
            _windGrid.Update();
        }

        private void DrawVines()
        {
            Vector2 unscaledPosition = Main.Camera.UnscaledPosition;
            Vector2 zero = Vector2.Zero;
            for (int i = 0; i < _specialsCount; i++)
            {
                Point point = _vineRootsPositions[i];
                int x = point.X;
                int y = point.Y;
                DrawVineStrip(unscaledPosition, zero, x, y);
            }
        }

        private void DrawVineStrip(Vector2 screenPosition, Vector2 offSet, int x, int startY)
        {
            int num = 0;
            int num2 = 0;
            Vector2 vector = new Vector2(x * 16 + 8, startY * 16 - 2);
            float amount = Math.Abs(Main.WindForVisuals) / 1.2f;
            amount = MathHelper.Lerp(0.2f, 1f, amount);
            float num3 = -0.08f * amount;
            float windCycle = GetWindCycle(x, startY, _vineWindCounter);
            float num4 = 0f;
            float num5 = 0f;
            for (int i = startY; i < Main.maxTilesY - 10; i++)
            {
                Tile tile = Main.tile[x, i];
                if (tile == null)
                    continue;

                ushort type = tile.TileType;
                if (!tile.HasTile || !TileID.Sets.VineThreads[type])
                    break;

                num++;
                if (num2 >= 5)
                    num3 += 0.0075f * amount;

                if (num2 >= 2)
                    num3 += 0.0025f;

                if (Main.remixWorld)
                {
                    if (WallID.Sets.AllowsWind[tile.WallType] && (double)i > Main.worldSurface)
                        num2++;
                }
                else if (WallID.Sets.AllowsWind[tile.WallType] && (double)i < Main.worldSurface)
                {
                    num2++;
                }

                float windGridPush = GetWindGridPush(x, i, 20, 0.01f);
                num4 = ((windGridPush != 0f || num5 == 0f) ? (num4 - windGridPush) : (num4 * -0.78f));
                num5 = windGridPush;
                short tileFrameX = tile.TileFrameX;
                short tileFrameY = tile.TileFrameY;
                Color color = Lighting.GetColor(x, i);
                GetTileDrawData(x, i, tile, type, ref tileFrameX, ref tileFrameY, out var tileWidth, out var tileHeight, out var tileTop, out var halfBrickHeight, out var addFrX, out var addFrY, out var tileSpriteEffect, out var glowTexture, out var glowSourceRect, out var glowColor);
                Vector2 position = new Vector2(-(int)screenPosition.X, -(int)screenPosition.Y) + offSet + vector + new Vector2(12 * 16, 12 * 16); // that last vector is because the vines were drawing 12 tiles off in each direction for whatever reason. Bandaid fix but it works
                if (tile.IsTileFullbright)
                    color = Color.White;

                float num6 = (float)num2 * num3 * windCycle + num4;
                if (!Main.dedServ && Main.LocalPlayer.biomeSight)
                {
                    Color sightColor = Color.White;
                    if (Main.IsTileBiomeSightable(x, i, ref sightColor))
                    {
                        if (color.R < sightColor.R)
                            color.R = sightColor.R;

                        if (color.G < sightColor.G)
                            color.G = sightColor.G;

                        if (color.B < sightColor.B)
                            color.B = sightColor.B;

                        if ((!Main.gamePaused && Main.instance.IsActive) && Main.rand.Next(480) == 0)
                        {
                            Color newColor = sightColor;
                            int num7 = Dust.NewDust(new Vector2(x * 16, i * 16), 16, 16, DustID.RainbowMk2, 0f, 0f, 150, newColor, 0.3f);
                            Main.dust[num7].noGravity = true;
                            Main.dust[num7].fadeIn = 1f;
                            Main.dust[num7].velocity *= 0.1f;
                            Main.dust[num7].noLightEmittence = true;
                        }
                    }
                }

                //Texture2D tileDrawTexture = ;
                //if (tileDrawTexture == null)
                //    break;

                if (IsVisible(tile))
                {
                    //Main.spriteBatch.Draw(tileDrawTexture, position, new Rectangle(tileFrameX + addFrX, tileFrameY + addFrY, tileWidth, tileHeight - halfBrickHeight), color, num6, new Vector2(tileWidth / 2, halfBrickHeight - tileTop), 1f, tileSpriteEffect, 0f);
                    if (glowTexture != null)
                        Main.spriteBatch.Draw(glowTexture, position, glowSourceRect, glowColor, num6, new Vector2(tileWidth / 2, halfBrickHeight - tileTop), 1f, tileSpriteEffect, 0f);
                }

                vector += (num6 + (float)Math.PI / 2f).ToRotationVector2() * 16f;
            }
        }

        private float GetWindCycle(int x, int y, double windCounter)
        {
            if (!Main.SettingsEnabled_TilesSwayInWind)
                return 0f;

            float num = (float)x * 0.5f + (float)(y / 100) * 0.5f;
            float num2 = (float)Math.Cos(windCounter * 6.2831854820251465 + (double)num) * 0.5f;
            if (Main.remixWorld)
            {
                if (!((double)y > Main.worldSurface))
                    return 0f;

                num2 += Main.WindForVisuals;
            }
            else
            {
                if (!((double)y < Main.worldSurface))
                    return 0f;

                num2 += Main.WindForVisuals;
            }

            float lerpValue = Utils.GetLerpValue(0.08f, 0.18f, Math.Abs(Main.WindForVisuals), clamped: true);
            return num2 * lerpValue;
        }

        private float GetWindGridPush(int i, int j, int pushAnimationTimeTotal, float pushForcePerFrame)
        {
            _windGrid.GetWindTime(i, j, pushAnimationTimeTotal, out var windTimeLeft, out var directionX, out var _);
            if (windTimeLeft >= pushAnimationTimeTotal / 2)
                return (float)(pushAnimationTimeTotal - windTimeLeft) * pushForcePerFrame * (float)directionX;

            return (float)windTimeLeft * pushForcePerFrame * (float)directionX;
        }

        private void EnsureWindGridSize()
        {
            Vector2 unscaledPosition = Main.Camera.UnscaledPosition;
            Vector2 offSet = new Vector2(Main.offScreenRange, Main.offScreenRange);
            if (Main.drawToScreen)
                offSet = Vector2.Zero;

            GetScreenDrawArea(unscaledPosition, offSet, out var firstTileX, out var lastTileX, out var firstTileY, out var lastTileY);
            _windGrid.SetSize(lastTileX - firstTileX, lastTileY - firstTileY);
        }

        private void GetScreenDrawArea(Vector2 screenPosition, Vector2 offSet, out int firstTileX, out int lastTileX, out int firstTileY, out int lastTileY)
        {
            firstTileX = (int)((screenPosition.X - offSet.X) / 16f - 1f);
            lastTileX = (int)((screenPosition.X + (float)Main.screenWidth + offSet.X) / 16f) + 2;
            firstTileY = (int)((screenPosition.Y - offSet.Y) / 16f - 1f);
            lastTileY = (int)((screenPosition.Y + (float)Main.screenHeight + offSet.Y) / 16f) + 5;
            if (firstTileX < 4)
                firstTileX = 4;

            if (lastTileX > Main.maxTilesX - 4)
                lastTileX = Main.maxTilesX - 4;

            if (firstTileY < 4)
                firstTileY = 4;

            if (lastTileY > Main.maxTilesY - 4)
                lastTileY = Main.maxTilesY - 4;

            if (Main.sectionManager.AnyUnfinishedSections)
            {
                TimeLogger.DetailedDrawReset();
                WorldGen.SectionTileFrameWithCheck(firstTileX, firstTileY, lastTileX, lastTileY);
                TimeLogger.DetailedDrawTime(5);
            }

            if (Main.sectionManager.AnyNeedRefresh)
                WorldGen.RefreshSections(firstTileX, firstTileY, lastTileX, lastTileY);
        }

        private void GetTileDrawData(int x, int y, Tile tileCache, ushort typeCache, ref short tileFrameX, ref short tileFrameY, out int tileWidth, out int tileHeight, out int tileTop, out int halfBrickHeight, out int addFrX, out int addFrY, out SpriteEffects tileSpriteEffect, out Texture2D glowTexture, out Rectangle glowSourceRect, out Color glowColor)
        {
            //tileTop = 0
            tileWidth = 16;
            tileHeight = 16;
            halfBrickHeight = 0;
            addFrY = Main.tileFrame[typeCache] * 38;
            addFrX = 0;
            tileSpriteEffect = SpriteEffects.None;
            //glowTexture = null;
            //glowSourceRect = Rectangle.Empty;
            //glowColor = Color.Transparent;
            Color color = Lighting.GetColor(x, y);

            if (x % 2 == 0)
                tileSpriteEffect = SpriteEffects.FlipHorizontally;

            if (tileCache.IsHalfBlock)
                halfBrickHeight = 8;

            tileTop = -2;
            glowTexture = this.glowTexture.Value;
            glowSourceRect = new Rectangle(tileFrameX + addFrX, tileFrameY + addFrY, tileWidth, tileHeight - halfBrickHeight);
            glowColor = Color.White
                * MathHelper.Lerp(0.1f, 0.7f, (float)Math.Pow(Math.Sin(NoiseHelper.GetDynamicNoise(new Vector2(x * 0.02f, y * 0.02f), Main.GlobalTimeWrappedHourly * 0.25f)), 2));

        }
    }
}
