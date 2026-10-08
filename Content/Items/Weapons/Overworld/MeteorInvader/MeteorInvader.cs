using System;
using System.Collections.Generic;
using System.IO;





using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;



namespace AerovelenceMod.Content.Items.Weapons.Overworld;

public class MeteorInvader : ModItem
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Overworld/MeteorInvader/MeteorInvader";
    public override void SetStaticDefaults()
    {
        this.ModifyLocalization("Meteor Invader", "Summons three meteor invaders in the first slot, then one per additional slot\nThe fleet marches above a shared target, firing beams downward\nInvaders that crash into enemies explode and must be summoned again")
            .AddName(Language.Spanish, "Invasor meteórico")
            .AddTooltip(Language.Spanish, "Invoca tres invasores meteóricos en el primer espacio y uno por cada espacio adicional\nLa flota marcha sobre un objetivo común y dispara láseres explosivos hacia abajo\nLos invasores que chocan con enemigos explotan y deben invocarse de nuevo")
            .AddSkillStrike(Language.Default, "Sacrifice an invader by descending into an enemy")
            .AddSkillStrike(Language.Spanish, "Sacrifica un invasor descendiendo hasta chocar con un enemigo");
        ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
        ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
        ItemID.Sets.LockOnIgnoresCollision[Type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.damage = 17;
        Item.DamageType = DamageClass.Summon;
        Item.mana = 10;
        Item.knockBack = 1.5f;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<MeteorInvaderMinion>();
        Item.shootSpeed = 1f;
        Item.buffType = ModContent.BuffType<MeteorInvaderBuff>();
        Item.rare = ItemRarities.EarlyPHM;
        Item.value = Item.sellPrice(gold: 1);
        Item.UseSound = SoundID.Item44 with { Volume = .55f, Pitch = -.2f };
    }

    public override void Load() => On_Player.FreeUpPetsAndMinions += MakeRoom;
    public override void Unload() => On_Player.FreeUpPetsAndMinions -= MakeRoom;

    private void MakeRoom(On_Player.orig_FreeUpPetsAndMinions orig, Player player, Item item)
    {
        if (item.type == Type)
        {
            float slots = 0f;
            Projectile oldest = null;
            foreach (Projectile member in Main.ActiveProjectiles)
            {
                if (member.owner != player.whoAmI || !member.minion) continue;
                slots += member.minionSlots;
                if (member.type == Item.shoot && member.minionSlots >= 1f && (oldest == null || member.ai[1] > oldest.ai[1]))
                    oldest = member;
            }
            if (slots + 1f > player.maxMinions + .00001f && oldest != null)
                oldest.Kill();
        }
        orig(player, item);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        int members = 0;
        foreach (Projectile member in Main.ActiveProjectiles)
            if (member.owner == player.whoAmI && member.type == type) members++;
        int count = members == 0 ? 3 : 1;
        Projectile fleet = MeteorInvaderFleet.Find(player.whoAmI);
        if (fleet == null)
        {
            int index = Projectile.NewProjectile(source, player.Center - Vector2.UnitY * 90f, Vector2.Zero, ModContent.ProjectileType<MeteorInvaderFleet>(), 0, 0f, player.whoAmI);
            if (index >= Main.maxProjectiles) return false;
            fleet = Main.projectile[index];
            Main.projectile[index].netUpdate = true;
        }
        player.AddBuff(Item.buffType, 2);
        foreach (Projectile member in Main.ActiveProjectiles)
            if (member.owner == player.whoAmI && member.type == type)
            {
                member.ai[1] += count;
                member.netUpdate = true;
            }
        for (int i = 0; i < count; i++)
        {
            Vector2 spawn = fleet.Center + MeteorInvaderPattern.Cell(i, members + count);
            int index = Projectile.NewProjectile(source, spawn, Vector2.Zero, type, damage, knockback, player.whoAmI, fleet.identity, i, 1f / count);
            if (index < Main.maxProjectiles) Main.projectile[index].originalDamage = Item.damage;
        }
        return false;
    }

    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.MeteoriteBar, 18).AddIngredient(ItemID.FallenStar, 3).AddTile(TileID.Anvils).Register();
}

public class MeteorInvaderBuff : ModBuff
{
    public override string Texture => "AerovelenceMod/Content/Items/Weapons/Overworld/MeteorInvader/MeteorInvaderBuff";

