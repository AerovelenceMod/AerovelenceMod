using System;
using ReLogic.Content;
using AerovelenceMod.Common.Systems;
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

public class TumblerEnrichmentScepter : ModItem
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/BossDrops/CrystalTumbler/ConductorWand/ConductorWand";
	
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Tumbler Enrichment Scepter", "Hold to power an ethereal Tumbler's electric exercise wheel\nConsumes mana continuously; longer spins produce farther-reaching lightning")
            .AddName(Language.Spanish, "Cetro de enriquecimiento del Tumbler")
            .AddTooltip(Language.Spanish, "Mantén pulsado para alimentar la rueda eléctrica de un Tumbler etéreo\nConsume maná continuamente; al acelerar, sus rayos llegan más lejos")
            .AddSkillStrike(Language.Default, "Golden Sparks from a fast wheel Skill Strike")
            .AddSkillStrike(Language.Spanish, "Ataque de habilidad de chispas doradas de una rueda rápida");
        Item.staff[Type] = true;
    }
	
    public override void SetDefaults()
    {
        Item.width = Item.height = 36;
        Item.damage = 22;
        Item.DamageType = DamageClass.Magic;
        Item.mana = 5;
        Item.useTime = Item.useAnimation = 24;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.channel = true;
        Item.noMelee = true;
        Item.knockBack = 2f;
        Item.shoot = ModContent.ProjectileType<EnrichmentWheel>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Master;
        Item.master = true;
        Item.value = Item.sellPrice(gold: 2);
    }
	
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
	
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Vector2 offset = Main.MouseWorld - player.Center;
        if (offset.Length() > 650f) offset = offset.SafeNormalize(Vector2.UnitX) * 650f;
        Vector2 destination = player.Center + offset;
        if (!Collision.CanHitLine(player.Center, 1, 1, destination, 1, 1)) destination = player.Center + new Vector2(player.direction * 70f, -60f);
        Projectile.NewProjectile(source, destination, Vector2.Zero, type, damage, knockback, player.whoAmI);
        return false;
    }
}

