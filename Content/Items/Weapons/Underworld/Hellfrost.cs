using System;
using System.IO;
using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Weapons.Underworld;

public class Hellfrost : ModItem
{
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Hellfrost", "Hold to swing; release to throw\nImpacts erupt in fire, frost, and steam\nHas a chance to inflict Frostburn")
            .AddName(Language.Spanish, "Escarcha infernal")
            .AddTooltip(Language.Spanish, "Mantén pulsado para girar; suelta para lanzar\nLos impactos estallan en fuego, escarcha y vapor\nPuede infligir Quemadura gélida")
            .AddSkillStrike(Language.Default, "Explosions caused by striking terrain")
            .AddSkillStrike(Language.Spanish, "Las explosiones causadas al golpear el terreno");
    }
	
    public override void SetDefaults()
    {
        Item.width = Item.height = 56;
        Item.damage = 38;
        Item.DamageType = DamageClass.Melee;
        Item.knockBack = 5f;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = true;
        Item.noMelee = Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<HellfrostHead>();
        Item.shootSpeed = 16f;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.sellPrice(gold: 2);
    }
	
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.HellstoneBar, 15).AddIngredient(ItemID.IceBlock, 50).AddIngredient(ItemID.Shiverthorn, 3).AddTile(TileID.Anvils).Register();
}

