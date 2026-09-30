using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.UI;
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
        private bool blockPencilUntilMouseRelease = false;
        private List<string> availableStructures = new List<string>();
        private string selectedStructureName = "tumblerarena";
        private UserInterface structurePickerInterface;
        private StructurePickerUI structurePickerUI;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                structurePickerInterface = new UserInterface();
                structurePickerUI = new StructurePickerUI();
                structurePickerUI.Activate();
            }
        }

        public override void Unload()
        {
            structurePickerInterface = null;
            structurePickerUI = null;
        }

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
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (blockPencilUntilMouseRelease && !Main.mouseLeft)
            {
                blockPencilUntilMouseRelease = false;
            }

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
                Main.LocalPlayer.mouseInterface = true;
                structurePickerInterface?.Update(gameTime);

                if (Main.keyState.IsKeyDown(Keys.Escape) && Main.oldKeyState.IsKeyUp(Keys.Escape))
                {
                    CloseStructurePicker();
                }

                Main.blockInput = false;
            }
            else
            {
                Main.blockInput = false;
            }
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (mouseTextIndex != -1)
            {
                layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
                    "AerovelenceMod: Structure Picker",
                    delegate
                    {
                        if (structurePickerOpen)
                        {
                            structurePickerInterface?.Draw(Main.spriteBatch, new GameTime());
                        }

                        return true;
                    },
                    InterfaceScaleType.UI
                ));
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

        public bool IsStructurePickerOpen()
        {
            return structurePickerOpen;
        }

        public bool IsPencilUseBlocked()
        {
            return blockPencilUntilMouseRelease;
        }

        public void OpenStructurePicker()
        {
            RefreshStructureList();
            structurePickerUI.SetStructures(availableStructures, selectedStructureName, SelectStructure);
            structurePickerInterface.SetState(structurePickerUI);
            structurePickerOpen = true;
        }

        public void CloseStructurePicker()
        {
            structurePickerOpen = false;
            structurePickerInterface?.SetState(null);
        }

        public string GetSelectedStructureName()
        {
            if (availableStructures.Count == 0)
            {
                RefreshStructureList();
            }

            return selectedStructureName;
        }

        private void SelectStructure(string name)
        {
            selectedStructureName = name;
            blockPencilUntilMouseRelease = true;
            structurePickerUI.SetStructures(availableStructures, selectedStructureName, SelectStructure);
            Main.NewText($"Selected structure '{selectedStructureName}'.");
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
                return;
            }

            if (!availableStructures.Any(name => name.Equals(selectedStructureName, StringComparison.OrdinalIgnoreCase)))
            {
                selectedStructureName = availableStructures[0];
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
            Utils.DrawBorderString(spriteBatch, promptText, textPosition, Color.White);

            Vector2 inputPosition = textPosition + new Vector2(0, textSize.Y + 10);
            Utils.DrawBorderString(spriteBatch, structureName + "|", inputPosition, Color.Yellow);
        }

        private class StructurePickerUI : UIState
        {
            private UIPanel panel;
            private UIList structureList;
            private UIScrollbar scrollbar;

            public override void OnInitialize()
            {
                panel = new UIPanel();
                panel.Width.Set(440f, 0f);
                panel.Height.Set(420f, 0f);
                panel.HAlign = 0.5f;
                panel.VAlign = 0.5f;
                Append(panel);

                UIText title = new UIText("Select Structure", 1f, true);
                title.HAlign = 0.5f;
                title.Top.Set(8f, 0f);
                panel.Append(title);

                structureList = new UIList();
                structureList.Top.Set(45f, 0f);
                structureList.Width.Set(-28f, 1f);
                structureList.Height.Set(-55f, 1f);
                structureList.ListPadding = 4f;
                panel.Append(structureList);

                scrollbar = new UIScrollbar();
                scrollbar.Top.Set(45f, 0f);
                scrollbar.Left.Set(-20f, 1f);
                scrollbar.Height.Set(-55f, 1f);
                panel.Append(scrollbar);

                structureList.SetScrollbar(scrollbar);
            }

            public override void Update(GameTime gameTime)
            {
                base.Update(gameTime);

                if (panel.ContainsPoint(Main.MouseScreen))
                {
                    Main.LocalPlayer.mouseInterface = true;
                }
            }

            public void SetStructures(IEnumerable<string> structures, string selectedStructure, Action<string> selectStructure)
            {
                structureList.Clear();

                foreach (string structure in structures)
                {
                    string name = structure;
                    UITextPanel<string> button = new UITextPanel<string>(name, 0.9f, false);
                    button.Width.Set(0f, 1f);
                    button.Height.Set(34f, 0f);
                    button.TextHAlign = 0f;

                    if (name.Equals(selectedStructure, StringComparison.OrdinalIgnoreCase))
                    {
                        button.BackgroundColor = new Color(70, 105, 150);
                        button.BorderColor = new Color(220, 190, 80);
                    }

                    button.OnLeftClick += (evt, element) => selectStructure(name);
                    structureList.Add(button);
                }

                Recalculate();
            }
        }
    }
}
