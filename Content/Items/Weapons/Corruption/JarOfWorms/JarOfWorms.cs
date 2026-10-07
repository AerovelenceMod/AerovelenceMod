using AerovelenceMod.Content.Dusts.GlowDusts;
using System;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace AerovelenceMod.Content.Items.Weapons.Corruption
{
    //we definitely need a sprite for this, the jar looks so bad i cant lie
    public sealed class JarOfWorms : TranslatableModItem
    {
        public override string Texture => "AerovelenceMod/Content/Items/Weapons/Corruption/JarOfWorms/JarOfWorms";

        public override void SetStaticDefaults()
        {
            this.ModifyLocalization("Jar of Worms", "Hold to conjure a jar of worms")
                .AddSkillStrike(Language.Default, "Break the jar by slamming it into enemies to Skill Strike");
        }
        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 20;
            Item.damage = 30;
            Item.DamageType = DamageClass.Summon;
            Item.knockBack = 2f;
            Item.mana = 6;
            Item.useTime = Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = Item.noUseGraphic = Item.channel = true;
            Item.shoot = ModContent.ProjectileType<WormJar>();
            Item.shootSpeed = 1f;
            Item.rare = ItemRarities.EarlyPHM;
            Item.value = Item.sellPrice(gold: 1);
        }
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 cursor = Main.MouseWorld;
            Projectile.NewProjectile(source, cursor, Vector2.Zero, type, damage, knockback, player.whoAmI, 0f, cursor.X, cursor.Y);
            return false;
        }
        public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Bottle)
            .AddIngredient(ItemID.Worm, 5).AddIngredient(ItemID.DemoniteBar, 8)
            .AddIngredient(ItemID.ShadowScale, 4).AddTile(TileID.Anvils).Register();
        private int Cooldown
        {
            get
            {
                if (Main.gameMenu || Main.LocalPlayer.ownedProjectileCounts[Item.shoot] == 0) return 0;
                foreach (Projectile projectile in Main.ActiveProjectiles)
                    if (projectile.owner == Main.myPlayer && projectile.type == Item.shoot && projectile.ai[0] > 0f)
                        return (int)projectile.ai[0];
                return 0;
            }
        }
        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            ItemCooldownDraw.DrawBase(spriteBatch, TextureAssets.Item[Type].Value, position, frame, drawColor, origin, scale, Cooldown, new Color(165, 120, 230), new Color(125, 95, 160));
            return false;
        }
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            ItemCooldownDraw.DrawFill(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, Cooldown, WormJar.RegenerationTime, new Color(165, 120, 230), new Color(45, 20, 70));
        }
    }
    public sealed class CorruptJarWorm : ModProjectile
    {
        public override Terraria.Localization.LocalizedText DisplayName => ModContent.GetInstance<JarOfWorms>().DisplayName;
        public override string Texture => "Terraria/Images/NPC_" + NPCID.DevourerHead;
        private int age;
        internal static int PiercedDamage(int damage) => Math.Max(1, (int)(damage * .45f));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }
        public override void AI()
        {
            age++;
            Projectile.velocity.Y = Math.Min(12f, Projectile.velocity.Y + .18f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Projectile.owner == Main.myPlayer && age > 30)
            {
                Rectangle screen = new((int)Main.screenPosition.X - 120, (int)Main.screenPosition.Y - 120, Main.screenWidth + 240, Main.screenHeight + 240);
                if (!screen.Intersects(Projectile.Hitbox)) Projectile.Kill();
            }
            if (Main.dedServ) return;
            Lighting.AddLight(Projectile.Center, .12f, .04f, .2f);
            if (age == 1)
                Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<GlowPixelCross>(), -Projectile.velocity * .15f, 0, new Color(140, 85, 195), .2f);
            if (age % 6 == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.CorruptGibs, -Projectile.velocity * .1f, 120, default, .6f);
                dust.noGravity = true;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.damage = PiercedDamage(Projectile.damage);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = 0;
            return projHitbox.Intersects(targetHitbox) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - Projectile.velocity, Projectile.Center, 8f, ref distance);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D head = TextureAssets.Projectile[Type].Value;
            Texture2D body = ModContent.Request<Texture2D>("Terraria/Images/NPC_" + NPCID.DevourerBody).Value;
            Texture2D tail = ModContent.Request<Texture2D>("Terraria/Images/NPC_" + NPCID.DevourerTail).Value;
            float fade = Math.Min(1f, Projectile.timeLeft / 20f);
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null, new Color(120, 65, 175, 0) * (.23f * fade), Projectile.rotation, glow.Size() * .5f, new Vector2(.45f, .7f), SpriteEffects.None);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = new(-direction.Y, direction.X);
            for (int i = 3; i >= 0; i--)
            {
                Texture2D texture = i == 0 ? head : i == 3 ? tail : body;
                int npc = i == 0 ? NPCID.DevourerHead : i == 3 ? NPCID.DevourerTail : NPCID.DevourerBody;
                Rectangle frame = new(0, 0, texture.Width, texture.Height / Main.npcFrameCount[npc]);
                float wiggle = MathF.Sin(age * .28f - i * .9f + Projectile.identity) * i * 1.1f;
                Vector2 point = Projectile.Center - direction * (i * 6f) + normal * wiggle;
                Main.EntitySpriteDraw(texture, point - Main.screenPosition, frame, Color.Lerp(lightColor, new Color(175, 145, 205), .35f) * fade, Projectile.rotation + MathF.Cos(age * .28f - i * .9f) * .16f, frame.Size() * .5f, .5f, SpriteEffects.None);
            }
            return false;
        }
    }
    public sealed class WormJar : ModProjectile
    {
        internal const int RegenerationTime = 600;
        private readonly WormJarMotion motion = new();
        private CrackUtil.CrackData[] crackPatterns;
        private WormJarShake shake;
        private Vector2 previousCenter, previousCursor, burstCenter;
        private int age, emissionTimer, impactCooldown, lastTarget = -1, shownCracks;
        private byte cracks;
        private int spinDirection = 1;
        private float angularVelocity, hitFlash;
        private bool impactArmed = true, burstShown;
        private bool Regenerating => Projectile.ai[0] > 0;
        private float RegenerationProgress => ItemCooldownDraw.Progress((int)Projectile.ai[0], RegenerationTime);
        private float CrackRepair(int index) => Regenerating ? MathHelper.Clamp(RegenerationProgress * 3f - index, 0f, 1f) : 0f;
        public override Terraria.Localization.LocalizedText DisplayName => ModContent.GetInstance<JarOfWorms>().DisplayName;
        public override string Texture => "AerovelenceMod/Content/Items/Weapons/Corruption/JarOfWorms/WormjarConjure";
        public override bool ShouldUpdatePosition() => false;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }
        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }
        public override bool? CanDamage() => Projectile.owner == Main.myPlayer && !Regenerating && age > 10
            && impactArmed && impactCooldown == 0 && Projectile.velocity.LengthSquared() >= 196f ? null : false;
        public override bool? CanHitNPC(NPC target) => !Regenerating && impactArmed && impactCooldown == 0
            && target.CanBeChasedBy(Projectile) ? null : false;
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = 0;
            return projHitbox.Intersects(targetHitbox) || Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previousCenter, Projectile.Center, 22f, ref distance);
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead && !Regenerating)
            {
                Projectile.Kill();
                return;
            }
            Projectile.timeLeft = 2;
            hitFlash *= .82f;
            previousCenter = Projectile.Center;
            if (Regenerating)
            {
                Regenerate(player);
                return;
            }
            if (player.dead || player.CCed || player.noItems || !player.channel || player.HeldItem.type != ModContent.ItemType<JarOfWorms>())
            {
                Projectile.Kill();
                return;
            }
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 cursor = Main.MouseWorld;
                if (age == 0) previousCursor = cursor;
                motion.Update(cursor - previousCursor);
                previousCursor = cursor;
                WormJarShake next = motion.Shake;
                if (age % 4 == 0 || next != shake) Projectile.netUpdate = true;
                shake = next;
                spinDirection = motion.SpinDirection;
                Projectile.ai[1] = cursor.X;
                Projectile.ai[2] = cursor.Y;
                if (age > 0 && age % 20 == 0 && !player.CheckMana(2, true))
                {
                    Projectile.Kill();
                    return;
                }
                player.manaRegenDelay = player.maxRegenDelay;
            }
            Vector2 destination = new(Projectile.ai[1], Projectile.ai[2]);
            Projectile.velocity = (destination - Projectile.Center) * .42f;
            if (Projectile.velocity.LengthSquared() > 96f * 96f) Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero) * 96f;
            Projectile.Center += Projectile.velocity;
            if (shake == WormJarShake.Circle)
            {
                angularVelocity = MathHelper.Lerp(angularVelocity, spinDirection * .24f, .18f);
                Projectile.rotation += angularVelocity;
            }
            else
            {
                float target = shake == WormJarShake.Vertical ? MathHelper.Pi : MathHelper.Clamp(Projectile.velocity.X * .025f, -.45f, .45f);
                if (shake == WormJarShake.Horizontal) target += MathF.Sin(age * .9f) * .25f;
                angularVelocity = (angularVelocity + MathHelper.WrapAngle(target - Projectile.rotation) * .12f) * .72f;
                Projectile.rotation = MathHelper.WrapAngle(Projectile.rotation + angularVelocity);
            }
            if (impactCooldown > 0) impactCooldown--;
            if (!impactArmed && impactCooldown == 0
                && (lastTarget < 0 || !Main.npc[lastTarget].active || !Projectile.Hitbox.Intersects(Main.npc[lastTarget].Hitbox))) impactArmed = true;
            player.ChangeDir(Projectile.Center.X >= player.Center.X ? 1 : -1);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation = 2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, (Projectile.Center - player.MountedCenter).ToRotation() - MathHelper.PiOver2);
            if (shake == WormJarShake.None) emissionTimer = 0;
            else if (++emissionTimer >= 8)
            {
                emissionTimer = 0;
                if (Projectile.owner == Main.myPlayer && (shake != WormJarShake.Vertical || Math.Abs(MathHelper.WrapAngle(Projectile.rotation - MathHelper.Pi)) < .6f)) Spill();
            }
            if (!Main.dedServ)
            {
                if (age == 0) SoundEngine.PlaySound(SoundID.Item8 with { Volume = .35f, Pitch = -.3f }, Projectile.Center);
                if (cracks > shownCracks)
                {
                    shownCracks = cracks;
                    hitFlash = 1f;
                    SoundEngine.PlaySound(SoundID.Shatter with { Volume = .28f, Pitch = .4f }, Projectile.Center);
                    for (int i = 0; i < 4; i++)
                        Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<GlowPixelCross>(), Main.rand.NextVector2Circular(3f, 3f), 0, new Color(210, 170, 255), .25f);
                    for (int i = 0; i < 8; i++)
                        Dust.NewDustPerfect(Projectile.Center + new Vector2(0, 12).RotatedBy(Projectile.rotation), DustID.Glass, Main.rand.NextVector2Circular(3f, 3f) + Projectile.velocity * .15f, 80, new Color(180, 130, 230), .8f);
                }
                Lighting.AddLight(Projectile.Center, new Vector3(.22f, .08f, .34f) * (.5f + (shake == WormJarShake.None ? 0 : .5f)));
            }
            age++;
        }
        private void Spill()
        {
            Vector2 mouth = Projectile.Center + new Vector2(0, -22).RotatedBy(Projectile.rotation);
            Vector2 outward = new Vector2(0, -1).RotatedBy(Projectile.rotation);
            Vector2 velocity = shake switch
            {
                WormJarShake.Vertical => outward * 5f + new Vector2(Main.rand.NextFloat(-2.2f, 2.2f), 0),
                WormJarShake.Circle => outward * 7f,
                _ => outward * 3.5f + new Vector2(Main.rand.NextFloat(-4f, 4f), -1.5f)
            };
            SpawnWorm(mouth, velocity + Projectile.velocity * .22f, .55f);
        }
        private void SpawnWorm(Vector2 position, Vector2 velocity, float damageScale)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int index = Projectile.NewProjectile(Projectile.GetSource_FromAI(), position, velocity, ModContent.ProjectileType<CorruptJarWorm>(), Math.Max(1, (int)(Projectile.damage * damageScale)), Projectile.knockBack, Projectile.owner);
                if (index < Main.maxProjectiles)
                {
                    Main.projectile[index].originalDamage = Math.Max(1, (int)(Projectile.originalDamage * damageScale));
                    Main.projectile[index].netUpdate = true;
                }
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (cracks == 2) SkillStrikeUtil.setSkillStrike(Projectile, 3.5f, 1, .6f, 1f);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            impactArmed = false;
            impactCooldown = 20;
            lastTarget = target.whoAmI;
            cracks++;
            Projectile.netUpdate = true;
            if (cracks < 3) return;
            burstCenter = Projectile.Center;
            Projectile.ai[0] = RegenerationTime;
            Projectile.friendly = false;
            for (int i = 0; i < 24; i++)
            {
                float angle = i * MathHelper.TwoPi / 24f;
                SpawnWorm(burstCenter, angle.ToRotationVector2() * Main.rand.NextFloat(5f, 9f), .7f);
            }
        }
        private void Regenerate(Player player)
        {
            if (!burstShown)
            {
                burstShown = true;
                if (!Main.dedServ && Projectile.ai[0] > RegenerationTime - 30)
                {
                    SoundEngine.PlaySound(SoundID.Shatter with { Volume = .7f, Pitch = -.3f }, burstCenter);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = .4f, Pitch = .4f }, burstCenter);
                    float power = 4f * MathHelper.Clamp(1f - Vector2.Distance(Main.LocalPlayer.Center, burstCenter) / 850f, 0, 1);
                    AeroPlayer camera = Main.LocalPlayer.GetModPlayer<AeroPlayer>();
                    camera.ScreenShakePower = Math.Max(camera.ScreenShakePower, power);
                    for (int i = 0; i < 16; i++)
                        Dust.NewDustPerfect(burstCenter, ModContent.DustType<GlowPixelCross>(), Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 8f), 0, i % 3 == 0 ? new Color(165, 190, 90) : new Color(170, 100, 240), Main.rand.NextFloat(.25f, .45f));
                    for (int i = 0; i < 36; i++)
                    {
                        Vector2 velocity = (i * MathHelper.TwoPi / 36f).ToRotationVector2() * Main.rand.NextFloat(2f, 7f);
                        Dust dust = Dust.NewDustPerfect(burstCenter, i % 3 == 0 ? DustID.Glass : DustID.PurpleTorch, velocity, 50, default, Main.rand.NextFloat(.8f, 1.5f));
                        dust.noGravity = i % 3 != 0;
                    }
                }
            }
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = Vector2.Lerp(Projectile.Center, player.MountedCenter - new Vector2(0, 65), .08f);
            Projectile.rotation = MathHelper.WrapAngle(Projectile.rotation * .9f);
            Projectile.ai[0]--;
            if (Projectile.ai[0] > 0) return;
            if (!Main.dedServ && player.HeldItem.type == ModContent.ItemType<JarOfWorms>())
            {
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = .4f, Pitch = .3f }, Projectile.Center);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 offset = (i * MathHelper.TwoPi / 12f).ToRotationVector2() * 24;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.PurpleTorch, -offset * .08f, 50, default, .9f);
                    dust.noGravity = true;
                }
            }
            Projectile.Kill();
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)shake);
            writer.Write((sbyte)spinDirection);
            writer.Write(cracks);
            writer.Write(Projectile.rotation);
            writer.Write(angularVelocity);
            writer.Write(burstCenter.X);
            writer.Write(burstCenter.Y);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            shake = (WormJarShake)reader.ReadByte();
            spinDirection = reader.ReadSByte();
            cracks = reader.ReadByte();
            Projectile.rotation = reader.ReadSingle();
            angularVelocity = reader.ReadSingle();
            burstCenter = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
        public override bool PreDraw(ref Color lightColor)
        {
            float appear = MathHelper.SmoothStep(0, 1, Math.Min(1f, age / 16f));
            float refill = RegenerationProgress;
            if (Regenerating)
            {
                DrawBurst();
                if (Main.player[Projectile.owner].HeldItem.type != ModContent.ItemType<JarOfWorms>()) return false;
                appear = .25f + refill * .75f;
            }
            Texture2D glass = TextureAssets.Projectile[Type].Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
            float summonFlash = Regenerating ? 0f : MathF.Sin(Math.Min(1f, age / 20f) * MathHelper.Pi);
            float pulse = .9f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * .1f;
            Main.EntitySpriteDraw(glow, center, null, new Color(150, 90, 225, 0) * ((.22f + summonFlash * .65f + hitFlash * .5f) * appear), 0f, glow.Size() * .5f, (Regenerating ? .9f : 1.1f) * pulse, SpriteEffects.None);
            if (!Regenerating && Projectile.velocity.LengthSquared() > 144f)
                for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero) continue;
                    Main.EntitySpriteDraw(glass, Projectile.oldPos[i] + Projectile.Size * .5f - Main.screenPosition, null, new Color(135, 70, 200, 0) * ((1f - i / (float)Projectile.oldPos.Length) * .18f * appear), Projectile.oldRot[i], glass.Size() * .5f, 1f, SpriteEffects.None);
                }
            Color color = Color.Lerp(lightColor, new Color(195, 170, 230), .25f);
            Main.EntitySpriteDraw(glass, center, null, color * appear, Projectile.rotation, glass.Size() * .5f, 1f, SpriteEffects.None);
            if (hitFlash > .01f)
                Main.EntitySpriteDraw(glass, center, null, new Color(220, 200, 255, 0) * (hitFlash * .7f), Projectile.rotation, glass.Size() * .5f, 1f, SpriteEffects.None);
            if (cracks > 0 && crackPatterns == null)
            {
                Rectangle bounds = new(-14, -14, 28, 34);
                CrackSettings settings = new() { Length = 14f, Branches = 2, Thickness = 2f, Taper = 1.5f };
                crackPatterns =
                [
                    CrackUtil.Create(new Vector2(-8f, -8f), bounds, Projectile.identity, settings),
                    CrackUtil.Create(new Vector2(8f, 8f), bounds, Projectile.identity + 31, settings),
                    CrackUtil.Create(new Vector2(-8f, 14f), bounds, Projectile.identity + 67, settings)
                ];
            }
            Vector2 crackPosition = center + new Vector2(-1f, 0f).RotatedBy(Projectile.rotation);
            for (int i = 0; i < cracks; i++)
            {
                float repair = CrackRepair(i);
                float purple = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(0f, .45f, repair, true));
                crackPatterns[i].Retraction = MathHelper.SmoothStep(0f, 1f, Utils.GetLerpValue(.4f, 1f, repair, true));
                if (crackPatterns[i].Closed) continue;
                Color crackColor = Color.Lerp(new Color(50, 25, 70), new Color(225, 220, 255), hitFlash);
                crackColor = Color.Lerp(crackColor, new Color(180, 95, 245), purple).MultiplyRGB(color) * (.35f * appear);
                CrackUtil.Draw(crackPatterns[i], Main.spriteBatch, crackPosition, crackColor, Projectile.rotation, applyLighting: false);
            }
            if (Regenerating) DrawRegenerationBar(center, refill, appear);
            return false;
        }
        private static void DrawRegenerationBar(Vector2 center, float progress, float opacity)
        {
            Texture2D bar = ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Weapons/BossDrops/Cyvercry/CyverCannonBar").Value;
            Texture2D border = ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Weapons/BossDrops/Cyvercry/CyverCannonBarBorderGlow").Value;
            Texture2D fill = ModContent.Request<Texture2D>("AerovelenceMod/Content/Items/Weapons/BossDrops/Cyvercry/CyverCannonBarFill").Value;
            Vector2 position = center + new Vector2(0f, 34f);
            position = new Vector2(MathF.Round(position.X), MathF.Round(position.Y));
            float pulse = .8f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * .12f;
            Color color = Color.Lerp(new Color(165, 120, 230), new Color(220, 180, 255), pulse * .25f);
            Main.EntitySpriteDraw(border, position, null, (color with { A = 0 }) * (.18f * opacity), 0f, border.Size() * .5f, 1f, SpriteEffects.None);
            Main.EntitySpriteDraw(bar, position, null, Color.White * opacity, 0f, bar.Size() * .5f, 1f, SpriteEffects.None);
            int width = Math.Clamp((int)MathF.Ceiling(fill.Width * progress), 0, fill.Width);
            if (width == 0) return;
            Main.EntitySpriteDraw(fill, position, new Rectangle(0, 0, width, fill.Height), color * opacity, 0f, fill.Size() * .5f, 1f, SpriteEffects.None);
        }
        private void DrawBurst()
        {
            float progress = (RegenerationTime - Projectile.ai[0]) / 24f;
            if (progress > 1) return;
            float radius = MathHelper.Lerp(12, 100, 1f - (1f - progress) * (1f - progress));
            Vector2 center = burstCenter - Main.screenPosition;
            float fade = (1f - progress) * (1f - progress);
            Texture2D ring = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Ring/GlowRing").Value;
            Texture2D glow = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Orbs/SoftGlow64").Value;
            Texture2D flare = ModContent.Request<Texture2D>("AerovelenceMod/Assets/Flare/CrispStarPMA").Value;
            Main.EntitySpriteDraw(ring, center, null, new Color(175, 110, 245, 0) * (.8f * fade), 0f, ring.Size() * .5f, Vector2.One * radius * 2f / ring.Size(), SpriteEffects.None);
            Main.EntitySpriteDraw(glow, center, null, new Color(160, 100, 235, 0) * fade, 0f, glow.Size() * .5f, Vector2.One * (90f + progress * 100f) / glow.Size(), SpriteEffects.None);
            Main.EntitySpriteDraw(flare, center, null, new Color(235, 215, 255, 0) * fade, .35f, flare.Size() * .5f, new Vector2(90f, 55f) * fade / flare.Size(), SpriteEffects.None);
        }

    }
    internal enum WormJarShake : byte { None, Horizontal, Vertical, Circle }
    internal sealed class WormJarMotion
    {
        private Vector2 previousStep;
        private int horizontalDirection, verticalDirection, quietTicks;
        private float horizontalTravel, verticalTravel, horizontalShakes, verticalShakes;
        private float circleTurn, circleTravel;
        internal int SpinDirection => circleTurn < 0 ? -1 : 1;
        internal WormJarShake Shake { get; private set; }
        internal void Update(Vector2 step)
        {
            horizontalShakes = Math.Max(0, horizontalShakes - .025f);
            verticalShakes = Math.Max(0, verticalShakes - .025f);
            circleTurn *= .99f;
            circleTravel *= .99f;
            float speed = step.Length();
            if (speed > 160f)
            {
                Reset();
                return;
            }
            quietTicks = speed < 3f ? quietTicks + 1 : 0;
            if (quietTicks > 14)
            {
                Reset();
                return;
            }
            Axis(step.X, Math.Abs(step.Y), ref horizontalDirection, ref horizontalTravel, ref horizontalShakes);
            Axis(step.Y, Math.Abs(step.X), ref verticalDirection, ref verticalTravel, ref verticalShakes);
            if (speed >= 4f && previousStep.LengthSquared() >= 16f)
            {
                float turn = MathF.Atan2(previousStep.X * step.Y - previousStep.Y * step.X, Vector2.Dot(previousStep, step));
                if (Math.Abs(turn) > 1.75f)
                    circleTurn = circleTravel = 0;
                else if (Math.Abs(turn) > .015f)
                {
                    if (turn * circleTurn < 0) circleTurn *= .3f;
                    circleTurn += turn;
                }
                if (circleTurn != 0) circleTravel += speed;
            }
            previousStep = step;
            Shake = Math.Abs(circleTurn) >= 4.2f && circleTravel >= 160f ? WormJarShake.Circle
                : Math.Max(horizontalShakes, verticalShakes) < 1.7f ? WormJarShake.None
                : verticalShakes > horizontalShakes ? WormJarShake.Vertical : WormJarShake.Horizontal;
        }
        private static void Axis(float step, float other, ref int direction, ref float travel, ref float shakes)
        {
            if (Math.Abs(step) < 3f || Math.Abs(step) < other * 1.35f) return;
            int next = Math.Sign(step);
            if (direction != 0 && next != direction)
            {
                if (travel >= 22f) shakes = Math.Min(4f, shakes + 1f);
                travel = 0;
            }
            direction = next;
            travel += Math.Abs(step);
        }
        private void Reset()
        {
            previousStep = Vector2.Zero;
            horizontalDirection = verticalDirection = quietTicks = 0;
            horizontalTravel = verticalTravel = horizontalShakes = verticalShakes = circleTurn = circleTravel = 0;
            Shake = WormJarShake.None;
        }
    }
}
