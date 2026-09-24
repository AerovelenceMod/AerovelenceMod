using System;
using System.Collections.Generic;
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

public class BatteryBackpack : ModItem
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/BossDrops/CrystalTumbler/BatteryBackpack/BatteryBackpack";
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Battery Backpack", "Attach clamps to two different enemies to discharge and charge the pack\nEach charge grants 2% movement speed")
            .AddName(Language.Spanish, "Mochila batería")
            .AddTooltip(Language.Spanish, "Conecta las pinzas roja y azul a dos enemigos distintos para descargar y cargar la mochila\nConectar ambas al mismo enemigo desconecta los cables\nCada carga otorga un 2% de velocidad")
            .AddSkillStrike(Language.Default, "Completing five charges Skill Strikes a random enemy on screen")
            .AddSkillStrike(Language.Spanish, "Completar cinco cargas lanza un Ataque de habilidad contra un enemigo aleatorio en pantalla");
    }
	
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 44;
        Item.damage = 22;
        Item.DamageType = DamageClass.Melee;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.knockBack = 2f;
        Item.shoot = ModContent.ProjectileType<BatteryClamp>();
        Item.shootSpeed = 11f;
        Item.rare = ItemRarityID.Green;
        Item.value = Item.sellPrice(gold: 1);
        Item.UseSound = SoundID.Item1 with { Volume = .6f, Pitch = -.1f };
    }
	
    public override bool CanUseItem(Player player)
    {
        int count = 0;
        bool attached = false;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == player.whoAmI && projectile.type == ModContent.ProjectileType<BatteryClamp>())
            { count++; attached |= projectile.ai[2] > 0; }
        return count == 0 || count == 1 && attached;
    }
	
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile battery = BatteryCircuit.Find(player.whoAmI);
        if (battery == null)
        {
            int index = Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<BatteryCircuit>(), damage, knockback, player.whoAmI);
            if (index >= Main.maxProjectiles) return false;
            battery = Main.projectile[index];
        }
        int wire = player.ownedProjectileCounts[type] == 0 ? 0 : 1;
        float speed = 11f + battery.ai[0] * 1.5f;
        Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction) * speed, type, damage, knockback, player.whoAmI, battery.identity, wire);
        return false;
    }
	
    public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
        spriteBatch.Draw(glow, position, frame, Color.White * .8f, 0f, origin, scale, SpriteEffects.None, 0f);
    }
	
    public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
    {
        Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glowmask").Value;
        spriteBatch.Draw(glow, Item.Center - Main.screenPosition, null, Color.White * .8f, rotation, glow.Size() * .5f, scale, SpriteEffects.None, 0f);
    }

}

public class BatteryCircuit : ModProjectile
{
    public override string Texture => "AerovelenceMod/Blank";
	
