using System;
using AerovelenceMod.Common.Systems;
using System.IO;
using AerovelenceMod.Common.Systems.Language;
using AerovelenceMod.Common.Utilities;
using AerovelenceMod.Content.NPCs.Bosses.CrystalTumbler;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace AerovelenceMod.Content.Items.Weapons.BossDrops.CrystalTumbler;

public class Staticstring : ModItem
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/BossDrops/CrystalTumbler/Staticstring/Staticstring";
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Staticstring", "Hold to draw the bow; release to fire an arrow\nFully drawn shots launch ball lightning that pursues and zaps your nearest arrow")
            .AddName(Language.Spanish, "Cuerda estática")
            .AddTooltip(Language.Spanish, "Mantén pulsado para tensar el arco; suelta para disparar\nLos disparos completamente tensados lanzan una esfera que persigue y electrifica tu flecha más cercana")
            .AddSkillStrike(Language.Default, "Ball lightning Skill Strikes")
            .AddSkillStrike(Language.Spanish, "Ataques de habilidad de rayo globular");
    }
	
    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 56;
        Item.damage = 21;
        Item.DamageType = DamageClass.Ranged;
        Item.useTime = Item.useAnimation = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = true;
        Item.noUseGraphic = Item.noMelee = true;
        Item.useAmmo = AmmoID.Arrow;
        Item.shoot = ProjectileID.WoodenArrowFriendly;
        Item.shootSpeed = 10f;
        Item.knockBack = 2f;
        Item.rare = ItemRarityID.Green;
        Item.value = Item.sellPrice(gold: 1);
    }
	
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[ModContent.ProjectileType<StaticstringHeld>()] == 0;
	
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        int index = Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<StaticstringHeld>(), damage, knockback, player.whoAmI, type, 0f, velocity.ToRotation());
        if (index < Main.maxProjectiles && Main.projectile[index].ModProjectile is StaticstringHeld bow)
        {
            bow.ArrowSpeed = velocity.Length();
            Main.projectile[index].netUpdate = true;
        }
        return false;
    }
}

