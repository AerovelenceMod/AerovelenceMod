using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Items.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Projectiles.Gas;

public sealed class GasVacuumHeld : ModProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.LeafBlower;
    public override Terraria.Localization.LocalizedText DisplayName => ModContent.GetInstance<GasVacuum>().DisplayName;
    private int age;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.noItems || player.CCed || player.HeldItem.type != ModContent.ItemType<GasVacuum>())
        {
            Projectile.Kill();
            return;
        }
        if (Projectile.owner == Main.myPlayer)
        {
            if (!player.channel)
            {
                Projectile.Kill();
                return;
            }
            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > 0.0004f || age % 15 == 0)
            {
                Projectile.velocity = aim;
                Projectile.netUpdate = true;
            }
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
        Projectile.Center = player.MountedCenter + direction * 28f;
        Projectile.rotation = direction.ToRotation();
        Projectile.timeLeft = 2;
        player.ChangeDir(direction.X >= 0f ? 1 : -1);
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        player.itemRotation = (direction * player.direction).ToRotation();
        Vector2 nozzle = player.MountedCenter + direction * 46f;
        if (!Collision.CanHitLine(player.MountedCenter, 1, 1, nozzle, 1, 1))
            nozzle = player.MountedCenter;
        GasUtil.Vacuum(nozzle, direction, Projectile.ai[0] == 1f);
        if (!Main.dedServ && age % 30 == 0)
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.28f, Pitch = -0.65f, MaxInstances = 1 }, Projectile.Center);
        age++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        float rotation = Projectile.rotation;
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        SpriteEffects effects = player.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, null, lightColor, rotation, texture.Size() * 0.5f, 1f, effects, 0f);
        return false;
    }
}
