using System;
using System.Collections.Generic;
using System.IO;
using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Projectiles.Gas;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace AerovelenceMod.Content.Items.Tools;

public sealed class GasVfxWand : TranslatableModItem
{
    private GasVisualStyle style;
    public override string Texture => "Terraria/Images/Item_" + ItemID.RainbowRod;

    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Gas VFX Wand", "Left-click spawns selected gas type\nRight-click to cycle visual style\nUp + right-click to clear gas")
            .AddText(Language.Default, "Status", "Visual style: {0}")
            .AddText(Language.Default, "GroundMist", "Ground mist")
            .AddText(Language.Default, "Steam", "Steam")
            .AddText(Language.Default, "Wispies", "Wispies")
            .AddText(Language.Default, "Embers", "Embers")
            .AddText(Language.Default, "Nebula", "Nebula");
    }

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 40;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 24;
        Item.shootSpeed = 1f;
        Item.shoot = ModContent.ProjectileType<GasCloud>();
        Item.noMelee = true;
        Item.rare = ItemRarityID.LightPurple;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer)
            return false;
        if (player.altFunctionUse == 2)
        {
            if (player.controlUp)
                GasUtil.ClearVisuals();
            else
            {
                style = (GasVisualStyle)(((int)style + 1) % Enum.GetValues<GasVisualStyle>().Length);
                Main.NewText(Status(), new Color(145, 225, 235));
            }
            SoundEngine.PlaySound(SoundID.MenuTick, player.Center);
            return false;
        }
        Vector2 target = Main.MouseWorld;
        if (Vector2.DistanceSquared(player.Center, target) > 800f * 800f || Collision.SolidCollision(target - new Vector2(2f), 4, 4))
            return false;
        GasEmitter emitter = GasUtil.CreateEmitter(target, GasSettings.ForVisual(style), GasEmitterSettings.For(style) with { Duration = 720 });
        if (emitter is not null)
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.25f, Pitch = 0.35f, MaxInstances = 1 }, target);
        return false;
    }

    private string Status() => string.Format(this.GetLocalizedText("Status"), this.GetLocalizedText(style.ToString()));

    public override void ModifyTooltips(List<TooltipLine> tooltips) => tooltips.Add(new TooltipLine(Mod, "GasVisualStyle", Status()) { OverrideColor = new Color(145, 225, 235) });

    public override void SaveData(TagCompound tag) => tag["visualStyle"] = (byte)style;

    public override void LoadData(TagCompound tag) => style = (GasVisualStyle)(tag.GetByte("visualStyle") % Enum.GetValues<GasVisualStyle>().Length);

    public override void NetSend(BinaryWriter writer) => writer.Write((byte)style);

    public override void NetReceive(BinaryReader reader) => style = (GasVisualStyle)(reader.ReadByte() % Enum.GetValues<GasVisualStyle>().Length);

    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.DirtBlock).Register();
}