public class StaticstringHeld : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/BossDrops/CrystalTumbler/Staticstring/Staticstring";
    public const float DrawTime = 60f;
    public float ArrowSpeed = 10f;
    private bool released;
    private int releaseAge;
    private float Charge => MathHelper.Clamp(Projectile.ai[1] / DrawTime, 0f, 1f);
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(ArrowSpeed); writer.Write(released); writer.Write(releaseAge); }
    public override void ReceiveExtraAI(BinaryReader reader) { ArrowSpeed = reader.ReadSingle(); released = reader.ReadBoolean(); releaseAge = reader.ReadInt32(); }
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
        Projectile.DamageType = DamageClass.Ranged;
    }
	
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
	
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.CCed || player.noItems || player.HeldItem.type != ModContent.ItemType<Staticstring>()) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        if (!released)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                float aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction).ToRotation();
                if (Math.Abs(MathHelper.WrapAngle(aim - Projectile.ai[2])) > .025f || (int)Projectile.ai[1] % 12 == 0) Projectile.netUpdate = true;
                Projectile.ai[2] = aim;
            }
            Projectile.ai[1] = Math.Min(DrawTime, Projectile.ai[1] + 1f);
            if (Projectile.ai[1] == DrawTime - 1f)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = .4f, Pitch = .45f }, Projectile.Center);
                for (int i = 0; i < 12; i++) TumblerVFX.SpawnSpark(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f), Color.LightCyan, .25f);
            }
        }
        Vector2 direction = Projectile.ai[2].ToRotationVector2();
        Projectile.rotation = Projectile.ai[2];
        Projectile.Center = player.MountedCenter + direction * 24f + new Vector2(0f, player.gfxOffY);
        player.ChangeDir(direction.X < 0f ? -1 : 1);
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        player.itemRotation = Projectile.rotation + (player.direction < 0 ? MathHelper.Pi : 0f);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        player.SetCompositeArmBack(true, Charge > .5f ? Player.CompositeArmStretchAmount.Quarter : Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation - MathHelper.PiOver2);
        if (!released && !player.channel && Projectile.owner == Main.myPlayer)
        {
            released = true;
            Projectile.netUpdate = true;
            Vector2 muzzle = Projectile.Center + direction * 12f;
            if (!Collision.CanHitLine(player.MountedCenter, 1, 1, muzzle, 1, 1)) muzzle = player.MountedCenter;
            int damage = Math.Max(1, (int)(Projectile.damage * MathHelper.Lerp(.55f, 1.3f, Charge)));
            int index = Projectile.NewProjectile(Projectile.GetSource_FromAI(), muzzle, direction * ArrowSpeed * MathHelper.Lerp(.65f, 1.5f, Charge), (int)Projectile.ai[0], damage, Projectile.knockBack, Projectile.owner);
            if (Charge >= 1f && index < Main.maxProjectiles)
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), muzzle, direction * Math.Min(7f, ArrowSpeed * .65f), ModContent.ProjectileType<StaticstringBall>(), Projectile.damage * 2, Projectile.knockBack * 2f, Projectile.owner, Main.projectile[index].identity);
            SoundEngine.PlaySound(SoundID.Item5 with { Volume = .7f, Pitch = -.15f + Charge * .25f }, Projectile.Center);
        }
        if (released && ++releaseAge >= 12) Projectile.Kill();
        if (!released && Charge > .4f && Main.GameUpdateCount % 5 == 0)
            TumblerVFX.SpawnSpark(Projectile.Center + new Vector2(-8f - Charge * 10f, Main.rand.NextFloat(-15f, 15f)).RotatedBy(Projectile.rotation), direction * .5f, Color.Cyan, .15f + Charge * .08f);
        Lighting.AddLight(Projectile.Center, new Vector3(.06f, .2f, .28f) * Charge);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D bow = TextureAssets.Projectile[Type].Value;
        Texture2D thread = ModContent.Request<Texture2D>(Texture + "String").Value;
        Vector2 center = Projectile.Center - Main.screenPosition;
        float fade = released ? 1f - releaseAge / 12f : 1f;
        float pull = released ? Charge * MathF.Exp(-releaseAge * .45f) * MathF.Cos(releaseAge * 1.3f) : Charge;
        Main.EntitySpriteDraw(bow, center, null, lightColor * fade, Projectile.rotation, bow.Size() * .5f, 1f, SpriteEffects.None);
        for (int y = 12; y < 44; y += 2)
        {
            float across = (y - 12f) / 32f;
            float bend = 1f - Math.Abs(across * 2f - 1f);
            Vector2 offset = new Vector2(2f - bow.Width * .5f - 11f * pull * bend, y - bow.Height * .5f);
            Vector2 next = new Vector2(offset.X - 11f * pull * ((1f - Math.Abs((across + 2f / 32f) * 2f - 1f)) - bend), offset.Y + 2f);
            Vector2 delta = next - offset;
            Vector2 point = center + offset.RotatedBy(Projectile.rotation);
            Color color = Color.Lerp(new Color(95, 145, 210), Color.LightCyan, Charge * (.7f + .3f * MathF.Sin(across * 6f - Main.GlobalTimeWrappedHourly * 7f)));
            float rotation = Projectile.rotation + delta.ToRotation() - MathHelper.PiOver2;
            Rectangle source = new(2, y, 2, 2);
            Vector2 scale = new(1f, delta.Length() / 2f);
            Main.EntitySpriteDraw(thread, point, source, color * fade, rotation, Vector2.Zero, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(thread, point, source, TumblerVFX.Glow(color, Charge * .75f * fade), rotation, Vector2.Zero, scale, SpriteEffects.None);
        }
        if (!released)
        {
            int arrowType = (int)Projectile.ai[0];
            if (arrowType > 0 && arrowType < TextureAssets.Projectile.Length)
            {
                Main.instance.LoadProjectile(arrowType);
                Texture2D arrow = TextureAssets.Projectile[arrowType].Value;
                Rectangle frame = arrow.Frame(1, Math.Max(1, Main.projFrames[arrowType]));
                Main.EntitySpriteDraw(arrow, center + new Vector2(-6f - pull * 11f, 0f).RotatedBy(Projectile.rotation), frame, Color.Lerp(lightColor, Color.LightCyan, Charge * .4f), Projectile.rotation + MathHelper.PiOver2, frame.Size() * .5f, 1f, SpriteEffects.None);
            }
        }
        return false;
    }
}

