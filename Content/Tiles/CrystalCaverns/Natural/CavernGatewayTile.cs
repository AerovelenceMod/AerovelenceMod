using System;




using AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler;

using ReLogic.Content;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;


namespace AerovelenceMod.Content.Tiles.CrystalCaverns.Natural
{
    public class CavernGatewayTile : ModTile
    {
        private const int TileWidth = 15;
        private const int TileHeight = 12;
        private const int DoorTravel = 112;
        private static readonly Rectangle DoorOpening = new(52, 60, 136, 132);

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = false;
            CommonTileHelper.SetupMultiTile(this, TileWidth, TileHeight, [16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16]);
            AddMapEntry(new Color(200, 200, 200));
            CommonTileHelper.SetTileProtection(this);
        }

        public override void SetDrawPositions(int i, int j, ref int width, ref int offsetY, ref int height, ref short tileFrameX, ref short tileFrameY) => offsetY = 2;

        public override void DrawEffects(int i, int j, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (drawData.tileFrameX % (TileWidth * 18) == 0 && drawData.tileFrameY % (TileHeight * 18) == 0)
                Main.instance.TilesRenderer.AddSpecialPoint(i, j, TileDrawing.TileCounterType.CustomNonSolid);
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            GetDoorState(out float openProgress, out float glowOpacity);
            Tile tile = Framing.GetTileSafely(i, j);
            int tileX = i - tile.TileFrameX / 18 % TileWidth;
            int tileY = j - tile.TileFrameY / 18 % TileHeight;
            Vector2 topLeft = new Vector2(tileX * 16f, tileY * 16f + 2f) - Main.screenPosition;
            Rectangle opening = new((int)topLeft.X + DoorOpening.X, (int)topLeft.Y + DoorOpening.Y, DoorOpening.Width, DoorOpening.Height);
            int travel = (int)MathF.Round(MathHelper.SmoothStep(0f, DoorTravel, openProgress));
            Texture2D behind = ModContent.Request<Texture2D>(Texture + "Behind", AssetRequestMode.ImmediateLoad).Value;
            Texture2D leftDoor = ModContent.Request<Texture2D>(Texture + "DoorLeft", AssetRequestMode.ImmediateLoad).Value;
            Texture2D rightDoor = ModContent.Request<Texture2D>(Texture + "DoorRight", AssetRequestMode.ImmediateLoad).Value;
            Texture2D glowmask = ModContent.Request<Texture2D>(Texture + "DoorRight_Glowmask", AssetRequestMode.ImmediateLoad).Value;
            DrawPaddedLayer(spriteBatch, behind, topLeft, tileX, tileY, 0, null, Color.White, true, 2);
            DrawPaddedLayer(spriteBatch, leftDoor, topLeft, tileX, tileY, -travel, opening, Color.White, true);
            DrawPaddedLayer(spriteBatch, rightDoor, topLeft, tileX, tileY, travel, opening, Color.White, true);
            DrawPaddedLayer(spriteBatch, TextureAssets.Tile[Type].Value, topLeft, tileX, tileY, 0, null, Color.White, true);
            if (glowOpacity > 0f)
                DrawPaddedLayer(spriteBatch, glowmask, topLeft, tileX, tileY, travel, opening, new Color(255, 255, 255, 0) * glowOpacity, false);
        }

        private static void GetDoorState(out float openProgress, out float glowOpacity)
        {
            openProgress = 0f;
            glowOpacity = 0f;
            int index = NPC.FindFirstNPC(ModContent.NPCType<CrystalTumbler>());
            if (index < 0)
                return;
            NPC boss = Main.npc[index];
            if (boss.ai[0] != (float)TumblerState.Spawn)
            {
                openProgress = 1f;
                return;
            }
            float time = boss.ai[1];
            openProgress = MathHelper.Clamp((time - CrystalTumbler.DoorOpenStart) / (CrystalTumbler.DoorOpenEnd - CrystalTumbler.DoorOpenStart), 0f, 1f);
            float flashIn = MathHelper.Clamp((time - CrystalTumbler.DoorFlashStart) / (CrystalTumbler.DoorFlashPeak - CrystalTumbler.DoorFlashStart), 0f, 1f);
            float flashOut = 1f - MathHelper.Clamp((time - CrystalTumbler.DoorFlashPeak) / (CrystalTumbler.DoorFlashEnd - CrystalTumbler.DoorFlashPeak), 0f, 1f);
            glowOpacity = flashIn * flashOut;
        }

        private static void DrawPaddedLayer(SpriteBatch spriteBatch, Texture2D texture, Vector2 topLeft, int tileX, int tileY, int offsetX, Rectangle? clip, Color tint, bool lit, int sourceScale = 1)
        {
            for (int y = 0; y < TileHeight; y++)
            {
                for (int x = 0; x < TileWidth; x++)
                {
                    Rectangle source = new(x * 18 * sourceScale, y * 18 * sourceScale, 16 * sourceScale, 16 * sourceScale);
                    Rectangle destination = new((int)topLeft.X + x * 16 + offsetX, (int)topLeft.Y + y * 16, 16, 16);
                    if (clip.HasValue)
                    {
                        Rectangle clipped = Rectangle.Intersect(destination, clip.Value);
                        if (clipped.Width <= 0 || clipped.Height <= 0)
                            continue;
                        source.X += (clipped.X - destination.X) * sourceScale;
                        source.Y += (clipped.Y - destination.Y) * sourceScale;
                        source.Width = clipped.Width * sourceScale;
                        source.Height = clipped.Height * sourceScale;
                        destination = clipped;
                    }
                    Color color = lit ? Lighting.GetColor(tileX + x, tileY + y) : tint;
                    spriteBatch.Draw(texture, destination, source, color);
                }
            }
        }
    }
}
