/*using AerovelenceMod.Common.Utilities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Sets.Phantic.Armor
{
    [AutoloadEquip(EquipType.Body)]
    public class PhanticChestplate : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 18;
            Item.value = 10;
            Item.rare = ItemRarities.MidPHM;
            Item.defense = 5;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += 3;
        }
        public override void AddRecipes()
        {
            CreateRecipe(1)
                .AddIngredient(ModContent.ItemType<PhanticBar>(), 17)
                .AddRecipeGroup("AerovelenceMod:EvilMaterials", 15)
                .AddTile(TileID.Anvils)
                .Register();

        }
    }
}*/
