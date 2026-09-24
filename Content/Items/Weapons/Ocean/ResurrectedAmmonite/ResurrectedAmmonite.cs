using System;
using AerovelenceMod.Common.Systems.Language;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Weapons.Ocean;

public class ResurrectedAmmonite : ModItem
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Ocean/ResurrectedAmmonite/ResurrectedAmmonite";
	
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Resurrected Ammonite", "Summons a reborn ammonite to fight for you")
            .AddName(Language.Spanish, "Ammonite resucitado")
            .AddTooltip(Language.Spanish, "Invoca un ammonite renacido para luchar por ti");
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
    }
	
    public override void SetDefaults()
    {
        Item.width = Item.height = 34;
        Item.damage = 12;
        Item.DamageType = DamageClass.Summon;
        Item.mana = 10;
        Item.knockBack = 1.5f;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<AmmoniteMinion>();
        Item.buffType = ModContent.BuffType<AmmoniteCompanion>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.sellPrice(silver: 35);
        Item.UseSound = SoundID.Item44 with { Volume = .55f };
    }
	
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 2);
        Vector2 offset = Main.MouseWorld - player.Center;
        if (offset.Length() > 600f) offset = offset.SafeNormalize(Vector2.UnitX) * 600f;
        int index = Projectile.NewProjectile(source, player.Center + offset, Vector2.Zero, type, damage, knockback, player.whoAmI);
        if (index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
	
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Seashell, 5).AddIngredient(ItemID.Amber, 2).AddIngredient(ItemID.DesertFossil, 10).AddTile(TileID.WorkBenches).Register();
}

public class AmmoniteCompanion : ModBuff
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Ocean/ResurrectedAmmonite/ResurrectedAmmoniteBuff";
    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = Main.buffNoTimeDisplay[Type] = true;
        LocalizationManager.Bind(DisplayName.Key, DisplayName);
        LocalizationManager.Bind(Description.Key, Description);
        LocalizationManager.RegisterTranslation(DisplayName.Key, "Resurrected Ammonite", "default");
        LocalizationManager.RegisterTranslation(Description.Key, "A little life from a distant sea fihghts for you", "default");
        LocalizationManager.RegisterTranslation(DisplayName.Key, "Ammonite resucitado", "es-ES");
        LocalizationManager.RegisterTranslation(Description.Key, "Una pequeña vida de un mar remoto lucha por ti", "es-ES");
    }
	
    public override void Update(Player player, ref int buffIndex)
    {
        if (!player.dead && player.ownedProjectileCounts[ModContent.ProjectileType<AmmoniteMinion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public class AmmoniteMinion : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Ocean/ResurrectedAmmonite/ResurrectedAmmoniteAmmonite";
    private int age;
	
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }
	
    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 26;
        Projectile.minion = true;
        Projectile.minionSlots = 1f;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }
	
    public override bool? CanDamage() => false;
	
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || !player.HasBuff<AmmoniteCompanion>()) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        age++;
        if (age <= 20 && age % 2 == 0) ShellYeah.BubbleDust(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f));
        if (Vector2.DistanceSquared(player.Center, Projectile.Center) > 1600f * 1600f)
        {
            Projectile.Center = player.Center - Vector2.UnitY * 50f;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }
        NPC target = null;
        if (player.HasMinionAttackTargetNPC)
        {
            NPC npc = Main.npc[player.MinionAttackTargetNPC];
            if (npc.CanBeChasedBy() && Vector2.DistanceSquared(npc.Center, Projectile.Center) < 800f * 800f && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)) target = npc;
        }
        if (target == null)
        {
            float best = 650f * 650f;
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy() && Vector2.DistanceSquared(npc.Center, Projectile.Center) < best && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                { target = npc; best = Vector2.DistanceSquared(npc.Center, Projectile.Center); }
        }
        float bob = MathF.Sin(age * .045f + Projectile.minionPos * 1.7f);
        Vector2 home = player.Center + new Vector2((Projectile.minionPos % 2 == 0 ? -1f : 1f) * (50f + Projectile.minionPos * 16f), -65f + bob * 12f);
        Vector2 goal = target == null ? home : target.Center + new Vector2(Projectile.Center.X < target.Center.X ? -170f : 170f, -70f + bob * 20f);
        Vector2 desired = (goal - Projectile.Center) * .075f;
        if (desired.Length() > 9f) desired = desired.SafeNormalize(Vector2.UnitX) * 9f;
        Projectile.velocity = (Projectile.velocity * 19f + desired) / 20f;
        foreach (Projectile other in Main.ActiveProjectiles)
            if (other.owner == Projectile.owner && other.type == Type && other.whoAmI != Projectile.whoAmI && Vector2.DistanceSquared(other.Center, Projectile.Center) < 32f * 32f)
                Projectile.velocity += (Projectile.Center - other.Center).SafeNormalize(Vector2.UnitX) * .08f;
        Vector2 aim = target == null ? new Vector2(player.direction, 0f) : (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
        Projectile.spriteDirection = aim.X < 0f ? -1 : 1;
        Projectile.rotation = Projectile.velocity.X * .035f;
        if (target == null) { Projectile.ai[0] = Math.Min(Projectile.ai[0], 40f); return; }
        Projectile.ai[0]++;
        if (Projectile.ai[0] >= 90f) Projectile.ai[0] = 0f;
        if (Projectile.ai[0] == 60f || Projectile.ai[0] == 68f || Projectile.ai[0] == 76f)
        {
            Vector2 nozzle = Projectile.Center + aim * 14f;
            SoundEngine.PlaySound(SoundID.Item85 with { Volume = .25f, Pitch = .2f + (Projectile.ai[0] - 60f) * .015f }, nozzle);
            Projectile.velocity -= aim * .9f;
            if (Projectile.owner == Main.myPlayer)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), nozzle, aim * 3.4f, ModContent.ProjectileType<AmmoniteSpiralBubble>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, (Projectile.ai[0] - 60f) / 8f * MathHelper.TwoPi / 3f);
        }
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Math.Min(1f, age / 20f);
        Vector2 center = Projectile.Center - Main.screenPosition;
        Texture2D shell = TextureAssets.Projectile[Type].Value;
        SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        float breath = MathF.Sin(age * .065f) * .025f;
        Vector2 drawScale = new(1f + breath, 1f - breath);
        Vector2 TendrilPoint(float x, float y) => center + (new Vector2(x * Projectile.spriteDirection, y) * drawScale).RotatedBy(Projectile.rotation);
        for (int i = 0; i < 4; i++)
        {
            Vector2 previous = TendrilPoint(9f, 7f);
            for (int j = 1; j <= 7; j++)
            {
                float t = j / 7f;
                Vector2 next = TendrilPoint(9f + 16f * t, 7f + i * 2f + MathF.Sin(age * .07f + i + t * 3f) * t * 5f);
                ShellYeah.Line(previous, next, new Color(230, 173, 122) * fade, 2f - t);
                previous = next;
            }
        }
        Main.EntitySpriteDraw(shell, center, null, Color.Lerp(lightColor, Color.White, .15f) * fade, Projectile.rotation, shell.Size() * .5f, drawScale, flip);
        return false;
    }
	
    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 10; i++) ShellYeah.BubbleDust(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f));
    }
}

