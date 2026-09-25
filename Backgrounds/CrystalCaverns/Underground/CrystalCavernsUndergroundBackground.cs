using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.Graphics;
using Terraria.ModLoader;

namespace AerovelenceMod.Backgrounds.CrystalCaverns.Underground
{
    public class CrystalCavernsConceptBackgroundStyle : ModUndergroundBackgroundStyle
    {
        public override void FillTextureArray(int[] textureSlots)
        {
            int border = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/CrystalCaverns/Underground/CrystalCavernsBlankBorder");
            int fill = BackgroundTextureLoader.GetBackgroundSlot(Mod, "Backgrounds/CrystalCaverns/Underground/CrystalCavernsBlankFill");

            textureSlots[0] = border;
            textureSlots[1] = fill;
            textureSlots[2] = border;
            textureSlots[3] = fill;
        }
    }

    public class CrystalCavernsConceptBackgroundSystem : ModSystem
    {
        private const string BackgroundRoot = "AerovelenceMod/Backgrounds/CrystalCaverns/Underground/";

        private const float FadeInSpeed = 0.05f;
        private const float FadeOutSpeed = 0.025f;

        private const float BackgroundScale = 1.12f;

        private const float Layer1ParallaxX = 0.14f;
        private const float Layer2ParallaxX = 0.11f;
        private const float Layer3ParallaxX = 0.08f;
        private const float Layer4ParallaxX = 0.05f;
        private const float Layer5ParallaxX = 0.025f;

        private const float Layer1ParallaxY = 1.00f;

        private const float Layer2TopParallaxY = 0.42f;
        private const float Layer2BottomParallaxY = 0.92f;

        private const float Layer3TopParallaxY = 0.34f;
        private const float Layer3BottomParallaxY = 0.74f;

        private const float Layer4TopParallaxY = 0.26f;
        private const float Layer4BottomParallaxY = 0.56f;

        private const float Layer5ParallaxY = 0.18f;

        private const float Layer1VerticalOffset = 50f;

        private const float Layer2TopVerticalOffset = -50f;
        private const float Layer2BottomVerticalOffset = -150f;

        private const float Layer3TopVerticalOffset = -50f;
        private const float Layer3BottomVerticalOffset = -150f;

        private const float Layer4TopVerticalOffset = -50f;
        private const float Layer4BottomVerticalOffset = -150f;

        private const float Layer5VerticalOffset = -200f;

        private static bool visualTargetActive;
        private static float visualOpacity;

        private static bool parallaxAnchorValid;
        private static float parallaxAnchorX;

        private static BackgroundLayer[] layers;

        private sealed class BackgroundLayer
        {
            public Asset<Texture2D> Texture;
            public float ParallaxX;
            public float ParallaxY;
            public float VerticalOffset;

            public BackgroundLayer(
                Asset<Texture2D> texture,
                float parallaxX,
                float parallaxY,
                float verticalOffset)
            {
                Texture = texture;
                ParallaxX = parallaxX;
                ParallaxY = parallaxY;
                VerticalOffset = verticalOffset;
            }
        }

