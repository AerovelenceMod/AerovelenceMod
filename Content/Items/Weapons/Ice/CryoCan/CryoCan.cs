using System;





using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Creative;



namespace AerovelenceMod.Content.Items.Weapons.Ice.CryoCan;

[LegacyName("AerosolGun")]
public sealed class CryoCan : TranslatableModItem
{
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        this.ModifyLocalization("Cryo Can", "Sprays freezing aerosol\nRight-click to blow gas away with compressed air");
    }

    public override void SetDefaults()
    {
        Item.width = 10;
        Item.height = 26;
        Item.damage = 18;
        Item.DamageType = DamageClass.Magic;
        Item.mana = 2;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 12;
        Item.shootSpeed = 14f;
        Item.shoot = ModContent.ProjectileType<CryoCanHeld>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.autoReuse = true;
        Item.rare = ItemRarities.EarlyPHM;
        Item.value = Item.sellPrice(silver: 30);
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        Item.mana = player.altFunctionUse == 2 ? 0 : 2;
        return player.ownedProjectileCounts[Item.shoot] == 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI, player.altFunctionUse == 2 ? 1f : 0f);
        return false;
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient(ItemID.IceBlock, 30)
        .AddRecipeGroup(RecipeGroupID.IronBar, 8)
        .AddTile(TileID.Anvils)
        .Register();
}

public sealed class CryoCanHeld : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Ice/CryoCan/CryoCan";
    public override Terraria.Localization.LocalizedText DisplayName => ModContent.GetInstance<CryoCan>().DisplayName;
    private static readonly GasSettings aerosol = GasSettings.ForAerosol() with
    {
        Color = new Color(35, 165, 255),
        ColorFadeRate = 0.065f
    };
    private int age;

    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 26;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        bool compressedAir = Projectile.ai[0] == 1f;
        if (!player.active || player.dead || player.noItems || player.CCed || player.HeldItem.type != ModContent.ItemType<CryoCan>())
        {
            Projectile.Kill();
            return;
        }
        if (Projectile.owner == Main.myPlayer)
        {
            if (!player.channel || (!compressedAir && age > 0 && age % 12 == 0 && !player.CheckMana(player.HeldItem, -1, true)))
            {
                Projectile.Kill();
                return;
            }
            Vector2 aim = (Projectile.AimWorld() - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > 0.0001f || age % 12 == 0)
            {
                Projectile.velocity = aim;
                if (age % 6 == 0)
                    Projectile.netUpdate = true;
            }
            if (!compressedAir)
                player.manaRegenDelay = player.maxRegenDelay;
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
        player.ChangeDir(direction.X >= 0f ? 1 : -1);
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        player.itemRotation = (direction * player.direction).ToRotation();
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2);
        Vector2 hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, direction.ToRotation() - MathHelper.PiOver2) + new Vector2(0f, player.gfxOffY);
        Vector2 handOffset = direction.RotatedBy(player.direction < 0 ? -MathHelper.PiOver2 : MathHelper.PiOver2) * 3f - direction * 1.5f;
        Projectile.Center = hand + handOffset;
        Projectile.rotation = direction.ToRotation() + (player.direction < 0 ? MathHelper.Pi : 0f);
        Projectile.timeLeft = 2;
        Vector2 muzzle = Projectile.Center + (Projectile.rotation - MathHelper.PiOver2).ToRotationVector2() * 11f + direction * 4f;
        if (Projectile.owner == Main.myPlayer && age % 2 == 0)
        {
            if (!Collision.CanHitLine(player.MountedCenter, 1, 1, muzzle, 1, 1))
                muzzle = player.MountedCenter;
            if (compressedAir)
                GasUtil.Blow(Projectile.GetSource_FromThis(), muzzle, direction * 28f, owner: player.whoAmI);
            else
            {
                float shimmer = 0.62f + MathF.Sin(Main.GameUpdateCount * 0.035f) * 0.06f;
                Color settled = Color.Lerp(new Color(70, 220, 245), new Color(235, 255, 255), shimmer);
                GasSettings settings = aerosol with { FadeColor = settled };
                GasUtil.Emit(Projectile.GetSource_FromThis(), muzzle, direction * player.HeldItem.shootSpeed, settings, Projectile.damage, player.whoAmI);
            }
        }
        if (!Main.dedServ && age % 10 == 0)
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.16f, Pitch = 0.7f, PitchVariance = 0.08f, MaxInstances = 1 }, muzzle);
        age++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, Main.player[Projectile.owner].direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        return false;
    }
}