    public override void SetStaticDefaults()
    {
        Main.buffNoSave[Type] = Main.buffNoTimeDisplay[Type] = true;
        LocalizationManager.Bind(DisplayName.Key, DisplayName);
        LocalizationManager.Bind(Description.Key, Description);
        LocalizationManager.RegisterTranslation(DisplayName.Key, "Meteor Invader", "default");
        LocalizationManager.RegisterTranslation(Description.Key, "The invaders are coming", "default");
        LocalizationManager.RegisterTranslation(DisplayName.Key, "Invasor meteórico", "es-ES");
        LocalizationManager.RegisterTranslation(Description.Key, "Llegan los invasores", "es-ES");
    }

    public override void Update(Player player, ref int buffIndex)
    {
        if (!player.dead && player.ownedProjectileCounts[ModContent.ProjectileType<MeteorInvaderMinion>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public sealed class MeteorInvaderPattern
{
    public float Sweep;
    public float Depth;
    public int Direction = 1;
    public int Beat;
    public bool Advance()
    {
        if (++Beat < 6) return false;
        Beat = 0;
        Sweep += Direction * 18f;
        if (Math.Abs(Sweep) < 108f) return false;
        Sweep = MathHelper.Clamp(Sweep, -108f, 108f);
        Direction *= -1;
        Depth += 30f;
        return true;
    }
    public void Reset() { Sweep = Depth = Beat = 0; Direction = 1; }
    public static Vector2 Cell(int slot, int count)
    {
        int row = slot / 4;
        int columns = Math.Min(4, count - row * 4);
        return new Vector2((slot % 4 - (columns - 1) * .5f) * 38f, row * 34f);
    }
}

public class MeteorInvaderFleet : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    internal readonly MeteorInvaderPattern Pattern = new();
    private readonly List<Projectile> members = new();
    internal int MemberCount => members.Count;
    private Vector2 anchor;
    private int highestRow;
    private int resetPause;
    public bool Attacking => Projectile.ai[0] > 0f && resetPause == 0;

    public static Projectile Find(int owner)
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == owner && projectile.type == ModContent.ProjectileType<MeteorInvaderFleet>()) return projectile;
        return null;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor) => false;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(anchor.X); writer.Write(anchor.Y);
        writer.Write(Pattern.Sweep); writer.Write(Pattern.Depth); writer.Write(Pattern.Direction); writer.Write(Pattern.Beat);
        writer.Write(resetPause);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        anchor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        Pattern.Sweep = reader.ReadSingle(); Pattern.Depth = reader.ReadSingle(); Pattern.Direction = reader.ReadInt32(); Pattern.Beat = reader.ReadInt32();
        resetPause = reader.ReadInt32();
    }

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || !player.HasBuff<MeteorInvaderBuff>()) { Projectile.Kill(); return; }
        members.Clear();
        foreach (Projectile member in Main.ActiveProjectiles)
            if (member.owner == Projectile.owner && member.type == ModContent.ProjectileType<MeteorInvaderMinion>())
                members.Add(member);
        if (members.Count == 0) { Projectile.Kill(); return; }
        members.Sort(static (first, second) => first.ai[1].CompareTo(second.ai[1]));
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i].ai[1] == i) continue;
            members[i].ai[1] = i;
            if (Projectile.owner == Main.myPlayer) members[i].netUpdate = true;
        }
        highestRow = (members.Count - 1) / 4;
        Projectile.timeLeft = 2;
        Projectile.ai[1]++;
        int targetIndex = (int)Projectile.ai[0] - 1;
        NPC target = targetIndex >= 0 && targetIndex < Main.maxNPCs ? Main.npc[targetIndex] : null;
        bool valid = target != null && target.CanBeChasedBy() && Vector2.DistanceSquared(player.Center, target.Center) < 1000f * 1000f;
        if (Projectile.owner == Main.myPlayer && (!valid || (int)Projectile.ai[1] % 15 == 0))
        {
            NPC chosen = valid ? target : null;
            if (player.HasMinionAttackTargetNPC && (uint)player.MinionAttackTargetNPC < Main.maxNPCs)
            {
                NPC marked = Main.npc[player.MinionAttackTargetNPC];
                if (marked.CanBeChasedBy() && Vector2.DistanceSquared(player.Center, marked.Center) < 900f * 900f) chosen = marked;
            }
            if (chosen == null)
            {
                float best = 800f * 800f;
                foreach (NPC npc in Main.ActiveNPCs)
                    if (npc.CanBeChasedBy() && Vector2.DistanceSquared(player.Center, npc.Center) < best &&
                        (Collision.CanHitLine(player.Center, 1, 1, npc.Center, 1, 1) || Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)))
                    { chosen = npc; best = Vector2.DistanceSquared(player.Center, npc.Center); }
            }
            int newTarget = chosen?.whoAmI + 1 ?? 0;
            if (newTarget != Projectile.ai[0])
            {
                Projectile.ai[0] = newTarget;
                Pattern.Reset();
                resetPause = 35;
                anchor = chosen == null ? player.Center - new Vector2(0f, 90f + highestRow * 34f) : chosen.Top - new Vector2(0f, 180f + highestRow * 34f);
                Projectile.netUpdate = true;
            }
            target = chosen;
            valid = chosen != null;
        }
        if (!valid)
        {
            Pattern.Reset();
            anchor = Vector2.Lerp(anchor == Vector2.Zero ? Projectile.Center : anchor, player.Center - new Vector2(0f, 85f + highestRow * 34f), .12f);
            Projectile.Center = anchor;
            return;
        }
        if (resetPause > 0) resetPause--;
        anchor.X = MathHelper.Lerp(anchor.X, target.Center.X, .025f);
        if (resetPause == 0 && Pattern.Advance())
        {
            if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            SoundEngine.PlaySound(SoundID.Item15 with { Volume = .14f, Pitch = Math.Min(.7f, Pattern.Depth / 500f) }, Projectile.Center);
        }
        Vector2 bottom = anchor + new Vector2(Pattern.Sweep, Pattern.Depth + highestRow * 34f);
        if (bottom.Y > target.Bottom.Y + 80f || Vector2.DistanceSquared(bottom, target.Center) > 700f * 700f)
        {
            Pattern.Reset();
            anchor = target.Top - new Vector2(0f, 180f + highestRow * 34f);
            resetPause = 40;
            if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        }
        Projectile.Center = anchor + new Vector2(Pattern.Sweep, Pattern.Depth);
        if (Projectile.owner == Main.myPlayer && (int)Projectile.ai[1] % 90 == 0) Projectile.netUpdate = true;
    }
}

