
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Armor.Misc.JellyfishHat
{
    [AutoloadEquip(EquipType.Head)]
    public class JellyfishHat : ModItem
    {
        private const int AuraCooldown = 480;
        public override void SetDefaults()
        {
            Item.value = Item.sellPrice(gold: 1, silver: 10, copper: 5);
            Item.rare = ItemRarities.EarlyPHM;
            Item.defense = 3;
        }
        public override void UpdateEquip(Player player)
        {
            if (player.whoAmI != Main.myPlayer) return;
            if (player.armor[0].type != Type) return;
            int projectileType = ModContent.ProjectileType<JellyfishAuraProjectile>();
            if (player.ownedProjectileCounts[projectileType] > 0) return;
            if (player.GetModPlayer<JellyfishHatPlayer>().Cooldown > 0) return;
            Projectile.NewProjectile(player.GetSource_Misc("JellyfishHat"), player.Center, Vector2.Zero, projectileType, 10, 5f, player.whoAmI);
            player.GetModPlayer<JellyfishHatPlayer>().Cooldown = AuraCooldown;
        }
        public override void AddRecipes()
        {
            int[] jellyfishTypes = { ItemID.BlueJellyfish, ItemID.PinkJellyfish, ItemID.GreenJellyfish };
            foreach (int jellyfish in jellyfishTypes)
            {
                CreateRecipe()
                    .AddIngredient(jellyfish)
                    .AddRecipeGroup(RecipeGroupID.IronBar, 4)
                    .AddTile(TileID.Anvils)
                    .Register();
            }
        }
    }

    public class JellyfishHatPlayer : ModPlayer
    {
        public int Cooldown;
        public override void PostUpdateEquips()
        {
            if (Cooldown > 0) Cooldown--;
            if (Player.armor[0].type != ModContent.ItemType<JellyfishHat>()) Cooldown = 0;
        }
    }

    public class JellyfishAuraProjectile : ModProjectile
    {
        private const int FrameCount = 3;
        private const int AnimationSpeed = 5;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }
        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Generic;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.width = 40;
            Projectile.height = 58;
            Projectile.timeLeft = 200;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.armor[0].type != ModContent.ItemType<JellyfishHat>())
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = player.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= AnimationSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }
        }
    }
}