public class EnrichmentWheel : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/NPCs/Bosses/CrystalTumbler/CrystalTumbler";
    private int age;
    private float treadAngle;
    private float cloneAngle;
    private bool wasGolden;
    public const float WheelRadius = 112f;
    public const float WheelHalfWidth = 100f;
    public const float BodyRadius = 60f;
    public static float RollingAngleStep(float wheelStep) => wheelStep * WheelHalfWidth / BodyRadius;
    public static float AdvanceSpeed(float speed, bool coasting) => MathHelper.Clamp(coasting ? speed - 1f / 240f : speed + (1f - speed) * .0025f, 0f, 1f);
    public static bool Golden(float speed) => speed >= .65f;
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 248;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }
	
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
	
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        age++;
        if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, Projectile.Center) > 2000f * 2000f)
            Projectile.ai[1] = 1f;
        if (Projectile.owner == Main.myPlayer && Projectile.ai[1] == 0f)
        {
            bool running = player.channel && !player.CCed && !player.noItems && player.HeldItem.type == ModContent.ItemType<TumblerEnrichmentScepter>();
            if (running && age % 15 == 0)
                running = player.CheckMana(player.HeldItem, Math.Max(1, (int)(2f * player.manaCost)), true);
            if (!running)
            {
                Projectile.ai[1] = 1f;
                Projectile.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item92 with { Volume = .35f, Pitch = -.4f }, Projectile.Center);
            }
            else player.manaRegenDelay = player.maxRegenDelay;
        }
        Projectile.ai[0] = AdvanceSpeed(Projectile.ai[0], Projectile.ai[1] != 0f);
        if (Projectile.ai[1] != 0f && Projectile.ai[0] == 0f) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        float speed = Projectile.ai[0];
        float wheelStep = speed * .19f;
        treadAngle += wheelStep;
        cloneAngle += RollingAngleStep(wheelStep);
        if (Projectile.ai[1] == 0f)
        {
            player.itemTime = player.itemAnimation = 2;
            player.ChangeDir(Projectile.Center.X >= player.Center.X ? 1 : -1);
            player.itemRotation = (Projectile.Center - player.MountedCenter).ToRotation() + (player.direction < 0 ? MathHelper.Pi : 0f);
        }
        bool golden = Golden(speed);
        Color color = Color.Lerp(TumblerVFX.PhaseColor(0), TumblerVFX.PhaseColor(1), MathHelper.Clamp((speed - .5f) / .2f, 0f, 1f));
        if (golden && !wasGolden)
        {
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = .5f, Pitch = .3f }, Projectile.Center);
            for (int i = 0; i < 24; i++) TumblerVFX.SpawnSpark(Projectile.Center + (i * MathHelper.TwoPi / 24).ToRotationVector2() * WheelRadius, (i * MathHelper.TwoPi / 24).ToRotationVector2() * 4f, color, .3f);
        }
        wasGolden = golden;
        Lighting.AddLight(Projectile.Center, color.ToVector3() * (.3f + speed * .6f));
        if (age % 6 == 0) TumblerVFX.SpawnSpark(Projectile.Center + new Vector2(Main.rand.NextFloat(-20f, 20f), WheelRadius - 3f), new Vector2(Main.rand.NextFloat(-3f, 3f), -2f), color);
        if (Projectile.owner != Main.myPlayer || player.dead || !player.active) return;
        if (age % 45 == 0) Projectile.netUpdate = true;
        int interval = (int)MathHelper.Lerp(36f, 12f, speed);
        if (age > 25 && age % interval == 0)
        {
            float reach = 100f + 350f * speed;
            NPC nearest = null;
            float best = reach * reach;
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy() && Vector2.DistanceSquared(npc.Center, Projectile.Center) < best && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                { best = Vector2.DistanceSquared(npc.Center, Projectile.Center); nearest = npc; }
            float angle = nearest == null ? Main.rand.NextFloat(MathHelper.TwoPi) : (nearest.Center - Projectile.Center).ToRotation();
            int count = 1 + (int)(speed * 2.1f);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (angle + (i - (count - 1) * .5f) * .28f).ToRotationVector2();
                Vector2 rim = Projectile.Center + direction * 94f;
                if (!Collision.CanHitLine(Projectile.Center, 1, 1, rim, 1, 1)) rim = Projectile.Center;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), rim, direction, ModContent.ProjectileType<EnrichmentArc>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Math.Max(50f, reach - Vector2.Distance(rim, Projectile.Center)), golden ? 1f : 0f);
            }
            if (age % 3 == 0) SoundEngine.PlaySound(SoundID.Item93 with { Volume = .16f, Pitch = speed * .7f }, Projectile.Center);
        }
        if (golden && age % 12 == 0)
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + new Vector2(Main.rand.NextFloat(-32f, 32f), WheelRadius + 2f), new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), 2f), ModContent.ProjectileType<EnrichmentSpark>(), Projectile.damage, 1f, Projectile.owner);
    }
	
    private void DrawWheel(Color color, float fade, bool foreground)
    {
        Texture2D slice = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Trails/Clear/GlowTrailSlice").Value;
        void Rail(Vector2 start, Vector2 end, float strength, float thickness)
        {
            Vector2 delta = end - start;
            Main.EntitySpriteDraw(slice, start - Main.screenPosition, null, TumblerVFX.Glow(color, fade * strength * .22f), delta.ToRotation(), new Vector2(0f, slice.Height * .5f), new Vector2((delta.Length() + 1f) / slice.Width, 9f / slice.Height), SpriteEffects.None);
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), TumblerVFX.Glow(Color.Lerp(color, Color.White, .4f), fade * strength), delta.ToRotation(), new Vector2(0f, .5f), new Vector2(delta.Length() + .5f, thickness), SpriteEffects.None);
        }
        for (int rim = -1; rim <= 1; rim += 2)
        {
            for (int i = 0; i < 80; i++)
            {
                float angle = i * MathHelper.TwoPi / 80f;
                bool inFront = MathF.Sin(angle) > .45f && rim > 0;
                if (inFront != foreground) continue;
                Vector2 offset = new(rim * 13f, 0f);
                Vector2 start = Projectile.Center + offset + angle.ToRotationVector2() * new Vector2(WheelHalfWidth, WheelRadius);
                Vector2 end = Projectile.Center + offset + (angle + MathHelper.TwoPi / 80f).ToRotationVector2() * new Vector2(WheelHalfWidth, WheelRadius);
                Rail(start, end, rim < 0 ? .28f : .65f, rim < 0 ? 1.5f : 2f);
            }
        }
        for (int i = 0; i < 30; i++)
        {
            float angle = treadAngle + i * MathHelper.TwoPi / 30f;
            bool inFront = MathF.Sin(angle) > .6f;
            if (inFront != foreground) continue;
            Vector2 radial = angle.ToRotationVector2() * new Vector2(WheelHalfWidth, WheelRadius);
            Rail(Projectile.Center + radial - new Vector2(13f, 0f), Projectile.Center + radial + new Vector2(13f, 0f), inFront ? .7f : .28f, 2f);
        }
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        float speed = Projectile.ai[0];
        float fade = Math.Min(1f, age / 30f) * (Projectile.ai[1] == 0f ? 1f : Math.Min(1f, speed * 8f));
        Color color = Color.Lerp(TumblerVFX.PhaseColor(0), TumblerVFX.PhaseColor(1), MathHelper.Clamp((speed - .5f) / .2f, 0f, 1f));
        DrawWheel(color, fade, false);
        Texture2D body = TextureAssets.Projectile[Type].Value;
        int index = Golden(speed) ? 1 : 0;
        Rectangle frame = body.Frame(1, 2, 0, index);
        frame.Height -= 2;
        Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0f, WheelRadius - BodyRadius);
        Vector2 scale = new(BodyRadius * 2f / frame.Width, BodyRadius * 2f / frame.Height);
        Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
        Texture2D bloom = ModContent.Request<Texture2D>(Texture + "_Glowmask_Bloom").Value;
        Texture2D fuzzy = ModContent.Request<Texture2D>(Texture + "_Fuzzy").Value;
        float breath = .88f + MathF.Sin(age * .075f) * .12f;
        ulong seed = Main.TileFrameSeed ^ (ulong)(Projectile.identity + 1) * 7919UL;
        for (int pass = 0; pass < 7; pass++)
        {
            Vector2 jitter = new(Utils.RandomInt(ref seed, -10, 11) * .2f, Utils.RandomInt(ref seed, -10, 1) * .4f);
            Main.EntitySpriteDraw(fuzzy, center + jitter, null, TumblerVFX.Glow(color, fade * breath * .075f), cloneAngle, fuzzy.Size() * .5f, new Vector2(136f) / fuzzy.Size(), SpriteEffects.None);
            Main.EntitySpriteDraw(body, center + jitter, frame, TumblerVFX.Glow(Color.Lerp(color, Color.White, .3f), fade * .115f), cloneAngle, frame.Size() * .5f, scale, SpriteEffects.None);
        }
        Main.EntitySpriteDraw(body, center, frame, Color.Lerp(color, Color.White, .5f) * (fade * .23f), cloneAngle, frame.Size() * .5f, scale, SpriteEffects.None);
        Main.EntitySpriteDraw(glow, center, frame, TumblerVFX.Glow(color, fade * .65f), cloneAngle, frame.Size() * .5f, scale, SpriteEffects.None);
        Rectangle bloomFrame = bloom.Frame(1, 2, 0, index);
        bloomFrame.Height -= 2;
        Main.EntitySpriteDraw(bloom, center, bloomFrame, TumblerVFX.Glow(color, fade * (.13f + speed * .15f)), cloneAngle, bloomFrame.Size() * .5f, new Vector2(120f) / bloomFrame.Size(), SpriteEffects.None);
        Texture2D eye = ModContent.Request<Texture2D>(Texture + "_Eye", AssetRequestMode.ImmediateLoad).Value;
        Rectangle eyeFrame = eye.Frame(1, 2, 0, index);
        Vector2 eyeOrigin = new(eyeFrame.Width * (61f / 120f), eyeFrame.Height * (61f / 130f));
        for (int pass = 0; pass < 7; pass++)
        {
            Vector2 jitter = new(Utils.RandomInt(ref seed, -10, 11) * .15f, Utils.RandomInt(ref seed, -10, 1) * .35f);
            Main.EntitySpriteDraw(eye, center + jitter, eyeFrame, new Color(100, 100, 100, 0) * (fade * .7f), 0f, eyeOrigin, 1f, SpriteEffects.None);
        }
        Main.EntitySpriteDraw(eye, center, eyeFrame, TumblerVFX.Glow(Color.White, fade * .8f), 0f, eyeOrigin, 1f, SpriteEffects.None);
        DrawWheel(color, fade, true);
        return false;
    }
	
    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 18; i++) TumblerVFX.SpawnSpark(Projectile.Center + Main.rand.NextVector2Circular(100f, WheelRadius), Main.rand.NextVector2Circular(2f, 2f), TumblerVFX.PhaseColor(0));
    }
}