public class MeteorInvaderMinion : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private int age;
    private int fireTimer;
    private float heat;
    private int marchFrame;

    public override void SetStaticDefaults()
    {
        Main.projPet[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 26;
        Projectile.height = 22;
        Projectile.minion = true;
        Projectile.minionSlots = 1f;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void OnSpawn(IEntitySource source) => Projectile.minionSlots = Projectile.ai[2] > 0f ? Projectile.ai[2] : 1f;

    public override void AI()
    {
        Projectile.minionSlots = Projectile.ai[2] > 0f ? Projectile.ai[2] : 1f;
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || !player.HasBuff<MeteorInvaderBuff>()) { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        age++;
        if (age == 1) fireTimer = 90 - (int)Projectile.ai[1] % 4 * 18;
        Projectile fleet = MeteorInvaderFleet.Find(Projectile.owner);
        if (fleet?.ModProjectile is not MeteorInvaderFleet commander)
        {
            if (age > 30) Projectile.Kill();
            return;
        }
        Vector2 destination = fleet.Center + MeteorInvaderPattern.Cell((int)Projectile.ai[1], Math.Max(commander.MemberCount, (int)Projectile.ai[1] + 1));
        marchFrame = (int)fleet.ai[1] / 6 % 2;
        if (Vector2.DistanceSquared(player.Center, Projectile.Center) > 1600f * 1600f) Projectile.Center = player.Center - Vector2.UnitY * 60f;
        Vector2 oldCenter = Projectile.Center;
        Projectile.Center = Vector2.Lerp(Projectile.Center, destination, .35f);
        bool settled = Vector2.DistanceSquared(Projectile.Center, destination) < 24f * 24f;
        heat = commander.Attacking ? MathHelper.Clamp(commander.Pattern.Depth / 180f, 0f, 1f) : 0f;
        Lighting.AddLight(Projectile.Center, new Vector3(.35f, .09f + heat * .07f, .03f));
        if (Projectile.owner != Main.myPlayer || !commander.Attacking || !settled) return;
        if (age > 30 && commander.Pattern.Depth >= 60f)
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || !Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)) continue;
                float collision = 0f;
                if (!Collision.CheckAABBvLineCollision(npc.position, npc.Size, oldCenter, Projectile.Center, 20f, ref collision)) continue;
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MeteorInvaderBlast>(), Projectile.damage * 2, Projectile.knockBack * 2f, Projectile.owner, 1f);
                Projectile.Kill();
                return;
            }
        if (++fireTimer >= 90)
        {
            fireTimer = 0;
            Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Bottom, Vector2.UnitY * 9f, ModContent.ProjectileType<MeteorInvaderLaser>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        MeteorInvaderArt.DrawInvader(Projectile.Center - Main.screenPosition, marchFrame, 1f, Math.Min(1f, age / 20f), heat);
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 12; i++) MeteorInvaderArt.Spark(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f), heat >= .75f);
    }
}

