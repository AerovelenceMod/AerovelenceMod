using System;
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

public class Rubbleswarm : ModItem
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/CrystalCaverns/StackerRock/StackerRock";
	
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Rubbleswarm", "Summons a flying clump of magnetized rubbl")
            .AddName(Language.Spanish, "Enjambre de escombros")
            .AddTooltip(Language.Spanish, "Invoca un cúmulo volador de escombros magnetizados")
            .AddSkillStrike(Language.Default, "Fragments during the first half-second after shattering Skill Strike")
            .AddSkillStrike(Language.Spanish, "Los fragmentos durante el primer medio segundo después de romperse realizan un Ataque de Habilidad");
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
    }
	
    public override void SetDefaults()
    {
        Item.width = Item.height = 30;
        Item.damage = 16;
        Item.DamageType = DamageClass.Summon;
        Item.mana = 10;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.knockBack = 2f;
        Item.shoot = ModContent.ProjectileType<RubbleboundMinion>();
        Item.buffType = ModContent.BuffType<RubbleboundBuff>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarityID.Green;
        Item.value = Item.sellPrice(gold: 1);
        Item.UseSound = SoundID.Item44;
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
}

public class RubbleboundBuff : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_162";
	
    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = true;
        Main.buffNoTimeDisplay[Type] = true;
        LocalizationManager.Bind(DisplayName.Key, DisplayName);
        LocalizationManager.Bind(Description.Key, Description);
        LocalizationManager.RegisterTranslation(DisplayName.Key, "Rubbleswarm", "default");
        LocalizationManager.RegisterTranslation(Description.Key, "Magnetized rubble fights for you", "default");
        LocalizationManager.RegisterTranslation(DisplayName.Key, "Enjambre de escombros", "es-ES");
        LocalizationManager.RegisterTranslation(Description.Key, "Los escombros magnetizados luchan por ti", "es-ES");
    }
	
    public override void Update(Player player, ref int buffIndex)
    {
        if (!player.dead && player.ownedProjectileCounts[ModContent.ProjectileType<RubbleboundMinion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public class RubbleboundMinion : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/NPCs/Bosses/CrystalTumbler/Magnetic_Platform_Debris";
    public const int FragmentCount = 7;
    public const int ReassemblyTime = 68;
    private readonly Vector2[,] history = new Vector2[FragmentCount, 12];
    private readonly float[,] rotations = new float[FragmentCount, 12];
    private static readonly Rectangle[] Pieces = { new(0, 0, 12, 22), new(16, 0, 18, 22), new(38, 0, 26, 22) };
    private float trailStrength = .4f;
    private int historyCount;
    private int age;
	
    public static Vector2 FragmentOffset(float ticks, int piece, float heading)
    {
        float t = MathHelper.Clamp(ticks / ReassemblyTime, 0f, 1f);
        float reach = MathF.Sin(MathF.Pow(t, .65f) * MathHelper.Pi) * (1f - MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((t - .78f) / .22f, 0f, 1f)));
        float angle = heading + (piece - 3) * .32f + MathF.Sin(piece * 2.7f) * .16f;
        float distance = 62f + piece % 3 * 21f;
        Vector2 outward = angle.ToRotationVector2() * distance;
        Vector2 curl = (angle + MathHelper.PiOver2).ToRotationVector2() * (MathF.Sin(t * MathHelper.Pi) * (piece % 2 == 0 ? 30f : -24f));
        return (outward + curl) * reach;
    }
	
    public static Vector2 ReturnVelocity(Vector2 velocity, Vector2 home, float ticks, int identity)
    {
        float turn = (identity % 2 == 0 ? 1f : -1f) * .023f * (1f - MathHelper.Clamp(ticks / 35f, 0f, 1f));
        Vector2 desired = home * .085f;
        if (desired.Length() > 16f) desired = desired.SafeNormalize(Vector2.UnitX) * 16f;
        return Vector2.Lerp(velocity.RotatedBy(turn), desired, MathHelper.SmoothStep(.004f, .16f, MathHelper.Clamp(ticks / 60f, 0f, 1f)));
    }
	
    private Vector2 PiecePosition(int piece)
    {
        Vector2 cluster = (Projectile.rotation + piece * 2.399f).ToRotationVector2() * (piece == 0 ? 0f : 6f + piece % 2 * 2f);
        return Projectile.Center + cluster + (Projectile.ai[0] == 2f ? FragmentOffset(Projectile.ai[1], piece, Projectile.ai[2]) : Vector2.Zero);
    }
	
    private float PieceRotation(int piece) => Projectile.rotation + piece * 1.7f + (Projectile.ai[0] == 2f ? MathF.Sin(Projectile.ai[1] / ReassemblyTime * MathHelper.Pi) * Projectile.ai[1] * (.09f + piece * .022f) : 0f);
    public override bool ShouldUpdatePosition() => false;
	
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 300;
    }
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 26;
        Projectile.minion = true;
        Projectile.minionSlots = 1f;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 25;
    }
    public override bool MinionContactDamage() => true;
    public override bool? CanDamage() => Projectile.ai[0] == 1f || Projectile.ai[0] == 2f && Projectile.ai[1] < ReassemblyTime - 10 ? null : false;
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Projectile.ai[0] != 2f) return projHitbox.Intersects(targetHitbox);
        for (int i = 0; i < FragmentCount; i++)
        {
            Vector2 point = PiecePosition(i);
            Vector2 closest = Vector2.Clamp(point, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            if (Vector2.DistanceSquared(point, closest) < 11f * 11f && Collision.CanHitLine(point, 1, 1, closest, 1, 1)) return true;
        }
        return false;
    }
	
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        Projectile.GetGlobalProjectile<Common.Globals.SkillStrikes.SkillStrikeGProj>().SkillStrike = false;
        if (Projectile.ai[0] == 2f && Projectile.ai[1] < 30f) SkillStrikeUtil.setSkillStrike(Projectile, 1.6f);
    }
	
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Shatter(Projectile.velocity);
	
    private void Shatter(Vector2 velocity)
    {
        if (Projectile.ai[0] != 1f) return;
        Projectile.ai[0] = 2f;
        Projectile.ai[1] = 0f;
        Projectile.ai[2] = velocity.ToRotation();
        Projectile.velocity = velocity * .86f;
        Projectile.netUpdate = true;
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<RubbleReassemblyBurst>(), 0, 0f, Projectile.owner);
    }
	
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Vector2 reflected = new Vector2(Math.Abs(Projectile.velocity.X - oldVelocity.X) > .01f ? -oldVelocity.X * .65f : oldVelocity.X,
            Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > .01f ? -oldVelocity.Y * .65f : oldVelocity.Y);
        Shatter(reflected);
        return false;
    }
	
    public override void PostAI()
    {
        if (!Projectile.active) return;
        if (Projectile.ai[0] != 1f) Projectile.position += Projectile.velocity;
        else
        {
            int steps = Math.Max(1, (int)Math.Ceiling(Projectile.velocity.Length() / 5f));
            Vector2 step = Projectile.velocity / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector2 allowed = Collision.TileCollision(Projectile.position, step, Projectile.width, Projectile.height);
                Projectile.position += allowed;
                if (Vector2.DistanceSquared(step, allowed) < .001f) continue;
                Vector2 incoming = Projectile.velocity;
                Projectile.velocity = allowed * steps;
                OnTileCollide(incoming);
                break;
            }
        }
        for (int i = 0; i < FragmentCount; i++)
        {
            for (int j = 11; j > 0; j--) { history[i, j] = history[i, j - 1]; rotations[i, j] = rotations[i, j - 1]; }
            history[i, 0] = PiecePosition(i);
            rotations[i, 0] = PieceRotation(i);
        }
        historyCount = Math.Min(12, historyCount + 1);
    }
	
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || !player.HasBuff<RubbleboundBuff>()) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        age++;
        trailStrength = MathHelper.Lerp(trailStrength, Projectile.ai[0] == 2f ? .7f : .4f, .055f);
        Projectile.ai[1]++;
        Lighting.AddLight(Projectile.Center, .06f, .22f, .38f);
        if (Vector2.DistanceSquared(Projectile.Center, player.Center) > 1800f * 1800f)
        {
            Projectile.Center = player.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.ai[0] = Projectile.ai[1] = 0f;
            historyCount = 0;
            Projectile.netUpdate = true;
        }
        Projectile.rotation += .025f + Projectile.velocity.X * .022f;
        if (Projectile.ai[0] == 2f)
        {
            Vector2 home = player.Center + new Vector2(MathF.Sin(Projectile.identity * 1.7f) * 55f, -65f);
            Projectile.velocity = ReturnVelocity(Projectile.velocity, home - Projectile.Center, Projectile.ai[1], Projectile.identity);
            if (Projectile.ai[1] >= ReassemblyTime)
            {
                Projectile.ai[0] = Projectile.ai[1] = 0f;
                Projectile.netUpdate = true;
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = .25f, Pitch = .5f }, Projectile.Center);
            }
            return;
        }
        if (Projectile.ai[0] == 1f)
        {
            if (Projectile.ai[1] < 10f)
            {
                Vector2 thrust = Projectile.ai[2].ToRotationVector2() * 17f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, thrust, .19f);
            }
            Projectile.velocity.Y += .12f;
            if (Projectile.ai[1] > 35f) Shatter(Projectile.velocity);
            return;
        }
        NPC target = null;
        float distance = 700f * 700f;
        if (player.HasMinionAttackTargetNPC)
        {
            NPC marked = Main.npc[player.MinionAttackTargetNPC];
            if (marked.CanBeChasedBy() && Vector2.DistanceSquared(marked.Center, Projectile.Center) < 1000f * 1000f) target = marked;
        }
        if (target == null)
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy() && Vector2.DistanceSquared(npc.Center, Projectile.Center) < distance && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                { target = npc; distance = Vector2.DistanceSquared(npc.Center, Projectile.Center); }
        float orbit = age * .025f + Projectile.minionPos * 2.4f;
        Vector2 goal = target == null ? player.Center + new Vector2(MathF.Cos(orbit) * 65f, -55f + MathF.Sin(orbit) * 15f)
            : target.Center + new Vector2(MathF.Cos(orbit) * 100f, -65f + MathF.Sin(orbit) * 35f);
        Vector2 desired = (goal - Projectile.Center) * .08f;
        if (desired.Length() > 10f) desired = desired.SafeNormalize(Vector2.UnitX) * 10f;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .08f);
        if (target != null && Projectile.ai[1] > 36f && Projectile.owner == Main.myPlayer && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
        {
            Projectile.ai[2] = (target.Center + target.velocity * 9f - Projectile.Center).ToRotation();
            Projectile.ai[0] = 1f;
            Projectile.ai[1] = 0f;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = .3f, Pitch = .4f }, Projectile.Center);
        }
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D rock = TextureAssets.Projectile[Type].Value;
        float fade = Math.Min(1f, age / 20f);
        float energy = MathHelper.Clamp((trailStrength - .4f) / .3f, 0f, 1f);
        for (int i = 0; i < FragmentCount; i++)
        {
            Rectangle frame = Pieces[i % Pieces.Length];
            float scale = (10f + i % 3 * 2f) / Math.Max(frame.Width, frame.Height);
            for (int j = historyCount - 1; j >= 1; j--)
            {
                float strength = 1f - j / 12f;
                if (Vector2.DistanceSquared(history[i, j], history[i, 0]) < 2f) continue;
                Color trail = Color.Lerp(Color.DeepSkyBlue, Color.LightCyan, strength * .5f);
                Main.EntitySpriteDraw(rock, history[i, j] - Main.screenPosition, frame, trail with { A = 0 } * (strength * strength * fade * trailStrength), rotations[i, j], frame.Size() * .5f, scale * (.5f + strength * .5f), SpriteEffects.None);
            }
            Vector2 point = PiecePosition(i) - Main.screenPosition;
            float rotation = PieceRotation(i);
            for (int k = 0; k < 4; k++)
            {
                Vector2 jitter = (k * MathHelper.PiOver2 + age * .17f + i).ToRotationVector2() * (1f + .6f * energy);
                Main.EntitySpriteDraw(rock, point + jitter, frame, TumblerVFX.Glow(Color.DeepSkyBlue, fade * (.2f + .3f * energy)), rotation, frame.Size() * .5f, scale * 1.1f, SpriteEffects.None);
            }
            Main.EntitySpriteDraw(rock, point, frame, Color.Lerp(lightColor, Color.White, .25f) * fade, rotation, frame.Size() * .5f, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(rock, point, frame, TumblerVFX.Glow(Color.LightSkyBlue, .2f * fade), rotation, frame.Size() * .5f, scale, SpriteEffects.None);
        }
        return false;
    }
	
    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < FragmentCount; i++)
        {
            Dust dust = Dust.NewDustPerfect(PiecePosition(i), DustID.Stone, Main.rand.NextVector2Circular(2f, 2f));
            dust.noGravity = true;
        }
    }
}

public class RubbleReassemblyBurst : ModProjectile
{
    public override string Texture => "AerovelenceMod/Assets/Orbs/SoftGlow";
    public override void SetDefaults() { Projectile.width = Projectile.height = 2; Projectile.tileCollide = false; Projectile.timeLeft = 24; }
    public override bool? CanDamage() => false;
	
    public override void AI()
    {
        if (Projectile.timeLeft != 24 || Main.dedServ) return;
        SoundEngine.PlaySound(SoundID.Shatter with { Volume = .6f, Pitch = -.2f }, Projectile.Center);
        for (int i = 0; i < 24; i++) TumblerVFX.SpawnSpark(Projectile.Center, Main.rand.NextVector2Circular(6f, 6f), Color.LightCyan, .3f);
        for (int i = 0; i < 12; i++)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Stone, Main.rand.NextVector2Circular(3f, 3f), 0, default, .7f);
            dust.noGravity = true;
        }
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Projectile.timeLeft / 24f;
        Texture2D glow = TextureAssets.Projectile[Type].Value;
        Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, TumblerVFX.Glow(Color.LightCyan, fade * fade), 0f, glow.Size() * .5f, (120f - 70f * fade) / glow.Width, SpriteEffects.None);

        return false;
    }
}