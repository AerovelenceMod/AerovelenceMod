using System;
using System.IO;
using AerovelenceMod.Common.Systems;
using AerovelenceMod.Common.Globals.SkillStrikes;
using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Weapons.Ocean;

public class CoralClaws : ModItem
{
    private int nextSide = -1;
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Ocean/CoralClaws/CoralClaws";
	
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Coral Claws", "Alternating coral claws reach toward your cursor and snip")
            .AddName(Language.Spanish, "Pinzas de coral")
            .AddTooltip(Language.Spanish, "Las pinzas de coral se alternan para alcanzar el cursor y cortar")
            .AddSkillStrike(Language.Default, "Snip enemies almost directly above you")
            .AddSkillStrike(Language.Spanish, "Corta a enemigos situados casi directamente encima de ti");
    }
	
    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 32;
        Item.damage = 19;
        Item.DamageType = DamageClass.Melee;
        Item.knockBack = 4f;
        Item.useTime = Item.useAnimation = 23;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<CoralClawHand>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.sellPrice(silver: 40);
    }
	
    public override void HoldItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer || player.dead || player.CCed || player.noItems) return;
        EnsureClaws(player);
    }
	
    private void EnsureClaws(Player player)
    {
        for (int side = -1; side <= 1; side += 2)
        {
            bool found = false;
            foreach (Projectile projectile in Main.ActiveProjectiles)
                if (projectile.owner == player.whoAmI && projectile.type == Item.shoot && projectile.ai[0] == side && projectile.ai[2] == 0f) { found = true; break; }
            if (!found)
                Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter + new Vector2(side * 42f, -6f), Vector2.Zero, Item.shoot, player.GetWeaponDamage(Item), player.GetWeaponKnockback(Item), player.whoAmI, side);
        }
    }
	
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        EnsureClaws(player);
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == player.whoAmI && projectile.ModProjectile is CoralClawHand claw && projectile.ai[0] == nextSide && projectile.ai[2] == 0f)
            {
                claw.Snip(Main.MouseWorld - player.MountedCenter, damage, knockback, Math.Max(8, player.itemAnimationMax * 3 / 2));
                nextSide *= -1;
                break;
            }
        return false;
    }

}

public class CoralClawsCrabDrop : GlobalNPC
{
    public override bool AppliesToEntity(NPC entity, bool lateInstantiation) => entity.type == NPCID.Crab;
    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot) => npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CoralClaws>(), 4));
}

