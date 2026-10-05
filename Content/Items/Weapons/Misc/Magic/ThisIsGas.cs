using System.Collections.Generic;
using System.IO;


using AerovelenceMod.Content.Projectiles;

using Microsoft.Xna.Framework.Input;

using Terraria.Audio;
using Terraria.DataStructures;


using Terraria.ModLoader.IO;

namespace AerovelenceMod.Content.Items.Weapons.Misc.Magic;

public sealed class ThisIsGas : TranslatableModItem
{
    private GasKind kind;
    private bool tileCollision = true;
    private bool buoyancy = true;
    private bool turbulence = true;

    public override string Texture => "Terraria/Images/Item_" + ItemID.Clentaminator;

    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("This Is Gas", "Left-click to spray gas\nRight-click to cycle gas types\nUp + right-click to toggle tile collision\nDown + right-click to toggle buoyancy\nCtrl + right-click to toggle turbulence")
            .AddText(Language.Default, "Status", "Gas: {0} | Tile collision: {1} | Buoyancy: {2} | Turbulence: {3}")
            .AddText(Language.Default, "Fog", "Fog")
            .AddText(Language.Default, "Smoke", "Smoke")
            .AddText(Language.Default, "Sapfog", "Sapfog")
            .AddText(Language.Default, "On", "On")
            .AddText(Language.Default, "Off", "Off");
    }

    public override void SetDefaults()
    {
        Item.width = 54;
        Item.height = 28;
        Item.damage = 18;
        Item.DamageType = DamageClass.Magic;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 2;
        Item.shootSpeed = 7f;
        Item.shoot = ModContent.ProjectileType<GasCloud>();
        Item.noMelee = true;
        Item.autoReuse = true;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool CanUseItem(Player player)
    {
        Item.useTime = Item.useAnimation = player.altFunctionUse == 2 ? 24 : 2;
        return true;
    }

    public override Vector2? HoldoutOffset() => new Vector2(-8, 0);

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer)
            return false;
        if (player.altFunctionUse == 2)
        {
            if (player.controlUp)
                tileCollision = !tileCollision;
            else if (player.controlDown)
                buoyancy = !buoyancy;
            else if (Main.keyState.IsKeyDown(Keys.LeftControl) || Main.keyState.IsKeyDown(Keys.RightControl))
                turbulence = !turbulence;
            else
                kind = (GasKind)(((int)kind + 1) % 3);
            SoundEngine.PlaySound(SoundID.MenuTick, player.Center);
            Main.NewText(Status(), new Color(145, 225, 235));
            return false;
        }

        Vector2 muzzle = position + velocity.SafeNormalize(Vector2.UnitX * player.direction) * 38f;
        if (tileCollision && !Collision.CanHitLine(position, 1, 1, muzzle, 12, 12))
            muzzle = position;
        float hue = (Main.GameUpdateCount % 420) / 420f;
        Color color = Main.hslToRgb(hue, 0.78f, 0.62f);
        GasSettings preset = GasSettings.For(kind);
        GasSettings settings = preset with
        {
            Color = kind == GasKind.Sapfog ? preset.Color : color,
            EmissionTicks = 2,
            TileCollision = tileCollision,
            Buoyancy = buoyancy,
            Turbulence = turbulence
        };
        GasUtil.Emit(source, muzzle, velocity, settings, damage, player.whoAmI);
        if (Main.GameUpdateCount % 12 < 6)
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.2f, Pitch = 0.35f, MaxInstances = 1 }, muzzle);
        return false;
    }

    private string Status() => string.Format(this.GetLocalizedText("Status"),
        this.GetLocalizedText(kind.ToString()), Switch(tileCollision), Switch(buoyancy), Switch(turbulence));

    private string Switch(bool value) => this.GetLocalizedText(value ? "On" : "Off");

    public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.Add(new TooltipLine(Mod, "GasSettings", Status()) { OverrideColor = new Color(145, 225, 235) });

    public override void SaveData(TagCompound tag)
    {
        tag["gasKind"] = (byte)kind;
        tag["tileCollision"] = tileCollision;
        tag["buoyancy"] = buoyancy;
        tag["turbulence"] = turbulence;
    }

    public override void LoadData(TagCompound tag)
    {
        kind = (GasKind)(tag.GetByte("gasKind") % 3);
        tileCollision = !tag.ContainsKey("tileCollision") || tag.GetBool("tileCollision");
        buoyancy = !tag.ContainsKey("buoyancy") || tag.GetBool("buoyancy");
        turbulence = !tag.ContainsKey("turbulence") || tag.GetBool("turbulence");
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write((byte)kind);
        writer.Write(tileCollision);
        writer.Write(buoyancy);
        writer.Write(turbulence);
    }

    public override void NetReceive(BinaryReader reader)
    {
        kind = (GasKind)(reader.ReadByte() % 3);
        tileCollision = reader.ReadBoolean();
        buoyancy = reader.ReadBoolean();
        turbulence = reader.ReadBoolean();
    }
}