public class AmmoniteSpiralBubble : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Ocean/H2OBubble";
    private Vector2 previous;
	
    public static Vector2 SpiralOffset(float age, float phase)
    {
        float radius = Math.Min(65f, 2.5f * (MathF.Exp(Math.Min(age, 90f) * .038f) - 1f));
        return (age * .16f + phase).ToRotationVector2() * radius;
    }
	
    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 120;
        Projectile.penetrate = 1;
    }
	
    public override bool ShouldUpdatePosition() => false;
	
    public override void AI()
    {
        previous = Projectile.Center;
        float t = Projectile.ai[0]++;
        Vector2 movement = Projectile.velocity + SpiralOffset(t + 1f, Projectile.ai[1]) - SpiralOffset(t, Projectile.ai[1]);
        Vector2 allowed = Collision.TileCollision(Projectile.position, movement, Projectile.width, Projectile.height);
        Projectile.Center += allowed;
        if (allowed != movement) { Projectile.Kill(); return; }
        Projectile.rotation += .05f;
        if (Projectile.timeLeft % 8 == 0) ShellYeah.BubbleDust(Projectile.Center, -movement * .1f);
        Lighting.AddLight(Projectile.Center, .04f, .12f, .15f);
    }
	
    public override bool? CanDamage() => Projectile.timeLeft > 12 ? null : false;
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previous, Projectile.Center, 14f, ref point);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Math.Min(1f, Projectile.timeLeft / 20f) * Math.Min(1f, Projectile.ai[0] / 6f);
        Texture2D bubble = TextureAssets.Projectile[Type].Value;
        Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow").Value;
        Vector2 center = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(glow, center, null, new Color(50, 200, 230, 0) * (.32f * fade), 0f, glow.Size() * .5f, 38f / glow.Width, SpriteEffects.None);
        Main.EntitySpriteDraw(bubble, center, null, Color.Lerp(lightColor, Color.White, .85f) * fade, Projectile.rotation, bubble.Size() * .5f, .8f, SpriteEffects.None);
        Main.EntitySpriteDraw(bubble, center, null, new Color(110, 230, 255, 0) * (.55f * fade), Projectile.rotation, bubble.Size() * .5f, .9f, SpriteEffects.None);
        return false;
    }
	
    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Item54 with { Volume = .2f, Pitch = .5f }, Projectile.Center);
        for (int i = 0; i < 7; i++) ShellYeah.BubbleDust(Projectile.Center, Main.rand.NextVector2Circular(2.5f, 2.5f));
    }
}

internal static class ShellYeah
{
    internal static void Line(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(), new Vector2(0f, .5f), new Vector2(delta.Length() + 1f, width), SpriteEffects.None);
    }
	
    internal static void BubbleDust(Vector2 position, Vector2 velocity)
    {
        if (Main.dedServ) return;
        Dust dust = Dust.NewDustPerfect(position, DustID.Water, velocity, 100, new Color(110, 220, 240), .8f);
        dust.noGravity = true;
    }
}