using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.UI;
using ReLogic.Graphics;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.GameInput;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AerovelenceMod.Common.Utilities.Generation.StructureStamper
{
    public class StructureStamperSystem : ModSystem
    {
        private Vector2? Point1;
        private Vector2? Point2;
        private bool awaitingName = false;
        private string structureName = string.Empty;
        private bool structurePickerOpen = false;
        private List<string> availableStructures = new List<string>();
        private string selectedStructureName = "tumblerarena";
        private int structureScroll = 0;
        private const int VisibleStructureRows = 10;
        private const int StructureRowHeight = 32;

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (Point1.HasValue && Point2.HasValue)
            {
                Rectangle selectionRectangle = new(
                    (int)Point1.Value.X * 16,
                    (int)Point1.Value.Y * 16,
                    (int)(Point2.Value.X - Point1.Value.X + 1) * 16,
                    (int)(Point2.Value.Y - Point1.Value.Y + 1) * 16
                );

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, selectionRectangle, Color.White * 0.5f);
            }

            if (awaitingName)
            {
                DrawNameInputUI(spriteBatch);
            }

            if (structurePickerOpen)
            {
                DrawStructurePickerUI(spriteBatch);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (awaitingName)
            {
                HandleTextInput();

                if (Main.keyState.IsKeyDown(Keys.Enter))
                {
                    awaitingName = false;
                    Main.drawingPlayerChat = false;
                    StructureStamper.ExtractStructure(Point1.Value, Point2.Value, structureName);
                    Point1 = null;
                    Point2 = null;
                    structureName = string.Empty;
                }

                PlayerInput.SetZoom_UI();
                Main.blockInput = true;
            }
            else if (structurePickerOpen)
            {
                HandleStructurePickerInput();
                PlayerInput.SetZoom_UI();
                Main.blockInput = true;
            }
            else
            {
                Main.blockInput = false;
            }
        }

        public void StartNamingProcess(Vector2 point1, Vector2 point2)
        {
            Point1 = point1;
            Point2 = point2;
            awaitingName = true;
            Main.drawingPlayerChat = true;
        }

        public void SetPoint1(Vector2 point)
        {
            Point1 = point;
            Main.NewText($"Point 1 set at {Point1}.");
        }

        public void SetPoint2(Vector2 point)
        {
            Point2 = point;
            Main.NewText($"Point 2 set at {Point2}.");

            if (Point1.HasValue && Point2.HasValue)
            {
                StartNamingProcess(Point1.Value, Point2.Value);
            }
        }

        public Vector2? GetPoint1()
        {
            return Point1;
        }

        public Vector2? GetPoint2()
        {
            return Point2;
        }

        public void OpenStructurePicker()
        {
            if (structurePickerOpen)
            {
                return;
            }

            RefreshStructureList();
            structurePickerOpen = true;
        }

        public string GetSelectedStructureName()
        {
            if (availableStructures.Count == 0)
            {
                RefreshStructureList();
            }

            return selectedStructureName;
        }

        private void RefreshStructureList()
        {
            const string structurePath = "Common/Utilities/Generation/StructureStamper/Structures/";

            availableStructures = Mod.GetFileNames()
                .Where(file => file.StartsWith(structurePath, StringComparison.OrdinalIgnoreCase) && file.EndsWith(".dat", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (availableStructures.Count == 0)
            {
                selectedStructureName = string.Empty;
                structureScroll = 0;
                return;
            }

            int selectedIndex = availableStructures.FindIndex(name => name.Equals(selectedStructureName, StringComparison.OrdinalIgnoreCase));
            if (selectedIndex == -1)
            {
                selectedStructureName = availableStructures[0];
                selectedIndex = 0;
            }

            structureScroll = Math.Clamp(selectedIndex - VisibleStructureRows / 2, 0, Math.Max(0, availableStructures.Count - VisibleStructureRows));
        }

        private void HandleStructurePickerInput()
        {
            Main.LocalPlayer.mouseInterface = true;

            if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
            {
                structurePickerOpen = false;
                return;
            }

            int maxScroll = Math.Max(0, availableStructures.Count - VisibleStructureRows);
            if (PlayerInput.ScrollWheelDeltaForUI != 0)
            {
                structureScroll -= Math.Sign(PlayerInput.ScrollWheelDeltaForUI);
                structureScroll = Math.Clamp(structureScroll, 0, maxScroll);
            }

            if (!Main.mouseLeft || !Main.mouseLeftRelease)
            {
                return;
            }

            Rectangle panel = GetStructurePickerRectangle();
            int rowCount = Math.Min(VisibleStructureRows, availableStructures.Count);

            for (int row = 0; row < rowCount; row++)
            {
                int index = structureScroll + row;
                Rectangle rowRectangle = GetStructureRowRectangle(panel, row);

                if (rowRectangle.Contains(Main.mouseX, Main.mouseY))
                {
                    selectedStructureName = availableStructures[index];
                    structurePickerOpen = false;
                    Main.mouseLeftRelease = false;
                    Main.NewText($"Selected structure '{selectedStructureName}'.");
                    return;
                }
            }
        }

        private Rectangle GetStructurePickerRectangle()
        {
            int rowCount = Math.Max(1, Math.Min(VisibleStructureRows, availableStructures.Count));
            int width = 420;
            int height = 92 + rowCount * StructureRowHeight;
            return new Rectangle(Main.screenWidth / 2 - width / 2, Main.screenHeight / 2 - height / 2, width, height);
        }

        private Rectangle GetStructureRowRectangle(Rectangle panel, int row)
        {
            return new Rectangle(panel.X + 12, panel.Y + 50 + row * StructureRowHeight, panel.Width - 24, StructureRowHeight - 4);
        }

        private void DrawStructurePickerUI(SpriteBatch spriteBatch)
        {
            Rectangle panel = GetStructurePickerRectangle();
            Rectangle border = new Rectangle(panel.X - 2, panel.Y - 2, panel.Width + 4, panel.Height + 4);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, border, Color.Black * 0.9f);
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, panel, new Color(28, 32, 44) * 0.96f);

            string title = "Select Structure";
            Vector2 titleSize = FontAssets.MouseText.Value.MeasureString(title);
            spriteBatch.DrawString(FontAssets.MouseText.Value, title, new Vector2(panel.Center.X - titleSize.X / 2, panel.Y + 14), Color.White);

            if (availableStructures.Count == 0)
            {
                string emptyText = "No structures found";
                Vector2 emptySize = FontAssets.MouseText.Value.MeasureString(emptyText);
                spriteBatch.DrawString(FontAssets.MouseText.Value, emptyText, new Vector2(panel.Center.X - emptySize.X / 2, panel.Y + 58), Color.Gray);
                return;
            }

            int rowCount = Math.Min(VisibleStructureRows, availableStructures.Count);
            for (int row = 0; row < rowCount; row++)
            {
                int index = structureScroll + row;
                string name = availableStructures[index];
                Rectangle rowRectangle = GetStructureRowRectangle(panel, row);
                bool selected = name.Equals(selectedStructureName, StringComparison.OrdinalIgnoreCase);
                bool hovered = rowRectangle.Contains(Main.mouseX, Main.mouseY);

                Color backgroundColor = selected ? new Color(70, 105, 150) : hovered ? new Color(55, 62, 78) : new Color(39, 44, 58);
                Color textColor = selected ? Color.Yellow : Color.White;

                spriteBatch.Draw(TextureAssets.MagicPixel.Value, rowRectangle, backgroundColor);
                spriteBatch.DrawString(FontAssets.MouseText.Value, name, new Vector2(rowRectangle.X + 10, rowRectangle.Y + 5), textColor);
            }

            if (availableStructures.Count > VisibleStructureRows)
            {
                string rangeText = $"{structureScroll + 1}-{Math.Min(structureScroll + VisibleStructureRows, availableStructures.Count)} / {availableStructures.Count}";
                Vector2 rangeSize = FontAssets.MouseText.Value.MeasureString(rangeText);
                spriteBatch.DrawString(FontAssets.MouseText.Value, rangeText, new Vector2(panel.Right - rangeSize.X - 12, panel.Bottom - 26), Color.Gray);
            }
        }

        private void HandleTextInput()
        {
            if (Main.keyState.IsKeyDown(Keys.Back) && structureName.Length > 0)
            {
                structureName = structureName.Substring(0, structureName.Length - 1);
            }

            foreach (Keys key in Enum.GetValues(typeof(Keys)))
            {
                if (Main.keyState.IsKeyDown(key) && Main.oldKeyState.IsKeyUp(key))
                {
                    string keyString = key.ToString();

                    if (keyString.Length == 1)
                    {
                        if (Main.keyState.IsKeyDown(Keys.LeftShift) || Main.keyState.IsKeyDown(Keys.RightShift))
                        {
                            structureName += keyString.ToUpper();
                        }
                        else
                        {
                            structureName += keyString.ToLower();
                        }
                    }

                    if (key == Keys.Space)
                    {
                        structureName += " ";
                    }
                }
            }
        }

        private void DrawNameInputUI(SpriteBatch spriteBatch)
        {
            Vector2 uiPosition = new(Main.screenWidth / 2, Main.screenHeight / 2);
            string promptText = "Enter Structure Name:";
            Vector2 textSize = FontAssets.MouseText.Value.MeasureString(promptText);
            Vector2 textPosition = uiPosition - textSize / 2;

            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)textPosition.X - 10, (int)textPosition.Y - 10, (int)textSize.X + 20, (int)textSize.Y + 40), Color.Black * 0.8f);
            spriteBatch.DrawString(FontAssets.MouseText.Value, promptText, textPosition, Color.White);

            Vector2 inputPosition = textPosition + new Vector2(0, textSize.Y + 10);
            spriteBatch.DrawString(FontAssets.MouseText.Value, structureName + "|", inputPosition, Color.Yellow);
        }
    }
}
