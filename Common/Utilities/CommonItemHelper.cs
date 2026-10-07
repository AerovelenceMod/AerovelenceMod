namespace AerovelenceMod.Common.Utilities
{
    public static class CommonItemHelper
    {
        public static void SetupPlaceableItem(ModItem modItem, int width, int height, int value, int createTileType, int rare = ItemRarities.BasicMaterials, int placeStyle = 0)
        {
            modItem.Item.width = width;
            modItem.Item.height = height;
            modItem.Item.value = value;
            modItem.Item.rare = rare;
            modItem.Item.DefaultToPlaceableTile(createTileType, placeStyle);
        }

        public static void SetupTorch(ModItem modItem, int tileType, float lightR = 1f, float lightG = 1f, float lightB = 1f)
        {
            ItemID.Sets.ShimmerTransformToItem[modItem.Type] = ItemID.ShimmerTorch;

            modItem.Item.DefaultToTorch(tileType, 0, false);

            ItemID.Sets.SingleUseInGamepad[modItem.Type] = true;
            ItemID.Sets.Torches[modItem.Type] = true;

            modItem.Item.GetGlobalItem<TorchGlobalItem>().LightR = lightR;
            modItem.Item.GetGlobalItem<TorchGlobalItem>().LightG = lightG;
            modItem.Item.GetGlobalItem<TorchGlobalItem>().LightB = lightB;

            modItem.Item.ResearchUnlockCount = 100;
        }

        public class TorchGlobalItem : GlobalItem
        {
            public float LightR;
            public float LightG;
            public float LightB;

            public override bool InstancePerEntity => true;

            public override void HoldItem(Item item, Player player)
            {
                if (item.createTile != -1 && ItemID.Sets.Torches[item.type])
                {
                    if (!player.wet)
                    {
                        Vector2 position = player.RotatedRelativePoint(player.itemLocation, true);
                        Lighting.AddLight(position, LightR, LightG, LightB);
                    }
                }
            }

            public override void PostUpdate(Item item)
            {
                if (item.createTile != -1 && ItemID.Sets.Torches[item.type])
                {
                    if (!item.wet)
                    {
                        Lighting.AddLight(item.Center, LightR, LightG, LightB);
                    }
                }
            }
        }
    }
}
