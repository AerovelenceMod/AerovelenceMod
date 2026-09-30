using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Content.Projectiles.Gas;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Tools;

public sealed class GasVacuum : TranslatableModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.LeafBlower;

    public override void SetStaticDefaults() => this.ModifyLocalization("Gas Vacuum", "Pulls gas towards the nozezl\nRight-click pulls gas inward from every direction");

    public override void SetDefaults()
    {
        Item.width = 44;
        Item.height = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 20;
        Item.channel = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<GasVacuumHeld>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Pink;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, 0, 0f, player.whoAmI, player.altFunctionUse == 2 ? 1f : 0f);
        return false;
    }
}