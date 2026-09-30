using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using AerovelenceMod.Content.Items.Weapons.Aurora.Eos;

namespace AerovelenceMod.Common.Utilities.Generation.StructureStamper
{
    public class StructurePencil : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item1;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            StructureStamperSystem system = ModContent.GetInstance<StructureStamperSystem>();

            if (system.IsStructurePickerOpen() || system.IsPencilUseBlocked())
            {
                return false;
            }

            if (player.altFunctionUse == 2)
            {
                system.OpenStructurePicker();
                return false;
            }
            else
            {
                string structureName = system.GetSelectedStructureName();
                if (string.IsNullOrEmpty(structureName))
                {
                    Main.NewText("No structures were found.");
                    return true;
                }

                AeroStructure structure = StructureStamper.LoadStructure(Vector2.Zero, structureName, placeStructure: false, checkIfProtected: false);
                if (structure != AeroStructure.Empty)
                {
                    Vector2 position = player.position.ToTileCoordinates().ToVector2();
                    position.X -= structure.Width / 2;
                    position.Y -= structure.Height / 2;
                    structure = StructureStamper.LoadStructure(position, structureName, placeStructure: true, checkIfProtected: true, ignoreTheTile: false);
                    if (structure != AeroStructure.Empty)
                    {
                        structure.ProtectStructure();
                    }
                }
            }

            return true;
        }
    }
}
