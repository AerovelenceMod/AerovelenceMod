using System;
using System.Collections.Generic;
using System.IO;
using AerovelenceMod.Content.Dusts.GlowDusts;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Localization;

namespace AerovelenceMod.Content.Items.Weapons.Sky;

public sealed class CharmingFlower : TranslatableModItem
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Sky/CharmingFlower/CharmingFlower";
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Charming Flower", "Grows a branching flower that fires magic bolts");
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
    }
    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 46;
        Item.damage = 9;
        Item.DamageType = DamageClass.Summon;
        Item.mana = 10;
        Item.knockBack = 1f;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<CharmingFlowerMinion>();
        Item.buffType = ModContent.BuffType<CharmingFlowerBuff>();
        Item.shootSpeed = 1f;
        Item.rare = ItemRarities.EarlyPHM;
        Item.value = Item.sellPrice(silver: 40);
        Item.UseSound = SoundID.Item44 with { Volume = .5f, Pitch = .2f };
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        CharmingFlowerPlant plant = player.GetModPlayer<CharmingFlowerPlant>();
        plant.Refresh();
        int order = 0;
        foreach (Projectile flower in plant.Flowers) order = Math.Max(order, (int)flower.ai[1] + 1);
        int color = order < 3 ? order : Main.rand.Next(3);
        player.AddBuff(Item.buffType, 2);
        int index = Projectile.NewProjectile(source, plant.Root, Vector2.Zero, type, damage, knockback, player.whoAmI, color, order);
        if (index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        return false;
    }
}
public sealed class CharmingFlowerBuff : ModBuff
{
    public override string Texture => ModContent.GetInstance<CharmingFlower>().Texture;
    public override LocalizedText DisplayName => ModContent.GetInstance<CharmingFlower>().DisplayName;
    public override LocalizedText Description => this.Localize("Flowers grow from your back and fight for you");
    public override void SetStaticDefaults() => Main.buffNoSave[Type] = Main.buffNoTimeDisplay[Type] = true;
    public override void Update(Player player, ref int buffIndex)
    {
        if (!player.dead && player.ownedProjectileCounts[ModContent.ProjectileType<CharmingFlowerMinion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}
public sealed class CharmingFlowerPlant : ModPlayer
{
    internal readonly List<Projectile> Flowers = new();
    private ulong lastUpdate = ulong.MaxValue, lastGrowth = ulong.MaxValue;
    private float height;
    private Vector2 sway, swayVelocity;
    internal Vector2 Sway => sway;
    internal Vector2 Root => Player.RotatedRelativePoint(Player.MountedCenter + new Vector2(-Player.direction * 8f, 0f), false, false) + new Vector2(0f, Player.gfxOffY);
    internal void Invalidate() => lastUpdate = ulong.MaxValue;
    internal void Refresh()
    {
        if (lastUpdate == Main.GameUpdateCount) return;
        lastUpdate = Main.GameUpdateCount;
        Flowers.Clear();
        int type = ModContent.ProjectileType<CharmingFlowerMinion>();
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == Player.whoAmI && projectile.type == type) Flowers.Add(projectile);
        Flowers.Sort((a, b) => a.ai[1].CompareTo(b.ai[1]));
        if (Flowers.Count == 0) { height = 0f; sway = swayVelocity = Vector2.Zero; }
        else if (lastGrowth != Main.GameUpdateCount)
        {
            height = MathHelper.Lerp(height, 64f + (Flowers.Count - 1) * 18f, .14f);
            Vector2 movement = new(MathHelper.Clamp(-Player.velocity.X * .7f, -8f, 8f), MathHelper.Clamp(-Player.velocity.Y * .2f, -3f, 3f));
            swayVelocity = (swayVelocity + (movement - sway) * .06f) * .8f;
            sway += swayVelocity;
        }
        lastGrowth = Main.GameUpdateCount;
    }
    internal Vector2 StemPoint(float progress)
    {
        Vector2 root = Root;
        float breeze = MathF.Sin(Player.miscCounter * .04f) * 3f;
        Vector2 tip = root + new Vector2(-Player.direction * 18f + breeze, -height * Player.gravDir) + sway;
        if (Flowers.Count > 0 && Flowers[0].ModProjectile is CharmingFlowerMinion flower) tip += flower.RecoilOffset;
        Vector2 first = root + new Vector2(-Player.direction * 30f, -8f * Player.gravDir);
        Vector2 second = tip + new Vector2(0f, 24f * Player.gravDir);
        return CharmingFlowerMinion.CurvePoint(root, first, second, tip, progress);
    }
    internal Vector2 Joint(int index) => StemPoint(MathHelper.Clamp((40f + (index - 1) * 18f) / Math.Max(64f, height), 0f, 1f));
    internal Vector2 FlowerPosition(int index)
    {
        if (index == 0) return StemPoint(1f);
        float side = (index % 2 == 1 ? 1f : -1f) * Player.direction;
        float sway = MathF.Sin(Player.miscCounter * .04f + index * 1.7f) * 2f;
        return Joint(index) + new Vector2(side * (28f + index % 3 * 4f) + sway, -22f * Player.gravDir) + this.sway * .35f;
    }
}
public sealed class CharmingFlowerMinion : ModProjectile
{
    private const string AssetPath = "AerovelenceMod/Content/Items/Weapons/Sky/CharmingFlower/CharmingFlower";
    private int age;
    private float shotPulse;
    private Vector2 recoilOffset, recoilVelocity;
    internal Vector2 RecoilOffset => recoilOffset;
    private float GlowPulse => MathF.Sin(MathHelper.Pi * (1f - shotPulse / 20f)) * (shotPulse / 20f);
    private readonly List<DrawData> leaves = new();
    public override string Texture => AssetPath + "Red";
    public override LocalizedText DisplayName => ModContent.GetInstance<CharmingFlower>().DisplayName;
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 22;
        Projectile.minion = true;
        Projectile.minionSlots = 1f;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
        Projectile.hide = true;
    }
    public override void OnSpawn(IEntitySource source)
    {
        Main.player[Projectile.owner].GetModPlayer<CharmingFlowerPlant>().Invalidate();
        Projectile.ai[2] = -((int)Projectile.ai[1] * 7 % 36);
    }
    public override void OnKill(int timeLeft) => Main.player[Projectile.owner].GetModPlayer<CharmingFlowerPlant>().Invalidate();
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || !player.HasBuff<CharmingFlowerBuff>()) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        CharmingFlowerPlant plant = player.GetModPlayer<CharmingFlowerPlant>();
        plant.Refresh();
        int index = plant.Flowers.IndexOf(Projectile);
        if (index < 0) return;
        recoilVelocity = (recoilVelocity - recoilOffset * .12f) * .76f;
        recoilOffset += recoilVelocity;
        shotPulse = Math.Max(0f, shotPulse - 1f);
        Projectile.Center = plant.FlowerPosition(index) + (index == 0 ? Vector2.Zero : recoilOffset);
        Projectile.velocity = Vector2.Zero;
        Projectile.rotation = (player.gravDir < 0f ? MathHelper.Pi : 0f) + MathF.Sin(player.miscCounter * .04f + index * 1.7f) * .1f
            + (plant.Sway.X * .015f + recoilOffset.X * .035f) * player.gravDir;
        if (!Main.dedServ && shotPulse > 0f)
            Lighting.AddLight(Projectile.Center, CharmingFlowerBolt.FlowerColor((int)Projectile.ai[0]).ToVector3() * (GlowPulse * .15f));
        age++;
        if (!Main.dedServ && age == 1)
            for (int i = 0; i < 5; i++)
                Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<GlowPixelCross>(), Main.rand.NextVector2Circular(2f, 2f), 0, CharmingFlowerBolt.FlowerColor((int)Projectile.ai[0]), .15f);
        if (Projectile.owner != Main.myPlayer || ++Projectile.ai[2] < 50f) return;
        float range = Projectile.ai[0] == 1f ? 900f : 600f;
        NPC target = null;
        if (player.HasMinionAttackTargetNPC)
        {
            NPC marked = Main.npc[player.MinionAttackTargetNPC];
            if (marked.CanBeChasedBy(Projectile) && Vector2.DistanceSquared(Projectile.Center, marked.Center) < range * range
                && Collision.CanHitLine(Projectile.Center, 1, 1, marked.Center, 1, 1)) target = marked;
        }
        if (target == null)
        {
            float nearest = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float distance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                if (!npc.CanBeChasedBy(Projectile) || distance >= nearest || !Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)) continue;
                target = npc;
                nearest = distance;
            }
        }
        if (target == null) { Projectile.ai[2] = 40f; return; }
        Projectile.ai[2] = 0f;
        float speed = (int)Projectile.ai[0] switch { 1 => CharmingFlowerBolt.YellowSpeed, 2 => CharmingFlowerBolt.BlueSpeed, _ => 11f };
        float travel = Vector2.Distance(Projectile.Center, target.Center) / speed;
        Vector2 destination = target.Center + target.velocity * Math.Min(12f, travel) - Projectile.Center;
        Vector2 aim = destination.SafeNormalize(Vector2.UnitY);
        if (Projectile.ai[0] == 2f)
        {
            float gravity = CharmingFlowerBolt.BlueGravity, speedSquared = speed * speed;
            float arc = speedSquared * speedSquared + 2f * gravity * destination.Y * speedSquared - gravity * gravity * destination.X * destination.X;
            if (arc >= 0f)
            {
                float flight = MathF.Sqrt(2f * destination.LengthSquared() / (speedSquared + gravity * destination.Y + MathF.Sqrt(arc)));
                if (flight > .01f) aim = (destination / flight - new Vector2(0f, gravity * flight * .5f)).SafeNormalize(aim);
            }
        }
        int damage = Projectile.ai[0] == 0f ? Math.Max(1, (int)MathF.Round(Projectile.damage * 1.15f)) : Projectile.damage;
        Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center + aim * 8f, aim * speed, ModContent.ProjectileType<CharmingFlowerBolt>(), damage, Projectile.knockBack, Projectile.owner, Projectile.ai[0], target.whoAmI);
        recoilVelocity -= aim * 1.4f;
        shotPulse = 20f;
        Projectile.netUpdate = true;
        if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item21 with { Volume = .18f, Pitch = .4f }, Projectile.Center);
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(recoilOffset.X); writer.Write(recoilOffset.Y);
        writer.Write(recoilVelocity.X); writer.Write(recoilVelocity.Y);
        writer.Write(shotPulse);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        recoilOffset = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        recoilVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        shotPulse = reader.ReadSingle();
    }
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => behindProjectiles.Add(index);
    internal static Vector2 CurvePoint(Vector2 start, Vector2 first, Vector2 second, Vector2 end, float progress)
    {
        float remaining = 1f - progress;
        return start * (remaining * remaining * remaining) + first * (3f * remaining * remaining * progress)
            + second * (3f * remaining * progress * progress) + end * (progress * progress * progress);
    }
    private void DrawBranch(Vector2 start, Vector2 first, Vector2 second, Vector2 end, float opacity)
    {
        Texture2D branch = ModContent.Request<Texture2D>(AssetPath + "Branch").Value;
        Texture2D leaf = ModContent.Request<Texture2D>(AssetPath + "Leaves").Value;
        int branchWidth = branch.Width - 2, branchHeight = branch.Height / 3;
        int leafHeight = leaf.Height / 2;
        int variation = (int)Projectile.ai[1];
        int steps = Math.Max(1, (int)MathF.Ceiling((Vector2.Distance(start, first) + Vector2.Distance(first, second) + Vector2.Distance(second, end)) / 2f));
        Vector2 previous = start, sliceStart = start;
        float traveled = 0f, nextSlice = 2f, nextLeaf = 14f + variation % 3 * 4f;
        int column = 0, section = 0, leafCount = 0;
        leaves.Clear();
        for (int i = 1; i <= steps; i++)
        {
            Vector2 point = CurvePoint(start, first, second, end, i / (float)steps);
            float length = Vector2.Distance(previous, point);
            if (length < .001f) continue;
            while (nextSlice <= traveled + length || (i == steps && sliceStart != end))
            {
                float progress = Math.Min(1f, (nextSlice - traveled) / length);
                Vector2 sliceEnd = Vector2.Lerp(previous, point, progress);
                Vector2 delta = sliceEnd - sliceStart;
                Vector2 center = (sliceStart + sliceEnd) * .5f;
                float rotation = delta.ToRotation();
                int frame = (variation + section) % 3;
                Main.EntitySpriteDraw(branch, center - Main.screenPosition, new Rectangle(column, frame * branchHeight, 2, branchHeight - 2),
                    Lighting.GetColor(center.ToTileCoordinates()) * opacity, rotation, new Vector2(1f, (branchHeight - 2) * .5f),
                    new Vector2(delta.Length() / 2f + .125f, 1f), SpriteEffects.None);
                if (nextSlice >= nextLeaf && Vector2.DistanceSquared(center, end) > 14f * 14f)
                {
                    bool flip = (leafCount + variation) % 2 == 1;
                    int leafFrame = (leafCount / 2 + variation) % 2;
                    Vector2 normal = new(-MathF.Sin(rotation), MathF.Cos(rotation));
                    Vector2 position = center + normal * (flip ? 2f : -2f);
                    Vector2 origin = new(2f, flip ? 2f : leafHeight - 4f);
                    leaves.Add(new DrawData(leaf, position - Main.screenPosition, new Rectangle(0, leafFrame * leafHeight, leaf.Width - 2, leafHeight - 2),
                        Lighting.GetColor(position.ToTileCoordinates()) * opacity, rotation, origin, 1f, flip ? SpriteEffects.FlipVertically : SpriteEffects.None));
                    leafCount++;
                    nextLeaf += 22f + (leafCount + variation) % 3 * 4f;
                }
                column += 2;
                if (column >= branchWidth) { column = 0; section++; }
                sliceStart = sliceEnd;
                nextSlice += 2f;
                if (progress == 1f) break;
            }
            traveled += length;
            previous = point;
        }
        foreach (DrawData draw in leaves) draw.Draw(Main.spriteBatch);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        CharmingFlowerPlant plant = player.GetModPlayer<CharmingFlowerPlant>();
        plant.Refresh();
        int index = plant.Flowers.IndexOf(Projectile);
        if (index < 0) return false;
        float appear = MathHelper.SmoothStep(0f, 1f, Math.Min(1f, age / 20f));
        if (index == 0)
        {
            Vector2 root = plant.Root;
            DrawBranch(root, root + new Vector2(-player.direction * 30f, -8f * player.gravDir), Projectile.Center + new Vector2(0f, 24f * player.gravDir), Projectile.Center, appear);
        }
        else
        {
            Vector2 joint = plant.Joint(index);
            DrawBranch(joint, joint + new Vector2((Projectile.Center.X - joint.X) * .7f, 0f), Projectile.Center + new Vector2(0f, 18f * player.gravDir), Projectile.Center, appear);
        }
        string color = (int)Projectile.ai[0] switch { 1 => "Yellow", 2 => "Blue", _ => "Red" };
        Texture2D flower = ModContent.Request<Texture2D>(AssetPath + color).Value;
        Texture2D glow = ModContent.Request<Texture2D>(AssetPath + color + "Glowy").Value;
        Vector2 position = Projectile.Center - Main.screenPosition;
        float scale = .6f + appear * .4f, pulse = GlowPulse;
        Color glowColor = (CharmingFlowerBolt.FlowerColor((int)Projectile.ai[0]) with { A = 0 }) * (pulse * appear);
        if (pulse > 0f) Main.EntitySpriteDraw(glow, position, null, glowColor * .6f, Projectile.rotation, glow.Size() * .5f, scale * (1f + pulse * .05f), SpriteEffects.None);
        Main.EntitySpriteDraw(flower, position, null, lightColor * appear, Projectile.rotation, flower.Size() * .5f, scale, SpriteEffects.None);
        if (pulse > 0f) Main.EntitySpriteDraw(glow, position, null, glowColor * .22f, Projectile.rotation, glow.Size() * .5f, scale, SpriteEffects.None);
        return false;
    }
}
public sealed class CharmingFlowerBolt : ModProjectile
{
    internal const float YellowSpeed = 9.5f, BlueSpeed = 8f, BlueGravity = .08f;
    public override string Texture => "AerovelenceMod/Assets/Orbs/SoftGlow64";
    public override LocalizedText DisplayName => ModContent.GetInstance<CharmingFlower>().DisplayName;
    internal static Color FlowerColor(int color) => color switch { 1 => new(255, 213, 95), 2 => new(95, 190, 255), _ => new(255, 95, 125) };
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.TrailCacheLength[Type] = 12;
        ProjectileID.Sets.TrailingMode[Type] = 0;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 70;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        Projectile.penetrate = Projectile.ai[0] == 0f ? 3 : Projectile.ai[0] == 1f ? 2 : 1;
        if (Projectile.ai[0] == 1f) Projectile.timeLeft = 140;
        else if (Projectile.ai[0] == 2f) Projectile.timeLeft = 110;
    }
    public override void AI()
    {
        Projectile.localAI[0]++;
        if (Projectile.ai[0] == 2f)
        {
            Projectile.velocity.Y = Math.Min(8f, Projectile.velocity.Y + BlueGravity);
            Tile tile = Framing.GetTileSafely(Projectile.Center.ToTileCoordinates());
            bool underwater = tile.LiquidType == LiquidID.Water && tile.LiquidAmount > 0
                && Projectile.Center.Y - MathF.Floor(Projectile.Center.Y / 16f) * 16f >= 16f - tile.LiquidAmount / 255f * 16f;
            if (Main.raining || underwater)
            {
                NPC target = (int)Projectile.ai[1] >= 0 && (int)Projectile.ai[1] < Main.maxNPCs ? Main.npc[(int)Projectile.ai[1]] : null;
                if (target != null && (!target.CanBeChasedBy(Projectile) || Vector2.DistanceSquared(Projectile.Center, target.Center) > 600f * 600f)) target = null;
                if (target == null && Projectile.owner == Main.myPlayer && (int)Projectile.localAI[0] % 10 == 1)
                {
                    float nearest = 480f * 480f;
                    foreach (NPC npc in Main.ActiveNPCs)
                    {
                        float distance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
                        if (!npc.CanBeChasedBy(Projectile) || distance >= nearest || !Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)) continue;
                        target = npc;
                        nearest = distance;
                    }
                    if (target != null) { Projectile.ai[1] = target.whoAmI; Projectile.netUpdate = true; }
                }
                if (target != null && Collision.CanHitLine(Projectile.Center, 1, 1, target.Center, 1, 1))
                {
                    float speed = MathHelper.Clamp(Projectile.velocity.Length(), BlueSpeed, BlueSpeed + 1f);
                    Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    Projectile.velocity = (Projectile.velocity * 11f + direction * speed).SafeNormalize(direction) * speed;
                }
            }
        }
        if (Main.dedServ) return;
        Color color = FlowerColor((int)Projectile.ai[0]);
        Lighting.AddLight(Projectile.Center, color.ToVector3() * .3f);
        if ((int)Projectile.localAI[0] % 6 == 0)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, -Projectile.velocity * .08f, 120, color, .4f);
            dust.noGravity = true;
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Projectile.ai[0] != 2f || ++Projectile.ai[2] > 5f) return true;
        if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X * .9f;
        if (Projectile.velocity.Y != oldVelocity.Y)
            Projectile.velocity.Y = oldVelocity.Y > 0f ? -Math.Max(2.5f, oldVelocity.Y * .85f) : -oldVelocity.Y * .85f;
        Projectile.netUpdate = true;
        if (!Main.dedServ)
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, Main.rand.NextVector2Circular(1.5f, 1.5f), 120, FlowerColor(2), .4f);
                dust.noGravity = true;
            }
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.ai[0] != 1f || Projectile.ai[2] != 0f || Projectile.owner != Main.myPlayer) return;
        Projectile.ai[2] = 1f;
        NPC next = null;
        float nearest = 360f * 360f;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float distance = Vector2.DistanceSquared(Projectile.Center, npc.Center);
            if (npc.whoAmI == target.whoAmI || Projectile.localNPCImmunity[npc.whoAmI] != 0 || !npc.CanBeChasedBy(Projectile)
                || distance >= nearest || !Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)) continue;
            next = npc;
            nearest = distance;
        }
        if (next == null) { Projectile.Kill(); return; }
        Projectile.ai[1] = next.whoAmI;
        float travel = Vector2.Distance(Projectile.Center, next.Center) / YellowSpeed;
        Projectile.velocity = (next.Center + next.velocity * Math.Min(8f, travel) - Projectile.Center).SafeNormalize(Vector2.UnitY) * YellowSpeed;
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, 45);
        Projectile.netUpdate = true;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Color color = FlowerColor((int)Projectile.ai[0]);
        Texture2D glow = TextureAssets.Projectile[Type].Value;
        Texture2D trail = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Trails/EvenThinnerGlowLine").Value;
        Rectangle slice = new(trail.Width / 2, trail.Height / 2 - 32, 2, 64);
        Vector2 previous = Projectile.Center;
        int points = Math.Min((int)Projectile.localAI[0], Projectile.oldPos.Length);
        for (int i = 0; i < points; i++)
        {
            Vector2 point = Projectile.oldPos[i] + Projectile.Size * .5f;
            Vector2 delta = previous - point;
            float length = delta.Length();
            if (length > 64f) break;
            if (length > .01f)
            {
                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 position = (point + previous) * .5f - Main.screenPosition;
                Vector2 origin = new(1f, slice.Height * .5f);
                Main.EntitySpriteDraw(trail, position, slice, (color with { A = 0 }) * (fade * .65f), delta.ToRotation(), origin,
                    new Vector2((length + .5f) / 2f, 12f * fade / slice.Height), SpriteEffects.None);
                Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, 1, 1),
                    (Color.Lerp(color, Color.White, .75f) with { A = 0 }) * (fade * .55f), delta.ToRotation(), new Vector2(.5f),
                    new Vector2(length + .5f, 2f * fade), SpriteEffects.None);
            }
            previous = point;
        }
        Vector2 center = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(glow, center, null, (color with { A = 0 }) * .7f, 0f, glow.Size() * .5f, .2f, SpriteEffects.None);
        Main.EntitySpriteDraw(glow, center, null, (Color.White with { A = 0 }) * .5f, 0f, glow.Size() * .5f, .08f, SpriteEffects.None);
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, center, new Rectangle(0, 0, 1, 1), color, 0f, new Vector2(.5f), new Vector2(6f, 2f), SpriteEffects.None);
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, center, new Rectangle(0, 0, 1, 1), color, 0f, new Vector2(.5f), new Vector2(2f, 6f), SpriteEffects.None);
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, center, new Rectangle(0, 0, 1, 1), Color.White, 0f, new Vector2(.5f), 2f, SpriteEffects.None);
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < 4; i++)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.FireworksRGB, Main.rand.NextVector2Circular(2f, 2f), 100, FlowerColor((int)Projectile.ai[0]), .55f);
            dust.noGravity = true;
        }
    }
}