public class MeteorInvaderLaser : ModProjectile
{
    public override string Texture => "AerovelenceMod/Assets/Pixel/CrispStarPMA";
    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;

    public override void SetDefaults()
    {
        Projectile.width = 8;
        Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = true;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = 1;
        Projectile.timeLeft = 80;
    }

    public override bool? CanDamage() => false;

    public override void AI()
    {
        if (Projectile.timeLeft == 80) SoundEngine.PlaySound(SoundID.Item12 with { Volume = .25f, Pitch = .35f }, Projectile.Center);
        Lighting.AddLight(Projectile.Center, .45f, .14f, .03f);
        if (Projectile.owner == Main.myPlayer)
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy() && npc.Hitbox.Intersects(Projectile.Hitbox)) { Projectile.Kill(); return; }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 center = Projectile.Center - Main.screenPosition;
        float fade = Math.Min(1f, Projectile.timeLeft / 12f);
        MeteorInvaderArt.Line(center - Vector2.UnitY * 25f, center + Vector2.UnitY * 5f, new Color(255, 85, 30, 0) * (.35f * fade), 8f);
        MeteorInvaderArt.Line(center - Vector2.UnitY * 22f, center + Vector2.UnitY * 5f, new Color(255, 220, 120, 0) * fade, 3f);
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer && timeLeft > 0)
            Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<MeteorInvaderBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        else MeteorInvaderArt.Spark(Projectile.Center, -Vector2.UnitY, false);
    }
}

public class MeteorInvaderBlast : ModProjectile
{
    public override string Texture => "AerovelenceMod/Assets/Orbs/SoftGlow";
    public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;
    public static float BlastRadius(bool sacrifice) => sacrifice ? 68f : 28f;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 140;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 22;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool? CanDamage() => Projectile.timeLeft >= 17 ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float radius = BlastRadius(Projectile.ai[0] == 1f);
        Vector2 closest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(Projectile.Center, closest) <= radius * radius && Collision.CanHitLine(Projectile.Center, 1, 1, closest, 1, 1);
    }

    public override void AI()
    {
        bool sacrifice = Projectile.ai[0] == 1f;
        if (Projectile.timeLeft == 22)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = sacrifice ? .55f : .18f, Pitch = sacrifice ? .1f : .6f }, Projectile.Center);
            for (int i = 0; i < (sacrifice ? 28 : 8); i++) MeteorInvaderArt.Spark(Projectile.Center, Main.rand.NextVector2Circular(sacrifice ? 6f : 3f, sacrifice ? 6f : 3f), sacrifice);
            if (!Main.dedServ)
                for (int i = 0; i < (sacrifice ? 10 : 4); i++)
                {
                    Dust smoke = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Smoke,
                        Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY, 100, new Color(100, 80, 75), sacrifice ? 1.4f : .9f);
                    smoke.noGravity = true;
                }
        }
        Lighting.AddLight(Projectile.Center, new Vector3(.8f, .35f, .05f) * (Projectile.timeLeft / 22f));
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Projectile.ai[0] == 1f) SkillStrikeUtil.setSkillStrike(Projectile, 1.75f, 1, .5f, .8f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float progress = 1f - Projectile.timeLeft / 22f;
        float fade = 1f - progress;
        float radius = BlastRadius(Projectile.ai[0] == 1f);
        Vector2 center = Projectile.Center - Main.screenPosition;
        Texture2D glow = TextureAssets.Projectile[Type].Value;
        float spread = MathHelper.SmoothStep(.15f, 1f, progress);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 2.4f + Projectile.identity * 1.7f;
            Vector2 offset = angle.ToRotationVector2() * (radius * spread * (.2f + i * .06f));
            float size = radius * (1.3f - i * .15f) * spread;
            Main.EntitySpriteDraw(glow, center + offset, null, new Color(255, 95, 25, 0) * (fade * fade * .5f), 0f,
                glow.Size() * .5f, size / glow.Width, SpriteEffects.None);
        }
        Main.EntitySpriteDraw(glow, center, null, new Color(255, 130, 50, 0) * (fade * .55f), 0f, glow.Size() * .5f, radius * 2f / glow.Width, SpriteEffects.None);
        Main.EntitySpriteDraw(glow, center, null, new Color(255, 215, 165, 0) * (fade * fade), 0f, glow.Size() * .5f, radius / glow.Width, SpriteEffects.None);
        return false;
    }
}