public class HellfrostHead : ModProjectile
{
    private int age;
    private int impactCooldown;
    private Vector2 previousCenter;
    private float fade = 1f;
    private float swingAngle;
    private int spinDirection;
    private float angularSpeed = .12f;
    private float swingRadius = 18f;
    public static float ReleaseAngle(float aim, int direction) => MathF.Atan2(-MathF.Cos(aim) * direction, MathF.Sin(aim) * direction / .8f);
    public static float ForwardArc(float start, float end, int direction) => ((end - start) * direction % MathHelper.TwoPi + MathHelper.TwoPi) % MathHelper.TwoPi;
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(swingAngle); writer.Write(spinDirection); writer.Write(angularSpeed); writer.Write(swingRadius); }
    public override void ReceiveExtraAI(BinaryReader reader) { swingAngle = reader.ReadSingle(); spinDirection = reader.ReadInt32(); angularSpeed = reader.ReadSingle(); swingRadius = reader.ReadSingle(); }
    public const float MaximumReach = 340f;
    public static bool ImpactReady(int cooldown, float speed) => cooldown <= 0 && speed >= 3f;
	
    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 34;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 24;
    }
	
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.ai[0] == 3f || age < 6 ? false : null;
	
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        previousCenter = Projectile.Center;
        age++;
        if (spinDirection == 0) spinDirection = player.direction;
        impactCooldown = Math.Max(0, impactCooldown - 1);
        Projectile.timeLeft = 2;
        if (!player.active || player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<Hellfrost>()) Projectile.ai[0] = 3f;
        if (Projectile.ai[0] == 3f)
        {
            fade -= .12f;
            if (fade <= 0f) Projectile.Kill();
            return;
        }
        Vector2 hand = player.MountedCenter;
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        float attackSpeed = MathHelper.Clamp(player.GetTotalAttackSpeed(DamageClass.Melee), .5f, 2.5f);
        if (Projectile.ai[0] == 0f)
        {
            Projectile.ai[1]++;
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aim = (Main.MouseWorld - hand).SafeNormalize(Vector2.UnitX * player.direction);
                player.ChangeDir(aim.X < 0f ? -1 : 1);
                Projectile.ai[2] = aim.ToRotation();
                if (!player.channel)
                {
                    Projectile.ai[0] = 4f;
                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = .7f, Pitch = -.3f }, Projectile.Center);
                }
                else if (age % 12 == 0) Projectile.netUpdate = true;
            }
            if (Projectile.ai[0] == 0f)
            {
                swingRadius = MathHelper.Lerp(swingRadius, 65f, .09f);
                angularSpeed = MathHelper.Lerp(angularSpeed, .26f * attackSpeed, .075f);
                float radius = swingRadius;
                swingAngle += angularSpeed * spinDirection;
                float angle = swingAngle;
                Vector2 desired = hand + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * .8f);
                MoveWithCollision(desired - Projectile.Center, false);
                if (age % 28 == 0) SoundEngine.PlaySound(new SoundStyle("AerovelenceMod/Sounds/Effects/Flail1") with { Volume = .3f, PitchVariance = .12f }, Projectile.Center);
            }
        }
        if (Projectile.ai[0] == 4f)
        {
            float targetAngle = ReleaseAngle(Projectile.ai[2], spinDirection);
            float remaining = ForwardArc(swingAngle, targetAngle, spinDirection);
            angularSpeed = MathHelper.Lerp(angularSpeed, .34f * attackSpeed, .09f);
            swingRadius = MathHelper.Lerp(swingRadius, 65f, .09f);
            float step = Math.Min(remaining, angularSpeed);
            swingAngle += step * spinDirection;
            Vector2 desired = hand + new Vector2(MathF.Cos(swingAngle) * swingRadius, MathF.Sin(swingAngle) * swingRadius * .8f);
            MoveWithCollision(desired - Projectile.Center, false);
            if (remaining <= step + .001f)
            {
                Projectile.ai[0] = 1f;
                Projectile.ai[1] = 0f;
                Vector2 tangent = new Vector2(-MathF.Sin(swingAngle), MathF.Cos(swingAngle) * .8f) * spinDirection;
                Projectile.velocity = tangent * (swingRadius * angularSpeed) + player.velocity * .35f;
                Projectile.ResetLocalNPCHitImmunity();
                Projectile.netUpdate = true;
            }
        }
        else if (Projectile.ai[0] == 1f)
        {
            Projectile.ai[1]++;
            Projectile.velocity.Y += .25f;
            MoveWithCollision(Projectile.velocity, true);
            if (Vector2.DistanceSquared(hand, Projectile.Center) >= MaximumReach * MaximumReach || Projectile.ai[1] > 38f)
            { Projectile.ai[0] = 2f; Projectile.ai[1] = 0f; Projectile.netUpdate = true; }
        }
        else if (Projectile.ai[0] == 2f)
        {
            Projectile.ai[1]++;
            Vector2 delta = hand - Projectile.Center;
            float speed = Math.Min(30f * attackSpeed, 10f + Projectile.ai[1] * 1.2f);
            if (delta.Length() <= speed + 10f)
            { Projectile.ai[0] = 3f; Projectile.netUpdate = true; return; }
            Projectile.velocity = ReturnVelocity(Projectile.velocity, delta, speed, Projectile.ai[1]);
            Projectile.Center += Projectile.velocity;
        }
        if (Vector2.DistanceSquared(hand, Projectile.Center) > 1800f * 1800f) { Projectile.Kill(); return; }
        Projectile.rotation += angularSpeed * spinDirection * (Projectile.ai[0] == 0f || Projectile.ai[0] == 4f ? 1f : .65f);
        player.itemRotation = (Projectile.Center - hand).ToRotation() + (player.direction < 0 ? MathHelper.Pi : 0f);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Projectile.Center - hand).ToRotation() - MathHelper.PiOver2);
        Vector2 movement = Projectile.Center - previousCenter;
        if (age % 2 == 0 && movement.LengthSquared() > 1f)
            HellfrostEffects.Steam(Vector2.Lerp(previousCenter, Projectile.Center, .5f), -movement * .035f - Vector2.UnitY * .7f, .35f);
        Lighting.AddLight(Projectile.Center, .3f, .3f, .4f);
    }
	
    public static Vector2 ReturnVelocity(Vector2 velocity, Vector2 delta, float speed, float ticks)
    {
        if (ticks < 0f) return velocity * .98f;
        Vector2 desired = delta.SafeNormalize(Vector2.UnitX) * speed;
        Vector2 force = (desired - velocity) * .09f;
        float maximumForce = 1.1f + Math.Min(1.3f, ticks * .025f);
        if (force.Length() > maximumForce) force = force.SafeNormalize(Vector2.Zero) * maximumForce;
        return velocity + force;
    }
	
    private void MoveWithCollision(Vector2 movement, bool thrown)
    {
        int steps = Math.Clamp((int)Math.Ceiling(movement.Length() / 6f), 1, 40);
        Vector2 step = movement / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 allowed = Collision.TileCollision(Projectile.position, step, Projectile.width, Projectile.height, true, true);
            Projectile.position += allowed;
            if (Vector2.DistanceSquared(allowed, step) < .001f) continue;
            if (ImpactReady(impactCooldown, movement.Length())) Explode(true);
            if (thrown)
            {
                Projectile.ai[0] = 2f;
                Projectile.ai[1] = -6f;
                Projectile.velocity = new Vector2(Math.Abs(allowed.X - step.X) > .001f ? -movement.X * .45f : movement.X * .85f, Math.Abs(allowed.Y - step.Y) > .001f ? -movement.Y * .45f : movement.Y * .85f);
                Projectile.netUpdate = true;
            }
            break;
        }
    }
	
    private void Explode(bool terrain)
    {
        if (impactCooldown > 0) return;
        impactCooldown = 24;
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HellfrostBurst>(), Math.Max(1, (int)(Projectile.damage * .7f)), 4f, Projectile.owner, terrain ? 1f : 0f);
    }
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (!Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.Center.ToVector2(), 1, 1)) return false;
        float distance = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previousCenter, Projectile.Center, 28f, ref distance);
    }
	
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.rand.NextBool(3)) target.AddBuff(BuffID.Frostburn, 240);
        Explode(false);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        Texture2D chain = ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Weapons/Underworld/HellfrostChain").Value;
        Vector2 direction = player.MountedCenter - Projectile.Center;
        float length = direction.Length();
        Vector2 unit = direction.SafeNormalize(Vector2.UnitY);
        int count = Math.Min(160, (int)(length / Math.Max(1, chain.Height - 2)) + 1);
        for (int i = 0; i < count; i++)
        {
            Vector2 point = Projectile.Center + unit * (i * (chain.Height - 2));
            Main.EntitySpriteDraw(chain, point - Main.screenPosition, null, Lighting.GetColor(point.ToTileCoordinates()) * fade, unit.ToRotation() + MathHelper.PiOver2, chain.Size() * .5f, 1f, SpriteEffects.None);
        }
        Texture2D head = TextureAssets.Projectile[Type].Value;
        Main.EntitySpriteDraw(head, Projectile.Center - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, .25f) * fade, Projectile.rotation, head.Size() * .5f, 1f, SpriteEffects.None);
        return false;
    }
	
    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 4; i++) HellfrostEffects.Steam(Projectile.Center, Main.rand.NextVector2Circular(1f, 1f) - Vector2.UnitY, .3f);
    }
}