public class StaticstringBall : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/NPCs/Bosses/CrystalTumbler/TumblerOrb";
    private int struckNPC = -1;
    private readonly StaticstringLightning[] surface = { new(), new() };
    public override void SetStaticDefaults() => Main.projFrames[Type] = 4;
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 28;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 210;
        Projectile.netImportant = true;
    }
	
    public static bool IsArrow(Projectile candidate, int owner, int trackedIdentity) => candidate.active && candidate.owner == owner && candidate.friendly && (candidate.arrow || candidate.identity == trackedIdentity) && candidate.velocity.LengthSquared() > .01f;
	
    public override void AI()
    {
        Projectile.ai[1]++;
        Projectile.frame = (int)Projectile.ai[1] / 7 % 4;
        Projectile.rotation += .075f;
        Projectile nearest = null;
        float distance = 620f * 620f;
        foreach (Projectile arrow in Main.ActiveProjectiles)
        {
            if (arrow.whoAmI == Projectile.whoAmI || !IsArrow(arrow, Projectile.owner, (int)Projectile.ai[0])) continue;
            float separation = Vector2.DistanceSquared(arrow.Center, Projectile.Center);
            if (separation < distance) { nearest = arrow; distance = separation; }
        }
        if (nearest != null)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.ai[0] != nearest.identity) { Projectile.ai[0] = nearest.identity; Projectile.netUpdate = true; }
            Vector2 behind = nearest.Center - nearest.velocity.SafeNormalize(Vector2.UnitX) * 30f;
            float speed = Math.Min(7f, nearest.velocity.Length() * .7f);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, (behind - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed, .055f);
            if ((int)Projectile.ai[1] % 18 == 0 && Projectile.owner == Main.myPlayer && Collision.CanHitLine(Projectile.Center, 1, 1, nearest.Center, 1, 1))
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, nearest.Center - Projectile.Center, ModContent.ProjectileType<StaticstringZap>(), Math.Max(1, Projectile.damage / 3), .5f, Projectile.owner);
                SoundEngine.PlaySound(SoundID.Item93 with { Volume = .18f, Pitch = .4f }, Projectile.Center);
            }
        }
        if ((int)Projectile.ai[1] % 3 == 0) TumblerVFX.SpawnSpark(Projectile.Center + Main.rand.NextVector2Circular(13f, 13f), -Projectile.velocity * .25f, Color.LightCyan, .22f);
        for (int i = 0; i < surface.Length; i++)
        {
            float angle = Projectile.ai[1] * .14f + i * MathHelper.Pi;
            surface[i].Update(Projectile, Projectile.Center + angle.ToRotationVector2() * 14f, Projectile.Center + (angle + 1.5f).ToRotationVector2() * 18f, .3f);
        }
        Lighting.AddLight(Projectile.Center, .12f, .5f, .7f);
    }
	
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => SkillStrikeUtil.setSkillStrike(Projectile, 1.6f);
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => struckNPC = target.whoAmI;
	
    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<StaticstringBurst>(), Projectile.damage, Projectile.knockBack, Projectile.owner, struckNPC);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Rectangle frame = texture.Frame(1, 4, 0, Projectile.frame);
        Vector2 position = Projectile.Center - Main.screenPosition;
        float fade = Math.Min(1f, Projectile.ai[1] / 8f);
        Main.EntitySpriteDraw(texture, position, frame, TumblerVFX.Glow(Color.White, fade * .8f), Projectile.rotation, frame.Size() * .5f, .42f, SpriteEffects.None);
        foreach (StaticstringLightning arc in surface) arc.Draw(Main.spriteBatch, Color.Aqua, fade * .65f, 1.2f);
        return false;
    }
}

