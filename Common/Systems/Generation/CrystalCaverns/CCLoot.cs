using AerovelenceMod.Common.Utilities.Generation.StructureStamper;
using AerovelenceMod.Content.Items.Accessories.SmallAccessories;
using AerovelenceMod.Content.Items.Ammo;
using AerovelenceMod.Content.Items.BossSummons;
using AerovelenceMod.Content.Items.Potions;
using AerovelenceMod.Content.Items.Weapons.CrystalCaverns;
using AerovelenceMod.Content.Items.Weapons.CrystalCaverns.CrystalCrescent;
using AerovelenceMod.Content.Tiles.CrystalCaverns.Natural;
using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Common.Systems.Generation.CrystalCaverns
{
    public static class CCLoot
    {
        public static List<PrimaryItemConfiguration> CreatePrimaryLootPool() => new()
        {
            new(ModContent.ItemType<CrystalCrescent>(), 1, 1, 1f),
            new(ModContent.ItemType<BandOfCrystallization>(), 1, 1, 1f),
            new(ModContent.ItemType<SpikesInABottle>(), 1, 1, 1f),
            new(ModContent.ItemType<SilkenScarf>(), 1, 1, 1f),
            new(ModContent.ItemType<Content.Items.Weapons.CrystalCaverns.TheSling.TheSling>(), 1, 1, 1f),
            new(ModContent.ItemType<RockRumbler>(), 1, 1, 1f),
            new(ModContent.ItemType<CavernousRampart>(), 1, 1, 1f),
            new(ModContent.ItemType<Content.Items.Weapons.CrystalCaverns.TumblerCommander.TumblerCommander>(), 1, 1, 1f),
            new(ModContent.ItemType<SaplingCane>(), 1, 1, 1f),
            new(ModContent.ItemType<CrystalStompers>(), 1, 1, 1f)
        };

        public static List<ItemConfiguration> CreateSecondaryLootPool() => new()
        {
            new(ModContent.ItemType<StoneSlug>(), 30, 60, 1f / 2),
            new(ModContent.ItemType<MineralWater>(), 2, 4, 1f / 2),
            new(ItemID.SuspiciousLookingEye, 1, 1, 1f/5),
            new(ItemID.Dynamite, 25, 50, 1f/3),
            new(new List<int> { ItemID.SilverBar, ItemID.TungstenBar, ItemID.GoldBar, ItemID.PlatinumBar }, 3, 10, 1f/2),
            new(ItemID.HealingPotion, 3, 5, 1f/2),
            new(new List<int>
            {
                ItemID.SpelunkerPotion, ItemID.FeatherfallPotion, ItemID.NightOwlPotion, ItemID.WaterWalkingPotion,
                ItemID.ArcheryPotion, ItemID.GravitationPotion, ItemID.ThornsPotion, ItemID.InvisibilityPotion,
                ItemID.HunterPotion, ItemID.BattlePotion, ItemID.TeleportationPotion
            }, 1, 2, 2f/3), // Vanilla splits the potions into two item slots for caverns chests
            new(ItemID.RecallPotion, 1, 2, 1f/2),
            new(new List<int> { ItemID.Torch, ItemID.Glowstick }, 15, 29, 1f/2),
            new(ModContent.ItemType<CavernCrystalItem>(), 10, 30, 1f),
            new(ItemID.GoldCoin, 1, 2, 1f/2)
        };
    }
}