internal static class MeteorInvaderArt
{
    private const string InvaderTexture = "AerovelenceMod/Content/Items/Weapons/Overworld/MeteorInvader/MeteorInvaderInvaders";

    private static Texture2D cachedTexture;
    private static Color[] cachedPixels;
    private static int cachedWidth;
    private static int cachedHeight;

    internal static void DrawInvader(Vector2 center, int frame, float scale, float fade, float heat)
    {
        Texture2D texture = ModContent.Request<Texture2D>(InvaderTexture).Value;
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Texture2D bloom = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow").Value;
        Color glow = Color.Lerp(new Color(255, 95, 35, 0), new Color(255, 175, 85, 0), heat);
        int frameIndex = Math.Abs(frame) % 2;
        CachePixels(texture);

        Main.EntitySpriteDraw(bloom, center, null, glow * (.15f * fade), 0f, bloom.Size() * .5f, 50f * scale / bloom.Width, SpriteEffects.None);

        for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                if (!CellFilled(texture, frameIndex, x, y)) continue;
                Vector2 position = center + new Vector2((x - 3.5f) * 3f, (y - 3.5f) * 3f) * scale;
                Color body = Color.Lerp(new Color(120, 55, 83), new Color(245, 148, 88), 1f - y / 9f);
                Main.EntitySpriteDraw(pixel, position, new Rectangle(0, 0, 1, 1), body * fade, 0f, new Vector2(.5f), new Vector2(3f * scale), SpriteEffects.None);
                Main.EntitySpriteDraw(pixel, position, new Rectangle(0, 0, 1, 1), glow * (.25f * fade), 0f, new Vector2(.5f), new Vector2(2f * scale), SpriteEffects.None);
            }
    }


    private static void CachePixels(Texture2D texture)
    {
        int width = texture.Width;
        int height = texture.Height;
        int size = width * height;
        if (ReferenceEquals(cachedTexture, texture) && cachedPixels != null && cachedPixels.Length == size && cachedWidth == width && cachedHeight == height) return;
        cachedTexture = texture;
        cachedWidth = width;
        cachedHeight = height;
        cachedPixels = new Color[size];
        texture.GetData(cachedPixels);
    }

    private static bool CellFilled(Texture2D texture, int frame, int cellX, int cellY)
    {
        if (cachedPixels == null || cachedPixels.Length != texture.Width * texture.Height) CachePixels(texture);
        int startX = cellX * 2;
        int startY = frame * 18 + cellY * 2;
        for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
            {
                int px = startX + x;
                int py = startY + y;
                if (px < 0 || py < 0 || px >= texture.Width || py >= texture.Height) continue;
                int index = py * texture.Width + px;
                if (index >= 0 && index < cachedPixels.Length && cachedPixels[index].A > 16) return true;
            }
        return false;
    }

    internal static void Line(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 delta = end - start;
        Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, start, new Rectangle(0, 0, 1, 1), color, delta.ToRotation(), new Vector2(0f, .5f), new Vector2(delta.Length() + 1f, width), SpriteEffects.None);
    }


    internal static void Spark(Vector2 position, Vector2 velocity, bool golden)
    {
        if (Main.dedServ) return;
        Dust dust = Dust.NewDustPerfect(position, DustID.Torch, velocity, 80, golden ? new Color(255, 170, 80) : Color.Coral, golden ? 1.4f : .85f);
        dust.noGravity = true;
    }
}