    public static Projectile Find(int owner)
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == owner && projectile.type == ModContent.ProjectileType<BatteryCircuit>()) return projectile;
        return null;
    }
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
        Projectile.DamageType = DamageClass.Melee;
    }
	
    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor) => false;
	
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead) { Projectile.Kill(); return; }
        Projectile.Center = player.MountedCenter;
        Projectile.timeLeft = 2;
        bool holding = player.HeldItem.type == ModContent.ItemType<BatteryBackpack>();
        if (holding && Projectile.ai[0] > 0) player.AddBuff(ModContent.BuffType<BatteryCharge>(), 2);
        Projectile.ai[1]++;
        if (Projectile.owner != Main.myPlayer) return;
        if (Projectile.ai[1] >= 480f && Projectile.ai[0] > 0f)
        {
            Projectile.ai[0]--;
            Projectile.ai[1] = 360f;
            Projectile.netUpdate = true;
            for (int i = 0; i < 6; i++) TumblerVFX.SpawnSpark(player.Center - new Vector2(player.direction * 12f, 0f), Main.rand.NextVector2Circular(2f, 2f), Color.Cyan);
        }
        Projectile first = null;
        Projectile second = null;
        foreach (Projectile clamp in Main.ActiveProjectiles)
            if (clamp.owner == Projectile.owner && clamp.type == ModContent.ProjectileType<BatteryClamp>() && clamp.ai[0] == Projectile.identity)
            {
                if (!holding && clamp.ai[2] != -1f) { clamp.ai[2] = -1f; clamp.netUpdate = true; }
                if (clamp.ai[2] > 0f)
                {
                    if (clamp.ai[1] == 0f) first = clamp;
                    else second = clamp;
                }
            }
        if (!holding && Projectile.ai[0] <= 0f && player.ownedProjectileCounts[ModContent.ProjectileType<BatteryClamp>()] == 0) { Projectile.Kill(); return; }
        if (Projectile.ai[2] > 0f)
        {
            Projectile.ai[2]--;
            if (Projectile.ai[2] == 0f || first == null || second == null)
            {
                foreach (Projectile clamp in Main.ActiveProjectiles)
                    if (clamp.owner == Projectile.owner && clamp.type == ModContent.ProjectileType<BatteryClamp>() && clamp.ai[0] == Projectile.identity)
                    { clamp.ai[2] = -1f; clamp.netUpdate = true; }
                Projectile.ai[2] = 0f;
                Projectile.netUpdate = true;
            }
            return;
        }
        if (first == null || second == null) return;
        int firstTarget = (int)first.ai[2] - 1;
        int secondTarget = (int)second.ai[2] - 1;
        if (firstTarget != secondTarget)
        {
            int discharge = Projectile.NewProjectile(Projectile.GetSource_FromAI(), first.Center, second.Center - first.Center, ModContent.ProjectileType<BatteryDischarge>(), Math.Max(first.damage, second.damage), 2f, Projectile.owner, firstTarget + 1, secondTarget + 1);
            if (discharge < Main.maxProjectiles) Main.projectile[discharge].originalDamage = Projectile.originalDamage;
            bool overflow = Projectile.ai[0] >= 5f;
            Projectile.ai[0] = Math.Min(5f, Projectile.ai[0] + 1f);
            Projectile.ai[1] = 0f;
            Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = .5f, Pitch = .1f * Projectile.ai[0] }, player.Center);
            if (overflow)
            {
                List<NPC> candidates = new();
                Rectangle screen = new((int)Main.screenPosition.X, (int)Main.screenPosition.Y, Main.screenWidth, Main.screenHeight);
                foreach (NPC npc in Main.ActiveNPCs)
                    if (npc.CanBeChasedBy() && screen.Intersects(npc.Hitbox)) candidates.Add(npc);
                if (candidates.Count > 0)
                {
                    NPC target = candidates[Main.rand.Next(candidates.Count)];
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), target.Center - Vector2.UnitY * 360f, Vector2.Zero, ModContent.ProjectileType<BatteryOverflowStrike>(), Projectile.damage * 2, 4f, Projectile.owner, target.whoAmI + 1);
                }
            }
        }
        else SoundEngine.PlaySound(SoundID.Item7 with { Volume = .4f, Pitch = -.4f }, player.Center);
        if (firstTarget != secondTarget) Projectile.ai[2] = 24f;
        else first.ai[2] = second.ai[2] = -1f;
        first.netUpdate = second.netUpdate = true;
    }
}

