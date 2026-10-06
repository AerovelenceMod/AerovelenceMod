/*
using System;



using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;

using System.Collections.Generic;
using Terraria.Graphics;
using ReLogic.Content;
using MonoMod.Utils;
using static Humanizer.In;
using System.Runtime.InteropServices;
using Terraria.DataStructures;

namespace AerovelenceMod.Content.Items.Sets.Burnshock.ThunderLance
{
    public class ThunderLance : ModItem
    {
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarities.EarlyHardmode;

            Item.width = 58;
            Item.height = 58;

            Item.damage = 87;
            Item.knockBack = 2f;
            Item.useTime = 60;
            Item.useAnimation = 60;


            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;

            Item.shoot = ModContent.ProjectileType<ThunderLanceProj>();
            Item.shootSpeed = 1;
        }

        public override void AddRecipes()
		{
			CreateRecipe()
				.AddIngredient<BurnshockBar>(20)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
    }
}*/