        public override void Load()
        {
            if (Main.dedServ)
                return;

            layers = new[]
            {
                new BackgroundLayer(
                    ModContent.Request<Texture2D>(
                        BackgroundRoot + "ConceptCC5",
                        AssetRequestMode.ImmediateLoad
                    ),
                    Layer5ParallaxX,
                    Layer5ParallaxY,
                    Layer5VerticalOffset
                ),

                new BackgroundLayer(
                    ModContent.Request<Texture2D>(
                        BackgroundRoot + "ConceptCC4_Bottom",
                        AssetRequestMode.ImmediateLoad
                    ),
                    Layer4ParallaxX,
                    Layer4BottomParallaxY,
                    Layer4BottomVerticalOffset
                ),

                new BackgroundLayer(
                    ModContent.Request<Texture2D>(
                        BackgroundRoot + "ConceptCC4_Top",
                        AssetRequestMode.ImmediateLoad
                    ),
                    Layer4ParallaxX,
                    Layer4TopParallaxY,
                    Layer4TopVerticalOffset
                ),

                new BackgroundLayer(
                    ModContent.Request<Texture2D>(
                        BackgroundRoot + "ConceptCC3_Bottom",
                        AssetRequestMode.ImmediateLoad
                    ),
                    Layer3ParallaxX,
                    Layer3BottomParallaxY,
                    Layer3BottomVerticalOffset
                ),

                new BackgroundLayer(
                    ModContent.Request<Texture2D>(
                        BackgroundRoot + "ConceptCC3_Top",
                        AssetRequestMode.ImmediateLoad
                    ),
                    Layer3ParallaxX,
                    Layer3TopParallaxY,
                    Layer3TopVerticalOffset
                ),

                new BackgroundLayer(
                    ModContent.Request<Texture2D>(
                        BackgroundRoot + "ConceptCC2_Bottom",
                        AssetRequestMode.ImmediateLoad
                    ),
                    Layer2ParallaxX,
                    Layer2BottomParallaxY,
                    Layer2BottomVerticalOffset
                ),

                new BackgroundLayer(
                    ModContent.Request<Texture2D>(
                        BackgroundRoot + "ConceptCC2_Top",
                        AssetRequestMode.ImmediateLoad
                    ),
                    Layer2ParallaxX,
                    Layer2TopParallaxY,
                    Layer2TopVerticalOffset
                ),

                new BackgroundLayer(
                    ModContent.Request<Texture2D>(
                        BackgroundRoot + "ConceptCC1",
                        AssetRequestMode.ImmediateLoad
                    ),
                    Layer1ParallaxX,
                    Layer1ParallaxY,
                    Layer1VerticalOffset
                )
            };

            On_Main.DrawBackgroundBlackFill += DrawCrystalCavernsBackground;
            IL_TileDrawing.DrawSingleTile += KeepZeroLightTilesVisible;
            IL_WallDrawing.DrawWalls += KeepZeroLightWallsVisible;
        }

        public override void Unload()
        {
            if (!Main.dedServ)
            {
                On_Main.DrawBackgroundBlackFill -= DrawCrystalCavernsBackground;
                IL_TileDrawing.DrawSingleTile -= KeepZeroLightTilesVisible;
                IL_WallDrawing.DrawWalls -= KeepZeroLightWallsVisible;
            }

            layers = null;

            visualTargetActive = false;
            visualOpacity = 0f;

            parallaxAnchorValid = false;
            parallaxAnchorX = 0f;
        }

        public override void OnWorldUnload()
        {
            visualTargetActive = false;
            visualOpacity = 0f;

            parallaxAnchorValid = false;
            parallaxAnchorX = 0f;
        }

        public override void PostUpdateEverything()
        {
            if (Main.gameMenu ||
                Main.dedServ ||
                Main.LocalPlayer == null ||
                !Main.LocalPlayer.active ||
                !Main.BackgroundEnabled)
            {
                SetVisualTarget(false);
                UpdateVisualOpacity();
                return;
            }

            Player player = Main.LocalPlayer;

            if (!player.dead)
            {
                int cavernTiles = ModContent
                    .GetInstance<Content.Biomes.CrystalCavernsTileCount>()
                    .CrystalTiles;

                bool underground =
                    player.ZoneDirtLayerHeight ||
                    player.ZoneRockLayerHeight;

                int requiredTiles =
                    visualTargetActive ? 500 : 1000;

                SetVisualTarget(
                    underground &&
                    cavernTiles >= requiredTiles
                );
            }

            UpdateVisualOpacity();

            if (Active)
                AddCrystalCavernsAmbientLight();
        }

        private static void SetVisualTarget(bool value)
        {
            if (visualTargetActive == value)
                return;

            bool wasCompletelyHidden =
                visualOpacity <= 0f;

            visualTargetActive = value;

            if (value)
            {
                if (!parallaxAnchorValid)
                {
                    parallaxAnchorX =
                        Main.screenPosition.X;

                    parallaxAnchorValid = true;
                }

                if (wasCompletelyHidden &&
                    !Main.dedServ)
                {
                    Main.renderNow = true;
                }
            }
        }