public class CoralClawHand : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Ocean/CoralClaws/CoralClaws";
    public const float Reach = 190f;
    private Vector2 reachOffset;
    private int duration = 34;
    private int age;
    private float fade = 1f;
    private bool snapped;
    private Vector2 mouth;
    private float Progress => Projectile.ai[1] <= 0 ? 0f : Projectile.ai[1] / duration;
    public static bool Overhead(Vector2 relativeTarget) => relativeTarget.Y < -24f && Math.Abs(relativeTarget.X) <= -relativeTarget.Y * .55f;
    public static bool DamageWindow(float progress) => progress >= .46f && progress <= .63f;
    public static Vector2 ClampReach(Vector2 aim) => aim.LengthSquared() > Reach * Reach ? aim.SafeNormalize(-Vector2.UnitY) * Reach : aim;
	
    public void Snip(Vector2 aim, int damage, float knockback, int ticks)
    {
        reachOffset = ClampReach(aim);
        if (reachOffset.LengthSquared() < 32f * 32f) reachOffset = reachOffset.SafeNormalize(new Vector2(Projectile.ai[0], -1f)) * 32f;
        duration = ticks;
        Projectile.ai[1] = 1f;
        Projectile.damage = damage;
        Projectile.knockBack = knockback;
        Array.Clear(Projectile.localNPCImmunity);
        snapped = false;
        Projectile.netUpdate = true;
        SoundEngine.PlaySound(SoundID.Item1 with { Volume = .5f, Pitch = .15f }, Projectile.Center);
    }
	
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(reachOffset.X); writer.Write(reachOffset.Y); writer.Write(duration);
    }
	
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        reachOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        duration = Math.Max(8, reader.ReadInt32());
        if (Progress < .46f) snapped = false;
    }
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 36;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
	
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.ai[2] == 0f && DamageWindow(Progress) ? null : false;
	
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        age++;
        Projectile.timeLeft = 2;
        if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<CoralClaws>() || player.CCed || player.noItems) Projectile.ai[2] = 1f;
        if (Projectile.ai[2] != 0f)
        {
            fade -= .08f;
            if (fade <= 0f) Projectile.Kill();
            return;
        }
        Vector2 homeOffset = new(Projectile.ai[0] * 44f, -4f + MathF.Sin(age * .06f + Projectile.ai[0]) * 5f);
        float progress = Progress;
        float extension = progress == 0f ? 0f : progress < .46f ? MathHelper.SmoothStep(0f, 1f, progress / .46f) : progress <= .63f ? 1f : MathHelper.SmoothStep(1f, 0f, (progress - .63f) / .37f);
        Vector2 desired = Vector2.Lerp(homeOffset, reachOffset, extension);
        Vector2 direction = desired.SafeNormalize(Vector2.UnitX * Projectile.ai[0]);
        float length = desired.Length();
        for (float distance = 20f; distance < length; distance += 6f)
            if (Collision.SolidCollision(player.MountedCenter + direction * distance - new Vector2(6f), 12, 12))
            { desired = direction * Math.Max(18f, distance - 12f); break; }
        mouth = player.MountedCenter + desired;
        float goalRotation = Projectile.ai[1] == 0f ? (Projectile.ai[0] < 0f ? -2f : -1.14f) : direction.ToRotation();
        Projectile.rotation = Projectile.rotation.AngleLerp(goalRotation, .4f);
        Projectile.Center = mouth - Projectile.rotation.ToRotationVector2() * 20f;
        if (DamageWindow(progress) && !snapped)
        {
            snapped = true;
            bool golden = Overhead(mouth - player.MountedCenter);
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = .55f, Pitch = golden ? .3f : -.15f }, mouth);
            for (int i = 0; i < 12; i++) CoralClawArt.Dust(mouth, Main.rand.NextVector2Circular(3f, 3f), golden);
        }
        if (Projectile.ai[1] > 0f)
        {
            Projectile.ai[1]++;
            if (Projectile.ai[1] > duration) { Projectile.ai[1] = 0f; snapped = false; Projectile.netUpdate = true; }
        }
    }
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Player player = Main.player[Projectile.owner];
        if (!Collision.CanHitLine(player.MountedCenter, 1, 1, targetHitbox.Center.ToVector2(), 1, 1)) return false;
        Vector2 closest = Vector2.Clamp(mouth, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(closest, mouth) <= 24f * 24f;
    }
	
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        Projectile.GetGlobalProjectile<SkillStrikeGProj>().SkillStrike = false;
        if (Overhead(target.Center - Main.player[Projectile.owner].MountedCenter)) SkillStrikeUtil.setSkillStrike(Projectile, 1.75f, 1, .45f, .7f);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        float opacity = Math.Min(1f, age / 12f) * fade;
        float progress = Progress;
        float open = progress == 0f ? .27f : progress < .46f ? MathHelper.Lerp(.27f, .95f, progress / .46f) : progress < .57f ? MathHelper.Lerp(.95f, .02f, (progress - .46f) / .11f) : MathHelper.Lerp(.02f, .27f, (progress - .57f) / .43f);
        Vector2 start = player.MountedCenter + new Vector2(Projectile.ai[0] * 10f, 7f);
        Vector2 end = Projectile.Center;
        ModContent.GetInstance<NewPixelationSystem>().QueueRenderAction(RenderLayer.UnderProjectiles, () =>
        {
            Vector2 previous = start;
            for (int i = 1; i <= 24; i++)
            {
                float t = i / 24f;
                Vector2 next = Vector2.Lerp(start, end, t) + Vector2.UnitY * MathF.Sin(t * MathHelper.Pi) * 14f;
                Color coral = Color.Lerp(new Color(102, 65, 110), new Color(245, 138, 140), t);
                CoralClawArt.Line(previous - Main.screenPosition, next - Main.screenPosition, coral * opacity, 5f);
                CoralClawArt.Line(previous - Main.screenPosition, next - Main.screenPosition, new Color(255, 210, 170, 0) * (.22f * opacity), 2f);
                previous = next;
            }
        });
        bool golden = Projectile.ai[1] > 0f && Overhead(mouth - player.MountedCenter);
        CoralClawArt.Draw(Projectile.Center - Main.screenPosition, Projectile.rotation, open, 1f, opacity, lightColor, golden);
        if (DamageWindow(progress))
        {
            Texture2D star = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/CrispStarPMA").Value;
            float flash = MathF.Sin((progress - .46f) / .17f * MathHelper.Pi);
            Main.EntitySpriteDraw(star, mouth - Main.screenPosition, null, (golden ? new Color(255, 215, 90, 0) : new Color(145, 255, 239, 0)) * flash, Projectile.rotation, star.Size() * .5f, new Vector2(.7f, .3f), SpriteEffects.None);
        }
        return false;
    }
	
    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 5; i++) CoralClawArt.Dust(Projectile.Center, Main.rand.NextVector2Circular(1f, 1f), false);
    }
}