public class BatteryClamp : ModProjectile
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/CrystalCaverns/PinCushionStone/PinCushionNeedle";
    private Vector2 attachment;
    private int targetType;
    private int age;
    private Vector2 previous;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 480;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
    }
	
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(attachment.X); writer.Write(attachment.Y); writer.Write(targetType);
    }
	
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        attachment = new Vector2(reader.ReadSingle(), reader.ReadSingle()); targetType = reader.ReadInt32();
    }
	
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.ai[2] == 0f ? null : false;
	
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        previous = Projectile.Center;
        age++;
        if (!player.active || player.dead) { Projectile.Kill(); return; }
        Projectile.tileCollide = false;
        if (Projectile.ai[2] > 0f)
        {
            NPC target = Main.npc[(int)Projectile.ai[2] - 1];
            if (!target.active || target.type != targetType || target.dontTakeDamage || Vector2.DistanceSquared(player.Center, target.Center) > 640f * 640f || Projectile.timeLeft < 45)
            { Projectile.ai[2] = -1f; Projectile.netUpdate = true; }
            else
            {
                Projectile.Center = target.Center + attachment;
                Projectile.velocity = Vector2.Zero;
                if (age % 12 == 0) TumblerVFX.SpawnSpark(Projectile.Center, Main.rand.NextVector2Circular(1f, 1f), Projectile.ai[1] == 0f ? Color.OrangeRed : Color.Cyan);
            }
        }
        if (Projectile.ai[2] == -1f)
        {
            Vector2 delta = player.MountedCenter - Projectile.Center;
            if (delta.Length() < 24f) { Projectile.Kill(); return; }
            Projectile.velocity = delta.SafeNormalize(Vector2.UnitX) * 22f;
            Projectile.Center += Projectile.velocity;
        }
        else if (Projectile.ai[2] == 0f)
        {
            Vector2 allowed = Collision.TileCollision(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            Projectile.Center += allowed;
            if (allowed != Projectile.velocity || Vector2.DistanceSquared(player.Center, Projectile.Center) > 340f * 340f || age > 50)
            { Projectile.ai[2] = -1f; Projectile.tileCollide = false; Projectile.netUpdate = true; }
        }
        Projectile.rotation = (Projectile.Center - player.MountedCenter).ToRotation();
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
    }
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previous, Projectile.Center, 12f, ref distance);
    }
	
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.owner != Main.myPlayer) return;
        Projectile.ai[2] = target.whoAmI + 1;
        targetType = target.type;
        attachment = Vector2.Clamp(Projectile.Center - target.Center, -target.Size * .35f, target.Size * .35f);
        Projectile.velocity = Vector2.Zero;
        Projectile.tileCollide = false;
        Projectile.netUpdate = true;
        SoundEngine.PlaySound(SoundID.Dig with { Volume = .5f, Pitch = .4f }, target.Center);
        for (int i = 0; i < 8; i++) TumblerVFX.SpawnSpark(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f), Projectile.ai[1] == 0f ? Color.OrangeRed : Color.Cyan);
    }
	
    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        Color color = Projectile.ai[1] == 0f ? new Color(255, 90, 80) : new Color(50, 180, 255);
        Vector2 start = player.MountedCenter + new Vector2(-player.direction * 11f, 3f);
        Vector2 last = start;
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        for (int i = 1; i <= 24; i++)
        {
            float t = i / 24f;
            Vector2 point = Vector2.Lerp(start, Projectile.Center, t) + Vector2.UnitY * MathF.Sin(t * MathHelper.Pi) * Math.Min(35f, Vector2.Distance(start, Projectile.Center) * .1f);
            Vector2 delta = point - last;
            Main.EntitySpriteDraw(pixel, last - Main.screenPosition, new Rectangle(0, 0, 1, 1), color * .8f, delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length() + 1f, 3f), SpriteEffects.None);
            Main.EntitySpriteDraw(pixel, last - Main.screenPosition, new Rectangle(0, 0, 1, 1), TumblerVFX.Glow(color, .3f), delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length() + 1f, 1f), SpriteEffects.None);
            last = point;
        }
        Texture2D clamp = TextureAssets.Projectile[Type].Value;
        for (int side = -1; side <= 1; side += 2)
        {
            float angle = Projectile.rotation + side * (Projectile.ai[2] > 0f ? .12f : .35f);
            Vector2 position = Projectile.Center + (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * side * 3f;
            Main.EntitySpriteDraw(clamp, position - Main.screenPosition, null, Color.Lerp(lightColor, color, .65f), angle + MathHelper.PiOver4, clamp.Size() * .5f, 17f / Math.Max(clamp.Width, clamp.Height), SpriteEffects.None);
            Main.EntitySpriteDraw(clamp, position - Main.screenPosition, null, TumblerVFX.Glow(color, .5f), angle + MathHelper.PiOver4, clamp.Size() * .5f, 17f / Math.Max(clamp.Width, clamp.Height), SpriteEffects.None);
        }
        return false;
    }
}

public class BatteryDischarge : ModProjectile
{
    public override string Texture => "AerovelenceMod/Blank";
    private readonly TumblerLightningVisual visual = new();
	
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 18;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanHitNPC(NPC target) => target.whoAmI + 1 == (int)Projectile.ai[0] || target.whoAmI + 1 == (int)Projectile.ai[1] ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => true;
    public override bool? CanDamage() => Projectile.timeLeft >= 15 ? null : false;
	
    public override void AI()
    {
        int first = (int)Projectile.ai[0] - 1;
        int second = (int)Projectile.ai[1] - 1;
        if (first >= 0 && first < Main.maxNPCs && second >= 0 && second < Main.maxNPCs && Main.npc[first].active && Main.npc[second].active)
        {
            Projectile.Center = Main.npc[first].Center;
            Projectile.velocity = Main.npc[second].Center - Projectile.Center;
        }
        visual.Update(Projectile, Projectile.Center, Projectile.Center + Projectile.velocity, .7f, true);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        visual.Draw(Main.spriteBatch, Color.LightCyan, Projectile.timeLeft / 18f, 3f);
        return false;
    }
}

public class BatteryOverflowStrike : ModProjectile
{
    public override string Texture => "AerovelenceMod/Blank";
    private readonly TumblerLightningVisual visual = new();
    private readonly TumblerLightningVisual antenna = new();
    private Vector2 end;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 28;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
	