        private static void UpdateVisualOpacity()
        {
            float previousOpacity =
                visualOpacity;

            if (visualTargetActive)
            {
                visualOpacity =
                    MathHelper.Clamp(
                        visualOpacity + FadeInSpeed,
                        0f,
                        1f
                    );
            }
            else
            {
                visualOpacity =
                    MathHelper.Clamp(
                        visualOpacity - FadeOutSpeed,
                        0f,
                        1f
                    );
            }

            if (previousOpacity > 0f &&
                visualOpacity <= 0f)
            {
                visualOpacity = 0f;

                parallaxAnchorValid = false;
                parallaxAnchorX = 0f;

                if (!Main.dedServ)
                    Main.renderNow = true;
            }
        }

        private static bool Active =>
            visualOpacity > 0f &&
            !Main.gameMenu &&
            !Main.dedServ &&
            Main.BackgroundEnabled;

        private static bool IsWalllessOpen(int x, int y)
        {
            if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY)
                return false;

            Tile tile = Framing.GetTileSafely(x, y);

            if (tile.WallType != 0)
                return false;

            if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                return false;

            return true;
        }

        private static void AddCrystalCavernsAmbientLight()
        {
            int startX = (int)(Main.screenPosition.X / 16f) - 4;
            int endX = (int)((Main.screenPosition.X + Main.screenWidth) / 16f) + 4;
            int startY = (int)(Main.screenPosition.Y / 16f) - 4;
            int endY = (int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + 4;

            startX = Utils.Clamp(startX, 1, Main.maxTilesX - 2);
            endX = Utils.Clamp(endX, 1, Main.maxTilesX - 2);
            startY = Utils.Clamp(startY, 1, Main.maxTilesY - 2);
            endY = Utils.Clamp(endY, 1, Main.maxTilesY - 2);

            float strength = visualOpacity;

            float r = 0.24f * strength;
            float g = 0.34f * strength;
            float b = 0.48f * strength;

            for (int x = startX; x <= endX; x += 2)
            {
                for (int y = startY; y <= endY; y += 2)
                {
                    Tile tile = Framing.GetTileSafely(x, y);

                    if (tile.WallType != 0)
                        continue;

                    if (tile.HasTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                        continue;

                    int openNeighbors = 0;

                    if (IsWalllessOpen(x + 1, y))
                        openNeighbors++;
                    if (IsWalllessOpen(x - 1, y))
                        openNeighbors++;
                    if (IsWalllessOpen(x, y + 1))
                        openNeighbors++;
                    if (IsWalllessOpen(x, y - 1))
                        openNeighbors++;

                    if (openNeighbors < 2)
                        continue;

                    Lighting.AddLight(
                        new Vector2(x * 16 + 8, y * 16 + 8),
                        r,
                        g,
                        b
                    );
                }
            }
        }

        private static void KeepZeroLightTilesVisible(ILContext il)
        {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(
                MoveType.After,
                instruction =>
                    instruction.MatchCall(
                        typeof(Lighting),
                        nameof(Lighting.GetColor)
                    )))
            {
                cursor.EmitDelegate<Func<Color, Color>>(
                    ClampBlackColor
                );
            }
        }

        private static void KeepZeroLightWallsVisible(ILContext il)
        {
            ILCursor cursor = new(il);

            while (cursor.TryGotoNext(
                MoveType.After,
                instruction =>
                    instruction.MatchCall(
                        typeof(Lighting),
                        nameof(Lighting.GetColor)
                    )))
            {
                cursor.EmitDelegate<Func<Color, Color>>(
                    ClampBlackColor
                );
            }

            cursor.Index = 0;

            int vertexColorsLocal = -1;

            if (cursor.TryGotoNext(
                MoveType.After,
                instruction =>
                    instruction.MatchLdloca(
                        out vertexColorsLocal
                    ),
                instruction =>
                    instruction.MatchLdcR4(1f),
                instruction =>
                    instruction.MatchCall(
                        typeof(Lighting),
                        nameof(Lighting.GetCornerColors)
                    )))
            {
                cursor.EmitLdloc(vertexColorsLocal);

                cursor.EmitDelegate<
                    Func<VertexColors, VertexColors>>(
                    ClampBlackCorners
                );

                cursor.EmitStloc(vertexColorsLocal);
            }
        }

        private static Color ClampBlackColor(Color color)
        {
            if (Active &&
                color.R == 0 &&
                color.G == 0 &&
                color.B == 0)
            {
                color.R = 1;
                color.G = 1;
                color.B = 1;
            }

            return color;
        }

        private static VertexColors ClampBlackCorners(VertexColors colors)
        {
            if (!Active)
                return colors;

            colors.TopLeftColor =
                ClampBlackColor(
                    colors.TopLeftColor
                );

            colors.TopRightColor =
                ClampBlackColor(
                    colors.TopRightColor
                );

            colors.BottomLeftColor =
                ClampBlackColor(
                    colors.BottomLeftColor
                );

            colors.BottomRightColor =
                ClampBlackColor(
                    colors.BottomRightColor
                );

            return colors;
        }

        private static void DrawCrystalCavernsBackground(
            On_Main.orig_DrawBackgroundBlackFill orig,
            Main self)
        {
            orig(self);

            if (!Active ||
                layers == null ||
                layers.Length == 0)
            {
                return;
            }

            if (!parallaxAnchorValid)
            {
                parallaxAnchorX =
                    Main.screenPosition.X;

                parallaxAnchorValid = true;
            }

            float cameraDeltaX =
                Main.screenPosition.X -
                parallaxAnchorX;

            float verticalParallax =
                MathHelper.Clamp(
                    (Main.screenPosition.Y -
                    (float)Main.rockLayer * 16f) *
                    0.02f,
                    -160f,
                    160f
                );

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );

            for (int i = 0;
                i < layers.Length;
                i++)
            {
                DrawLayer(
                    layers[i],
                    cameraDeltaX,
                    verticalParallax
                );
            }

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix
            );
        }

        private static void DrawLayer(
            BackgroundLayer layer,
            float cameraDeltaX,
            float verticalParallax)
        {
            Texture2D texture =
                layer.Texture.Value;

            float scale =
                Main.screenHeight /
                (float)texture.Height;

            scale *= BackgroundScale;

            float scaledWidth =
                texture.Width *
                scale;

            float scaledHeight =
                texture.Height *
                scale;

            float centeredX =
                (Main.screenWidth -
                scaledWidth) *
                0.5f;

            float centeredY =
                (Main.screenHeight -
                scaledHeight) *
                0.5f;

            float horizontalOffset =
                -cameraDeltaX *
                layer.ParallaxX;

            horizontalOffset %=
                scaledWidth;

            float verticalRoom =
                Math.Max(
                    0f,
                    (scaledHeight -
                    Main.screenHeight) *
                    0.5f
                );

            float layerVerticalParallax =
                verticalParallax *
                layer.ParallaxY;

            float verticalOffset =
                MathHelper.Clamp(
                    layerVerticalParallax,
                    -verticalRoom,
                    verticalRoom
                );

            float startX =
                centeredX +
                horizontalOffset;

            while (startX > 0f)
                startX -= scaledWidth;

            while (startX +
                scaledWidth < 0f)
            {
                startX += scaledWidth;
            }

            float drawY =
                centeredY -
                verticalOffset +
                layer.VerticalOffset;

            Color drawColor =
                Color.White *
                visualOpacity;

            for (
                float drawX = startX;
                drawX < Main.screenWidth;
                drawX += scaledWidth)
            {
                Main.spriteBatch.Draw(
                    texture,
                    new Vector2(
                        drawX,
                        drawY
                    ),
                    null,
                    drawColor,
                    0f,
                    Vector2.Zero,
                    scale,
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}