internal static class CoralClawArt
{
    internal static void Line(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(), new Vector2(0f, .5f), new Vector2(delta.Length() + 1f, width), SpriteEffects.None);
    }
	
    internal static void Draw(Vector2 center, float rotation, float open, float scale, float fade, Color light, bool golden)
    {
        const string path = "AerovelenceMod/Content/Items/Weapons/Ocean/CoralClaws/CoralClaws";
        Texture2D body = ModContent.Request<Texture2D>(path + "Base").Value;
        Texture2D upper = ModContent.Request<Texture2D>(path + "Top").Value;
        Texture2D lower = ModContent.Request<Texture2D>(path + "Bottom").Value;
        Color tint = Color.Lerp(light, Color.White, .18f) * fade;
        Vector2 origin = new(9f, 16f);
        Vector2 topPivot = new(9f, 12f);
        Vector2 bottomPivot = new(9f, 20f);
        float opening = MathHelper.Clamp(open, 0f, 1f) * .65f;
        Vector2 top = center + (topPivot - origin).RotatedBy(rotation) * scale;
        Vector2 bottom = center + (bottomPivot - origin).RotatedBy(rotation) * scale;
        Main.EntitySpriteDraw(upper, top, null, tint, rotation + .32f - opening, topPivot, scale, SpriteEffects.None);
        Main.EntitySpriteDraw(lower, bottom, null, tint, rotation - .32f + opening, bottomPivot, scale, SpriteEffects.None);
        Main.EntitySpriteDraw(body, center, null, tint, rotation, origin, scale, SpriteEffects.None);
        if (golden)
        {
            Color glow = new Color(255, 205, 85, 0) * (.35f * fade);
            Main.EntitySpriteDraw(upper, top, null, glow, rotation + .32f - opening, topPivot, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(lower, bottom, null, glow, rotation - .32f + opening, bottomPivot, scale, SpriteEffects.None);
        }
    }

    internal static void Dust(Vector2 position, Vector2 velocity, bool golden)
    {
        if (Main.dedServ) return;
        Dust dust = Terraria.Dust.NewDustPerfect(position, golden ? DustID.GoldFlame : DustID.Water, velocity, 90, golden ? Color.Gold : Color.Aquamarine, .85f);
        dust.noGravity = true;
    }
}