    public override bool? CanDamage() => Projectile.timeLeft <= 18 && Projectile.timeLeft > 10 ? null : false;
	
    public override void AI()
    {
        int index = (int)Projectile.ai[0] - 1;
        if (end == Vector2.Zero) end = Projectile.Center + Vector2.UnitY * 360f;
        if (Projectile.timeLeft > 18 && index >= 0 && index < Main.maxNPCs && Main.npc[index].active)
        {
            end = Main.npc[index].Center;
            Projectile.Center = end - Vector2.UnitY * 360f;
        }
        visual.Update(Projectile, Projectile.Center, end, .7f, true);
        Player player = Main.player[Projectile.owner];
        antenna.Update(Projectile, player.MountedCenter + new Vector2(-player.direction * 12f, -22f), Projectile.Center, .6f);
        if (Projectile.timeLeft == 18)
        {
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = .6f, Pitch = -.5f }, end);
            for (int i = 0; i < 18; i++) TumblerVFX.SpawnSpark(end, Main.rand.NextVector2Circular(5f, 5f), Color.Gold, .3f);
        }
    }
	
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, 24f, ref distance);
    }
	
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => SkillStrikeUtil.setSkillStrike(Projectile, 2f);
	
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Projectile.timeLeft > 18 ? .2f : Projectile.timeLeft / 18f;
        visual.Draw(Main.spriteBatch, Color.Gold, fade, Projectile.timeLeft > 18 ? 1f : 3.5f);
        antenna.Draw(Main.spriteBatch, Color.Gold, fade * .6f, 1.5f);
        return false;
    }
}

public class BatteryCharge : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_178";
	
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.buffNoSave[Type] = true;
        LocalizationManager.Bind(DisplayName.Key, DisplayName);
        LocalizationManager.Bind(Description.Key, Description);
        LocalizationManager.RegisterTranslation(DisplayName.Key, "Charged battery", "default");
        LocalizationManager.RegisterTranslation(Description.Key, "+2% movement speed per battery charge while holding the backpack", "default");
        LocalizationManager.RegisterTranslation(DisplayName.Key, "Batería cargada", "es-ES");
        LocalizationManager.RegisterTranslation(Description.Key, "+2% de velocidad de movimiento por carga al sostener la mochila", "es-ES");
    }
	
    public override void Update(Player player, ref int buffIndex)
    {
        Projectile battery = BatteryCircuit.Find(player.whoAmI);
        if (player.HeldItem.type == ModContent.ItemType<BatteryBackpack>() && battery != null) player.moveSpeed += battery.ai[0] * .02f;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public class BatteryBackLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Torso);
	
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (player.dead || player.invis || drawInfo.shadow != 0f || player.HeldItem.type != ModContent.ItemType<BatteryBackpack>()) return;
        Projectile battery = BatteryCircuit.Find(player.whoAmI);
        Vector2 position = drawInfo.Position + player.Size * .5f - Main.screenPosition + new Vector2(-player.direction * 11f, player.gfxOffY);
        BatteryPackArt.AddDrawData(drawInfo.DrawDataCache, position, player.direction, (int)(battery?.ai[0] ?? 0f), 1f, drawInfo.colorArmorBody);
    }
}

internal static class BatteryPackArt
{
    public static void AddDrawData(List<DrawData> data, Vector2 position, int direction, int charge, float scale, Color color)
    {
        const string path = "AerovelenceMod/Content/Items/Weapons/BossDrops/CrystalTumbler/BatteryBackpack/BatteryBackpack";
        Texture2D body = ModContent.Request<Texture2D>(path).Value;
        Texture2D glow = ModContent.Request<Texture2D>(path + "_Glowmask").Value;
        SpriteEffects flip = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Vector2 origin = new(body.Width * .5f, 24f);
        data.Add(new DrawData(body, position, null, color, 0f, origin, scale, flip));
        data.Add(new DrawData(glow, position, null, Color.Black * .8f, 0f, origin, scale, flip));
        float breath = .8f + .2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 5f);
        for (int i = 0; i < Math.Clamp(charge, 0, 5); i++)
        {
            Rectangle row = new(0, 32 - i * 4, glow.Width, 2);
            Vector2 rowOrigin = origin - new Vector2(0f, row.Y);
            data.Add(new DrawData(glow, position, row, Color.White * breath, 0f, rowOrigin, scale, flip));
            data.Add(new DrawData(glow, position, row, new Color(255, 255, 255, 0) * (.28f * breath), 0f, rowOrigin, scale, flip));
        }
    }
}