public class StaticstringZap : ModProjectile
{
    public override string Texture => "AerovelenceMod/Assets/Orbs/SoftGlow";
    private readonly StaticstringLightning lightning = new();
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 14;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
	
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.timeLeft >= 11 ? null : false;
    public override void AI() => lightning.Update(Projectile, Projectile.Center, Projectile.Center + Projectile.velocity, .65f, true);
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float distance = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + Projectile.velocity, 7f, ref distance);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        lightning.Draw(Main.spriteBatch, Color.Cyan, Projectile.timeLeft / 14f, 1.7f);
        return false;
    }
}

public class StaticstringBurst : ModProjectile
{
    public override string Texture => "AerovelenceMod/Assets/Orbs/SoftGlow";
    private readonly StaticstringLightning[] arcs = { new(), new(), new(), new(), new(), new() };
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 128;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 24;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
	
    public override bool? CanDamage() => Projectile.timeLeft >= 19 ? null : false;
    public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[0] ? false : null;
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(closest, Projectile.Center) <= 64f * 64f && Collision.CanHitLine(Projectile.Center, 1, 1, closest, 1, 1);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => SkillStrikeUtil.setSkillStrike(Projectile, 1.6f);
	
    public override void AI()
    {
        float progress = 1f - Projectile.timeLeft / 24f;
        for (int i = 0; i < arcs.Length; i++)
            arcs[i].Update(Projectile, Projectile.Center, Projectile.Center + (i * MathHelper.TwoPi / arcs.Length + Projectile.identity).ToRotationVector2() * MathHelper.Lerp(28f, 80f, progress), .7f, true);
        if (Projectile.timeLeft != 24 || Main.dedServ) return;
        SoundEngine.PlaySound(SoundID.Item93 with { Volume = .55f, Pitch = -.25f }, Projectile.Center);
        for (int i = 0; i < 25; i++) TumblerVFX.SpawnSpark(Projectile.Center, Main.rand.NextVector2Circular(7f, 7f), i % 3 == 0 ? Color.White : Color.Cyan, .35f);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Projectile.timeLeft / 24f;
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, TumblerVFX.Glow(Color.LightCyan, fade * fade * .8f), 0f, texture.Size() * .5f, (160f - fade * 80f) / texture.Width, SpriteEffects.None);
        foreach (StaticstringLightning arc in arcs) arc.Draw(Main.spriteBatch, Color.Cyan, fade, 2f);
        return false;
    }
}

internal sealed class StaticstringLightning
{
    private readonly Vector2[] points = new Vector2[12];
    private readonly Vector2[][] forks = { new Vector2[4], new Vector2[4] };
    private readonly float[] offsets = new float[12];
    private int ticks;
    private bool ready;
    private Color sparkColor = Color.Aqua;
	