public class HellfrostBurst : ModProjectile
{
    public override string Texture => "AerovelenceMod/Assets/Orbs/SoftGlow";
    public const float Radius = 72f;
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 148;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 26;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
	
    public override bool? CanDamage() => Projectile.timeLeft >= 20 ? null : false;
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(closest, Projectile.Center) <= Radius * Radius && Collision.CanHitLine(Projectile.Center, 1, 1, closest, 1, 1);
    }
	
    public override void AI()
    {
        if (Projectile.timeLeft == 26 && !Main.dedServ)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .45f, Pitch = .15f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Shatter with { Volume = .35f, Pitch = -.15f }, Projectile.Center);
            float distance = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
            float shake = 5f * MathHelper.Clamp(1f - distance / 850f, 0f, 1f);
            AeroPlayer camera = Main.LocalPlayer.GetModPlayer<AeroPlayer>();
            camera.ScreenShakePower = Math.Max(camera.ScreenShakePower, shake);
            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = (i * MathHelper.TwoPi / 28f).ToRotationVector2() * Main.rand.NextFloat(2f, 6f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, i % 2 == 0 ? DustID.Torch : DustID.IceTorch, velocity, 30, default, 1.4f);
                dust.noGravity = true;
                if (i % 3 == 0) HellfrostEffects.Steam(Projectile.Center, velocity * .5f - Vector2.UnitY, .55f);
            }
        }
        Lighting.AddLight(Projectile.Center, new Vector3(.65f, .4f, .55f) * (Projectile.timeLeft / 26f));
    }
	
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Projectile.ai[0] == 1f) SkillStrikeUtil.setSkillStrike(Projectile, 1.75f, 1, .4f, .7f);
    }
	
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.rand.NextBool(3)) target.AddBuff(BuffID.Frostburn, 240);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        float progress = 1f - Projectile.timeLeft / 26f;
        float fade = 1f - progress;
        Vector2 center = Projectile.Center - Main.screenPosition;
        Texture2D glow = TextureAssets.Projectile[Type].Value;
        for (int side = -1; side <= 1; side += 2)
        {
            Color color = side < 0 ? new Color(255, 100, 35, 0) : new Color(90, 200, 255, 0);
            Main.EntitySpriteDraw(glow, center + new Vector2(side * 18f, 0f), null, color * (.28f * fade), 0f, glow.Size() * .5f, 148f / glow.Width, SpriteEffects.None);
            Texture2D smoke = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Smoke/WispSmoke1").Value;
            for (int i = 0; i < 5; i++)
            {
                float angle = i * MathHelper.TwoPi / 5f + Projectile.identity * .7f + side * .25f;
                float spread = MathHelper.SmoothStep(8f, 52f, progress);
                Vector2 point = center + angle.ToRotationVector2() * spread + new Vector2(side * 10f, -progress * 15f);
                float size = MathHelper.Lerp(36f, 90f, progress) * (1f + .13f * MathF.Sin(i * 3f));
                Main.EntitySpriteDraw(smoke, point, null, color * (fade * fade * .38f), angle + side * progress * .65f, smoke.Size() * .5f, size / smoke.Width, SpriteEffects.None);
            }
            Texture2D ring = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Ring/GlowRing").Value;
            Main.EntitySpriteDraw(ring, center, null, color * (fade * fade * .35f), side * progress, ring.Size() * .5f, new Vector2(1f, .8f) * MathHelper.Lerp(18f, Radius * 2f, progress) / ring.Width, SpriteEffects.None);
        }
        Main.EntitySpriteDraw(glow, center, null, new Color(255, 235, 215, 0) * (fade * fade * .25f), 0f, glow.Size() * .5f, 72f / glow.Width, SpriteEffects.None);
        return false;
    }
}

internal static class HellfrostEffects
{
    internal static void Steam(Vector2 position, Vector2 velocity, float scale)
    {
        if (Main.dedServ) return;
        Dust steam = Dust.NewDustPerfect(position, ModContent.DustType<MediumSmoke>(), velocity, 0, new Color(215, 227, 235), scale);
        steam.noGravity = true;
        steam.customData = new MediumSmokeBehavior(36, .97f, .009f, .7f);
    }
}