public class EnrichmentArc : ModProjectile
{
    public override string Texture => "AerovelenceMod/Blank";
    private readonly EnrichmentLightning visual = new();
    private Vector2 end;
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 14;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
	
    public override bool ShouldUpdatePosition() => false;
	
    public override void AI()
    {
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        float length = 0f;
        while (length < Projectile.ai[0] && !Collision.SolidCollision(Projectile.Center + direction * length - new Vector2(3f), 6, 6)) length += 8f;
        end = Projectile.Center + direction * Math.Min(length, Projectile.ai[0]);
        visual.Update(Projectile, Projectile.Center, end, .6f, true);
    }
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collision = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, 12f, ref collision);
    }
	
    public override bool? CanDamage() => Projectile.timeLeft > 7 ? null : false;
	
    public override bool PreDraw(ref Color lightColor)
    {
        visual.Draw(Main.spriteBatch, TumblerVFX.PhaseColor(Projectile.ai[1]), Projectile.timeLeft / 14f, 2f);
        return false;
    }
}

public class EnrichmentSpark : ModProjectile
{
    public override string Texture => "AerovelenceMod/Assets/Pixel/CrispStarPMA";
    private readonly EnrichmentLightning visual = new();
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = 100;
        Projectile.penetrate = 1;
    }
	
    public override void AI()
    {
        Projectile.velocity.Y = Math.Min(12f, Projectile.velocity.Y + .16f);
        visual.Update(Projectile, Projectile.Center - Projectile.velocity * 5f, Projectile.Center, .3f);
        if (Projectile.timeLeft % 4 == 0) TumblerVFX.SpawnSpark(Projectile.Center, -Projectile.velocity * .1f, Color.Gold);
    }
	
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => SkillStrikeUtil.setSkillStrike(Projectile, 1.6f);
	
    public override bool PreDraw(ref Color lightColor)
    {
        visual.Draw(Main.spriteBatch, Color.Gold, Math.Min(1f, Projectile.timeLeft / 15f), 1.5f);
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, TumblerVFX.Glow(Color.LightGoldenrodYellow), Projectile.velocity.ToRotation(), texture.Size() * .5f, new Vector2(.35f, .14f), SpriteEffects.None);
        return false;
    }
	
    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 5; i++) TumblerVFX.SpawnSpark(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f), Color.Gold);
    }
}

internal sealed class EnrichmentLightning
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