    public void Update(Projectile owner, Vector2 start, Vector2 end, float intensity = .55f, bool unused = false)
    {
        if (Main.dedServ) return;
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < 1f) { ready = false; return; }
        ticks++;
        Vector2 tangent = delta / length;
        Vector2 normal = new(-tangent.Y, tangent.X);
        float amplitude = Math.Min(11f, length * .075f) * (.55f + intensity);
        for (int i = 0; i < points.Length; i++)
        {
            float t = i / (float)(points.Length - 1);
            if (!ready || ticks % 3 == 0) offsets[i] = Main.rand.NextFloat(-1f, 1f) * amplitude * MathF.Sin(t * MathHelper.Pi);
            float flicker = MathF.Sign(MathF.Sin(ticks * .8f + i * 2.7f)) * amplitude * .12f * MathF.Sin(t * MathHelper.Pi);
            points[i] = Vector2.Lerp(start, end, t) + normal * (offsets[i] + flicker);
        }
        for (int b = 0; b < forks.Length; b++)
        {
            int root = b == 0 ? 3 : 7;
            forks[b][0] = points[root];
            Vector2 direction = tangent.RotatedBy((b == 0 ? -1f : 1f) * (.6f + .15f * MathF.Sin(ticks * .3f)));
            for (int i = 1; i < forks[b].Length; i++)
                forks[b][i] = forks[b][0] + direction * (i * Math.Min(8f, length * .035f)) + normal * (MathF.Sin(ticks * .9f + i * 4.1f + b) * 2f);
        }
        ready = true;
        if (ticks % 3 == 0)
        {
            Vector2 point = points[Main.rand.Next(1, points.Length - 1)];
            TumblerVFX.SpawnSpark(point, tangent * Main.rand.NextFloat(.5f, 2f) + normal * Main.rand.NextFloat(-1f, 1f), sparkColor, .18f);
            Dust dust = Dust.NewDustPerfect(point, DustID.Electric, normal * Main.rand.NextFloat(-1f, 1f), 80, Color.White, .65f);
            dust.noGravity = true;
        }
    }
	
    public void Draw(SpriteBatch spriteBatch, Color color, float opacity, float width = 2f)
    {
        if (!ready || Main.dedServ || opacity <= 0f) return;
        sparkColor = color;
        Vector2[] path = (Vector2[])points.Clone();
        Vector2[][] branches = { (Vector2[])forks[0].Clone(), (Vector2[])forks[1].Clone() };
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Trails/Clear/GlowTrailSlice").Value;
        Texture2D star = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Pixel/CrispStarPMA").Value;
        float pulse = .8f + .2f * MathF.Sin(ticks * .9f);
        float phase = ticks * .1f;
        Color tint = Color.Lerp(color, Color.White, .28f);
        Color core = Color.Lerp(Color.LightGoldenrodYellow, Color.White, .55f);
        PixellationSystem.QueuePixelationAction(() =>
        {
            void Stroke(Vector2[] vertices, float strength, float thickness, bool bloom)
            {
                for (int i = 0; i < vertices.Length - 1; i++)
                {
                    Vector2 point = (vertices[i] - Main.screenPosition) * .5f;
                    Vector2 delta = (vertices[i + 1] - vertices[i]) * .5f;
                    float rotation = delta.ToRotation();
                    float length = delta.Length();
                    if (bloom)
                        Main.spriteBatch.Draw(glow, point, null, tint * (opacity * strength * .3f), rotation, new Vector2(0f, glow.Height * .5f), new Vector2((length + 1f) / glow.Width, thickness * 6f / glow.Height), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(pixel, point, new Rectangle(0, 0, 1, 1), tint * (opacity * strength * .4f), rotation, new Vector2(0f, .5f), new Vector2(length + .5f, thickness * 2f), SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(pixel, point, new Rectangle(0, 0, 1, 1), core * (opacity * strength * pulse), rotation, new Vector2(0f, .5f), new Vector2(length + .5f, Math.Max(.5f, thickness * .55f)), SpriteEffects.None, 0f);
                }
            }
            Stroke(path, 1f, width, true);
            foreach (Vector2[] branch in branches) Stroke(branch, .5f, width * .55f, false);
            for (int i = 0; i < 2; i++)
            {
                Vector2 point = (path[i == 0 ? 0 : path.Length - 1] - Main.screenPosition) * .5f;
                Main.spriteBatch.Draw(star, point, null, tint * (opacity * .65f), phase * (i == 0 ? 1f : -1f), star.Size() * .5f, 7f / star.Width, SpriteEffects.None, 0f);
            }
        }, PixellationSystem.RenderType.Additive);
    }
}